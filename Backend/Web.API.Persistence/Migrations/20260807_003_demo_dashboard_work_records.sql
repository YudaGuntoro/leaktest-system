-- Demo dashboard leak test records for richer chart previews.

DROP TEMPORARY TABLE IF EXISTS demo_dashboard_leak_test_plan;
CREATE TEMPORARY TABLE demo_dashboard_leak_test_plan (
    check_date DATE NOT NULL PRIMARY KEY,
    total_count INT NOT NULL,
    ng_count INT NOT NULL
);

INSERT INTO demo_dashboard_leak_test_plan
    (check_date, total_count, ng_count)
VALUES
    ('2026-01-15', 22, 3),
    ('2026-02-12', 18, 2),
    ('2026-03-18', 26, 4),
    ('2026-04-16', 21, 3),
    ('2026-05-20', 28, 5),
    ('2026-06-17', 24, 4),
    ('2026-08-15', 5, 1),
    ('2026-08-16', 6, 1),
    ('2026-08-17', 8, 2),
    ('2026-08-18', 7, 1),
    ('2026-08-19', 9, 2),
    ('2026-08-20', 6, 1),
    ('2026-08-21', 8, 1),
    ('2026-08-22', 7, 2),
    ('2026-08-23', 9, 1),
    ('2026-08-24', 6, 1),
    ('2026-08-25', 8, 2),
    ('2026-08-26', 7, 1),
    ('2026-08-27', 6, 1),
    ('2026-09-16', 20, 2),
    ('2026-10-14', 23, 3),
    ('2026-11-18', 25, 4),
    ('2026-12-16', 27, 3);

DROP TEMPORARY TABLE IF EXISTS demo_dashboard_seq;
CREATE TEMPORARY TABLE demo_dashboard_seq (
    n INT NOT NULL PRIMARY KEY
);

INSERT INTO demo_dashboard_seq (n)
VALUES
    (1), (2), (3), (4), (5), (6), (7), (8),
    (9), (10), (11), (12), (13), (14), (15), (16),
    (17), (18), (19), (20), (21), (22), (23), (24),
    (25), (26), (27), (28), (29), (30), (31), (32);

INSERT INTO leak_test_work_records
    (engine_model_id, engine_number, check_date, check_time, machine_name, operator_id, parameter_pressure, pressure_input, cycle_time_leak_test_minutes, result, created_at, updated_at)
SELECT
    CASE MOD(seq.n + DAYOFMONTH(plan.check_date), 12)
        WHEN 0 THEN 1
        WHEN 1 THEN 2
        WHEN 2 THEN 3
        WHEN 3 THEN 4
        WHEN 4 THEN 5
        WHEN 5 THEN 6
        WHEN 6 THEN 7
        WHEN 7 THEN 8
        WHEN 8 THEN 14
        WHEN 9 THEN 18
        WHEN 10 THEN 31
        ELSE 33
    END AS engine_model_id,
    CONCAT('DASH-', DATE_FORMAT(plan.check_date, '%Y%m%d'), '-', LPAD(seq.n, 3, '0')) AS engine_number,
    plan.check_date,
    TIME_FORMAT(SEC_TO_TIME(27000 + ((seq.n - 1) * 720)), '%H:%i:%s') AS check_time,
    CONCAT('Leak Tester Machine ', ((seq.n - 1) % 4) + 1) AS machine_name,
    ((seq.n - 1) % 4) + 1 AS operator_id,
    CASE MOD(seq.n, 4)
        WHEN 0 THEN 0.30
        WHEN 1 THEN 0.32
        WHEN 2 THEN 0.25
        ELSE 0.35
    END AS parameter_pressure,
    CASE
        WHEN seq.n <= plan.ng_count THEN
            CASE MOD(seq.n, 4)
                WHEN 0 THEN 0.24
                WHEN 1 THEN 0.27
                WHEN 2 THEN 0.20
                ELSE 0.29
            END
        ELSE
            CASE MOD(seq.n, 4)
                WHEN 0 THEN 0.30
                WHEN 1 THEN 0.32
                WHEN 2 THEN 0.25
                ELSE 0.35
            END
    END AS pressure_input,
    CASE MOD(seq.n, 4)
        WHEN 0 THEN 5.00
        WHEN 1 THEN 4.75
        WHEN 2 THEN 5.25
        ELSE 5.50
    END AS cycle_time_leak_test_minutes,
    CASE WHEN seq.n <= plan.ng_count THEN 'NG' ELSE 'OK' END AS result,
    TIMESTAMP(plan.check_date, TIME_FORMAT(SEC_TO_TIME(27000 + ((seq.n - 1) * 720)), '%H:%i:%s')) AS created_at,
    TIMESTAMP(plan.check_date, TIME_FORMAT(SEC_TO_TIME(27000 + ((seq.n - 1) * 720)), '%H:%i:%s')) AS updated_at
FROM demo_dashboard_leak_test_plan plan
JOIN demo_dashboard_seq seq ON seq.n <= plan.total_count
WHERE NOT EXISTS (
    SELECT 1
    FROM leak_test_work_records existing
    WHERE existing.engine_number = CONCAT('DASH-', DATE_FORMAT(plan.check_date, '%Y%m%d'), '-', LPAD(seq.n, 3, '0'))
);

DROP TEMPORARY TABLE IF EXISTS demo_dashboard_seq;
DROP TEMPORARY TABLE IF EXISTS demo_dashboard_leak_test_plan;
