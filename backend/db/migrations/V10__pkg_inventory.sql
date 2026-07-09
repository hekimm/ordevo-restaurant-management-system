
CREATE OR REPLACE PACKAGE PKG_INVENTORY AS
  PROCEDURE ADD_MOVEMENT(
    p_stock_item_id IN VARCHAR2, p_type IN VARCHAR2, p_qty_signed IN NUMBER,
    p_unit_cost IN NUMBER, p_ref_type IN VARCHAR2, p_ref_id IN VARCHAR2,
    p_note IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE RECEIVE_PURCHASE(p_purchase_id IN VARCHAR2, p_user_id IN VARCHAR2);
  PROCEDURE CONSUME_FOR_ORDER(p_order_id IN VARCHAR2);
  PROCEDURE ADJUST_STOCK(p_stock_item_id IN VARCHAR2, p_new_qty IN NUMBER, p_reason IN VARCHAR2, p_user_id IN VARCHAR2);
  PROCEDURE RECORD_WASTAGE(p_stock_item_id IN VARCHAR2, p_qty IN NUMBER, p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_wastage_id OUT VARCHAR2);
END PKG_INVENTORY;
/

CREATE OR REPLACE PACKAGE BODY PKG_INVENTORY AS

  PROCEDURE ADD_MOVEMENT(
    p_stock_item_id IN VARCHAR2, p_type IN VARCHAR2, p_qty_signed IN NUMBER,
    p_unit_cost IN NUMBER, p_ref_type IN VARCHAR2, p_ref_id IN VARCHAR2,
    p_note IN VARCHAR2, p_user_id IN VARCHAR2)
  IS
    v_tenant VARCHAR2(36);
    v_branch VARCHAR2(36);
  BEGIN
    SELECT TENANT_ID, BRANCH_ID INTO v_tenant, v_branch FROM STOCK_ITEMS WHERE ID = p_stock_item_id;

    INSERT INTO STOCK_MOVEMENTS (ID, TENANT_ID, BRANCH_ID, STOCK_ITEM_ID, MOVE_TYPE, QUANTITY, UNIT_COST, REF_TYPE, REF_ID, NOTE, CREATED_BY)
    VALUES (PKG_ORDERING.NEW_ID, v_tenant, v_branch, p_stock_item_id, p_type, p_qty_signed, NVL(p_unit_cost,0), p_ref_type, p_ref_id, p_note, p_user_id);

    UPDATE STOCK_ITEMS
       SET ON_HAND = ON_HAND + p_qty_signed, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE ID = p_stock_item_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20202, 'Stock item not found.');
  END ADD_MOVEMENT;

  PROCEDURE RECEIVE_PURCHASE(p_purchase_id IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_status VARCHAR2(16);
    v_total  NUMBER(18,4) := 0;
  BEGIN
    BEGIN
      SELECT STATUS INTO v_status FROM PURCHASE_ORDERS WHERE ID = p_purchase_id;
    EXCEPTION WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20203, 'Purchase not found.');
    END;
    IF v_status <> 'draft' THEN RAISE_APPLICATION_ERROR(-20201, 'Purchase already processed.'); END IF;

    FOR pl IN (SELECT STOCK_ITEM_ID, QUANTITY, UNIT_COST, LINE_TOTAL FROM PURCHASE_LINES WHERE PURCHASE_ID = p_purchase_id) LOOP
      ADD_MOVEMENT(pl.STOCK_ITEM_ID, 'purchase', pl.QUANTITY, pl.UNIT_COST, 'purchase', p_purchase_id, NULL, p_user_id);
      UPDATE STOCK_ITEMS SET UNIT_COST = pl.UNIT_COST WHERE ID = pl.STOCK_ITEM_ID;
      v_total := v_total + pl.LINE_TOTAL;
    END LOOP;

    UPDATE PURCHASE_ORDERS SET STATUS = 'received', TOTAL = v_total, RECEIVED_AT = SYSTIMESTAMP WHERE ID = p_purchase_id;
  END RECEIVE_PURCHASE;

  PROCEDURE CONSUME_FOR_ORDER(p_order_id IN VARCHAR2) IS
    v_qty NUMBER(18,4);
  BEGIN
    FOR oi IN (SELECT ID, MENU_ITEM_ID, QUANTITY FROM ORDER_ITEMS WHERE ORDER_ID = p_order_id AND STATUS <> 'void') LOOP
      FOR rl IN (SELECT rl.STOCK_ITEM_ID, rl.QUANTITY, NVL(r.YIELD_QTY,1) AS YIELD_QTY
                   FROM RECIPES r JOIN RECIPE_LINES rl ON rl.RECIPE_ID = r.ID
                  WHERE r.MENU_ITEM_ID = oi.MENU_ITEM_ID) LOOP
        v_qty := rl.QUANTITY * oi.QUANTITY / rl.YIELD_QTY;
        ADD_MOVEMENT(rl.STOCK_ITEM_ID, 'sale', -v_qty, NULL, 'order', p_order_id, NULL, NULL);
      END LOOP;
    END LOOP;
  END CONSUME_FOR_ORDER;

  PROCEDURE ADJUST_STOCK(p_stock_item_id IN VARCHAR2, p_new_qty IN NUMBER, p_reason IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_cur NUMBER(18,4);
  BEGIN
    BEGIN
      SELECT ON_HAND INTO v_cur FROM STOCK_ITEMS WHERE ID = p_stock_item_id;
    EXCEPTION WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20202, 'Stock item not found.');
    END;
    ADD_MOVEMENT(p_stock_item_id, 'adjustment', p_new_qty - v_cur, NULL, 'adjustment', NULL, p_reason, p_user_id);
  END ADJUST_STOCK;

  PROCEDURE RECORD_WASTAGE(p_stock_item_id IN VARCHAR2, p_qty IN NUMBER, p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_wastage_id OUT VARCHAR2) IS
    v_tenant VARCHAR2(36);
    v_branch VARCHAR2(36);
  BEGIN
    IF p_qty <= 0 THEN RAISE_APPLICATION_ERROR(-20204, 'Wastage quantity must be positive.'); END IF;
    BEGIN
      SELECT TENANT_ID, BRANCH_ID INTO v_tenant, v_branch FROM STOCK_ITEMS WHERE ID = p_stock_item_id;
    EXCEPTION WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20202, 'Stock item not found.');
    END;

    p_wastage_id := PKG_ORDERING.NEW_ID;
    INSERT INTO WASTAGE (ID, TENANT_ID, BRANCH_ID, STOCK_ITEM_ID, QUANTITY, REASON, CREATED_BY)
    VALUES (p_wastage_id, v_tenant, v_branch, p_stock_item_id, p_qty, p_reason, p_user_id);

    ADD_MOVEMENT(p_stock_item_id, 'wastage', -p_qty, NULL, 'wastage', p_wastage_id, p_reason, p_user_id);
  END RECORD_WASTAGE;

END PKG_INVENTORY;
/

CREATE OR REPLACE TRIGGER TRG_ORDER_CLOSED_CONSUME
AFTER UPDATE OF STATUS ON ORDERS
FOR EACH ROW
WHEN (NEW.STATUS = 'closed' AND OLD.STATUS <> 'closed')
BEGIN
  PKG_INVENTORY.CONSUME_FOR_ORDER(:NEW.ID);
END;
/
