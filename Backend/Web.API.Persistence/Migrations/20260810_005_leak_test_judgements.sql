-- Leaktester Work Record - judgement master data.

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
    (1, 'DUMMY-1', 'NG', 'Temporary dummy judgement', 0),
    (2, 'OK', 'OK', 'Gateway judgement OK', 0),
    (3, 'DUMMY-3', 'NG', 'Temporary dummy judgement', 0),
    (4, 'NG', 'NG', 'Gateway judgement NG', 0),
    (5, 'DUMMY-5', 'NG', 'Temporary dummy judgement', 0),
    (6, 'DUMMY-6', 'NG', 'Temporary dummy judgement', 0),
    (7, 'DUMMY-7', 'NG', 'Temporary dummy judgement', 0)
ON DUPLICATE KEY UPDATE
    judgement_name = VALUES(judgement_name),
    result = VALUES(result),
    note = VALUES(note),
    is_deleted = VALUES(is_deleted),
    updated_at = CURRENT_TIMESTAMP;
