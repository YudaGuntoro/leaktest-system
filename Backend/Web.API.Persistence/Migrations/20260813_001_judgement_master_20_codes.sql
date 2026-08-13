-- Expand judgement master to 20 active HMI codes.

INSERT INTO leak_test_judgements
    (judgement_code, judgement_name, result, note, is_deleted)
VALUES
    (11, '', '', '', 0),
    (12, '', '', '', 0),
    (13, '', '', '', 0),
    (14, '', '', '', 0),
    (15, '', '', '', 0),
    (16, '', '', '', 0),
    (17, '', '', '', 0),
    (18, '', '', '', 0),
    (19, '', '', '', 0),
    (20, '', '', '', 0)
ON DUPLICATE KEY UPDATE
    is_deleted = VALUES(is_deleted),
    updated_at = CURRENT_TIMESTAMP;

UPDATE leak_test_judgements
SET is_deleted = 1, updated_at = CURRENT_TIMESTAMP
WHERE judgement_code > 20;
