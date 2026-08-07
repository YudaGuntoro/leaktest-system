-- Extra demo volume so monthly dashboard bars are balanced and readable.

DROP TEMPORARY TABLE IF EXISTS demo_dashboard_monthly_volume_plan;
CREATE TEMPORARY TABLE demo_dashboard_monthly_volume_plan (
    check_date DATE NOT NULL PRIMARY KEY,
    total_count INT NOT NULL,
    ng_count INT NOT NULL
);

INSERT INTO demo_dashboard_monthly_volume_plan
    (check_date, total_count, ng_count)
VALUES
    ('2026-01-04', 30, 4),
    ('2026-01-09', 29, 5),
    ('2026-01-21', 31, 4),
    ('2026-01-27', 28, 3),
    ('2026-02-03', 32, 4),
    ('2026-02-10', 31, 3),
    ('2026-02-19', 32, 4),
    ('2026-02-25', 31, 5),
    ('2026-03-02', 31, 5),
    ('2026-03-09', 32, 4),
    ('2026-03-23', 30, 5),
    ('2026-03-30', 31, 4),
    ('2026-04-06', 30, 4),
    ('2026-04-13', 29, 3),
    ('2026-04-20', 31, 5),
    ('2026-04-27', 29, 4),
    ('2026-05-04', 28, 4),
    ('2026-05-11', 26, 3),
    ('2026-05-18', 27, 4),
    ('2026-05-25', 25, 3),
    ('2026-05-29', 26, 4),
    ('2026-06-01', 25, 3),
    ('2026-06-08', 26, 4),
    ('2026-06-15', 24, 3),
    ('2026-06-22', 27, 4),
    ('2026-06-29', 24, 3),
    ('2026-07-06', 20, 3),
    ('2026-07-13', 19, 2),
    ('2026-07-20', 19, 3),
    ('2026-09-07', 28, 4),
    ('2026-09-14', 26, 3),
    ('2026-09-21', 27, 4),
    ('2026-09-28', 25, 3),
    ('2026-09-30', 24, 3),
    ('2026-10-05', 28, 4),
    ('2026-10-12', 28, 4),
    ('2026-10-19', 27, 3),
    ('2026-10-26', 27, 4),
    ('2026-10-30', 27, 3),
    ('2026-11-02', 29, 4),
    ('2026-11-09', 29, 5),
    ('2026-11-16', 29, 4),
    ('2026-11-23', 29, 4),
    ('2026-11-30', 29, 4),
    ('2026-12-01', 31, 5),
    ('2026-12-08', 31, 4),
    ('2026-12-15', 31, 5),
    ('2026-12-22', 30, 4),
    ('2026-12-29', 30, 4);

DROP TEMPORARY TABLE IF EXISTS demo_dashboard_monthly_volume_seq;
CREATE TEMPORARY TABLE demo_dashboard_monthly_volume_seq (
    n INT NOT NULL PRIMARY KEY
);

INSERT INTO demo_dashboard_monthly_volume_seq (n)
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
    CONCAT('DASH-VOL-', DATE_FORMAT(plan.check_date, '%Y%m%d'), '-', LPAD(seq.n, 3, '0')) AS engine_number,
    plan.check_date,
    TIME_FORMAT(SEC_TO_TIME(26100 + ((seq.n - 1) * 600)), '%H:%i:%s') AS check_time,
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
    TIMESTAMP(plan.check_date, TIME_FORMAT(SEC_TO_TIME(26100 + ((seq.n - 1) * 600)), '%H:%i:%s')) AS created_at,
    TIMESTAMP(plan.check_date, TIME_FORMAT(SEC_TO_TIME(26100 + ((seq.n - 1) * 600)), '%H:%i:%s')) AS updated_at
FROM demo_dashboard_monthly_volume_plan plan
JOIN demo_dashboard_monthly_volume_seq seq ON seq.n <= plan.total_count
WHERE NOT EXISTS (
    SELECT 1
    FROM leak_test_work_records existing
    WHERE existing.engine_number = CONCAT('DASH-VOL-', DATE_FORMAT(plan.check_date, '%Y%m%d'), '-', LPAD(seq.n, 3, '0'))
);

DROP TEMPORARY TABLE IF EXISTS demo_dashboard_monthly_volume_seq;
DROP TEMPORARY TABLE IF EXISTS demo_dashboard_monthly_volume_plan;
