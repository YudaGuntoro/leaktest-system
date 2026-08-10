-- Force activate HMI judgement master values.

INSERT INTO leak_test_judgements
    (judgement_code, judgement_name, result, note, is_deleted)
VALUES
    (1, 'LL NG', 'NG', 'HMI judgement', 0),
    (2, 'PASS', 'OK', 'HMI judgement', 0),
    (3, 'UL NG', 'NG', 'HMI judgement', 0),
    (4, 'LL2 NG', 'NG', 'HMI judgement', 0),
    (5, 'UL2 NG', 'NG', 'HMI judgement', 0),
    (6, 'ERROR', 'NG', 'HMI judgement', 0)
ON DUPLICATE KEY UPDATE
    judgement_name = VALUES(judgement_name),
    result = VALUES(result),
    note = VALUES(note),
    is_deleted = VALUES(is_deleted),
    updated_at = CURRENT_TIMESTAMP;

UPDATE leak_test_judgements
SET is_deleted = 1, updated_at = CURRENT_TIMESTAMP
WHERE judgement_code = 7;
