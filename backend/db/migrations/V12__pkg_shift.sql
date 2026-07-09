
CREATE OR REPLACE PACKAGE PKG_SHIFT AS
  PROCEDURE OPEN_SESSION(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_register_id IN VARCHAR2,
    p_opening_amount IN NUMBER, p_user_id IN VARCHAR2, p_session_id OUT VARCHAR2);

  PROCEDURE PAY_IN(p_session_id IN VARCHAR2, p_amount IN NUMBER, p_note IN VARCHAR2, p_user_id IN VARCHAR2);
  PROCEDURE PAY_OUT(p_session_id IN VARCHAR2, p_amount IN NUMBER, p_note IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE CLOSE_SESSION(
    p_session_id IN VARCHAR2, p_counted IN NUMBER, p_user_id IN VARCHAR2,
    p_expected OUT NUMBER, p_difference OUT NUMBER);
END PKG_SHIFT;
/

CREATE OR REPLACE PACKAGE BODY PKG_SHIFT AS

  PROCEDURE OPEN_SESSION(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_register_id IN VARCHAR2,
    p_opening_amount IN NUMBER, p_user_id IN VARCHAR2, p_session_id OUT VARCHAR2)
  IS
    v_cnt NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_cnt FROM REGISTER_SESSIONS WHERE REGISTER_ID = p_register_id AND STATUS = 'open';
    IF v_cnt > 0 THEN RAISE_APPLICATION_ERROR(-20301, 'Register already has an open session.'); END IF;

    p_session_id := PKG_ORDERING.NEW_ID;
    INSERT INTO REGISTER_SESSIONS (ID, TENANT_ID, BRANCH_ID, REGISTER_ID, STATUS, OPENING_AMOUNT, OPENED_BY)
    VALUES (p_session_id, p_tenant_id, p_branch_id, p_register_id, 'open', NVL(p_opening_amount,0), p_user_id);

    INSERT INTO CASH_MOVEMENTS (ID, TENANT_ID, BRANCH_ID, SESSION_ID, MOVE_TYPE, AMOUNT, NOTE, CREATED_BY)
    VALUES (PKG_ORDERING.NEW_ID, p_tenant_id, p_branch_id, p_session_id, 'opening', NVL(p_opening_amount,0), 'Opening float', p_user_id);
  END OPEN_SESSION;

  PROCEDURE ADD_SESSION_MOVEMENT(p_session_id IN VARCHAR2, p_type IN VARCHAR2, p_amount IN NUMBER, p_note IN VARCHAR2, p_user_id IN VARCHAR2) IS
    v_tenant VARCHAR2(36);
    v_branch VARCHAR2(36);
    v_status VARCHAR2(16);
  BEGIN
    BEGIN
      SELECT TENANT_ID, BRANCH_ID, STATUS INTO v_tenant, v_branch, v_status FROM REGISTER_SESSIONS WHERE ID = p_session_id;
    EXCEPTION WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20303, 'Session not found.');
    END;
    IF v_status <> 'open' THEN RAISE_APPLICATION_ERROR(-20302, 'Session is not open.'); END IF;
    IF p_amount <= 0 THEN RAISE_APPLICATION_ERROR(-20304, 'Amount must be positive.'); END IF;

    INSERT INTO CASH_MOVEMENTS (ID, TENANT_ID, BRANCH_ID, SESSION_ID, MOVE_TYPE, AMOUNT, NOTE, CREATED_BY)
    VALUES (PKG_ORDERING.NEW_ID, v_tenant, v_branch, p_session_id, p_type,
            CASE WHEN p_type = 'payout' THEN -p_amount ELSE p_amount END, p_note, p_user_id);
  END ADD_SESSION_MOVEMENT;

  PROCEDURE PAY_IN(p_session_id IN VARCHAR2, p_amount IN NUMBER, p_note IN VARCHAR2, p_user_id IN VARCHAR2) IS
  BEGIN
    ADD_SESSION_MOVEMENT(p_session_id, 'payin', p_amount, p_note, p_user_id);
  END PAY_IN;

  PROCEDURE PAY_OUT(p_session_id IN VARCHAR2, p_amount IN NUMBER, p_note IN VARCHAR2, p_user_id IN VARCHAR2) IS
  BEGIN
    ADD_SESSION_MOVEMENT(p_session_id, 'payout', p_amount, p_note, p_user_id);
  END PAY_OUT;

  PROCEDURE CLOSE_SESSION(
    p_session_id IN VARCHAR2, p_counted IN NUMBER, p_user_id IN VARCHAR2,
    p_expected OUT NUMBER, p_difference OUT NUMBER)
  IS
    v_tenant  VARCHAR2(36);
    v_branch  VARCHAR2(36);
    v_status  VARCHAR2(16);
    v_opening NUMBER(18,4);
    v_opened  TIMESTAMP WITH TIME ZONE;
    v_flow    NUMBER(18,4);
  BEGIN
    BEGIN
      SELECT TENANT_ID, BRANCH_ID, STATUS, OPENING_AMOUNT, OPENED_AT
        INTO v_tenant, v_branch, v_status, v_opening, v_opened
        FROM REGISTER_SESSIONS WHERE ID = p_session_id;
    EXCEPTION WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20303, 'Session not found.');
    END;
    IF v_status <> 'open' THEN RAISE_APPLICATION_ERROR(-20302, 'Session is not open.'); END IF;

    SELECT NVL(SUM(AMOUNT),0) INTO v_flow
      FROM CASH_MOVEMENTS
     WHERE BRANCH_ID = v_branch
       AND CREATED_AT >= v_opened
       AND MOVE_TYPE IN ('sale','refund','payin','payout','void');

    p_expected   := v_opening + v_flow;
    p_difference := p_counted - p_expected;

    UPDATE REGISTER_SESSIONS
       SET STATUS = 'closed', CLOSING_COUNTED = p_counted, CLOSING_EXPECTED = p_expected,
           DIFFERENCE = p_difference, CLOSED_BY = p_user_id, CLOSED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE ID = p_session_id;

    INSERT INTO CASH_MOVEMENTS (ID, TENANT_ID, BRANCH_ID, SESSION_ID, MOVE_TYPE, AMOUNT, NOTE, CREATED_BY)
    VALUES (PKG_ORDERING.NEW_ID, v_tenant, v_branch, p_session_id, 'closing', -p_counted, 'Drawer closed', p_user_id);
  END CLOSE_SESSION;

END PKG_SHIFT;
/
