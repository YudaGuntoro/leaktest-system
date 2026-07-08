-- Production Control Monitoring System - PT YKK AP Indonesia
-- Jalankan setelah migration users/auth pada database MySQL 8.

CREATE TABLE IF NOT EXISTS shift_masters (
    id INT AUTO_INCREMENT PRIMARY KEY,
    shift_code VARCHAR(50) NOT NULL,
    shift_name VARCHAR(100) NOT NULL,
    sort_order INT NOT NULL DEFAULT 0,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_shift_masters_code (shift_code),
    KEY ix_shift_masters_sort_order (sort_order)
);

CREATE TABLE IF NOT EXISTS pic_cards (
    id INT AUTO_INCREMENT PRIMARY KEY,
    card_uid VARCHAR(100) NOT NULL,
    employee_no VARCHAR(50) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    department VARCHAR(100) NOT NULL DEFAULT 'Production',
    shift VARCHAR(30) NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    last_scanned_at DATETIME NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_pic_cards_card_uid (card_uid),
    UNIQUE KEY uq_pic_cards_employee_no (employee_no)
);

CREATE TABLE IF NOT EXISTS cutting_lists (
    id INT AUTO_INCREMENT PRIMARY KEY,
    cutting_list_no VARCHAR(80) NOT NULL,
    product_code VARCHAR(80) NOT NULL,
    product_name VARCHAR(200) NOT NULL,
    line_code VARCHAR(50) NOT NULL,
    planned_qty INT NOT NULL DEFAULT 0,
    unit VARCHAR(20) NOT NULL DEFAULT 'PCS',
    plan_date DATE NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'OPEN',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_cutting_lists_no (cutting_list_no),
    KEY ix_cutting_lists_plan_line (plan_date, line_code)
);

CREATE TABLE IF NOT EXISTS production_work_orders (
    id INT AUTO_INCREMENT PRIMARY KEY,
    wo_number VARCHAR(80) NOT NULL,
    cutting_list_id INT NOT NULL,
    pic_card_id INT NULL,
    line_code VARCHAR(50) NOT NULL,
    target_qty INT NOT NULL DEFAULT 0,
    actual_qty INT NOT NULL DEFAULT 0,
    reject_qty INT NOT NULL DEFAULT 0,
    status VARCHAR(30) NOT NULL DEFAULT 'WAITING',
    started_at DATETIME NULL,
    completed_at DATETIME NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_production_work_orders_no (wo_number),
    KEY ix_production_work_orders_status_line (status, line_code),
    CONSTRAINT fk_production_wo_cutting_list FOREIGN KEY (cutting_list_id) REFERENCES cutting_lists(id),
    CONSTRAINT fk_production_wo_pic FOREIGN KEY (pic_card_id) REFERENCES pic_cards(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS production_activity_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    production_work_order_id INT NOT NULL,
    pic_card_id INT NULL,
    activity_type VARCHAR(40) NOT NULL,
    remarks TEXT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    KEY ix_production_logs_wo_created (production_work_order_id, created_at),
    CONSTRAINT fk_production_logs_wo FOREIGN KEY (production_work_order_id) REFERENCES production_work_orders(id) ON DELETE CASCADE,
    CONSTRAINT fk_production_logs_pic FOREIGN KEY (pic_card_id) REFERENCES pic_cards(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS production_work_order_operators (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    production_work_order_id INT NOT NULL,
    pic_card_id INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    scanned_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    removed_at DATETIME NULL,
    KEY ix_production_wo_operators_active (production_work_order_id, is_active),
    KEY ix_production_wo_operators_pic (pic_card_id),
    CONSTRAINT fk_production_wo_operators_wo FOREIGN KEY (production_work_order_id) REFERENCES production_work_orders(id) ON DELETE CASCADE,
    CONSTRAINT fk_production_wo_operators_pic FOREIGN KEY (pic_card_id) REFERENCES pic_cards(id)
);

INSERT INTO shift_masters (shift_code, shift_name, sort_order, is_active)
VALUES
    ('SHIFT_1', 'Shift 1', 1, 1),
    ('SHIFT_2', 'Shift 2', 2, 1),
    ('SHIFT_3', 'Shift 3', 3, 1),
    ('LONG_SHIFT_1', 'Long Shift 1', 4, 1),
    ('LONG_SHIFT_2', 'Long Shift 2', 5, 1)
ON DUPLICATE KEY UPDATE
    shift_name = VALUES(shift_name),
    sort_order = VALUES(sort_order),
    is_active = VALUES(is_active);

INSERT IGNORE INTO pic_cards (id, card_uid, employee_no, full_name, department, shift)
VALUES
    (1, 'YKK-PIC-0001', 'YKK001', 'Budi Santoso', 'Production', 'Shift 1'),
    (2, 'YKK-PIC-0002', 'YKK002', 'Siti Rahma', 'Production', 'Shift 1'),
    (3, 'YKK-PIC-0003', 'YKK003', 'Andi Pratama', 'Production', 'Shift 2');

INSERT IGNORE INTO cutting_lists
    (id, cutting_list_no, product_code, product_name, line_code, planned_qty, unit, plan_date, status)
VALUES
    (1, 'CL-YKK-001', 'AL-FRAME-01', 'Aluminium Frame Type A', 'LINE-01', 0, 'PCS', CURRENT_DATE, 'IN_PROGRESS'),
    (2, 'CL-YKK-002', 'AL-SASH-02', 'Aluminium Sash Type B', 'LINE-02', 0, 'PCS', CURRENT_DATE, 'RELEASED'),
    (3, 'CL-YKK-003', 'HDL-LOCK-01', 'Handle Lock Assembly', 'LINE-03', 0, 'SET', CURRENT_DATE, 'COMPLETED');

INSERT IGNORE INTO production_work_orders
    (id, wo_number, cutting_list_id, pic_card_id, line_code, target_qty, actual_qty, reject_qty, status, started_at, completed_at)
VALUES
    (1, 'WO-YKK-001', 1, 1, 'LINE-01', 0, 325, 3, 'IN_PROGRESS', TIMESTAMP(CURRENT_DATE, '07:30:00'), NULL),
    (2, 'WO-YKK-002', 2, NULL, 'LINE-02', 0, 0, 0, 'WAITING', NULL, NULL),
    (3, 'WO-YKK-003', 3, 2, 'LINE-03', 0, 240, 1, 'COMPLETED', TIMESTAMP(CURRENT_DATE, '07:15:00'), TIMESTAMP(CURRENT_DATE, '11:20:00'));

INSERT INTO production_work_order_operators (production_work_order_id, pic_card_id, is_active, scanned_at, removed_at)
SELECT id, pic_card_id, 1, COALESCE(started_at, updated_at, created_at), NULL
FROM production_work_orders pwo
WHERE pic_card_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM production_work_order_operators pwoo
      WHERE pwoo.production_work_order_id = pwo.id
        AND pwoo.pic_card_id = pwo.pic_card_id
        AND pwoo.is_active = 1
  );

INSERT INTO production_activity_logs (production_work_order_id, pic_card_id, activity_type, remarks)
SELECT 1, 1, 'PIC_SCAN', 'Initial demo PIC scan'
WHERE NOT EXISTS (
    SELECT 1 FROM production_activity_logs WHERE production_work_order_id = 1 AND activity_type = 'PIC_SCAN'
);
