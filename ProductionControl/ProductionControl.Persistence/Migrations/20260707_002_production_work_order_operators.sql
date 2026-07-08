-- Production Control - support multiple active operators per work order.

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

INSERT INTO production_work_order_operators (production_work_order_id, pic_card_id, is_active, scanned_at, removed_at)
SELECT pwo.id, pwo.pic_card_id, 1, COALESCE(pwo.started_at, pwo.updated_at, pwo.created_at), NULL
FROM production_work_orders pwo
WHERE pwo.pic_card_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM production_work_order_operators pwoo
      WHERE pwoo.production_work_order_id = pwo.id
        AND pwoo.pic_card_id = pwo.pic_card_id
        AND pwoo.is_active = 1
  );
