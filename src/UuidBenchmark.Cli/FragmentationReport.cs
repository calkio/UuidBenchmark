using Npgsql;

namespace UuidBenchmark.Cli;

public record TableStats(
    string TableName,
    long TableSizeBytes,
    long TupleCount,
    double DeadTuplePercent,
    double FreePercent,
    long IndexSizeBytes,
    double AvgLeafDensity,
    double LeafFragmentation);

public static class FragmentationReport
{
    /// <summary>
    /// avg_leaf_density — насколько плотно заполнены страницы индекса
    /// (у здорового b-tree с fillfactor по умолчанию — около 90%; заметно
    /// более низкое значение = много "воздуха" из-за page split'ов).
    /// leaf_fragmentation — % листовых страниц, физически лежащих не по
    /// порядку (это и есть прямое проявление случайных вставок v4).
    /// </summary>
    public static async Task<TableStats> GetAsync(
    NpgsqlConnection connection,
    string tableName)
    {
        long tableLen;
        long tupleCount;
        double deadTuplePercent;
        double freePercent;

        // Статистика таблицы.
        await using (var cmd = new NpgsqlCommand(
            """
            select
                table_len,
                tuple_count,
                dead_tuple_percent,
                free_percent
            from pgstattuple($1::regclass)
            """,
            connection))
        {
            cmd.Parameters.AddWithValue(tableName);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    $"Не удалось получить статистику таблицы '{tableName}'.");
            }

            tableLen = reader.GetInt64(0);
            tupleCount = reader.GetInt64(1);
            deadTuplePercent = reader.GetDouble(2);
            freePercent = reader.GetDouble(3);
        }

        long indexSize = 0;
        double avgLeafDensity = 0;
        double leafFragmentation = 0;

        // Находим primary key index и сразу приводим его к regclass.
        await using (var idxCmd = new NpgsqlCommand(
            """
            select i.oid::regclass::text
            from pg_index x
            join pg_class c on c.oid = x.indrelid
            join pg_class i on i.oid = x.indexrelid
            where c.oid = $1::regclass
              and x.indisprimary
            """,
            connection))
        {
            idxCmd.Parameters.AddWithValue(tableName);

            var indexName = (string?)await idxCmd.ExecuteScalarAsync();

            if (indexName is not null)
            {
                await using var statCmd = new NpgsqlCommand(
                    """
                    select
                        pg_relation_size($1::regclass),
                        avg_leaf_density,
                        leaf_fragmentation
                    from pgstatindex($1::regclass)
                    """,
                    connection);

                statCmd.Parameters.AddWithValue(indexName);

                await using var reader = await statCmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    indexSize = reader.GetInt64(0);
                    avgLeafDensity = reader.GetDouble(1);
                    leafFragmentation = reader.GetDouble(2);
                }
            }
        }

        return new TableStats(
            tableName,
            tableLen,
            tupleCount,
            deadTuplePercent,
            freePercent,
            indexSize,
            avgLeafDensity,
            leafFragmentation);
    }
}
