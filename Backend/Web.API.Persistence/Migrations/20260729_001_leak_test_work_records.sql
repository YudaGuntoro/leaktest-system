-- Leaktester Work Record - engine information and leak test result table.

CREATE TABLE IF NOT EXISTS leak_test_work_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    engine_model_id INT NOT NULL,
    engine_number VARCHAR(120) NOT NULL,
    check_date DATE NOT NULL,
    check_time VARCHAR(8) NOT NULL,
    machine_name VARCHAR(150) NOT NULL,
    parameter_pressure DECIMAL(8, 2) NOT NULL,
    pressure_input DECIMAL(8, 2) NOT NULL,
    cycle_time_leak_test_minutes DECIMAL(8, 2) NOT NULL,
    result VARCHAR(10) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY ix_leak_test_work_records_date_engine (check_date, engine_number),
    KEY ix_leak_test_work_records_result (result),
    KEY ix_leak_test_work_records_engine_model_id (engine_model_id),
    CONSTRAINT fk_leak_test_work_records_engine_model
        FOREIGN KEY (engine_model_id) REFERENCES engine_models (id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

INSERT INTO leak_test_work_records
    (id, engine_model_id, engine_number, check_date, check_time, machine_name, parameter_pressure, pressure_input, cycle_time_leak_test_minutes, result)
VALUES
    (1, 1, 'ENG-LT-0001', CURRENT_DATE, '08:15:00', 'Leak Tester Machine', 0.30, 0.29, 5.00, 'OK'),
    (2, 2, 'ENG-LT-0002', CURRENT_DATE, '09:05:00', 'Leak Tester Machine', 0.35, 0.34, 6.00, 'OK')
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
