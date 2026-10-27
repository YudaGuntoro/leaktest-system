-- Stores MQTT messages rejected by worker payload validation for later review/replay.
CREATE TABLE IF NOT EXISTS leak_test_rejected_mqtt_messages (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    topic VARCHAR(255) NOT NULL,
    payload LONGTEXT NOT NULL,
    error_message TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    attempt_count INT NOT NULL DEFAULT 0,
    received_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_attempt_at DATETIME NULL,
    processed_at DATETIME NULL,
    KEY ix_rejected_mqtt_status_received (status, received_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
