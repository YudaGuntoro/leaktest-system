-- Leaktester Work Record - parameter master data imported from machine Excel.

CREATE TABLE IF NOT EXISTS leak_test_parameters (
    id INT AUTO_INCREMENT PRIMARY KEY,
    channel_no VARCHAR(20) NOT NULL,
    model_parameter VARCHAR(150) NOT NULL,
    item_name VARCHAR(120) NOT NULL,
    item_value VARCHAR(80) NOT NULL,
    machine_names VARCHAR(1000) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_leak_test_parameters_channel_item (channel_no, item_name),
    KEY ix_leak_test_parameters_channel_no (channel_no),
    KEY ix_leak_test_parameters_model_parameter (model_parameter)
);
