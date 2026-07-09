
CREATE OR REPLACE PACKAGE PKG_INTEGRATION AS
  FUNCTION NEW_ID RETURN VARCHAR2;

  PROCEDURE CREATE_CONNECTOR(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_code IN VARCHAR2,
    p_name IN VARCHAR2, p_connector_type IN VARCHAR2, p_provider_code IN VARCHAR2,
    p_base_url IN VARCHAR2, p_auth_type IN VARCHAR2, p_secret_ref IN VARCHAR2,
    p_settings IN CLOB, p_user_id IN VARCHAR2, p_connector_id OUT VARCHAR2);

  PROCEDURE SET_CONNECTOR_STATUS(
    p_tenant_id IN VARCHAR2, p_connector_id IN VARCHAR2, p_status IN VARCHAR2,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE CREATE_WEBHOOK_SUBSCRIPTION(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_connector_id IN VARCHAR2,
    p_name IN VARCHAR2, p_target_url IN VARCHAR2, p_secret_ref IN VARCHAR2,
    p_event_pattern IN VARCHAR2, p_event_filter IN CLOB, p_headers IN CLOB,
    p_max_attempts IN NUMBER, p_timeout_seconds IN NUMBER, p_user_id IN VARCHAR2,
    p_subscription_id OUT VARCHAR2);

  PROCEDURE SET_WEBHOOK_STATUS(
    p_tenant_id IN VARCHAR2, p_subscription_id IN VARCHAR2, p_status IN VARCHAR2,
    p_user_id IN VARCHAR2);

  PROCEDURE QUEUE_EVENT(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_source_module IN VARCHAR2,
    p_event_type IN VARCHAR2, p_aggregate_type IN VARCHAR2, p_aggregate_id IN VARCHAR2,
    p_payload IN CLOB, p_correlation_id IN VARCHAR2, p_user_id IN VARCHAR2,
    p_event_id OUT VARCHAR2, p_delivery_count OUT NUMBER);

  PROCEDURE MARK_DELIVERY_SUCCESS(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_status_code IN NUMBER,
    p_request_headers IN CLOB, p_response_body IN CLOB, p_latency_ms IN NUMBER);

  PROCEDURE MARK_DELIVERY_FAILURE(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_status_code IN NUMBER,
    p_request_headers IN CLOB, p_response_body IN CLOB, p_error_message IN VARCHAR2,
    p_latency_ms IN NUMBER, p_next_attempt_at IN TIMESTAMP WITH TIME ZONE);

  PROCEDURE REGISTER_TERMINAL(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_connector_id IN VARCHAR2,
    p_device_id IN VARCHAR2, p_name IN VARCHAR2, p_terminal_type IN VARCHAR2,
    p_provider_terminal_id IN VARCHAR2, p_connection_mode IN VARCHAR2,
    p_ip_address IN VARCHAR2, p_port IN NUMBER, p_serial_path IN VARCHAR2,
    p_settings IN CLOB, p_user_id IN VARCHAR2, p_terminal_id OUT VARCHAR2);

  PROCEDURE QUEUE_COMMAND(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_connector_id IN VARCHAR2,
    p_terminal_id IN VARCHAR2, p_order_id IN VARCHAR2, p_payment_id IN VARCHAR2,
    p_command_type IN VARCHAR2, p_idempotency_key IN VARCHAR2, p_payload IN CLOB,
    p_user_id IN VARCHAR2, p_command_id OUT VARCHAR2, p_status OUT VARCHAR2);

  PROCEDURE MARK_COMMAND_SENT(
    p_tenant_id IN VARCHAR2, p_command_id IN VARCHAR2, p_provider_reference IN VARCHAR2);

  PROCEDURE MARK_COMMAND_COMPLETED(
    p_tenant_id IN VARCHAR2, p_command_id IN VARCHAR2, p_provider_reference IN VARCHAR2,
    p_result_payload IN CLOB);

  PROCEDURE MARK_COMMAND_FAILED(
    p_tenant_id IN VARCHAR2, p_command_id IN VARCHAR2, p_error_code IN VARCHAR2,
    p_error_message IN VARCHAR2, p_result_payload IN CLOB);
END PKG_INTEGRATION;
/

CREATE OR REPLACE PACKAGE BODY PKG_INTEGRATION AS

  FUNCTION NEW_ID RETURN VARCHAR2 IS
    g VARCHAR2(32) := RAWTOHEX(SYS_GUID());
  BEGIN
    RETURN LOWER(SUBSTR(g,1,8)||'-'||SUBSTR(g,9,4)||'-'||SUBSTR(g,13,4)||'-'||SUBSTR(g,17,4)||'-'||SUBSTR(g,21,12));
  END NEW_ID;

  FUNCTION MATCHES_PATTERN(p_pattern IN VARCHAR2, p_event_type IN VARCHAR2) RETURN NUMBER IS
    v_pattern VARCHAR2(120) := NVL(TRIM(p_pattern), '*');
  BEGIN
    IF v_pattern = '*' THEN
      RETURN 1;
    END IF;

    IF SUBSTR(v_pattern, -1) = '*' THEN
      IF p_event_type LIKE REPLACE(v_pattern, '*', '%') THEN
        RETURN 1;
      END IF;
    ELSIF p_event_type = v_pattern THEN
      RETURN 1;
    END IF;

    RETURN 0;
  END MATCHES_PATTERN;

  PROCEDURE ASSERT_CONNECTOR(p_tenant_id IN VARCHAR2, p_connector_id IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    IF p_connector_id IS NULL THEN
      RETURN;
    END IF;

    SELECT COUNT(*) INTO v_count
      FROM INTEGRATION_CONNECTORS
     WHERE TENANT_ID = p_tenant_id AND ID = p_connector_id;

    IF v_count = 0 THEN
      RAISE_APPLICATION_ERROR(-20501, 'Connector not found.');
    END IF;
  END ASSERT_CONNECTOR;

  PROCEDURE UPDATE_EVENT_STATUS(p_tenant_id IN VARCHAR2, p_event_id IN VARCHAR2) IS
    v_pending NUMBER;
    v_failed NUMBER;
    v_total NUMBER;
    v_status VARCHAR2(20);
  BEGIN
    SELECT COUNT(*) INTO v_total
      FROM WEBHOOK_DELIVERIES
     WHERE TENANT_ID = p_tenant_id AND EVENT_ID = p_event_id;

    IF v_total = 0 THEN
      v_status := 'delivered';
    ELSE
      SELECT COUNT(*) INTO v_pending
        FROM WEBHOOK_DELIVERIES
       WHERE TENANT_ID = p_tenant_id AND EVENT_ID = p_event_id AND STATUS = 'pending';

      SELECT COUNT(*) INTO v_failed
        FROM WEBHOOK_DELIVERIES
       WHERE TENANT_ID = p_tenant_id AND EVENT_ID = p_event_id AND STATUS = 'failed';

      IF v_pending > 0 THEN
        v_status := 'pending';
      ELSIF v_failed > 0 THEN
        v_status := 'failed';
      ELSE
        v_status := 'delivered';
      END IF;
    END IF;

    UPDATE INTEGRATION_EVENTS
       SET STATUS = v_status,
           PROCESSED_AT = CASE WHEN v_status IN ('delivered','failed') THEN SYSTIMESTAMP ELSE PROCESSED_AT END,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_event_id;
  END UPDATE_EVENT_STATUS;

  PROCEDURE CREATE_CONNECTOR(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_code IN VARCHAR2,
    p_name IN VARCHAR2, p_connector_type IN VARCHAR2, p_provider_code IN VARCHAR2,
    p_base_url IN VARCHAR2, p_auth_type IN VARCHAR2, p_secret_ref IN VARCHAR2,
    p_settings IN CLOB, p_user_id IN VARCHAR2, p_connector_id OUT VARCHAR2)
  IS
  BEGIN
    p_connector_id := NEW_ID();

    INSERT INTO INTEGRATION_CONNECTORS (
      ID, TENANT_ID, BRANCH_ID, CODE, NAME, CONNECTOR_TYPE, PROVIDER_CODE,
      BASE_URL, AUTH_TYPE, SECRET_REF, SETTINGS, STATUS, IS_ACTIVE, CREATED_BY, UPDATED_BY)
    VALUES (
      p_connector_id, p_tenant_id, p_branch_id, LOWER(TRIM(p_code)), p_name,
      p_connector_type, LOWER(TRIM(p_provider_code)), p_base_url,
      NVL(p_auth_type, 'none'), p_secret_ref, p_settings, 'draft', 0, p_user_id, p_user_id);
  END CREATE_CONNECTOR;

  PROCEDURE SET_CONNECTOR_STATUS(
    p_tenant_id IN VARCHAR2, p_connector_id IN VARCHAR2, p_status IN VARCHAR2,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2)
  IS
  BEGIN
    IF p_status NOT IN ('draft','active','paused','error') THEN
      RAISE_APPLICATION_ERROR(-20506, 'Invalid connector status.');
    END IF;

    UPDATE INTEGRATION_CONNECTORS
       SET STATUS = p_status,
           IS_ACTIVE = CASE WHEN p_status = 'active' THEN 1 ELSE 0 END,
           FAILURE_REASON = CASE WHEN p_status = 'error' THEN p_reason ELSE NULL END,
           LAST_FAILURE_AT = CASE WHEN p_status = 'error' THEN SYSTIMESTAMP ELSE LAST_FAILURE_AT END,
           UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_connector_id;

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20501, 'Connector not found.');
    END IF;
  END SET_CONNECTOR_STATUS;

  PROCEDURE CREATE_WEBHOOK_SUBSCRIPTION(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_connector_id IN VARCHAR2,
    p_name IN VARCHAR2, p_target_url IN VARCHAR2, p_secret_ref IN VARCHAR2,
    p_event_pattern IN VARCHAR2, p_event_filter IN CLOB, p_headers IN CLOB,
    p_max_attempts IN NUMBER, p_timeout_seconds IN NUMBER, p_user_id IN VARCHAR2,
    p_subscription_id OUT VARCHAR2)
  IS
  BEGIN
    ASSERT_CONNECTOR(p_tenant_id, p_connector_id);
    p_subscription_id := NEW_ID();

    INSERT INTO WEBHOOK_SUBSCRIPTIONS (
      ID, TENANT_ID, BRANCH_ID, CONNECTOR_ID, NAME, TARGET_URL, SECRET_REF,
      EVENT_PATTERN, EVENT_FILTER, HEADERS, MAX_ATTEMPTS, TIMEOUT_SECONDS,
      CREATED_BY, UPDATED_BY)
    VALUES (
      p_subscription_id, p_tenant_id, p_branch_id, p_connector_id, p_name, p_target_url,
      p_secret_ref, NVL(TRIM(p_event_pattern), '*'), p_event_filter, p_headers,
      NVL(p_max_attempts, 5), NVL(p_timeout_seconds, 15), p_user_id, p_user_id);
  END CREATE_WEBHOOK_SUBSCRIPTION;

  PROCEDURE SET_WEBHOOK_STATUS(
    p_tenant_id IN VARCHAR2, p_subscription_id IN VARCHAR2, p_status IN VARCHAR2,
    p_user_id IN VARCHAR2)
  IS
  BEGIN
    IF p_status NOT IN ('active','paused','error') THEN
      RAISE_APPLICATION_ERROR(-20507, 'Invalid webhook status.');
    END IF;

    UPDATE WEBHOOK_SUBSCRIPTIONS
       SET STATUS = p_status,
           IS_ACTIVE = CASE WHEN p_status = 'active' THEN 1 ELSE 0 END,
           UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_subscription_id;

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20503, 'Webhook subscription not found.');
    END IF;
  END SET_WEBHOOK_STATUS;

  PROCEDURE QUEUE_EVENT(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_source_module IN VARCHAR2,
    p_event_type IN VARCHAR2, p_aggregate_type IN VARCHAR2, p_aggregate_id IN VARCHAR2,
    p_payload IN CLOB, p_correlation_id IN VARCHAR2, p_user_id IN VARCHAR2,
    p_event_id OUT VARCHAR2, p_delivery_count OUT NUMBER)
  IS
    v_delivery_id VARCHAR2(36);
  BEGIN
    p_event_id := NEW_ID();
    p_delivery_count := 0;

    INSERT INTO INTEGRATION_EVENTS (
      ID, TENANT_ID, BRANCH_ID, SOURCE_MODULE, EVENT_TYPE, AGGREGATE_TYPE,
      AGGREGATE_ID, PAYLOAD, CORRELATION_ID, CREATED_BY)
    VALUES (
      p_event_id, p_tenant_id, p_branch_id, p_source_module, p_event_type,
      p_aggregate_type, p_aggregate_id, p_payload, p_correlation_id, p_user_id);

    FOR s IN (
      SELECT ID, EVENT_PATTERN
        FROM WEBHOOK_SUBSCRIPTIONS
       WHERE TENANT_ID = p_tenant_id
         AND IS_ACTIVE = 1
         AND STATUS = 'active'
         AND (BRANCH_ID IS NULL OR BRANCH_ID = p_branch_id)
       ORDER BY CREATED_AT
    ) LOOP
      IF MATCHES_PATTERN(s.EVENT_PATTERN, p_event_type) = 1 THEN
        v_delivery_id := NEW_ID();
        INSERT INTO WEBHOOK_DELIVERIES (
          ID, TENANT_ID, EVENT_ID, SUBSCRIPTION_ID, ATTEMPT_NO, STATUS)
        VALUES (
          v_delivery_id, p_tenant_id, p_event_id, s.ID, 1, 'pending');
        p_delivery_count := p_delivery_count + 1;
      END IF;
    END LOOP;

    IF p_delivery_count = 0 THEN
      UPDATE INTEGRATION_EVENTS
         SET STATUS = 'delivered',
             PROCESSED_AT = SYSTIMESTAMP,
             ROW_VERSION = ROW_VERSION + 1
       WHERE TENANT_ID = p_tenant_id AND ID = p_event_id;
    END IF;
  END QUEUE_EVENT;

  PROCEDURE MARK_DELIVERY_SUCCESS(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_status_code IN NUMBER,
    p_request_headers IN CLOB, p_response_body IN CLOB, p_latency_ms IN NUMBER)
  IS
    v_event_id VARCHAR2(36);
    v_connector_id VARCHAR2(36);
  BEGIN
    SELECT d.EVENT_ID, s.CONNECTOR_ID INTO v_event_id, v_connector_id
      FROM WEBHOOK_DELIVERIES d
      JOIN WEBHOOK_SUBSCRIPTIONS s ON s.ID = d.SUBSCRIPTION_ID
     WHERE d.TENANT_ID = p_tenant_id AND d.ID = p_delivery_id
     FOR UPDATE;

    UPDATE WEBHOOK_DELIVERIES
       SET STATUS = 'success',
           STATUS_CODE = p_status_code,
           REQUEST_HEADERS = p_request_headers,
           RESPONSE_BODY = p_response_body,
           ERROR_MESSAGE = NULL,
           LATENCY_MS = p_latency_ms,
           SENT_AT = SYSTIMESTAMP
     WHERE TENANT_ID = p_tenant_id AND ID = p_delivery_id;

    UPDATE INTEGRATION_EVENTS
       SET ATTEMPTS = ATTEMPTS + 1,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = v_event_id;

    IF v_connector_id IS NOT NULL THEN
      UPDATE INTEGRATION_CONNECTORS
         SET LAST_SUCCESS_AT = SYSTIMESTAMP,
             FAILURE_REASON = NULL,
             UPDATED_AT = SYSTIMESTAMP,
             ROW_VERSION = ROW_VERSION + 1
       WHERE TENANT_ID = p_tenant_id AND ID = v_connector_id;
    END IF;

    UPDATE_EVENT_STATUS(p_tenant_id, v_event_id);
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20504, 'Webhook delivery not found.');
  END MARK_DELIVERY_SUCCESS;

  PROCEDURE MARK_DELIVERY_FAILURE(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_status_code IN NUMBER,
    p_request_headers IN CLOB, p_response_body IN CLOB, p_error_message IN VARCHAR2,
    p_latency_ms IN NUMBER, p_next_attempt_at IN TIMESTAMP WITH TIME ZONE)
  IS
    v_event_id VARCHAR2(36);
    v_subscription_id VARCHAR2(36);
    v_connector_id VARCHAR2(36);
    v_attempt_no NUMBER(3);
    v_max_attempts NUMBER(3);
    v_next_at TIMESTAMP WITH TIME ZONE;
  BEGIN
    SELECT d.EVENT_ID, d.SUBSCRIPTION_ID, d.ATTEMPT_NO, s.MAX_ATTEMPTS, s.CONNECTOR_ID
      INTO v_event_id, v_subscription_id, v_attempt_no, v_max_attempts, v_connector_id
      FROM WEBHOOK_DELIVERIES d
      JOIN WEBHOOK_SUBSCRIPTIONS s ON s.ID = d.SUBSCRIPTION_ID
     WHERE d.TENANT_ID = p_tenant_id AND d.ID = p_delivery_id
     FOR UPDATE;

    UPDATE WEBHOOK_DELIVERIES
       SET STATUS = 'failed',
           STATUS_CODE = p_status_code,
           REQUEST_HEADERS = p_request_headers,
           RESPONSE_BODY = p_response_body,
           ERROR_MESSAGE = p_error_message,
           LATENCY_MS = p_latency_ms,
           SENT_AT = SYSTIMESTAMP
     WHERE TENANT_ID = p_tenant_id AND ID = p_delivery_id;

    UPDATE INTEGRATION_EVENTS
       SET ATTEMPTS = ATTEMPTS + 1,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = v_event_id;

    IF v_connector_id IS NOT NULL THEN
      UPDATE INTEGRATION_CONNECTORS
         SET LAST_FAILURE_AT = SYSTIMESTAMP,
             FAILURE_REASON = p_error_message,
             UPDATED_AT = SYSTIMESTAMP,
             ROW_VERSION = ROW_VERSION + 1
       WHERE TENANT_ID = p_tenant_id AND ID = v_connector_id;
    END IF;

    IF v_attempt_no < v_max_attempts THEN
      v_next_at := NVL(
        p_next_attempt_at,
        SYSTIMESTAMP + NUMTODSINTERVAL(POWER(2, LEAST(v_attempt_no, 6)), 'MINUTE'));

      INSERT INTO WEBHOOK_DELIVERIES (
        ID, TENANT_ID, EVENT_ID, SUBSCRIPTION_ID, ATTEMPT_NO, STATUS, NEXT_ATTEMPT_AT)
      VALUES (
        NEW_ID(), p_tenant_id, v_event_id, v_subscription_id, v_attempt_no + 1,
        'pending', v_next_at);

      UPDATE INTEGRATION_EVENTS
         SET NEXT_ATTEMPT_AT = v_next_at,
             ROW_VERSION = ROW_VERSION + 1
       WHERE TENANT_ID = p_tenant_id AND ID = v_event_id;
    END IF;

    UPDATE_EVENT_STATUS(p_tenant_id, v_event_id);
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20504, 'Webhook delivery not found.');
  END MARK_DELIVERY_FAILURE;

  PROCEDURE REGISTER_TERMINAL(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_connector_id IN VARCHAR2,
    p_device_id IN VARCHAR2, p_name IN VARCHAR2, p_terminal_type IN VARCHAR2,
    p_provider_terminal_id IN VARCHAR2, p_connection_mode IN VARCHAR2,
    p_ip_address IN VARCHAR2, p_port IN NUMBER, p_serial_path IN VARCHAR2,
    p_settings IN CLOB, p_user_id IN VARCHAR2, p_terminal_id OUT VARCHAR2)
  IS
  BEGIN
    ASSERT_CONNECTOR(p_tenant_id, p_connector_id);

    IF p_provider_terminal_id IS NOT NULL THEN
      BEGIN
        SELECT ID INTO p_terminal_id
          FROM INTEGRATION_TERMINALS
         WHERE TENANT_ID = p_tenant_id
           AND BRANCH_ID = p_branch_id
           AND PROVIDER_TERMINAL_ID = p_provider_terminal_id
           AND ((CONNECTOR_ID IS NULL AND p_connector_id IS NULL) OR CONNECTOR_ID = p_connector_id)
           AND ROWNUM = 1
         FOR UPDATE;

        UPDATE INTEGRATION_TERMINALS
           SET DEVICE_ID = p_device_id,
               NAME = p_name,
               TERMINAL_TYPE = p_terminal_type,
               CONNECTION_MODE = NVL(p_connection_mode, CONNECTION_MODE),
               IP_ADDRESS = p_ip_address,
               PORT = p_port,
               SERIAL_PATH = p_serial_path,
               SETTINGS = p_settings,
               IS_ACTIVE = 1,
               LAST_SEEN_AT = SYSTIMESTAMP,
               UPDATED_BY = p_user_id,
               UPDATED_AT = SYSTIMESTAMP,
               ROW_VERSION = ROW_VERSION + 1
         WHERE ID = p_terminal_id;
        RETURN;
      EXCEPTION
        WHEN NO_DATA_FOUND THEN
          NULL;
      END;
    END IF;

    p_terminal_id := NEW_ID();
    INSERT INTO INTEGRATION_TERMINALS (
      ID, TENANT_ID, BRANCH_ID, CONNECTOR_ID, DEVICE_ID, NAME, TERMINAL_TYPE,
      PROVIDER_TERMINAL_ID, CONNECTION_MODE, IP_ADDRESS, PORT, SERIAL_PATH,
      SETTINGS, LAST_SEEN_AT, CREATED_BY, UPDATED_BY)
    VALUES (
      p_terminal_id, p_tenant_id, p_branch_id, p_connector_id, p_device_id, p_name,
      p_terminal_type, p_provider_terminal_id, NVL(p_connection_mode, 'cloud'),
      p_ip_address, p_port, p_serial_path, p_settings, SYSTIMESTAMP, p_user_id, p_user_id);
  END REGISTER_TERMINAL;

  PROCEDURE QUEUE_COMMAND(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_connector_id IN VARCHAR2,
    p_terminal_id IN VARCHAR2, p_order_id IN VARCHAR2, p_payment_id IN VARCHAR2,
    p_command_type IN VARCHAR2, p_idempotency_key IN VARCHAR2, p_payload IN CLOB,
    p_user_id IN VARCHAR2, p_command_id OUT VARCHAR2, p_status OUT VARCHAR2)
  IS
    v_connector_id VARCHAR2(36) := p_connector_id;
    v_terminal_connector_id VARCHAR2(36);
    v_terminal_active NUMBER(1);
  BEGIN
    IF p_idempotency_key IS NOT NULL THEN
      BEGIN
        SELECT ID, STATUS INTO p_command_id, p_status
          FROM INTEGRATION_COMMANDS
         WHERE TENANT_ID = p_tenant_id AND IDEMPOTENCY_KEY = p_idempotency_key;
        RETURN;
      EXCEPTION
        WHEN NO_DATA_FOUND THEN
          NULL;
      END;
    END IF;

    ASSERT_CONNECTOR(p_tenant_id, p_connector_id);

    IF p_terminal_id IS NOT NULL THEN
      BEGIN
        SELECT CONNECTOR_ID, IS_ACTIVE INTO v_terminal_connector_id, v_terminal_active
          FROM INTEGRATION_TERMINALS
         WHERE TENANT_ID = p_tenant_id
           AND BRANCH_ID = p_branch_id
           AND ID = p_terminal_id;
      EXCEPTION
        WHEN NO_DATA_FOUND THEN
          RAISE_APPLICATION_ERROR(-20511, 'Terminal not found.');
      END;

      IF v_terminal_active = 0 THEN
        RAISE_APPLICATION_ERROR(-20514, 'Terminal is inactive.');
      END IF;

      IF v_connector_id IS NULL THEN
        v_connector_id := v_terminal_connector_id;
      END IF;
    END IF;

    p_command_id := NEW_ID();
    p_status := 'queued';

    INSERT INTO INTEGRATION_COMMANDS (
      ID, TENANT_ID, BRANCH_ID, CONNECTOR_ID, TERMINAL_ID, ORDER_ID, PAYMENT_ID,
      COMMAND_TYPE, IDEMPOTENCY_KEY, PAYLOAD, STATUS, REQUESTED_BY)
    VALUES (
      p_command_id, p_tenant_id, p_branch_id, v_connector_id, p_terminal_id,
      p_order_id, p_payment_id, p_command_type, p_idempotency_key, p_payload,
      p_status, p_user_id);
  END QUEUE_COMMAND;

  PROCEDURE MARK_COMMAND_SENT(
    p_tenant_id IN VARCHAR2, p_command_id IN VARCHAR2, p_provider_reference IN VARCHAR2)
  IS
  BEGIN
    UPDATE INTEGRATION_COMMANDS
       SET STATUS = 'sent',
           PROVIDER_REFERENCE = NVL(p_provider_reference, PROVIDER_REFERENCE),
           SENT_AT = NVL(SENT_AT, SYSTIMESTAMP),
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_command_id
       AND STATUS IN ('queued','sent');

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20512, 'Integration command not found or already final.');
    END IF;
  END MARK_COMMAND_SENT;

  PROCEDURE MARK_COMMAND_COMPLETED(
    p_tenant_id IN VARCHAR2, p_command_id IN VARCHAR2, p_provider_reference IN VARCHAR2,
    p_result_payload IN CLOB)
  IS
  BEGIN
    UPDATE INTEGRATION_COMMANDS
       SET STATUS = 'completed',
           PROVIDER_REFERENCE = NVL(p_provider_reference, PROVIDER_REFERENCE),
           RESULT_PAYLOAD = p_result_payload,
           ERROR_CODE = NULL,
           ERROR_MESSAGE = NULL,
           COMPLETED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_command_id
       AND STATUS IN ('queued','sent');

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20512, 'Integration command not found or already final.');
    END IF;
  END MARK_COMMAND_COMPLETED;

  PROCEDURE MARK_COMMAND_FAILED(
    p_tenant_id IN VARCHAR2, p_command_id IN VARCHAR2, p_error_code IN VARCHAR2,
    p_error_message IN VARCHAR2, p_result_payload IN CLOB)
  IS
  BEGIN
    UPDATE INTEGRATION_COMMANDS
       SET STATUS = 'failed',
           ERROR_CODE = p_error_code,
           ERROR_MESSAGE = p_error_message,
           RESULT_PAYLOAD = p_result_payload,
           COMPLETED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_command_id
       AND STATUS IN ('queued','sent');

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20512, 'Integration command not found or already final.');
    END IF;
  END MARK_COMMAND_FAILED;
END PKG_INTEGRATION;
/
