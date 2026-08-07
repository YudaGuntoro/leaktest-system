-- Leaktester Work Record - manual rework engine history.

CREATE TABLE IF NOT EXISTS rework_engine_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    engine_model_id INT NULL,
    engine_model_text VARCHAR(80) NULL,
    engine_number VARCHAR(120) NOT NULL,
    barcode_scan VARCHAR(180) NOT NULL,
    rework_date DATE NOT NULL,
    rework_time VARCHAR(8) NOT NULL,
    operator_id INT NULL,
    parameter_pressure DECIMAL(8, 2) NOT NULL,
    pressure_input DECIMAL(8, 2) NOT NULL,
    result VARCHAR(10) NOT NULL,
    note VARCHAR(255) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY ix_rework_engine_records_date_engine (rework_date, engine_number),
    KEY ix_rework_engine_records_result (result),
    KEY ix_rework_engine_records_engine_model_id (engine_model_id),
    KEY ix_rework_engine_records_operator_id (operator_id),
    CONSTRAINT fk_rework_engine_records_engine_model
        FOREIGN KEY (engine_model_id) REFERENCES engine_models (id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,
    CONSTRAINT fk_rework_engine_records_operator
        FOREIGN KEY (operator_id) REFERENCES operators (id)
        ON UPDATE CASCADE
        ON DELETE SET NULL
);

INSERT INTO rework_engine_records
    (id, engine_model_id, engine_model_text, engine_number, barcode_scan, rework_date, rework_time, operator_id, parameter_pressure, pressure_input, result, note, created_at, updated_at)
VALUES
    (3001, 3, NULL, 'DUMMY-REWORK-20260807-001', 'TF65R DUMMY-REWORK-20260807-001', '2026-08-07', '07:45:00', 1, 0.30, 0.30, 'OK', 'Rework seal cover, retest normal', '2026-08-07 07:45:00', '2026-08-07 07:45:00'),
    (3002, 4, NULL, 'DUMMY-REWORK-20260807-002', 'TF65R-E2 DUMMY-REWORK-20260807-002', '2026-08-07', '08:10:00', 2, 0.30, 0.27, 'NG', 'Leak still detected near gasket area', '2026-08-07 08:10:00', '2026-08-07 08:10:00'),
    (3003, 2, NULL, 'DUMMY-REWORK-20260807-003', 'TF65L-YS DUMMY-REWORK-20260807-003', '2026-08-07', '08:35:00', 3, 0.25, 0.25, 'OK', 'Bolt torque correction completed', '2026-08-07 08:35:00', '2026-08-07 08:35:00'),
    (3004, 1, NULL, 'DUMMY-REWORK-20260807-004', 'TF65H DUMMY-REWORK-20260807-004', '2026-08-07', '09:05:00', 4, 0.35, 0.34, 'OK', 'Recheck after hose fitting adjustment', '2026-08-07 09:05:00', '2026-08-07 09:05:00'),
    (3005, 8, NULL, 'DUMMY-REWORK-20260807-005', 'TF70H-IV DUMMY-REWORK-20260807-005', '2026-08-07', '09:40:00', 1, 0.30, 0.22, 'NG', 'Pressure drop after 3 minutes', '2026-08-07 09:40:00', '2026-08-07 09:40:00'),
    (3006, 7, NULL, 'DUMMY-REWORK-20260807-006', 'TF70-EIV DUMMY-REWORK-20260807-006', '2026-08-07', '10:15:00', 2, 0.32, 0.32, 'OK', 'Rework pipe joint, no leak found', '2026-08-07 10:15:00', '2026-08-07 10:15:00'),
    (3007, 6, NULL, 'DUMMY-REWORK-20260807-007', 'TF70-EISA DUMMY-REWORK-20260807-007', '2026-08-07', '10:55:00', 3, 0.25, 0.24, 'OK', 'Retest after cleaning mating surface', '2026-08-07 10:55:00', '2026-08-07 10:55:00'),
    (3008, 5, NULL, 'DUMMY-REWORK-20260807-008', 'TF70-EIS DUMMY-REWORK-20260807-008', '2026-08-07', '11:30:00', 4, 0.35, 0.29, 'NG', 'Manual retest failed, hold for inspection', '2026-08-07 11:30:00', '2026-08-07 11:30:00'),
    (3009, 14, NULL, 'DUMMY-REWORK-20260807-009', 'TF85MH DUMMY-REWORK-20260807-009', '2026-08-07', '13:05:00', 1, 0.30, 0.30, 'OK', 'Oil seal replacement confirmed OK', '2026-08-07 13:05:00', '2026-08-07 13:05:00'),
    (3010, 18, NULL, 'DUMMY-REWORK-20260807-010', 'TF85MR DUMMY-REWORK-20260807-010', '2026-08-07', '13:45:00', 2, 0.32, 0.31, 'OK', 'Retest after clamp adjustment', '2026-08-07 13:45:00', '2026-08-07 13:45:00'),
    (3011, 31, NULL, 'DUMMY-REWORK-20260807-011', 'TF115MR DUMMY-REWORK-20260807-011', '2026-08-07', '14:20:00', 3, 0.35, 0.35, 'OK', 'Final rework inspection passed', '2026-08-07 14:20:00', '2026-08-07 14:20:00'),
    (3012, 33, NULL, 'DUMMY-REWORK-20260807-012', 'TF120M-EGN DUMMY-REWORK-20260807-012', '2026-08-07', '15:10:00', 4, 0.30, 0.24, 'NG', 'Repeat leak at intake side', '2026-08-07 15:10:00', '2026-08-07 15:10:00')
ON DUPLICATE KEY UPDATE
    engine_model_id = VALUES(engine_model_id),
    engine_model_text = VALUES(engine_model_text),
    engine_number = VALUES(engine_number),
    barcode_scan = VALUES(barcode_scan),
    rework_date = VALUES(rework_date),
    rework_time = VALUES(rework_time),
    operator_id = VALUES(operator_id),
    parameter_pressure = VALUES(parameter_pressure),
    pressure_input = VALUES(pressure_input),
    result = VALUES(result),
    note = VALUES(note),
    created_at = VALUES(created_at),
    updated_at = VALUES(updated_at);
