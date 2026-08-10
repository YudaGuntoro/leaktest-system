-- Extend judgement master to 16 HMI codes.

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
    (10, '', '', '', 0),
    (11, '', '', '', 0),
    (12, '', '', '', 0),
    (13, '', '', '', 0),
    (14, '', '', '', 0),
    (15, '', '', '', 0),
    (16, '', '', '', 0)
ON DUPLICATE KEY UPDATE
    judgement_name = VALUES(judgement_name),
    result = VALUES(result),
    note = VALUES(note),
    is_deleted = VALUES(is_deleted),
    updated_at = CURRENT_TIMESTAMP;
