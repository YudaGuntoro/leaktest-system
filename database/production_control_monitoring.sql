-- Fresh database bootstrap for Production Control Monitoring System
-- PT YKK AP Indonesia / MySQL 8

CREATE DATABASE IF NOT EXISTS db_production_control
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE db_production_control;

CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(80) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(150) NULL,
    phone VARCHAR(50) NULL,
    role VARCHAR(30) NOT NULL,
    status VARCHAR(30) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    password_salt VARCHAR(255) NOT NULL,
    last_login_at DATETIME NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_users_username (username),
    UNIQUE KEY uq_users_email (email)
);

-- Default login: admin / admin123
INSERT IGNORE INTO users
    (id, username, full_name, email, role, status, password_hash, password_salt)
VALUES
    (1, 'admin', 'Production Control Administrator', 'admin@ykkap.local', 'ADMIN', 'ACTIVE',
     'mV/QhZOhh7mvmWj0P1RgeXm3hZB1AkKHY5jfEcrC7PE=', 'Y21tcy1hZG1pbi1zYWx0LXYx');

SOURCE ProductionControl/ProductionControl.Persistence/Migrations/20260707_001_production_control_monitoring.sql;
SOURCE ProductionControl/ProductionControl.Persistence/Migrations/20260707_002_production_work_order_operators.sql;
SOURCE ProductionControl/ProductionControl.Persistence/Migrations/20260708_001_shift_master_and_no_target.sql;
