-- ============================================
-- ORDEVO - Database Functions
-- ============================================

-- Function: Archive Daily Data
CREATE OR REPLACE FUNCTION archive_daily_data(
  p_organization_id UUID,
  p_business_date DATE,
  p_user_id UUID
)
RETURNS JSON AS $$
DECLARE
  v_total_orders INTEGER;
  v_total_revenue DECIMAL(12,2);
  v_total_items INTEGER;
  v_result JSON;
BEGIN
  -- Calculate daily statistics
  SELECT 
    COUNT(DISTINCT o.id),
    COALESCE(SUM(oi.total_price), 0),
    COALESCE(SUM(oi.quantity), 0)
  INTO 
    v_total_orders,
    v_total_revenue,
    v_total_items
  FROM orders o
  LEFT JOIN order_items oi ON oi.order_id = o.id
  WHERE 
    o.organization_id = p_organization_id
    AND o.status = 'closed'
    AND DATE(o.closed_at) = p_business_date;

  -- Insert or update archive record
  INSERT INTO daily_sales_archive (
    organization_id,
    business_date,
    total_orders,
    total_revenue,
    total_items,
    archived_by_user_id
  ) VALUES (
    p_organization_id,
    p_business_date,
    v_total_orders,
    v_total_revenue,
    v_total_items,
    p_user_id
  )
  ON CONFLICT (organization_id, business_date)
  DO UPDATE SET
    total_orders = EXCLUDED.total_orders,
    total_revenue = EXCLUDED.total_revenue,
    total_items = EXCLUDED.total_items,
    archived_by_user_id = EXCLUDED.archived_by_user_id,
    archived_at = NOW();

  -- Return result
  v_result := json_build_object(
    'success', true,
    'business_date', p_business_date,
    'total_orders', v_total_orders,
    'total_revenue', v_total_revenue,
    'total_items', v_total_items
  );

  RETURN v_result;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function: Get Daily Statistics
CREATE OR REPLACE FUNCTION get_daily_statistics(
  p_organization_id UUID,
  p_date DATE DEFAULT CURRENT_DATE
)
RETURNS JSON AS $$
DECLARE
  v_result JSON;
BEGIN
  SELECT json_build_object(
    'date', p_date,
    'total_orders', COUNT(DISTINCT o.id),
    'total_revenue', COALESCE(SUM(oi.total_price), 0),
    'total_items', COALESCE(SUM(oi.quantity), 0),
    'open_orders', COUNT(DISTINCT CASE WHEN o.status = 'open' THEN o.id END),
    'closed_orders', COUNT(DISTINCT CASE WHEN o.status = 'closed' THEN o.id END)
  )
  INTO v_result
  FROM orders o
  LEFT JOIN order_items oi ON oi.order_id = o.id
  WHERE 
    o.organization_id = p_organization_id
    AND DATE(o.opened_at) = p_date;

  RETURN v_result;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function: Get Top Selling Items
CREATE OR REPLACE FUNCTION get_top_selling_items(
  p_organization_id UUID,
  p_start_date DATE,
  p_end_date DATE,
  p_limit INTEGER DEFAULT 10
)
RETURNS TABLE (
  item_id UUID,
  item_name TEXT,
  category_name TEXT,
  total_quantity BIGINT,
  total_revenue DECIMAL(12,2)
) AS $$
BEGIN
  RETURN QUERY
  SELECT 
    mi.id,
    mi.name,
    mc.name,
    SUM(oi.quantity)::BIGINT,
    SUM(oi.total_price)
  FROM order_items oi
  JOIN menu_items mi ON mi.id = oi.menu_item_id
  JOIN menu_categories mc ON mc.id = mi.category_id
  JOIN orders o ON o.id = oi.order_id
  WHERE 
    oi.organization_id = p_organization_id
    AND o.status = 'closed'
    AND DATE(o.closed_at) BETWEEN p_start_date AND p_end_date
  GROUP BY mi.id, mi.name, mc.name
  ORDER BY SUM(oi.quantity) DESC
  LIMIT p_limit;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function: Get Hourly Sales
CREATE OR REPLACE FUNCTION get_hourly_sales(
  p_organization_id UUID,
  p_date DATE DEFAULT CURRENT_DATE
)
RETURNS TABLE (
  hour INTEGER,
  order_count BIGINT,
  revenue DECIMAL(12,2)
) AS $$
BEGIN
  RETURN QUERY
  SELECT 
    EXTRACT(HOUR FROM o.closed_at)::INTEGER,
    COUNT(DISTINCT o.id)::BIGINT,
    COALESCE(SUM(oi.total_price), 0)
  FROM orders o
  LEFT JOIN order_items oi ON oi.order_id = o.id
  WHERE 
    o.organization_id = p_organization_id
    AND o.status = 'closed'
    AND DATE(o.closed_at) = p_date
  GROUP BY EXTRACT(HOUR FROM o.closed_at)
  ORDER BY EXTRACT(HOUR FROM o.closed_at);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
