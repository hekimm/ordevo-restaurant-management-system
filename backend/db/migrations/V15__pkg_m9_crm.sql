
CREATE OR REPLACE PACKAGE PKG_M9_CRM AS
  FUNCTION NEW_ID RETURN VARCHAR2;

  PROCEDURE CREATE_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_phone IN VARCHAR2, p_full_name IN VARCHAR2,
    p_email IN VARCHAR2, p_birthday IN DATE, p_sms_consent IN NUMBER,
    p_email_consent IN NUMBER, p_user_id IN VARCHAR2, p_customer_id OUT VARCHAR2);

  PROCEDURE UPDATE_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_full_name IN VARCHAR2,
    p_email IN VARCHAR2, p_birthday IN DATE, p_notes IN CLOB, p_preferences IN CLOB,
    p_sms_consent IN NUMBER, p_email_consent IN NUMBER, p_user_id IN VARCHAR2);

  PROCEDURE BLOCK_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_reason IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE UNBLOCK_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE ADD_CUSTOMER_ADDRESS(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_label IN VARCHAR2,
    p_address_line1 IN VARCHAR2, p_address_line2 IN VARCHAR2, p_district IN VARCHAR2,
    p_city IN VARCHAR2, p_postal_code IN VARCHAR2, p_latitude IN NUMBER,
    p_longitude IN NUMBER, p_delivery_note IN VARCHAR2, p_is_default IN NUMBER,
    p_user_id IN VARCHAR2, p_address_id OUT VARCHAR2);

  PROCEDURE ADD_LOYALTY_POINTS(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_order_id IN VARCHAR2,
    p_points IN NUMBER, p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_txn_id OUT VARCHAR2);

  PROCEDURE REDEEM_LOYALTY_POINTS(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_order_id IN VARCHAR2,
    p_points IN NUMBER, p_user_id IN VARCHAR2, p_txn_id OUT VARCHAR2);

  PROCEDURE ADJUST_LOYALTY_POINTS(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_points IN NUMBER,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_txn_id OUT VARCHAR2);

  FUNCTION CALCULATE_CAMPAIGN_DISCOUNT(
    p_tenant_id IN VARCHAR2, p_order_id IN VARCHAR2, p_customer_id IN VARCHAR2,
    p_campaign_code IN VARCHAR2) RETURN NUMBER;

  PROCEDURE APPLY_CAMPAIGN(
    p_tenant_id IN VARCHAR2, p_order_id IN VARCHAR2, p_customer_id IN VARCHAR2,
    p_campaign_code IN VARCHAR2, p_user_id IN VARCHAR2,
    p_usage_id OUT VARCHAR2, p_discount_amount OUT NUMBER);

  PROCEDURE CREATE_RESERVATION(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_customer_id IN VARCHAR2,
    p_customer_name IN VARCHAR2, p_customer_phone IN VARCHAR2, p_reservation_date IN DATE,
    p_reservation_time IN VARCHAR2, p_guest_count IN NUMBER, p_table_id IN VARCHAR2,
    p_notes IN CLOB, p_user_id IN VARCHAR2, p_reservation_id OUT VARCHAR2,
    p_reservation_no OUT NUMBER);

  PROCEDURE SET_RESERVATION_STATUS(
    p_tenant_id IN VARCHAR2, p_reservation_id IN VARCHAR2, p_status IN VARCHAR2,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE CREATE_DELIVERY(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_order_id IN VARCHAR2,
    p_customer_id IN VARCHAR2, p_zone_id IN VARCHAR2, p_delivery_address IN VARCHAR2,
    p_delivery_lat IN NUMBER, p_delivery_lng IN NUMBER, p_delivery_fee IN NUMBER,
    p_estimated_minutes IN NUMBER, p_user_id IN VARCHAR2, p_delivery_id OUT VARCHAR2);

  FUNCTION FIND_AVAILABLE_COURIER(p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2) RETURN VARCHAR2;

  PROCEDURE ASSIGN_COURIER(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_courier_id IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE SET_COURIER_STATUS(
    p_tenant_id IN VARCHAR2, p_courier_id IN VARCHAR2, p_status IN VARCHAR2);

  PROCEDURE UPDATE_COURIER_LOCATION(
    p_tenant_id IN VARCHAR2, p_courier_id IN VARCHAR2, p_latitude IN NUMBER, p_longitude IN NUMBER);

  PROCEDURE SET_DELIVERY_STATUS(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_status IN VARCHAR2, p_user_id IN VARCHAR2);

  PROCEDURE RATE_DELIVERY(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_rating IN NUMBER, p_feedback IN CLOB);
END PKG_M9_CRM;
/

CREATE OR REPLACE PACKAGE BODY PKG_M9_CRM AS

  FUNCTION NEW_ID RETURN VARCHAR2 IS
    g VARCHAR2(32) := RAWTOHEX(SYS_GUID());
  BEGIN
    RETURN LOWER(SUBSTR(g,1,8)||'-'||SUBSTR(g,9,4)||'-'||SUBSTR(g,13,4)||'-'||SUBSTR(g,17,4)||'-'||SUBSTR(g,21,12));
  END NEW_ID;

  PROCEDURE UPDATE_CUSTOMER_TIER(p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2) IS
    v_spent NUMBER(18,4);
    v_tier VARCHAR2(20);
  BEGIN
    SELECT TOTAL_SPENT INTO v_spent
      FROM CRM_CUSTOMERS
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

    IF v_spent >= 10000 THEN
      v_tier := 'platinum';
    ELSIF v_spent >= 5000 THEN
      v_tier := 'gold';
    ELSIF v_spent >= 1000 THEN
      v_tier := 'silver';
    ELSE
      v_tier := 'bronze';
    END IF;

    UPDATE CRM_CUSTOMERS
       SET LOYALTY_TIER = v_tier, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id AND LOYALTY_TIER <> v_tier;
  END UPDATE_CUSTOMER_TIER;

  PROCEDURE ASSERT_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_points OUT NUMBER, p_blocked OUT NUMBER) IS
  BEGIN
    SELECT LOYALTY_POINTS, IS_BLOCKED INTO p_points, p_blocked
      FROM CRM_CUSTOMERS
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id
     FOR UPDATE;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20322, 'Customer not found.');
  END ASSERT_CUSTOMER;

  PROCEDURE CREATE_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_phone IN VARCHAR2, p_full_name IN VARCHAR2,
    p_email IN VARCHAR2, p_birthday IN DATE, p_sms_consent IN NUMBER,
    p_email_consent IN NUMBER, p_user_id IN VARCHAR2, p_customer_id OUT VARCHAR2)
  IS
    v_phone VARCHAR2(40) := TRIM(p_phone);
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count
      FROM CRM_CUSTOMERS
     WHERE TENANT_ID = p_tenant_id AND PHONE = v_phone;

    IF v_count > 0 THEN
      RAISE_APPLICATION_ERROR(-20321, 'Phone already exists.');
    END IF;

    p_customer_id := NEW_ID();
    INSERT INTO CRM_CUSTOMERS (
      ID, TENANT_ID, PHONE, FULL_NAME, EMAIL, BIRTHDAY, SMS_CONSENT, EMAIL_CONSENT,
      CREATED_BY, UPDATED_BY)
    VALUES (
      p_customer_id, p_tenant_id, v_phone, p_full_name, LOWER(p_email), p_birthday,
      NVL(p_sms_consent,1), NVL(p_email_consent,1), p_user_id, p_user_id);
  END CREATE_CUSTOMER;

  PROCEDURE UPDATE_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_full_name IN VARCHAR2,
    p_email IN VARCHAR2, p_birthday IN DATE, p_notes IN CLOB, p_preferences IN CLOB,
    p_sms_consent IN NUMBER, p_email_consent IN NUMBER, p_user_id IN VARCHAR2)
  IS
  BEGIN
    UPDATE CRM_CUSTOMERS
       SET FULL_NAME = p_full_name,
           EMAIL = LOWER(p_email),
           BIRTHDAY = p_birthday,
           NOTES = p_notes,
           PREFERENCES = p_preferences,
           SMS_CONSENT = NVL(p_sms_consent, 1),
           EMAIL_CONSENT = NVL(p_email_consent, 1),
           UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20322, 'Customer not found.');
    END IF;
  END UPDATE_CUSTOMER;

  PROCEDURE BLOCK_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_reason IN VARCHAR2, p_user_id IN VARCHAR2)
  IS
  BEGIN
    UPDATE CRM_CUSTOMERS
       SET IS_BLOCKED = 1,
           BLOCK_REASON = p_reason,
           UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20322, 'Customer not found.');
    END IF;
  END BLOCK_CUSTOMER;

  PROCEDURE UNBLOCK_CUSTOMER(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_user_id IN VARCHAR2)
  IS
  BEGIN
    UPDATE CRM_CUSTOMERS
       SET IS_BLOCKED = 0,
           BLOCK_REASON = NULL,
           UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20322, 'Customer not found.');
    END IF;
  END UNBLOCK_CUSTOMER;

  PROCEDURE ADD_CUSTOMER_ADDRESS(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_label IN VARCHAR2,
    p_address_line1 IN VARCHAR2, p_address_line2 IN VARCHAR2, p_district IN VARCHAR2,
    p_city IN VARCHAR2, p_postal_code IN VARCHAR2, p_latitude IN NUMBER,
    p_longitude IN NUMBER, p_delivery_note IN VARCHAR2, p_is_default IN NUMBER,
    p_user_id IN VARCHAR2, p_address_id OUT VARCHAR2)
  IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count
      FROM CRM_CUSTOMERS
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

    IF v_count = 0 THEN
      RAISE_APPLICATION_ERROR(-20322, 'Customer not found.');
    END IF;

    IF NVL(p_is_default,0) = 1 THEN
      UPDATE CRM_CUSTOMER_ADDRESSES
         SET IS_DEFAULT = 0, UPDATED_AT = SYSTIMESTAMP, UPDATED_BY = p_user_id, ROW_VERSION = ROW_VERSION + 1
       WHERE TENANT_ID = p_tenant_id AND CUSTOMER_ID = p_customer_id;
    END IF;

    p_address_id := NEW_ID();
    INSERT INTO CRM_CUSTOMER_ADDRESSES (
      ID, TENANT_ID, CUSTOMER_ID, LABEL, ADDRESS_LINE1, ADDRESS_LINE2, DISTRICT, CITY,
      POSTAL_CODE, LATITUDE, LONGITUDE, DELIVERY_NOTE, IS_DEFAULT, CREATED_BY, UPDATED_BY)
    VALUES (
      p_address_id, p_tenant_id, p_customer_id, p_label, p_address_line1, p_address_line2,
      p_district, p_city, p_postal_code, p_latitude, p_longitude, p_delivery_note,
      NVL(p_is_default,0), p_user_id, p_user_id);
  END ADD_CUSTOMER_ADDRESS;

  PROCEDURE ADD_LOYALTY_POINTS(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_order_id IN VARCHAR2,
    p_points IN NUMBER, p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_txn_id OUT VARCHAR2)
  IS
    v_balance NUMBER;
    v_blocked NUMBER;
  BEGIN
    IF NVL(p_points,0) <= 0 THEN
      RAISE_APPLICATION_ERROR(-20324, 'Points must be positive.');
    END IF;

    ASSERT_CUSTOMER(p_tenant_id, p_customer_id, v_balance, v_blocked);
    IF v_blocked = 1 THEN
      RAISE_APPLICATION_ERROR(-20323, 'Customer is blocked.');
    END IF;

    v_balance := v_balance + p_points;
    UPDATE CRM_CUSTOMERS
       SET LOYALTY_POINTS = v_balance, UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

    p_txn_id := NEW_ID();
    INSERT INTO CRM_LOYALTY_TRANSACTIONS (
      ID, TENANT_ID, CUSTOMER_ID, TRANSACTION_TYPE, POINTS, BALANCE_AFTER,
      ORDER_ID, REASON, CREATED_BY)
    VALUES (
      p_txn_id, p_tenant_id, p_customer_id, 'earn', p_points, v_balance,
      p_order_id, p_reason, p_user_id);
  END ADD_LOYALTY_POINTS;

  PROCEDURE REDEEM_LOYALTY_POINTS(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_order_id IN VARCHAR2,
    p_points IN NUMBER, p_user_id IN VARCHAR2, p_txn_id OUT VARCHAR2)
  IS
    v_balance NUMBER;
    v_blocked NUMBER;
  BEGIN
    IF NVL(p_points,0) <= 0 THEN
      RAISE_APPLICATION_ERROR(-20324, 'Points must be positive.');
    END IF;

    ASSERT_CUSTOMER(p_tenant_id, p_customer_id, v_balance, v_blocked);
    IF v_blocked = 1 THEN
      RAISE_APPLICATION_ERROR(-20323, 'Customer is blocked.');
    END IF;
    IF v_balance < p_points THEN
      RAISE_APPLICATION_ERROR(-20325, 'Insufficient points.');
    END IF;

    v_balance := v_balance - p_points;
    UPDATE CRM_CUSTOMERS
       SET LOYALTY_POINTS = v_balance, UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

    p_txn_id := NEW_ID();
    INSERT INTO CRM_LOYALTY_TRANSACTIONS (
      ID, TENANT_ID, CUSTOMER_ID, TRANSACTION_TYPE, POINTS, BALANCE_AFTER,
      ORDER_ID, REASON, CREATED_BY)
    VALUES (
      p_txn_id, p_tenant_id, p_customer_id, 'redeem', -p_points, v_balance,
      p_order_id, 'loyalty redemption', p_user_id);
  END REDEEM_LOYALTY_POINTS;

  PROCEDURE ADJUST_LOYALTY_POINTS(
    p_tenant_id IN VARCHAR2, p_customer_id IN VARCHAR2, p_points IN NUMBER,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2, p_txn_id OUT VARCHAR2)
  IS
    v_balance NUMBER;
    v_blocked NUMBER;
  BEGIN
    IF NVL(p_points,0) = 0 THEN
      RAISE_APPLICATION_ERROR(-20324, 'Adjustment cannot be zero.');
    END IF;

    ASSERT_CUSTOMER(p_tenant_id, p_customer_id, v_balance, v_blocked);
    v_balance := v_balance + p_points;
    IF v_balance < 0 THEN
      RAISE_APPLICATION_ERROR(-20325, 'Insufficient points.');
    END IF;

    UPDATE CRM_CUSTOMERS
       SET LOYALTY_POINTS = v_balance, UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

    p_txn_id := NEW_ID();
    INSERT INTO CRM_LOYALTY_TRANSACTIONS (
      ID, TENANT_ID, CUSTOMER_ID, TRANSACTION_TYPE, POINTS, BALANCE_AFTER,
      REASON, CREATED_BY)
    VALUES (
      p_txn_id, p_tenant_id, p_customer_id, 'adjust', p_points, v_balance,
      p_reason, p_user_id);
  END ADJUST_LOYALTY_POINTS;

  FUNCTION CALCULATE_CAMPAIGN_DISCOUNT(
    p_tenant_id IN VARCHAR2, p_order_id IN VARCHAR2, p_customer_id IN VARCHAR2,
    p_campaign_code IN VARCHAR2) RETURN NUMBER
  IS
    v_order_total NUMBER(18,4);
    v_order_branch VARCHAR2(36);
    v_order_status VARCHAR2(16);
    v_discount_type VARCHAR2(20);
    v_discount_value NUMBER(18,4);
    v_max_discount NUMBER(18,4);
    v_min_order NUMBER(18,4);
    v_usage_per_customer NUMBER;
    v_total_limit NUMBER;
    v_usage_count NUMBER;
    v_customer_usage NUMBER;
    v_discount NUMBER(18,4);
    v_customer_blocked NUMBER;
  BEGIN
    SELECT TOTAL, BRANCH_ID, STATUS INTO v_order_total, v_order_branch, v_order_status
      FROM ORDERS
     WHERE TENANT_ID = p_tenant_id AND ID = p_order_id;

    IF v_order_status <> 'open' THEN
      RETURN 0;
    END IF;

    SELECT DISCOUNT_TYPE, DISCOUNT_VALUE, MAX_DISCOUNT_AMOUNT, MIN_ORDER_AMOUNT,
           USAGE_LIMIT_PER_CUSTOMER, TOTAL_USAGE_LIMIT, USAGE_COUNT
      INTO v_discount_type, v_discount_value, v_max_discount, v_min_order,
           v_usage_per_customer, v_total_limit, v_usage_count
      FROM CRM_CAMPAIGNS
     WHERE TENANT_ID = p_tenant_id
       AND UPPER(CODE) = UPPER(p_campaign_code)
       AND IS_ACTIVE = 1
       AND SYSTIMESTAMP >= STARTS_AT
       AND (ENDS_AT IS NULL OR SYSTIMESTAMP <= ENDS_AT)
       AND (BRANCH_ID IS NULL OR BRANCH_ID = v_order_branch);

    IF v_min_order IS NOT NULL AND v_order_total < v_min_order THEN
      RETURN 0;
    END IF;

    IF v_total_limit IS NOT NULL AND v_usage_count >= v_total_limit THEN
      RETURN 0;
    END IF;

    IF p_customer_id IS NOT NULL THEN
      SELECT IS_BLOCKED INTO v_customer_blocked
        FROM CRM_CUSTOMERS
       WHERE TENANT_ID = p_tenant_id AND ID = p_customer_id;

      IF v_customer_blocked = 1 THEN
        RETURN 0;
      END IF;

      IF v_usage_per_customer IS NOT NULL THEN
        SELECT COUNT(*) INTO v_customer_usage
          FROM CRM_CAMPAIGN_USAGE u
          JOIN CRM_CAMPAIGNS c ON c.ID = u.CAMPAIGN_ID
         WHERE u.TENANT_ID = p_tenant_id
           AND u.CUSTOMER_ID = p_customer_id
           AND UPPER(c.CODE) = UPPER(p_campaign_code);

        IF v_customer_usage >= v_usage_per_customer THEN
          RETURN 0;
        END IF;
      END IF;
    END IF;

    IF v_discount_type = 'percent' THEN
      v_discount := ROUND(v_order_total * v_discount_value / 100, 2);
      IF v_max_discount IS NOT NULL THEN
        v_discount := LEAST(v_discount, v_max_discount);
      END IF;
    ELSE
      v_discount := LEAST(v_discount_value, v_order_total);
    END IF;

    RETURN GREATEST(v_discount, 0);
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RETURN 0;
  END CALCULATE_CAMPAIGN_DISCOUNT;

  PROCEDURE APPLY_CAMPAIGN(
    p_tenant_id IN VARCHAR2, p_order_id IN VARCHAR2, p_customer_id IN VARCHAR2,
    p_campaign_code IN VARCHAR2, p_user_id IN VARCHAR2,
    p_usage_id OUT VARCHAR2, p_discount_amount OUT NUMBER)
  IS
    v_campaign_id VARCHAR2(36);
    v_order_branch VARCHAR2(36);
    v_order_status VARCHAR2(16);
    v_count NUMBER;
    v_disc_id VARCHAR2(36);
  BEGIN
    SELECT BRANCH_ID, STATUS INTO v_order_branch, v_order_status
      FROM ORDERS
     WHERE TENANT_ID = p_tenant_id AND ID = p_order_id
     FOR UPDATE;

    IF v_order_status <> 'open' THEN
      RAISE_APPLICATION_ERROR(-20332, 'Campaign can only be applied to open orders.');
    END IF;

    SELECT ID INTO v_campaign_id
      FROM CRM_CAMPAIGNS
     WHERE TENANT_ID = p_tenant_id
       AND UPPER(CODE) = UPPER(p_campaign_code)
       AND IS_ACTIVE = 1
       AND SYSTIMESTAMP >= STARTS_AT
       AND (ENDS_AT IS NULL OR SYSTIMESTAMP <= ENDS_AT)
       AND (BRANCH_ID IS NULL OR BRANCH_ID = v_order_branch)
     FOR UPDATE;

    SELECT COUNT(*) INTO v_count
      FROM CRM_CAMPAIGN_USAGE
     WHERE CAMPAIGN_ID = v_campaign_id AND ORDER_ID = p_order_id;
    IF v_count > 0 THEN
      RAISE_APPLICATION_ERROR(-20333, 'Campaign already used on this order.');
    END IF;

    p_discount_amount := CALCULATE_CAMPAIGN_DISCOUNT(p_tenant_id, p_order_id, p_customer_id, p_campaign_code);
    IF p_discount_amount <= 0 THEN
      RAISE_APPLICATION_ERROR(-20332, 'Campaign eligibility failed.');
    END IF;

    p_usage_id := NEW_ID();
    INSERT INTO CRM_CAMPAIGN_USAGE (
      ID, TENANT_ID, CAMPAIGN_ID, CUSTOMER_ID, ORDER_ID, DISCOUNT_AMOUNT, CREATED_BY)
    VALUES (
      p_usage_id, p_tenant_id, v_campaign_id, p_customer_id, p_order_id, p_discount_amount, p_user_id);

    UPDATE CRM_CAMPAIGNS
       SET USAGE_COUNT = USAGE_COUNT + 1, UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE ID = v_campaign_id;

    v_disc_id := NEW_ID();
    INSERT INTO ORDER_DISCOUNTS (
      ID, ORDER_ID, TENANT_ID, DISC_TYPE, DISC_VALUE, AMOUNT, REASON, CREATED_BY)
    VALUES (
      v_disc_id, p_order_id, p_tenant_id, 'amount', p_discount_amount,
      p_discount_amount, 'campaign:' || UPPER(p_campaign_code), p_user_id);

    IF p_customer_id IS NOT NULL THEN
      UPDATE ORDERS SET CUSTOMER_ID = p_customer_id, UPDATED_AT = SYSTIMESTAMP
       WHERE TENANT_ID = p_tenant_id AND ID = p_order_id;
    END IF;

    PKG_ORDERING.RECALC_ORDER(p_order_id);
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20331, 'Campaign not found.');
  END APPLY_CAMPAIGN;

  PROCEDURE CREATE_RESERVATION(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_customer_id IN VARCHAR2,
    p_customer_name IN VARCHAR2, p_customer_phone IN VARCHAR2, p_reservation_date IN DATE,
    p_reservation_time IN VARCHAR2, p_guest_count IN NUMBER, p_table_id IN VARCHAR2,
    p_notes IN CLOB, p_user_id IN VARCHAR2, p_reservation_id OUT VARCHAR2,
    p_reservation_no OUT NUMBER)
  IS
    v_count NUMBER;
  BEGIN
    IF NVL(p_guest_count,0) <= 0 THEN
      RAISE_APPLICATION_ERROR(-20342, 'Guest count must be positive.');
    END IF;

    IF p_table_id IS NOT NULL THEN
      SELECT COUNT(*) INTO v_count
        FROM DINING_TABLES
       WHERE TENANT_ID = p_tenant_id AND BRANCH_ID = p_branch_id AND ID = p_table_id AND IS_ACTIVE = 1;

      IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20342, 'Table is not valid for this branch.');
      END IF;

      SELECT COUNT(*) INTO v_count
        FROM CRM_RESERVATIONS
       WHERE TENANT_ID = p_tenant_id
         AND BRANCH_ID = p_branch_id
         AND TABLE_ID = p_table_id
         AND RESERVATION_DATE = TRUNC(p_reservation_date)
         AND RESERVATION_TIME = p_reservation_time
         AND STATUS IN ('pending','confirmed');

      IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(-20343, 'Table already reserved for this time.');
      END IF;
    END IF;

    SELECT CRM_RESERVATION_NO_SEQ.NEXTVAL INTO p_reservation_no FROM DUAL;
    p_reservation_id := NEW_ID();

    INSERT INTO CRM_RESERVATIONS (
      ID, TENANT_ID, BRANCH_ID, RESERVATION_NO, CUSTOMER_ID, CUSTOMER_NAME, CUSTOMER_PHONE,
      RESERVATION_DATE, RESERVATION_TIME, GUEST_COUNT, TABLE_ID, NOTES, STATUS, CREATED_BY, UPDATED_BY)
    VALUES (
      p_reservation_id, p_tenant_id, p_branch_id, p_reservation_no, p_customer_id,
      p_customer_name, p_customer_phone, TRUNC(p_reservation_date), p_reservation_time,
      p_guest_count, p_table_id, p_notes, 'pending', p_user_id, p_user_id);
  END CREATE_RESERVATION;

  PROCEDURE SET_RESERVATION_STATUS(
    p_tenant_id IN VARCHAR2, p_reservation_id IN VARCHAR2, p_status IN VARCHAR2,
    p_reason IN VARCHAR2, p_user_id IN VARCHAR2)
  IS
    v_status VARCHAR2(20);
  BEGIN
    SELECT STATUS INTO v_status
      FROM CRM_RESERVATIONS
     WHERE TENANT_ID = p_tenant_id AND ID = p_reservation_id
     FOR UPDATE;

    IF p_status = 'confirmed' AND v_status <> 'pending' THEN
      RAISE_APPLICATION_ERROR(-20342, 'Reservation cannot be confirmed.');
    ELSIF p_status = 'seated' AND v_status <> 'confirmed' THEN
      RAISE_APPLICATION_ERROR(-20342, 'Reservation cannot be seated.');
    ELSIF p_status = 'cancelled' AND v_status NOT IN ('pending','confirmed') THEN
      RAISE_APPLICATION_ERROR(-20342, 'Reservation cannot be cancelled.');
    ELSIF p_status = 'no_show' AND v_status <> 'confirmed' THEN
      RAISE_APPLICATION_ERROR(-20342, 'Reservation cannot be marked no-show.');
    ELSIF p_status NOT IN ('confirmed','seated','cancelled','no_show') THEN
      RAISE_APPLICATION_ERROR(-20342, 'Invalid reservation status.');
    END IF;

    UPDATE CRM_RESERVATIONS
       SET STATUS = p_status,
           CONFIRMED_AT = CASE WHEN p_status = 'confirmed' THEN SYSTIMESTAMP ELSE CONFIRMED_AT END,
           SEATED_AT = CASE WHEN p_status = 'seated' THEN SYSTIMESTAMP ELSE SEATED_AT END,
           CANCELLED_AT = CASE WHEN p_status IN ('cancelled','no_show') THEN SYSTIMESTAMP ELSE CANCELLED_AT END,
           CANCEL_REASON = CASE WHEN p_status = 'cancelled' THEN p_reason ELSE CANCEL_REASON END,
           UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_reservation_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20341, 'Reservation not found.');
  END SET_RESERVATION_STATUS;

  PROCEDURE CREATE_DELIVERY(
    p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2, p_order_id IN VARCHAR2,
    p_customer_id IN VARCHAR2, p_zone_id IN VARCHAR2, p_delivery_address IN VARCHAR2,
    p_delivery_lat IN NUMBER, p_delivery_lng IN NUMBER, p_delivery_fee IN NUMBER,
    p_estimated_minutes IN NUMBER, p_user_id IN VARCHAR2, p_delivery_id OUT VARCHAR2)
  IS
    v_order_branch VARCHAR2(36);
    v_count NUMBER;
  BEGIN
    SELECT BRANCH_ID INTO v_order_branch
      FROM ORDERS
     WHERE TENANT_ID = p_tenant_id AND ID = p_order_id;

    IF v_order_branch <> p_branch_id THEN
      RAISE_APPLICATION_ERROR(-20351, 'Order is not in this branch.');
    END IF;

    SELECT COUNT(*) INTO v_count FROM CRM_DELIVERIES WHERE TENANT_ID = p_tenant_id AND ORDER_ID = p_order_id;
    IF v_count > 0 THEN
      RAISE_APPLICATION_ERROR(-20352, 'Delivery already exists for this order.');
    END IF;

    IF p_zone_id IS NOT NULL THEN
      SELECT COUNT(*) INTO v_count
        FROM CRM_DELIVERY_ZONES
       WHERE TENANT_ID = p_tenant_id
         AND BRANCH_ID = p_branch_id
         AND ID = p_zone_id
         AND IS_ACTIVE = 1;

      IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20351, 'Delivery zone not found for this branch.');
      END IF;
    END IF;

    p_delivery_id := NEW_ID();
    INSERT INTO CRM_DELIVERIES (
      ID, TENANT_ID, BRANCH_ID, ORDER_ID, CUSTOMER_ID, DELIVERY_ZONE_ID,
      DELIVERY_ADDRESS, DELIVERY_LAT, DELIVERY_LNG, DELIVERY_FEE, ESTIMATED_MINUTES,
      STATUS, CREATED_BY, UPDATED_BY)
    VALUES (
      p_delivery_id, p_tenant_id, p_branch_id, p_order_id, p_customer_id, p_zone_id,
      p_delivery_address, p_delivery_lat, p_delivery_lng, NVL(p_delivery_fee,0),
      p_estimated_minutes, 'pending', p_user_id, p_user_id);

    UPDATE ORDERS
       SET ORDER_TYPE = 'delivery',
           CUSTOMER_ID = COALESCE(p_customer_id, CUSTOMER_ID),
           UPDATED_AT = SYSTIMESTAMP
     WHERE TENANT_ID = p_tenant_id AND ID = p_order_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20351, 'Delivery order not found.');
  END CREATE_DELIVERY;

  FUNCTION FIND_AVAILABLE_COURIER(p_tenant_id IN VARCHAR2, p_branch_id IN VARCHAR2) RETURN VARCHAR2
  IS
    v_courier_id VARCHAR2(36);
  BEGIN
    SELECT ID INTO v_courier_id
      FROM CRM_COURIERS
     WHERE TENANT_ID = p_tenant_id
       AND BRANCH_ID = p_branch_id
       AND IS_ACTIVE = 1
       AND STATUS = 'available'
     ORDER BY TOTAL_DELIVERIES ASC, UPDATED_AT ASC
     FETCH FIRST 1 ROW ONLY;

    RETURN v_courier_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RETURN NULL;
  END FIND_AVAILABLE_COURIER;

  PROCEDURE ASSIGN_COURIER(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_courier_id IN VARCHAR2, p_user_id IN VARCHAR2)
  IS
    v_branch_id VARCHAR2(36);
    v_status VARCHAR2(20);
    v_order_id VARCHAR2(36);
    v_old_courier VARCHAR2(36);
    v_courier_id VARCHAR2(36) := p_courier_id;
    v_courier_status VARCHAR2(20);
  BEGIN
    SELECT BRANCH_ID, STATUS, ORDER_ID, COURIER_ID
      INTO v_branch_id, v_status, v_order_id, v_old_courier
      FROM CRM_DELIVERIES
     WHERE TENANT_ID = p_tenant_id AND ID = p_delivery_id
     FOR UPDATE;

    IF v_status NOT IN ('pending','assigned') THEN
      RAISE_APPLICATION_ERROR(-20355, 'Delivery cannot be assigned in current status.');
    END IF;

    IF v_courier_id IS NULL THEN
      v_courier_id := FIND_AVAILABLE_COURIER(p_tenant_id, v_branch_id);
    END IF;

    IF v_courier_id IS NULL THEN
      RAISE_APPLICATION_ERROR(-20353, 'No available courier.');
    END IF;

    SELECT STATUS INTO v_courier_status
      FROM CRM_COURIERS
     WHERE TENANT_ID = p_tenant_id
       AND BRANCH_ID = v_branch_id
       AND ID = v_courier_id
       AND IS_ACTIVE = 1
     FOR UPDATE;

    IF v_courier_status <> 'available' AND NVL(v_old_courier, '-') <> v_courier_id THEN
      RAISE_APPLICATION_ERROR(-20354, 'Courier is unavailable.');
    END IF;

    IF v_old_courier IS NOT NULL AND v_old_courier <> v_courier_id THEN
      UPDATE CRM_COURIERS
         SET STATUS = 'available', CURRENT_ORDER_ID = NULL, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
       WHERE TENANT_ID = p_tenant_id AND ID = v_old_courier;
    END IF;

    UPDATE CRM_DELIVERIES
       SET COURIER_ID = v_courier_id,
           STATUS = 'assigned',
           ASSIGNED_AT = COALESCE(ASSIGNED_AT, SYSTIMESTAMP),
           UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_delivery_id;

    UPDATE CRM_COURIERS
       SET STATUS = 'on_delivery',
           CURRENT_ORDER_ID = v_order_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = v_courier_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20351, 'Delivery or courier not found.');
  END ASSIGN_COURIER;

  PROCEDURE SET_COURIER_STATUS(
    p_tenant_id IN VARCHAR2, p_courier_id IN VARCHAR2, p_status IN VARCHAR2)
  IS
  BEGIN
    IF p_status NOT IN ('off_duty','available') THEN
      RAISE_APPLICATION_ERROR(-20354, 'Invalid courier status.');
    END IF;

    UPDATE CRM_COURIERS
       SET STATUS = p_status,
           CURRENT_ORDER_ID = CASE WHEN p_status = 'off_duty' THEN NULL ELSE CURRENT_ORDER_ID END,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id
       AND ID = p_courier_id
       AND STATUS <> 'on_delivery';

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20354, 'Courier not found or currently on delivery.');
    END IF;
  END SET_COURIER_STATUS;

  PROCEDURE UPDATE_COURIER_LOCATION(
    p_tenant_id IN VARCHAR2, p_courier_id IN VARCHAR2, p_latitude IN NUMBER, p_longitude IN NUMBER)
  IS
  BEGIN
    UPDATE CRM_COURIERS
       SET LAST_LAT = p_latitude,
           LAST_LNG = p_longitude,
           LAST_LOCATION_AT = SYSTIMESTAMP,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_courier_id AND IS_ACTIVE = 1;

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20354, 'Courier not found.');
    END IF;
  END UPDATE_COURIER_LOCATION;

  PROCEDURE SET_DELIVERY_STATUS(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_status IN VARCHAR2, p_user_id IN VARCHAR2)
  IS
    v_courier_id VARCHAR2(36);
    v_status VARCHAR2(20);
  BEGIN
    IF p_status NOT IN ('assigned','picked_up','on_way','delivered','cancelled') THEN
      RAISE_APPLICATION_ERROR(-20355, 'Invalid delivery status.');
    END IF;

    SELECT COURIER_ID, STATUS INTO v_courier_id, v_status
      FROM CRM_DELIVERIES
     WHERE TENANT_ID = p_tenant_id AND ID = p_delivery_id
     FOR UPDATE;

    IF p_status IN ('assigned','picked_up','on_way','delivered') AND v_courier_id IS NULL THEN
      RAISE_APPLICATION_ERROR(-20355, 'Delivery has no courier.');
    END IF;

    UPDATE CRM_DELIVERIES
       SET STATUS = p_status,
           PICKED_UP_AT = CASE WHEN p_status = 'picked_up' THEN SYSTIMESTAMP ELSE PICKED_UP_AT END,
           DELIVERED_AT = CASE WHEN p_status = 'delivered' THEN SYSTIMESTAMP ELSE DELIVERED_AT END,
           UPDATED_BY = p_user_id,
           UPDATED_AT = SYSTIMESTAMP,
           ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_delivery_id;

    IF p_status IN ('delivered','cancelled') AND v_courier_id IS NOT NULL THEN
      UPDATE CRM_COURIERS
         SET STATUS = 'available',
             CURRENT_ORDER_ID = NULL,
             TOTAL_DELIVERIES = TOTAL_DELIVERIES + CASE WHEN p_status = 'delivered' THEN 1 ELSE 0 END,
             UPDATED_AT = SYSTIMESTAMP,
             ROW_VERSION = ROW_VERSION + 1
       WHERE TENANT_ID = p_tenant_id AND ID = v_courier_id;
    END IF;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20351, 'Delivery not found.');
  END SET_DELIVERY_STATUS;

  PROCEDURE RATE_DELIVERY(
    p_tenant_id IN VARCHAR2, p_delivery_id IN VARCHAR2, p_rating IN NUMBER, p_feedback IN CLOB)
  IS
    v_courier_id VARCHAR2(36);
    v_status VARCHAR2(20);
    v_avg_rating NUMBER(3,2);
  BEGIN
    IF p_rating < 1 OR p_rating > 5 THEN
      RAISE_APPLICATION_ERROR(-20356, 'Rating must be between 1 and 5.');
    END IF;

    SELECT COURIER_ID, STATUS INTO v_courier_id, v_status
      FROM CRM_DELIVERIES
     WHERE TENANT_ID = p_tenant_id AND ID = p_delivery_id
     FOR UPDATE;

    IF v_status <> 'delivered' THEN
      RAISE_APPLICATION_ERROR(-20355, 'Only delivered orders can be rated.');
    END IF;

    UPDATE CRM_DELIVERIES
       SET RATING = p_rating, FEEDBACK = p_feedback, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
     WHERE TENANT_ID = p_tenant_id AND ID = p_delivery_id;

    IF v_courier_id IS NOT NULL THEN
      SELECT ROUND(AVG(RATING), 2) INTO v_avg_rating
        FROM CRM_DELIVERIES
       WHERE TENANT_ID = p_tenant_id AND COURIER_ID = v_courier_id AND RATING IS NOT NULL;

      UPDATE CRM_COURIERS
         SET RATING = v_avg_rating, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
       WHERE TENANT_ID = p_tenant_id AND ID = v_courier_id;
    END IF;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20351, 'Delivery not found.');
  END RATE_DELIVERY;
END PKG_M9_CRM;
/
