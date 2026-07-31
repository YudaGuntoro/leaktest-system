-- Remove legacy production-control tables that are not used by Leaktester Work Record.
-- Keep: users, roles, engine_models, leak_test_work_records.

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `production_work_order_operators`;
DROP TABLE IF EXISTS `production_activity_logs`;
DROP TABLE IF EXISTS `production_work_orders`;
DROP TABLE IF EXISTS `cutting_lists`;
DROP TABLE IF EXISTS `pic_cards`;
DROP TABLE IF EXISTS `shift_masters`;
DROP TABLE IF EXISTS `machine_models`;
DROP TABLE IF EXISTS `model_master`;

SET FOREIGN_KEY_CHECKS = 1;
