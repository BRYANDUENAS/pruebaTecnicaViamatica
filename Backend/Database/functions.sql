-- Funciones de base de datos usadas por la API (PostgreSQL).
-- Se crean automáticamente con la migración "DatabaseFunctions";
-- este archivo es la copia de referencia solicitada en la entrega.

-- fn_create_turn
-- Crea un turno para una caja y su atención en estado pendiente.
-- La descripción se arma con el prefijo del tipo de atención (2 letras) y un
-- secuencial de 4 dígitos que se reinicia cada día: AC0001, AC0002, PS0001...
-- El bloqueo por prefijo evita que dos gestores obtengan el mismo número.
CREATE OR REPLACE FUNCTION fn_create_turn(
    p_attention_type_id varchar,
    p_cash_id integer,
    p_gestor_id integer
) RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_prefix  char(2);
    v_next    integer;
    v_turn_id integer;
    v_today   date := (now() AT TIME ZONE 'America/Guayaquil')::date;
BEGIN
    SELECT prefix INTO v_prefix
    FROM attention_types
    WHERE attention_type_id = p_attention_type_id;

    IF v_prefix IS NULL THEN
        RAISE EXCEPTION 'Tipo de atención % no existe', p_attention_type_id;
    END IF;

    PERFORM pg_advisory_xact_lock(hashtext('turn_' || v_prefix));

    SELECT COALESCE(MAX(SUBSTRING(description FROM 3)::integer), 0) + 1 INTO v_next
    FROM turns
    WHERE attention_type_id = p_attention_type_id
      AND (date AT TIME ZONE 'America/Guayaquil')::date = v_today;

    IF v_next > 9999 THEN
        RAISE EXCEPTION 'Se agotaron los turnos del día para el tipo %', p_attention_type_id;
    END IF;

    INSERT INTO turns (description, date, cash_id, user_gestor_id, attention_type_id, is_deleted)
    VALUES (v_prefix || LPAD(v_next::text, 4, '0'), now(), p_cash_id, p_gestor_id, p_attention_type_id, false)
    RETURNING turn_id INTO v_turn_id;

    INSERT INTO attentions (turn_id, attention_type_id, status_id)
    VALUES (v_turn_id, p_attention_type_id, 1);

    RETURN v_turn_id;
END;
$$;

-- fn_cash_indicators
-- Indicadores por caja en un rango de fechas, para el dashboard del administrador.
CREATE OR REPLACE FUNCTION fn_cash_indicators(
    p_from timestamptz,
    p_to   timestamptz
) RETURNS TABLE (
    cash_id          integer,
    cash_description varchar,
    total_turns      integer,
    attended         integer,
    pending          integer
)
LANGUAGE sql
STABLE
AS $$
    SELECT c.cash_id,
           c.cash_description,
           COUNT(t.turn_id)::integer,
           COUNT(t.turn_id) FILTER (WHERE a.status_id = 3)::integer,
           COUNT(t.turn_id) FILTER (WHERE a.status_id IN (1, 2))::integer
    FROM cashes c
    LEFT JOIN turns t
           ON t.cash_id = c.cash_id
          AND NOT t.is_deleted
          AND t.date >= p_from
          AND t.date < p_to
    LEFT JOIN attentions a ON a.turn_id = t.turn_id
    WHERE NOT c.is_deleted
    GROUP BY c.cash_id, c.cash_description
    ORDER BY c.cash_id;
$$;
