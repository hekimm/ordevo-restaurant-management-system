
CREATE OR REPLACE PACKAGE PKG_ORDERING AS
  FUNCTION NEW_ID RETURN VARCHAR2;

  PROCEDURE RECALC_ORDER(p_order_id IN VARCHAR2);

  PROCEDURE OPEN_ORDER(
    p_tenant_id  IN  VARCHAR2, p_branch_id IN VARCHAR2, p_table_id IN VARCHAR2,
    p_order_type IN  VARCHAR2, p_guest_count IN NUMBER, p_user_id IN VARCHAR2,
    p_order_id   OUT VARCHAR2, p_order_no  OUT NUMBER);

  PROCEDURE ADD_ITEM(
    p_order_id IN VARCHAR2, p_menu_item_id IN VARCHAR2, p_name IN VARCHAR2,
    p_unit_price IN NUMBER, p_qty IN NUMBER, p_vat_rate IN NUMBER,
    p_course_no IN NUMBER, p_note IN VARCHAR2, p_user_id IN VARCHAR2,
    p_item_id OUT VARCHAR2);

  PROCEDURE ADD_ITEM_MODIFIER(
    p_order_item_id IN VARCHAR2, p_modifier_id IN VARCHAR2,
    p_name IN VARCHAR2, p_price_delta IN NUMBER);

  PROCEDURE SET_ITEM_QTY(p_order_item_id IN VARCHAR2, p_qty IN NUMBER);
  PROCEDURE VOID_ITEM(p_order_item_id IN VARCHAR2, p_reason IN VARCHAR2, p_user_id IN VARCHAR2);
  PROCEDURE COMP_ITEM(p_order_item_id IN VARCHAR2, p_user_id IN VARCHAR2);
  PROCEDURE UPDATE_ITEM_STATUS(p_order_item_id IN VARCHAR2, p_status IN VARCHAR2);

  PROCEDURE APPLY_DISCOUNT(
    p_order_id IN VARCHAR2, p_disc_type IN VARCHAR2, p_disc_value IN NUMBER,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_disc_id OUT VARCHAR2);

  PROCEDURE MOVE_ITEM(p_order_item_id IN VARCHAR2, p_target_order_id IN VARCHAR2);
  PROCEDURE TRANSFER_ORDER(p_order_id IN VARCHAR2, p_to_table_id IN VARCHAR2, p_user_id IN VARCHAR2);
  PROCEDURE MERGE_ORDERS(p_source_order_id IN VARCHAR2, p_target_order_id IN VARCHAR2, p_user_id IN VARCHAR2);
  PROCEDURE CLOSE_ORDER(p_order_id IN VARCHAR2, p_user_id IN VARCHAR2);
  PROCEDURE CANCEL_ORDER(p_order_id IN VARCHAR2, p_reason IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE LOG_TRANSFER(
    p_tenant_id IN VARCHAR2, p_order_id IN VARCHAR2, p_action IN VARCHAR2,
    p_from_table IN VARCHAR2, p_to_table IN VARCHAR2, p_related_order IN VARCHAR2, p_user_id IN VARCHAR2);
END PKG_ORDERING;
/

CREATE OR REPLACE PACKAGE BODY PKG_ORDERING AS

  FUNCTION NEW_ID RETURN VARCHAR2 IS
    g VARCHAR2(32) := RAWTOHEX(SYS_GUID());
  BEGIN
    RETURN LOWER(SUBSTR(g,1,8)||'-'||SUBSTR(g,9,4)||'-'||SUBSTR(g,13,4)||'-'||SUBSTR(g,17,4)||'-'||SUBSTR(g,21,12));
  END NEW_ID;

  PROCEDURE RECALC_ORDER(p_order_id IN VARCHAR2) IS
    v_sub  NUMBER(18,4);
    v_tax  NUMBER(18,4);
    v_disc NUMBER(18,4);
  BEGIN
    SELECT NVL(SUM(LINE_TOTAL),0),
           NVL(SUM(LINE_TOTAL - LINE_TOTAL/(1 + VAT_RATE/100)),0)
      INTO v_sub, v_tax
      FROM ORDER_ITEMS
     WHERE ORDER_ID = p_order_id AND STATUS <> 'void' AND IS_COMP = 0;

    SELECT NVL(SUM(AMOUNT),0) INTO v_disc
      FROM ORDER_DISCOUNTS WHERE ORDER_ID = p_order_id;

    IF v_disc > v_sub THEN v_disc := v_sub; END IF;

    UPDATE ORDERS
       SET SUBTOTAL = v_sub, TAX_TOTAL = v_tax, DISCOUNT_TOTAL = v_disc,
           TOTAL = v_sub - v_disc, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE ID = p_order_id;
  END RECALC_ORDER;

  PROCEDURE ASSERT_OPEN(p_order_id IN VARCHAR2) IS
    v_status VARCHAR2(16);
  BEGIN
    SELECT STATUS INTO v_status FROM ORDERS WHERE ID = p_order_id;
    IF v_status <> 'open' THEN
      RAISE_APPLICATION_ERROR(-20002, 'Order is not open.');
    END IF;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20001, 'Order not found.');
  END ASSERT_OPEN;

  PROCEDURE OPEN_ORDER(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_table_id IN VARCHAR2,
    p_order_type IN VARCHAR2, p_guest_count IN NUMBER, p_user_id IN VARCHAR2,
    p_order_id OUT VARCHAR2, p_order_no OUT NUMBER)
  IS
    v_cnt NUMBER;
  BEGIN
    IF p_table_id IS NOT NULL THEN
      SELECT COUNT(*) INTO v_cnt FROM ORDERS WHERE TABLE_ID = p_table_id AND STATUS = 'open';
      IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20003, 'Table already has an open order.');
      END IF;
    END IF;

    p_order_id := NEW_ID;
    SELECT ORDER_NO_SEQ.NEXTVAL INTO p_order_no FROM DUAL;

    INSERT INTO ORDERS (ID, TENANT_ID, BRANCH_ID, ORDER_NO, TABLE_ID, ORDER_TYPE, STATUS, GUEST_COUNT, OPENED_BY)
    VALUES (p_order_id, p_tenant_id, p_branch_id, p_order_no, p_table_id,
            NVL(p_order_type,'dinein'), 'open', NVL(p_guest_count,1), p_user_id);

    IF p_table_id IS NOT NULL THEN
      UPDATE DINING_TABLES SET STATUS = 'occupied', UPDATED_AT = SYSTIMESTAMP WHERE ID = p_table_id;
    END IF;
  END OPEN_ORDER;

  PROCEDURE ADD_ITEM(
    p_order_id IN VARCHAR2, p_menu_item_id IN VARCHAR2, p_name IN VARCHAR2,
    p_unit_price IN NUMBER, p_qty IN NUMBER, p_vat_rate IN NUMBER,
    p_course_no IN NUMBER, p_note IN VARCHAR2, p_user_id IN VARCHAR2,
    p_item_id OUT VARCHAR2)
  IS
  BEGIN
    ASSERT_OPEN(p_order_id);
    p_item_id := NEW_ID;
    INSERT INTO ORDER_ITEMS (ID, ORDER_ID, TENANT_ID, MENU_ITEM_ID, NAME_SNAPSHOT, UNIT_PRICE, QUANTITY,
                             MODIFIER_TOTAL, LINE_TOTAL, VAT_RATE, COURSE_NO, STATUS, CREATED_BY)
    SELECT p_item_id, p_order_id, o.TENANT_ID, p_menu_item_id, p_name, p_unit_price, p_qty,
           0, p_unit_price * p_qty, NVL(p_vat_rate,10), NVL(p_course_no,1), 'pending', p_user_id
      FROM ORDERS o WHERE o.ID = p_order_id;

    IF p_note IS NOT NULL THEN
      UPDATE ORDER_ITEMS SET NOTE = p_note WHERE ID = p_item_id;
    END IF;

    RECALC_ORDER(p_order_id);
  END ADD_ITEM;

  PROCEDURE ADD_ITEM_MODIFIER(
    p_order_item_id IN VARCHAR2, p_modifier_id IN VARCHAR2,
    p_name IN VARCHAR2, p_price_delta IN NUMBER)
  IS
    v_order_id VARCHAR2(36);
  BEGIN
    INSERT INTO ORDER_ITEM_MODIFIERS (ID, ORDER_ITEM_ID, MODIFIER_ID, NAME_SNAPSHOT, PRICE_DELTA)
    VALUES (NEW_ID, p_order_item_id, p_modifier_id, p_name, NVL(p_price_delta,0));

    UPDATE ORDER_ITEMS
       SET MODIFIER_TOTAL = MODIFIER_TOTAL + NVL(p_price_delta,0)
     WHERE ID = p_order_item_id;
    UPDATE ORDER_ITEMS
       SET LINE_TOTAL = (UNIT_PRICE + MODIFIER_TOTAL) * QUANTITY, UPDATED_AT = SYSTIMESTAMP
     WHERE ID = p_order_item_id
    RETURNING ORDER_ID INTO v_order_id;

    RECALC_ORDER(v_order_id);
  END ADD_ITEM_MODIFIER;

  PROCEDURE SET_ITEM_QTY(p_order_item_id IN VARCHAR2, p_qty IN NUMBER) IS
    v_order_id VARCHAR2(36);
  BEGIN
    IF p_qty <= 0 THEN RAISE_APPLICATION_ERROR(-20004, 'Quantity must be positive.'); END IF;
    UPDATE ORDER_ITEMS
       SET QUANTITY = p_qty, LINE_TOTAL = (UNIT_PRICE + MODIFIER_TOTAL) * p_qty, UPDATED_AT = SYSTIMESTAMP
     WHERE ID = p_order_item_id
    RETURNING ORDER_ID INTO v_order_id;
    RECALC_ORDER(v_order_id);
  END SET_ITEM_QTY;

  PROCEDURE VOID_ITEM(p_order_item_id IN VARCHAR2, p_reason IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_order_id VARCHAR2(36);
  BEGIN
    UPDATE ORDER_ITEMS
       SET STATUS = 'void', VOID_REASON = p_reason, UPDATED_AT = SYSTIMESTAMP
     WHERE ID = p_order_item_id
    RETURNING ORDER_ID INTO v_order_id;
    RECALC_ORDER(v_order_id);
  END VOID_ITEM;

  PROCEDURE COMP_ITEM(p_order_item_id IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_order_id VARCHAR2(36);
  BEGIN
    UPDATE ORDER_ITEMS SET IS_COMP = 1, UPDATED_AT = SYSTIMESTAMP
     WHERE ID = p_order_item_id
    RETURNING ORDER_ID INTO v_order_id;
    RECALC_ORDER(v_order_id);
  END COMP_ITEM;

  PROCEDURE UPDATE_ITEM_STATUS(p_order_item_id IN VARCHAR2, p_status IN VARCHAR2) IS
  BEGIN
    UPDATE ORDER_ITEMS SET STATUS = p_status, UPDATED_AT = SYSTIMESTAMP WHERE ID = p_order_item_id;
  END UPDATE_ITEM_STATUS;

  PROCEDURE APPLY_DISCOUNT(
    p_order_id IN VARCHAR2, p_disc_type IN VARCHAR2, p_disc_value IN NUMBER,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_disc_id OUT VARCHAR2)
  IS
    v_sub NUMBER(18,4);
    v_amount NUMBER(18,4);
    v_tenant VARCHAR2(36);
  BEGIN
    ASSERT_OPEN(p_order_id);
    SELECT SUBTOTAL, TENANT_ID INTO v_sub, v_tenant FROM ORDERS WHERE ID = p_order_id;

    IF p_disc_type = 'percent' THEN
      v_amount := ROUND(v_sub * p_disc_value / 100, 4);
    ELSE
      v_amount := p_disc_value;
    END IF;

    p_disc_id := NEW_ID;
    INSERT INTO ORDER_DISCOUNTS (ID, ORDER_ID, TENANT_ID, DISC_TYPE, DISC_VALUE, AMOUNT, REASON, CREATED_BY)
    VALUES (p_disc_id, p_order_id, v_tenant, p_disc_type, p_disc_value, v_amount, p_reason, p_user_id);

    RECALC_ORDER(p_order_id);
  END APPLY_DISCOUNT;

  PROCEDURE MOVE_ITEM(p_order_item_id IN VARCHAR2, p_target_order_id IN VARCHAR2) IS
    v_src VARCHAR2(36);
    v_tenant VARCHAR2(36);
  BEGIN
    ASSERT_OPEN(p_target_order_id);
    SELECT ORDER_ID INTO v_src FROM ORDER_ITEMS WHERE ID = p_order_item_id;
    SELECT TENANT_ID INTO v_tenant FROM ORDERS WHERE ID = p_target_order_id;

    UPDATE ORDER_ITEMS SET ORDER_ID = p_target_order_id, TENANT_ID = v_tenant, UPDATED_AT = SYSTIMESTAMP
     WHERE ID = p_order_item_id;

    RECALC_ORDER(v_src);
    RECALC_ORDER(p_target_order_id);
  END MOVE_ITEM;

  PROCEDURE TRANSFER_ORDER(p_order_id IN VARCHAR2, p_to_table_id IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_from VARCHAR2(36);
    v_tenant VARCHAR2(36);
    v_cnt NUMBER;
  BEGIN
    ASSERT_OPEN(p_order_id);
    SELECT COUNT(*) INTO v_cnt FROM ORDERS WHERE TABLE_ID = p_to_table_id AND STATUS = 'open' AND ID <> p_order_id;
    IF v_cnt > 0 THEN RAISE_APPLICATION_ERROR(-20003, 'Target table already has an open order.'); END IF;

    SELECT TABLE_ID, TENANT_ID INTO v_from, v_tenant FROM ORDERS WHERE ID = p_order_id;
    UPDATE ORDERS SET TABLE_ID = p_to_table_id, UPDATED_AT = SYSTIMESTAMP WHERE ID = p_order_id;

    IF v_from IS NOT NULL AND v_from <> p_to_table_id THEN
      SELECT COUNT(*) INTO v_cnt FROM ORDERS WHERE TABLE_ID = v_from AND STATUS = 'open';
      IF v_cnt = 0 THEN UPDATE DINING_TABLES SET STATUS = 'idle' WHERE ID = v_from; END IF;
    END IF;
    UPDATE DINING_TABLES SET STATUS = 'occupied' WHERE ID = p_to_table_id;

    LOG_TRANSFER(v_tenant, p_order_id, 'transfer', v_from, p_to_table_id, NULL, p_user_id);
  END TRANSFER_ORDER;

  PROCEDURE MERGE_ORDERS(p_source_order_id IN VARCHAR2, p_target_order_id IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_from VARCHAR2(36);
    v_tenant VARCHAR2(36);
    v_cnt NUMBER;
  BEGIN
    ASSERT_OPEN(p_source_order_id);
    ASSERT_OPEN(p_target_order_id);
    SELECT TABLE_ID, TENANT_ID INTO v_from, v_tenant FROM ORDERS WHERE ID = p_source_order_id;

    UPDATE ORDER_ITEMS SET ORDER_ID = p_target_order_id WHERE ORDER_ID = p_source_order_id;
    UPDATE ORDER_DISCOUNTS SET ORDER_ID = p_target_order_id WHERE ORDER_ID = p_source_order_id;

    UPDATE ORDERS SET STATUS = 'cancelled', NOTE = 'Merged into ' || p_target_order_id,
           CLOSED_BY = p_user_id, CLOSED_AT = SYSTIMESTAMP, UPDATED_AT = SYSTIMESTAMP
     WHERE ID = p_source_order_id;

    IF v_from IS NOT NULL THEN
      SELECT COUNT(*) INTO v_cnt FROM ORDERS WHERE TABLE_ID = v_from AND STATUS = 'open';
      IF v_cnt = 0 THEN UPDATE DINING_TABLES SET STATUS = 'idle' WHERE ID = v_from; END IF;
    END IF;

    RECALC_ORDER(p_target_order_id);
    LOG_TRANSFER(v_tenant, p_target_order_id, 'merge', v_from, NULL, p_source_order_id, p_user_id);
  END MERGE_ORDERS;

  PROCEDURE CLOSE_ORDER(p_order_id IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_table VARCHAR2(36);
    v_cnt NUMBER;
  BEGIN
    ASSERT_OPEN(p_order_id);
    RECALC_ORDER(p_order_id);
    SELECT TABLE_ID INTO v_table FROM ORDERS WHERE ID = p_order_id;
    UPDATE ORDERS SET STATUS = 'closed', CLOSED_BY = p_user_id, CLOSED_AT = SYSTIMESTAMP, UPDATED_AT = SYSTIMESTAMP
     WHERE ID = p_order_id;

    IF v_table IS NOT NULL THEN
      SELECT COUNT(*) INTO v_cnt FROM ORDERS WHERE TABLE_ID = v_table AND STATUS = 'open';
      IF v_cnt = 0 THEN UPDATE DINING_TABLES SET STATUS = 'idle' WHERE ID = v_table; END IF;
    END IF;
  END CLOSE_ORDER;

  PROCEDURE CANCEL_ORDER(p_order_id IN VARCHAR2, p_reason IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_table VARCHAR2(36);
    v_cnt NUMBER;
  BEGIN
    ASSERT_OPEN(p_order_id);
    SELECT TABLE_ID INTO v_table FROM ORDERS WHERE ID = p_order_id;
    UPDATE ORDERS SET STATUS = 'cancelled', NOTE = p_reason, CLOSED_BY = p_user_id,
           CLOSED_AT = SYSTIMESTAMP, UPDATED_AT = SYSTIMESTAMP WHERE ID = p_order_id;

    IF v_table IS NOT NULL THEN
      SELECT COUNT(*) INTO v_cnt FROM ORDERS WHERE TABLE_ID = v_table AND STATUS = 'open';
      IF v_cnt = 0 THEN UPDATE DINING_TABLES SET STATUS = 'idle' WHERE ID = v_table; END IF;
    END IF;
  END CANCEL_ORDER;

  PROCEDURE LOG_TRANSFER(
    p_tenant_id IN VARCHAR2, p_order_id IN VARCHAR2, p_action IN VARCHAR2,
    p_from_table IN VARCHAR2, p_to_table IN VARCHAR2, p_related_order IN VARCHAR2, p_user_id IN VARCHAR2)
  IS
  BEGIN
    INSERT INTO ORDER_TRANSFERS (ID, TENANT_ID, ORDER_ID, ACTION, FROM_TABLE_ID, TO_TABLE_ID, RELATED_ORDER_ID, USER_ID)
    VALUES (NEW_ID, p_tenant_id, p_order_id, p_action, p_from_table, p_to_table, p_related_order, p_user_id);
  END LOG_TRANSFER;

END PKG_ORDERING;
/
