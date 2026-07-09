
CREATE OR REPLACE PACKAGE PKG_PAYMENT AS
  PROCEDURE PROCESS_PAYMENT(
    p_order_id IN VARCHAR2, p_method IN VARCHAR2, p_amount IN NUMBER, p_tip IN NUMBER,
    p_reference IN VARCHAR2, p_user_id IN VARCHAR2,
    p_payment_id OUT VARCHAR2, p_closed OUT NUMBER, p_change OUT NUMBER, p_balance OUT NUMBER);

  PROCEDURE VOID_PAYMENT(p_payment_id IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE REFUND(
    p_order_id IN VARCHAR2, p_payment_id IN VARCHAR2, p_amount IN NUMBER,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_refund_id OUT VARCHAR2);
END PKG_PAYMENT;
/

CREATE OR REPLACE PACKAGE BODY PKG_PAYMENT AS

  PROCEDURE PROCESS_PAYMENT(
    p_order_id IN VARCHAR2, p_method IN VARCHAR2, p_amount IN NUMBER, p_tip IN NUMBER,
    p_reference IN VARCHAR2, p_user_id IN VARCHAR2,
    p_payment_id OUT VARCHAR2, p_closed OUT NUMBER, p_change OUT NUMBER, p_balance OUT NUMBER)
  IS
    v_status  VARCHAR2(16);
    v_total   NUMBER(18,4);
    v_subtotal NUMBER(18,4);
    v_tax     NUMBER(18,4);
    v_tenant  VARCHAR2(36);
    v_branch  VARCHAR2(36);
    v_paid    NUMBER(18,4);
  BEGIN
    IF p_amount < 0 THEN RAISE_APPLICATION_ERROR(-20102, 'Amount cannot be negative.'); END IF;

    BEGIN
      SELECT STATUS, TOTAL, SUBTOTAL, TAX_TOTAL, TENANT_ID, BRANCH_ID
        INTO v_status, v_total, v_subtotal, v_tax, v_tenant, v_branch
        FROM ORDERS WHERE ID = p_order_id;
    EXCEPTION WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20101, 'Order not found.');
    END;

    IF v_status <> 'open' THEN RAISE_APPLICATION_ERROR(-20105, 'Order is not open.'); END IF;

    p_payment_id := PKG_ORDERING.NEW_ID;
    INSERT INTO PAYMENTS (ID, TENANT_ID, BRANCH_ID, ORDER_ID, METHOD, AMOUNT, TIP_AMOUNT, REFERENCE, CREATED_BY)
    VALUES (p_payment_id, v_tenant, v_branch, p_order_id, p_method, p_amount, NVL(p_tip,0), p_reference, p_user_id);

    IF p_method = 'cash' AND p_amount > 0 THEN
      INSERT INTO CASH_MOVEMENTS (ID, TENANT_ID, BRANCH_ID, MOVE_TYPE, AMOUNT, ORDER_ID, PAYMENT_ID, CREATED_BY)
      VALUES (PKG_ORDERING.NEW_ID, v_tenant, v_branch, 'sale', p_amount, p_order_id, p_payment_id, p_user_id);
    END IF;

    SELECT NVL(SUM(AMOUNT),0) INTO v_paid FROM PAYMENTS WHERE ORDER_ID = p_order_id AND IS_VOIDED = 0;

    IF v_paid >= v_total THEN
      p_change  := v_paid - v_total;
      p_balance := 0;
      p_closed  := 1;

      INSERT INTO INVOICES (ID, TENANT_ID, BRANCH_ID, ORDER_ID, INVOICE_NO, INVOICE_TYPE, SUBTOTAL, TAX_TOTAL, TOTAL)
      VALUES (PKG_ORDERING.NEW_ID, v_tenant, v_branch, p_order_id, INVOICE_NO_SEQ.NEXTVAL, 'receipt', v_subtotal, v_tax, v_total);

      PKG_ORDERING.CLOSE_ORDER(p_order_id, p_user_id);
    ELSE
      p_change  := 0;
      p_balance := v_total - v_paid;
      p_closed  := 0;
    END IF;
  END PROCESS_PAYMENT;

  PROCEDURE VOID_PAYMENT(p_payment_id IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_method VARCHAR2(20);
    v_amount NUMBER(18,4);
    v_order  VARCHAR2(36);
    v_tenant VARCHAR2(36);
    v_branch VARCHAR2(36);
    v_status VARCHAR2(16);
    v_rows   NUMBER;
  BEGIN
    BEGIN
      SELECT p.METHOD, p.AMOUNT, p.ORDER_ID, p.TENANT_ID, p.BRANCH_ID, o.STATUS
        INTO v_method, v_amount, v_order, v_tenant, v_branch, v_status
        FROM PAYMENTS p JOIN ORDERS o ON o.ID = p.ORDER_ID
       WHERE p.ID = p_payment_id;
    EXCEPTION WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20103, 'Payment not found.');
    END;

    IF v_status <> 'open' THEN RAISE_APPLICATION_ERROR(-20106, 'Cannot void payment on a closed order.'); END IF;

    UPDATE PAYMENTS SET IS_VOIDED = 1 WHERE ID = p_payment_id AND IS_VOIDED = 0;
    v_rows := SQL%ROWCOUNT;
    IF v_rows = 0 THEN RAISE_APPLICATION_ERROR(-20104, 'Payment already voided.'); END IF;

    IF v_method = 'cash' AND v_amount > 0 THEN
      INSERT INTO CASH_MOVEMENTS (ID, TENANT_ID, BRANCH_ID, MOVE_TYPE, AMOUNT, ORDER_ID, PAYMENT_ID, CREATED_BY)
      VALUES (PKG_ORDERING.NEW_ID, v_tenant, v_branch, 'void', -v_amount, v_order, p_payment_id, p_user_id);
    END IF;
  END VOID_PAYMENT;

  PROCEDURE REFUND(
    p_order_id IN VARCHAR2, p_payment_id IN VARCHAR2, p_amount IN NUMBER,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_refund_id OUT VARCHAR2)
  IS
    v_tenant VARCHAR2(36);
    v_branch VARCHAR2(36);
  BEGIN
    IF p_amount <= 0 THEN RAISE_APPLICATION_ERROR(-20107, 'Refund amount must be positive.'); END IF;
    BEGIN
      SELECT TENANT_ID, BRANCH_ID INTO v_tenant, v_branch FROM ORDERS WHERE ID = p_order_id;
    EXCEPTION WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20101, 'Order not found.');
    END;

    p_refund_id := PKG_ORDERING.NEW_ID;
    INSERT INTO REFUNDS (ID, TENANT_ID, ORDER_ID, PAYMENT_ID, AMOUNT, REASON, CREATED_BY)
    VALUES (p_refund_id, v_tenant, p_order_id, p_payment_id, p_amount, p_reason, p_user_id);

    INSERT INTO CASH_MOVEMENTS (ID, TENANT_ID, BRANCH_ID, MOVE_TYPE, AMOUNT, ORDER_ID, PAYMENT_ID, NOTE, CREATED_BY)
    VALUES (PKG_ORDERING.NEW_ID, v_tenant, v_branch, 'refund', -p_amount, p_order_id, p_payment_id, p_reason, p_user_id);
  END REFUND;

END PKG_PAYMENT;
/
