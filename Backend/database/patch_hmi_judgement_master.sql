USE yanmarleaktest;

CREATE TABLE IF NOT EXISTS leak_test_judgements (
    id INT AUTO_INCREMENT PRIMARY KEY,
    judgement_code INT NOT NULL,
    judgement_name VARCHAR(80) NOT NULL,
    result VARCHAR(10) NOT NULL,
    note VARCHAR(150) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_leak_test_judgements_code (judgement_code),
    KEY ix_leak_test_judgements_result (result)
);

INSERT INTO leak_test_judgements
    (judgement_code, judgement_name, result, note, is_deleted)
VALUES
    (1, 'LL NG', 'NG', 'HMI judgement', 0),
    (2, 'PASS', 'OK', 'HMI judgement', 0),
    (3, 'UL NG', 'NG', 'HMI judgement', 0),
    (4, 'LL2 NG', 'NG', 'HMI judgement', 0),
    (5, 'UL2 NG', 'NG', 'HMI judgement', 0),
    (6, 'ERROR', 'NG', 'HMI judgement', 0),
    (7, '', '', '', 0),
    (8, '', '', '', 0),
    (9, '', '', '', 0),
    (10, '', '', '', 0)
ON DUPLICATE KEY UPDATE
    judgement_name = VALUES(judgement_name),
    result = VALUES(result),
    note = VALUES(note),
    is_deleted = VALUES(is_deleted),
    updated_at = CURRENT_TIMESTAMP;

UPDATE leak_test_judgements
SET is_deleted = 1, updated_at = CURRENT_TIMESTAMP
WHERE judgement_code > 10;
