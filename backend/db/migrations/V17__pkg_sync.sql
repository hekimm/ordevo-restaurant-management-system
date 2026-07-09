
CREATE OR REPLACE PACKAGE PKG_SYNC AS
  FUNCTION NEW_ID RETURN VARCHAR2;

  PROCEDURE REGISTER_DEVICE(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_name IN VARCHAR2,
    p_device_type IN VARCHAR2, p_fingerprint IN VARCHAR2, p_auto_approve IN NUMBER,
    p_user_id IN VARCHAR2, p_device_id OUT VARCHAR2, p_is_approved OUT NUMBER);

  PROCEDURE HEARTBEAT(
    p_tenant_id IN VARCHAR2, p_device_id IN VARCHAR2, p_branch_id IN VARCHAR2,
    p_local_store_id IN VARCHAR2, p_app_version IN VARCHAR2);

  PROCEDURE ACK_PULL(
    p_tenant_id IN VARCHAR2, p_device_id IN VARCHAR2, p_branch_id IN VARCHAR2,
    p_last_pull_version IN NUMBER);

  PROCEDURE APPEND_CHANGE(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_entity_name IN VARCHAR2,
    p_entity_id IN VARCHAR2, p_operation IN VARCHAR2, p_row_version IN NUMBER,
    p_payload IN CLOB, p_origin_device_id IN VARCHAR2, p_origin_user_id IN VARCHAR2,
    p_change_version OUT NUMBER);

  PROCEDURE STAGE_MUTATION(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_device_id IN VARCHAR2,
    p_client_mutation_id IN VARCHAR2, p_entity_name IN VARCHAR2, p_entity_id IN VARCHAR2,
    p_operation IN VARCHAR2, p_base_change_version IN NUMBER, p_expected_row_version IN NUMBER,
    p_payload IN CLOB, p_user_id IN VARCHAR2, p_mutation_id OUT VARCHAR2,
    p_status OUT VARCHAR2);

  PROCEDURE MARK_MUTATION_APPLIED(
    p_tenant_id IN VARCHAR2, p_mutation_id IN VARCHAR2, p_change_version IN NUMBER);

  PROCEDURE MARK_MUTATION_CONFLICT(
    p_tenant_id IN VARCHAR2, p_mutation_id IN VARCHAR2, p_server_change_version IN NUMBER,
    p_server_payload IN CLOB, p_error_code IN VARCHAR2, p_error_message IN VARCHAR2);
END PKG_SYNC;
/

