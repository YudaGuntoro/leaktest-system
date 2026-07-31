-- Convert users.status text values to users.is_active boolean.

SET @has_is_active = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'is_active'
);

SET @sql = IF(
    @has_is_active = 0,
    "ALTER TABLE `users` ADD COLUMN `is_active` TINYINT(1) NULL AFTER `roles_id`",
    "SELECT 1"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_status = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'status'
);

SET @sql = IF(
    @has_status = 1,
    "UPDATE `users`
     SET `is_active` = CASE WHEN UPPER(`status`) = 'ACTIVE' THEN 1 ELSE 0 END
     WHERE `is_active` IS NULL",
    "UPDATE `users` SET `is_active` = 1 WHERE `is_active` IS NULL"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

ALTER TABLE `users`
    MODIFY COLUMN `is_active` TINYINT(1) NOT NULL DEFAULT 1;

SET @sql = IF(
    @has_status = 1,
    "ALTER TABLE `users` DROP COLUMN `status`",
    "SELECT 1"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
