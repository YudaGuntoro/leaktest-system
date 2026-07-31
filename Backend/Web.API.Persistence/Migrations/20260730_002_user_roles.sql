-- Normalize user roles into a roles master table and make users reference roles_id.

CREATE TABLE IF NOT EXISTS `roles` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `role_name` VARCHAR(30) NOT NULL,
    `description` VARCHAR(120) NULL,
    `is_active` TINYINT(1) NOT NULL DEFAULT 1,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uq_roles_role_name` (`role_name`)
);

INSERT INTO `roles` (`id`, `role_name`, `description`, `is_active`)
VALUES
    (1, 'ADMIN', 'Administrator', 1),
    (2, 'SUPERVISOR', 'Supervisor', 1),
    (3, 'OPERATOR', 'Operator', 1),
    (4, 'VIEWER', 'Viewer', 1)
ON DUPLICATE KEY UPDATE
    `role_name` = VALUES(`role_name`),
    `description` = VALUES(`description`),
    `is_active` = VALUES(`is_active`);

SET @has_roles_id = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'roles_id'
);

SET @sql = IF(
    @has_roles_id = 0,
    "ALTER TABLE `users` ADD COLUMN `roles_id` INT NULL AFTER `phone`",
    "SELECT 1"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_legacy_role = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'role'
);

SET @sql = IF(
    @has_legacy_role = 1,
    "UPDATE `users` u
     LEFT JOIN `roles` r ON r.`role_name` = UPPER(u.`role`)
     SET u.`roles_id` = COALESCE(r.`id`, 4)
     WHERE u.`roles_id` IS NULL",
    "UPDATE `users` SET `roles_id` = 4 WHERE `roles_id` IS NULL"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

ALTER TABLE `users`
    MODIFY COLUMN `roles_id` INT NOT NULL;

SET @has_users_roles_idx = (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND INDEX_NAME = 'ix_users_roles_id'
);

SET @sql = IF(
    @has_users_roles_idx = 0,
    "CREATE INDEX `ix_users_roles_id` ON `users` (`roles_id`)",
    "SELECT 1"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_users_roles_fk = (
    SELECT COUNT(*)
    FROM information_schema.REFERENTIAL_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND CONSTRAINT_NAME = 'fk_users_roles'
);

SET @sql = IF(
    @has_users_roles_fk = 0,
    "ALTER TABLE `users`
       ADD CONSTRAINT `fk_users_roles`
       FOREIGN KEY (`roles_id`) REFERENCES `roles` (`id`)
       ON UPDATE CASCADE
       ON DELETE RESTRICT",
    "SELECT 1"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = IF(
    @has_legacy_role = 1,
    "ALTER TABLE `users` DROP COLUMN `role`",
    "SELECT 1"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
