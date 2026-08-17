# UuidBenchmark

Сравнение способов генерации UUID при массовой вставке данных в PostgreSQL.

Проект сравнивает UUID v4 и несколько реализаций UUID v7 по двум основным направлениям:

- скорость массовой вставки данных;
- влияние порядка UUID на B-tree индекс PostgreSQL.

Основная идея benchmark — проверить, насколько последовательность UUID v7 помогает PostgreSQL эффективнее работать с индексом по сравнению со случайными UUID v4.

При вставке большого количества строк UUID v4 создают случайное распределение ключей внутри B-tree индекса. UUID v7 содержит временную составляющую, поэтому значения в основном генерируются в возрастающем порядке. Это должно уменьшать количество page split, повышать плотность leaf pages и снижать фрагментацию индекса.

---

## Состав

- **UuidBenchmark.Generation** — BenchmarkDotNet-проект для измерения чистой скорости генерации UUID без обращения к базе данных. Позволяет сравнить CPU и allocation overhead разных генераторов.
- **UuidBenchmark.Database** — EF Core-модели, 5 таблиц (по одной на каждую стратегию), конфигурация через Fluent API.
- **UuidBenchmark.Cli** — консольный раннер benchmark. Выполняет batch insert с периодическими чекпоинтами по времени и после завершения формирует отчёт по таблицам и индексам через `pgstattuple` / `pgstatindex`.
- **docker-compose.yml** — PostgreSQL 16 для локального запуска benchmark.
- **db-init/** — SQL-инициализация базы данных и функции, необходимые для генерации UUID v7 на стороне PostgreSQL.

Используемые стратегии:

| Стратегия | Описание |
|---|---|
| `v4` | UUID v4, случайный UUID |
| `v7builtin` | UUID v7 через встроенный `Guid.CreateVersion7()` |
| `v7uuidnext` | UUID v7 через `UUIDNext` |
| `v7medo` | UUID v7 через `Medo.Uuid7` |
| `v7dbside` | UUID v7 генерируется на стороне PostgreSQL |

Все стратегии используют UUID в качестве первичного ключа таблицы.

---

## Требования

Для запуска проекта необходимы:

- .NET 9 SDK;
- Docker Compose.

.NET 9 используется из-за наличия `Guid.CreateVersion7()`.

---

## Запуск

### Запуск PostgreSQL

Для запуска локального PostgreSQL:

```bash
docker compose up -d

dotnet restore

(Этот тест измеряет именно скорость генерации UUID и не включает работу с PostgreSQL.)
dotnet run --project src/UuidBenchmark.Generation -c Release

(Для запуска всех стратегий.)
dotnet run `
  --project src/UuidBenchmark.Cli `
  -- `
  --connection-string "Host=127.0.0.1;Port=5433;Database=uuid_benchmark;Username=postgres;Password=postgres" `
  --strategy all `
  --total-rows 10000000 `
  --batch-size 2000

(Запуск одной стратегии.)
dotnet run `
  --project src/UuidBenchmark.Cli `
  -- `
  --connection-string "Host=127.0.0.1;Port=5433;Database=uuid_benchmark;Username=postgres;Password=postgres" `
  --strategy v4 `
  --total-rows 1000 `
  --batch-size 100
```

## Результат локального benchmark

Локальный benchmark был выполнен с параметрами:

```bash
10 000 000 строк на стратегию
batch-size = 2 000
strategy = all
```

### Скорость вставки

| Стратегия    |    Время |         Скорость |
| ------------ | -------: | ---------------: |
| `v4`         | 197,66 s | 50 593 строк/сек |
| `v7builtin`  | 180,44 s | 55 420 строк/сек |
| `v7uuidnext` | 174,00 s | 57 472 строк/сек |
| `v7medo`     | 191,86 s | 52 120 строк/сек |
| `v7dbside`   | 344,24 s | 29 049 строк/сек |

### Фрагментация и размер индексов

| Таблица            |       Rows | Table size | Index size | Leaf density | Fragmentation | Dead tuple % |
| ------------------ | ---------: | ---------: | ---------: | -----------: | ------------: | -----------: |
| `rows_v4`          | 10 000 000 |    2,54 GB |  390,62 MB |        69,4% |         49,8% |         0,0% |
| `rows_v7_builtin`  | 10 000 000 |    2,54 GB |  300,82 MB |        90,0% |          0,0% |         0,0% |
| `rows_v7_uuidnext` | 10 000 000 |    2,54 GB |  300,82 MB |        90,0% |          0,0% |         0,0% |
| `rows_v7_medo`     | 10 000 000 |    2,54 GB |  300,82 MB |        90,0% |          0,0% |         0,0% |
| `rows_v7_dbside`   | 10 000 000 |    2,54 GB |  308,93 MB |        87,7% |          4,2% |         0,0% |