CREATE OR REPLACE PACKAGE BODY PKG_SYNC AS

  FUNCTION NEW_ID RETURN VARCHAR2 IS
    g VARCHAR2(32) := RAWTOHEX(SYS_GUID());
  BEGIN
    RETURN LOWER(SUBSTR(g,1,8)||'-'||SUBSTR(g,9,4)||'-'||SUBSTR(g,13,4)||'-'||SUBSTR(g,17,4)||'-'||SUBSTR(g,21,12));
  END NEW_ID;

  PROCEDURE ASSERT_DEVICE(p_tenant_id IN VARCHAR2, p_device_id IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count
      FROM DEVICES
     WHERE TENANT_ID = p_tenant_id AND ID = p_device_id AND IS_APPROVED = 1;

    IF v_count = 0 THEN
      RAISE_APPLICATION_ERROR(-20401, 'Device is not registered or approved.');
    END IF;
  END ASSERT_DEVICE;

  PROCEDURE ASSERT_ENTITY_PUSH(p_entity_name IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count
      FROM SYNC_ENTITY_CONFIG
     WHERE ENTITY_NAME = p_entity_name
       AND IS_ACTIVE = 1
       AND ALLOW_CLIENT_PUSH = 1;

    IF v_count = 0 THEN
      RAISE_APPLICATION_ERROR(-20402, 'Entity is not push-enabled.');
    END IF;
  END ASSERT_ENTITY_PUSH;

  PROCEDURE REGISTER_DEVICE(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_name IN VARCHAR2,
    p_device_type IN VARCHAR2, p_fingerprint IN VARCHAR2, p_auto_approve IN NUMBER,
    p_user_id IN VARCHAR2, p_device_id OUT VARCHAR2, p_is_approved OUT NUMBER)
  IS
  BEGIN
    BEGIN
      SELECT ID, IS_APPROVED INTO p_device_id, p_is_approved
        FROM DEVICES
       WHERE TENANT_ID = p_tenant_id AND FINGERPRINT = p_fingerprint
       FOR UPDATE;

      UPDATE DEVICES
         SET NAME = p_name,
             BRANCH_ID = p_branch_id,
             DEVICE_TYPE = NVL(p_device_type, DEVICE_TYPE),
             IS_APPROVED = CASE WHEN NVL(p_auto_approve,0) = 1 THEN 1 ELSE IS_APPROVED END,
             LAST_SEEN_AT = SYSTIMESTAMP,
             UPDATED_BY = p_user_id,
             UPDATED_AT = SYSTIMESTAMP,
             ROW_VERSION = ROW_VERSION + 1
       WHERE ID = p_device_id;

      SELECT IS_APPROVED INTO p_is_approved FROM DEVICES WHERE ID = p_device_id;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        p_device_id := NEW_ID();
        p_is_approved := CASE WHEN NVL(p_auto_approve,0) = 1 THEN 1 ELSE 0 END;
        INSERT INTO DEVICES (
          ID, TENANT_ID, BRANCH_ID, NAME, DEVICE_TYPE, FINGERPRINT,
          IS_APPROVED, LAST_SEEN_AT, CREATED_BY, UPDATED_BY)
        VALUES (
          p_device_id, p_tenant_id, p_branch_id, p_name, NVL(p_device_type,'pos'),
          p_fingerprint, p_is_approved, SYSTIMESTAMP, p_user_id, p_user_id);
    END;
  END REGISTER_DEVICE;

  PROCEDURE HEARTBEAT(
    p_tenant_id IN VARCHAR2, p_device_id IN VARCHAR2, p_branch_id IN VARCHAR2,
    p_local_store_id IN VARCHAR2, p_app_version IN VARCHAR2)
  IS
  BEGIN
    ASSERT_DEVICE(p_tenant_id, p_device_id);

    UPDATE DEVICES
       SET LAST_SEEN_AT = SYSTIMESTAMP,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_device_id;

    MERGE INTO SYNC_DEVICE_CHECKPOINTS target
    USING (
      SELECT p_tenant_id AS TENANT_ID, p_device_id AS DEVICE_ID FROM DUAL
    ) src
    ON (target.TENANT_ID = src.TENANT_ID AND target.DEVICE_ID = src.DEVICE_ID)
    WHEN MATCHED THEN UPDATE SET
      target.BRANCH_ID = p_branch_id,
      target.APP_VERSION = p_app_version,
      target.LOCAL_STORE_ID = p_local_store_id,
      target.UPDATED_AT = SYSTIMESTAMP
    WHEN NOT MATCHED THEN INSERT (
      TENANT_ID, DEVICE_ID, BRANCH_ID, APP_VERSION, LOCAL_STORE_ID)
    VALUES (
      p_tenant_id, p_device_id, p_branch_id, p_app_version, p_local_store_id);
  END HEARTBEAT;

  PROCEDURE ACK_PULL(
    p_tenant_id IN VARCHAR2, p_device_id IN VARCHAR2, p_branch_id IN VARCHAR2,
    p_last_pull_version IN NUMBER)
  IS
  BEGIN
    ASSERT_DEVICE(p_tenant_id, p_device_id);

    MERGE INTO SYNC_DEVICE_CHECKPOINTS target
    USING (
      SELECT p_tenant_id AS TENANT_ID, p_device_id AS DEVICE_ID FROM DUAL
    ) src
    ON (target.TENANT_ID = src.TENANT_ID AND target.DEVICE_ID = src.DEVICE_ID)
    WHEN MATCHED THEN UPDATE SET
      target.BRANCH_ID = p_branch_id,
      target.LAST_PULL_VERSION = GREATEST(target.LAST_PULL_VERSION, NVL(p_last_pull_version,0)),
      target.LAST_PULL_AT = SYSTIMESTAMP,
      target.UPDATED_AT = SYSTIMESTAMP
    WHEN NOT MATCHED THEN INSERT (
      TENANT_ID, DEVICE_ID, BRANCH_ID, LAST_PULL_VERSION, LAST_PULL_AT)
    VALUES (
      p_tenant_id, p_device_id, p_branch_id, NVL(p_last_pull_version,0), SYSTIMESTAMP);
  END ACK_PULL;

  PROCEDURE APPEND_CHANGE(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_entity_name IN VARCHAR2,
    p_entity_id IN VARCHAR2, p_operation IN VARCHAR2, p_row_version IN NUMBER,
    p_payload IN CLOB, p_origin_device_id IN VARCHAR2, p_origin_user_id IN VARCHAR2,
    p_change_version OUT NUMBER)
  IS
    v_count NUMBER;
    v_id VARCHAR2(36);
  BEGIN
    SELECT COUNT(*) INTO v_count
      FROM SYNC_ENTITY_CONFIG
     WHERE ENTITY_NAME = p_entity_name AND IS_ACTIVE = 1;

    IF v_count = 0 THEN
      RAISE_APPLICATION_ERROR(-20403, 'Unknown sync entity.');
    END IF;

    IF p_operation NOT IN ('upsert','delete','snapshot','custom') THEN
      RAISE_APPLICATION_ERROR(-20404, 'Invalid sync operation.');
    END IF;

    SELECT SYNC_CHANGE_SEQ.NEXTVAL INTO p_change_version FROM DUAL;
    v_id := NEW_ID();

    INSERT INTO SYNC_OUTBOX (
      CHANGE_VERSION, ID, TENANT_ID, BRANCH_ID, ENTITY_NAME, ENTITY_ID,
      OPERATION, ROW_VERSION, PAYLOAD, ORIGIN_DEVICE_ID, ORIGIN_USER_ID,
      RETENTION_UNTIL)
    VALUES (
      p_change_version, v_id, p_tenant_id, p_branch_id, p_entity_name, p_entity_id,
      p_operation, p_row_version, p_payload, p_origin_device_id, p_origin_user_id,
      SYSTIMESTAMP + INTERVAL '30' DAY);
  END APPEND_CHANGE;

  PROCEDURE STAGE_MUTATION(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_device_id IN VARCHAR2,
    p_client_mutation_id IN VARCHAR2, p_entity_name IN VARCHAR2, p_entity_id IN VARCHAR2,
    p_operation IN VARCHAR2, p_base_change_version IN NUMBER, p_expected_row_version IN NUMBER,
    p_payload IN CLOB, p_user_id IN VARCHAR2, p_mutation_id OUT VARCHAR2,
    p_status OUT VARCHAR2)
  IS
  BEGIN
    ASSERT_DEVICE(p_tenant_id, p_device_id);
    ASSERT_ENTITY_PUSH(p_entity_name);

    IF p_operation NOT IN ('upsert','delete','custom') THEN
      RAISE_APPLICATION_ERROR(-20404, 'Invalid mutation operation.');
    END IF;

    BEGIN
      SELECT ID, STATUS INTO p_mutation_id, p_status
        FROM SYNC_CLIENT_MUTATIONS
       WHERE TENANT_ID = p_tenant_id
         AND DEVICE_ID = p_device_id
         AND CLIENT_MUTATION_ID = p_client_mutation_id;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        p_mutation_id := NEW_ID();
        p_status := 'pending';
        INSERT INTO SYNC_CLIENT_MUTATIONS (
          ID, TENANT_ID, BRANCH_ID, DEVICE_ID, CLIENT_MUTATION_ID, ENTITY_NAME,
          ENTITY_ID, OPERATION, BASE_CHANGE_VERSION, EXPECTED_ROW_VERSION,
          PAYLOAD, STATUS, CREATED_BY)
        VALUES (
          p_mutation_id, p_tenant_id, p_branch_id, p_device_id, p_client_mutation_id,
          p_entity_name, p_entity_id, p_operation, p_base_change_version,
          p_expected_row_version, p_payload, p_status, p_user_id);
    END;
  END STAGE_MUTATION;

  PROCEDURE MARK_MUTATION_APPLIED(
    p_tenant_id IN VARCHAR2, p_mutation_id IN VARCHAR2, p_change_version IN NUMBER)
  IS
  BEGIN
    UPDATE SYNC_CLIENT_MUTATIONS
       SET STATUS = 'applied',
           ERROR_CODE = NULL,
           ERROR_MESSAGE = NULL,
           APPLIED_AT = SYSTIMESTAMP
     WHERE TENANT_ID = p_tenant_id AND ID = p_mutation_id;

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20405, 'Mutation not found.');
    END IF;
  END MARK_MUTATION_APPLIED;

  PROCEDURE MARK_MUTATION_CONFLICT(
    p_tenant_id IN VARCHAR2, p_mutation_id IN VARCHAR2, p_server_change_version IN NUMBER,
    p_server_payload IN CLOB, p_error_code IN VARCHAR2, p_error_message IN VARCHAR2)
  IS
    v_branch_id VARCHAR2(36);
    v_device_id VARCHAR2(36);
    v_entity_name VARCHAR2(80);
    v_entity_id VARCHAR2(100);
    v_client_payload CLOB;
  BEGIN
    SELECT BRANCH_ID, DEVICE_ID, ENTITY_NAME, ENTITY_ID, PAYLOAD
      INTO v_branch_id, v_device_id, v_entity_name, v_entity_id, v_client_payload
      FROM SYNC_CLIENT_MUTATIONS
     WHERE TENANT_ID = p_tenant_id AND ID = p_mutation_id
     FOR UPDATE;

    UPDATE SYNC_CLIENT_MUTATIONS
       SET STATUS = 'conflict',
           ERROR_CODE = p_error_code,
           ERROR_MESSAGE = p_error_message
     WHERE TENANT_ID = p_tenant_id AND ID = p_mutation_id;

    INSERT INTO SYNC_CONFLICTS (
      ID, TENANT_ID, BRANCH_ID, DEVICE_ID, MUTATION_ID, ENTITY_NAME, ENTITY_ID,
      SERVER_CHANGE_VERSION, CLIENT_PAYLOAD, SERVER_PAYLOAD)
    VALUES (
      NEW_ID(), p_tenant_id, v_branch_id, v_device_id, p_mutation_id,
      v_entity_name, v_entity_id, p_server_change_version, v_client_payload, p_server_payload);
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20405, 'Mutation not found.');
  END MARK_MUTATION_CONFLICT;
END PKG_SYNC;
/
