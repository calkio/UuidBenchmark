-- pgstattuple даёт нам pgstattuple() и pgstatindex() — функции для замера
-- физической "рыхлости" таблицы и индекса (то, из-за чего вообще
-- затевался переход на UUIDv7).
CREATE EXTENSION IF NOT EXISTS pgstattuple;

-- UUIDv7-совместимый генератор для PostgreSQL < 18 (нативная uuidv7()
-- появилась только в 18-й версии). Это широко используемая реализация
-- (автор — Daniel Vérité): берём случайный v4 UUID как заготовку (там уже
-- правильно выставлен variant), поверх первых 48 бит накладываем текущий
-- unix-timestamp в миллисекундах, затем выставляем биты версии в 0111 (7).
CREATE OR REPLACE FUNCTION uuid_generate_v7() RETURNS uuid AS $$
    SELECT encode(
        set_bit(
            set_bit(
                overlay(
                    uuid_send(gen_random_uuid())
                    placing substring(int8send((extract(epoch FROM clock_timestamp()) * 1000)::bigint) FROM 3)
                    FROM 1 FOR 6
                ),
                52, 1
            ),
            53, 1
        ),
        'hex'
    )::uuid;
$$ LANGUAGE sql VOLATILE;
