-- Demo leak test records for July 30, 2026.

INSERT INTO leak_test_work_records
    (id, engine_model_id, engine_number, check_date, check_time, machine_name, parameter_pressure, pressure_input, cycle_time_leak_test_minutes, result)
VALUES
    (2001, 1, 'LT-20260730-001', '2026-07-30', '07:35:00', 'Leak Tester Machine', 0.30, 0.30, 5.00, 'OK'),
    (2002, 2, 'LT-20260730-002', '2026-07-30', '07:48:00', 'Leak Tester Machine', 0.35, 0.34, 5.20, 'OK'),
    (2003, 3, 'LT-20260730-003', '2026-07-30', '08:02:00', 'Leak Tester Machine', 0.25, 0.20, 4.80, 'NG'),
    (2004, 4, 'LT-20260730-004', '2026-07-30', '08:18:00', 'Leak Tester Machine', 0.32, 0.32, 5.40, 'OK'),
    (2005, 5, 'LT-20260730-005', '2026-07-30', '08:31:00', 'Leak Tester Machine', 0.28, 0.22, 5.10, 'NG'),
    (2006, 6, 'LT-20260730-006', '2026-07-30', '08:47:00', 'Leak Tester Machine', 0.31, 0.31, 5.50, 'OK'),
    (2007, 7, 'LT-20260730-007', '2026-07-30', '09:05:00', 'Leak Tester Machine', 0.36, 0.28, 6.00, 'NG'),
    (2008, 8, 'LT-20260730-008', '2026-07-30', '09:20:00', 'Leak Tester Machine', 0.27, 0.27, 4.90, 'OK')
ON DUPLICATE KEY UPDATE
    engine_model_id = VALUES(engine_model_id),
    engine_number = VALUES(engine_number),
    check_date = VALUES(check_date),
    check_time = VALUES(check_time),
    machine_name = VALUES(machine_name),
    parameter_pressure = VALUES(parameter_pressure),
    pressure_input = VALUES(pressure_input),
    cycle_time_leak_test_minutes = VALUES(cycle_time_leak_test_minutes),
    result = VALUES(result);
