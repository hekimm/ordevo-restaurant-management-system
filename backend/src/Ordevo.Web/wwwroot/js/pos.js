(function () {
  'use strict';

  const D = window.POS_DATA || { menu: { categories: [] }, tables: [], sections: [], openOrders: [] };
  const money = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });
  const fmt = (n) => money.format(Number(n || 0));

  const state = {
    order: null,
    tables: D.tables || [],
    sections: D.sections || [],
    openOrders: D.openOrders || [],
    fiscal: D.fiscal || { terminals: [] },
    openByTable: new Map(),
    category: null,
    search: '',
    tableSearch: '',
    statusFilter: 'all',
    sectionFilter: 'all',
    payEntryTouched: false,
  };

  const el = (id) => document.getElementById(id);
  const antiforgery = () => (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';

  async function call(method, handler, body, query) {
    const qs = new URLSearchParams(Object.assign({ handler }, query || {})).toString();
    const opts = { method, headers: { 'RequestVerificationToken': antiforgery() } };
    if (body !== undefined) { opts.headers['Content-Type'] = 'application/json'; opts.body = JSON.stringify(body); }
    const res = await fetch(`/tables?${qs}`, opts);
    const text = await res.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { data = null; }
    if (!res.ok) throw new Error(friendlyError(data && data.error, res.status));
    return data;
  }
  const getJson = (handler, query) => call('GET', handler, undefined, query);
  const postJson = (handler, body) => call('POST', handler, body || {});

  function friendlyError(message, status) {
    return window.OrdevoUI?.friendlyError
      ? window.OrdevoUI.friendlyError(message, status)
      : 'İşlem tamamlanamadı. Lütfen tekrar deneyin.';
  }

  async function confirmAction(message) {
    return window.OrdevoUI?.confirm
      ? window.OrdevoUI.confirm(message)
      : false;
  }

  function toast(msg, ok) {
    if (window.OrdevoUI?.toast) {
      window.OrdevoUI.toast(msg, ok ? 'success' : 'error');
      return;
    }
    const t = el('posToast');
    if (!t) return;
    t.textContent = msg;
    t.className = 'pos-toast ' + (ok ? 'ok' : 'err') + ' show';
    setTimeout(() => (t.className = 'pos-toast'), 2600);
  }
  function bs(id) { return bootstrap.Modal.getOrCreateInstance(el(id)); }
  function escapeHtml(s) { return String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }
  function elapsed(openedAt) {
    const m = Math.max(0, Math.floor((Date.now() - Date.parse(openedAt)) / 60000));
    return m < 60 ? `${m} dk` : `${Math.floor(m / 60)}s ${m % 60}dk`;
  }
  function itemTotal(order) {
    return (order?.items || [])
      .filter((i) => !['cancelled', 'void'].includes(i.status))
      .reduce((sum, i) => sum + Number(i.lineTotal || 0), 0);
  }
  function effectiveTotal(order) {
    const serverTotal = Number(order?.total || 0);
    return serverTotal > 0 ? serverTotal : itemTotal(order);
  }
  function isDraft(order) {
    return !!order?.isDraft;
  }
  function summaryHasItems(order) {
    return Number(order?.itemCount || 0) > 0;
  }
  function activeRows(order) {
    return (order?.items || []).filter((i) => !['cancelled', 'void'].includes(i.status));
  }
  function tableState(table) {
    const open = state.openByTable.get(table.id);
    const status = open && summaryHasItems(open) ? 'busy' : table.status === 'reserved' ? 'reserved' : 'free';
    return { open, status };
  }
  function tableName(tableId) {
    return tableId ? ((state.tables.find((t) => t.id === tableId) || {}).name || 'Masa') : 'Paket';
  }

  function orderTypeLabel(order) {
    const type = typeof order === 'string' ? order : (order?.orderType || 'dinein');
    if (type === 'delivery') return 'Kurye';
    if (type === 'takeaway') return 'Paket';
    return 'Masa';
  }

  function parseAmount(value) {
    const raw = String(value ?? '').trim().replace(/[^\d,.-]/g, '');
    if (!raw) return 0;
    const comma = raw.lastIndexOf(',');
    const dot = raw.lastIndexOf('.');
    const sep = Math.max(comma, dot);
    if (sep >= 0) {
      const head = raw.slice(0, sep).replace(/[^\d-]/g, '') || '0';
      const tail = raw.slice(sep + 1).replace(/\D/g, '');
      return Number(`${head}.${tail}`) || 0;
    }
    return Number(raw.replace(/[^\d-]/g, '')) || 0;
  }

  function setPayAmount(value, touched) {
    const input = el('payAmount');
    if (!input) return;
    input.value = Number(value || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    state.payEntryTouched = Boolean(touched);
    updatePayPreview();
  }

  function packageOrders() {
    return state.openOrders
      .filter((order) => {
        const type = order.orderType || (order.tableId ? 'dinein' : 'takeaway');
        return !order.tableId && summaryHasItems(order) && (type === 'takeaway' || type === 'delivery');
      })
      .sort((a, b) => Date.parse(a.openedAt) - Date.parse(b.openedAt));
  }

  function showTables() { el('tablesView').hidden = false; el('adisyonView').hidden = true; renderTableCards(); }
  function showAdisyon() { el('tablesView').hidden = true; el('adisyonView').hidden = false; renderOrder(); }

  function renderTableCards() {
    const wrap = el('tablesGrid');
    wrap.innerHTML = '';
    const secName = new Map(state.sections.map((s) => [s.id, s.name]));
    const activeTables = state.tables.filter((t) => t.isActive);
    const busy = activeTables.filter((t) => tableState(t).status === 'busy').length;
    const reserved = activeTables.filter((t) => tableState(t).status === 'reserved').length;
    const openOrders = state.openOrders.filter(summaryHasItems);
    const packages = packageOrders();
    const validSections = new Set(['all', '__packages', ...activeTables.map((t) => t.sectionId || '__salon')]);
    if (!validSections.has(state.sectionFilter)) state.sectionFilter = 'all';
    if (el('sumOpen')) el('sumOpen').textContent = String(openOrders.length);
    if (el('sumBusy')) el('sumBusy').textContent = String(busy);
    if (el('sumFree')) el('sumFree').textContent = String(Math.max(0, activeTables.length - busy - reserved));
    if (el('sumTotal')) el('sumTotal').textContent = fmt(openOrders.reduce((sum, order) => sum + Number(order.total || 0), 0));
    renderSectionRail(activeTables, secName, packages);

    if (state.sectionFilter === '__packages') {
      renderPackageCards(wrap, packages);
      return;
    }

    const tables = activeTables
      .filter((t) => {
        const { status } = tableState(t);
        if (state.statusFilter !== 'all' && status !== state.statusFilter) return false;
        if (state.sectionFilter !== 'all' && (t.sectionId || '__salon') !== state.sectionFilter) return false;
        const q = state.tableSearch.trim().toLocaleLowerCase('tr');
        return !q || t.name.toLocaleLowerCase('tr').includes(q) || ((t.sectionId && secName.get(t.sectionId)) || 'Salon').toLocaleLowerCase('tr').includes(q);
      })
      .sort((a, b) => a.sortOrder - b.sortOrder);

    const grid = document.createElement('div');
    grid.className = 'tcard-grid';
    tables.forEach((t) => {
      const { open, status: st } = tableState(t);
      const activeOpen = open && summaryHasItems(open) ? open : null;
      const card = document.createElement('button');
      card.className = `tcard ${st}`;
      const statusText = st === 'busy' ? 'DOLU' : st === 'reserved' ? 'REZERVE' : 'BOŞ';
      card.innerHTML =
        `<i class="bi bi-table tc-icon"></i>` +
        `<strong class="tc-name">${escapeHtml(t.name)}</strong>` +
        (activeOpen
          ? `<span class="tc-time"><i class="bi bi-clock"></i>${elapsed(activeOpen.openedAt)}</span>` +
            `<span class="tc-amt">${fmt(activeOpen.total)}</span>`
          : '') +
        `<span class="tc-status">${statusText}</span>`;
      card.onclick = () => (activeOpen ? loadOrder(activeOpen.id) : openOrder(t.id));
      grid.appendChild(card);
    });
    wrap.appendChild(grid);
    if (!tables.length) wrap.innerHTML = '<div class="pos-empty"><i class="bi bi-grid-3x3-gap"></i><p>Bu bölümde masa yok.</p></div>';
  }

  function renderPackageCards(wrap, packages) {
    wrap.innerHTML = '';
    const grid = document.createElement('div');
    grid.className = 'tcard-grid package-card-grid';

    const starter = document.createElement('button');
    starter.type = 'button';
    starter.className = 'tcard package package-new free';
    starter.innerHTML =
      `<i class="bi bi-bag-plus tc-icon"></i>` +
      `<strong class="tc-name">Yeni Paket</strong>` +
      `<span class="tc-time">Adisyon aç</span>` +
      `<span class="tc-status">YENİ</span>`;
    starter.onclick = openTakeaway;
    grid.appendChild(starter);

    packages.forEach((order) => {
      const card = document.createElement('button');
      card.type = 'button';
      card.className = 'tcard package busy';
      card.innerHTML =
        `<i class="bi bi-bag tc-icon"></i>` +
        `<strong class="tc-name">Paket #${order.orderNo}</strong>` +
        `<span class="tc-time"><i class="bi bi-clock"></i>${elapsed(order.openedAt)}</span>` +
        `<span class="tc-amt">${fmt(order.total)}</span>` +
        `<span class="tc-status">${orderTypeLabel(order).toLocaleUpperCase('tr')}</span>`;
      card.onclick = () => loadOrder(order.id);
      grid.appendChild(card);
    });

    wrap.appendChild(grid);
  }

  function renderSectionRail(tables, secName, packages) {
    const rail = el('tablesSections');
    if (!rail) return;
    rail.innerHTML = '';
    const counts = new Map();
    tables.forEach((t) => {
      const id = t.sectionId || '__salon';
      const name = (t.sectionId && secName.get(t.sectionId)) || 'Salon';
      const item = counts.get(id) || { name, total: 0, busy: 0 };
      item.total += 1;
      if (tableState(t).status === 'busy') item.busy += 1;
      counts.set(id, item);
    });

    const make = (id, label, meta) => {
      const b = document.createElement('button');
      b.type = 'button';
      b.className = 'tsec-chip' + (state.sectionFilter === id ? ' active' : '') + (id === '__packages' ? ' is-package' : '');
      b.innerHTML = `<span>${escapeHtml(label)}</span><em>${escapeHtml(meta)}</em>`;
      b.onclick = () => { state.sectionFilter = id; renderTableCards(); };
      return b;
    };
    rail.appendChild(make('all', 'Tümü', `${tables.length} masa`));
    rail.appendChild(make('__packages', 'Paket', `${packages.length} açık`));
    [...counts.entries()]
      .sort((a, b) => a[1].name.localeCompare(b[1].name, 'tr'))
      .forEach(([id, item]) => rail.appendChild(make(id, item.name, `${item.busy}/${item.total} dolu`)));
  }

  function catIcon(name) {
    const n = (name || '').toLocaleLowerCase('tr');
    if (/(içecek|icecek|drink|bar|kahve|çay)/.test(n)) return 'bi-cup-straw';
    if (/(tatlı|dessert|pasta)/.test(n)) return 'bi-cake2';
    if (/(çorba|corba|soup|başlangıç|meze)/.test(n)) return 'bi-egg';
    if (/(salata|salad)/.test(n)) return 'bi-flower1';
    if (/(ana|main|yemek|burger|pizza|grill|ızgara)/.test(n)) return 'bi-egg-fried';
    return 'bi-basket3';
  }
  function renderCatRail() {
    const rail = el('catRail');
    rail.innerHTML = '';
    const categories = D.menu.categories || [];
    if (el('catCount')) el('catCount').textContent = String(categories.length);
    const mk = (id, label, icon) => {
      const b = document.createElement('button');
      b.className = 'catbtn' + (state.category === id ? ' active' : '');
      const count = id ? ((D.menu.categories || []).find((c) => c.id === id)?.items || []).length : (D.menu.categories || []).reduce((sum, c) => sum + c.items.length, 0);
      b.innerHTML = `<i class="bi ${icon}"></i><span>${escapeHtml(label)}</span><em>${count}</em>`;
      b.onclick = () => { state.category = id; renderCatRail(); renderMenu(); };
      return b;
    };
    rail.appendChild(mk(null, 'Tümü', 'bi-grid-fill'));
    (D.menu.categories || []).forEach((c) => rail.appendChild(mk(c.id, c.name, catIcon(c.name))));
  }

  function renderContext() {
    const o = state.order, ctx = el('ctxTable');
    if (!o) { ctx.textContent = ''; return; }
    ctx.textContent = tableName(o.tableId);
  }
  function renderMenu() {
    const grid = el('menuGrid');
    grid.innerHTML = '';
    const enabled = !!state.order && state.order.status === 'open';
    const q = state.search.trim().toLocaleLowerCase('tr');
    let rendered = 0;
    (D.menu.categories || []).forEach((c) => {
      if (state.category && c.id !== state.category) return;
      const tint = catColor(c.color);
      c.items.forEach((i) => {
        if (q && !(`${i.name} ${i.description || ''}`).toLocaleLowerCase('tr').includes(q)) return;
        rendered += 1;
        const b = document.createElement('button');
        b.className = 'pos-prod';
        b.innerHTML =
          `<span class="thumb" style="background:${tint.bg};color:${tint.fg}"><i class="bi ${foodIcon(i.name)}"></i></span>` +
          `<span class="body"><span class="pn">${escapeHtml(i.name)}</span>` +
          (i.description ? `<span class="pd">${escapeHtml(i.description)}</span>` : '') +
          `<span class="pp">${fmt(i.price)}</span></span>` +
          `<span class="fab"><i class="bi bi-plus-lg"></i></span>`;
        b.disabled = !enabled;
        b.onclick = () => addItem(i.id);
        grid.appendChild(b);
      });
    });
    if (el('menuCount')) el('menuCount').textContent = `${rendered} ürün`;
    if (!grid.children.length) grid.innerHTML = '<div class="pos-empty"><i class="bi bi-search"></i><p>Ürün bulunamadı</p></div>';
  }

  function renderOrder() {
    const o = state.order, items = el('orderItems'), foot = el('orderFoot'), badge = el('orderStatus');
    if (!o) { items.innerHTML = ''; foot.hidden = true; return; }

    const draft = isDraft(o);
    const currentTable = tableName(o.tableId);
    el('orderTitle').textContent = draft ? `Taslak · ${currentTable}` : `#${o.orderNo} · ${currentTable}`;
    el('orderSub').textContent = draft
      ? 'İlk ürün eklenince adisyon açılır'
      : `${orderTypeLabel(o)} · açılış ${new Date(o.openedAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}`;
    badge.hidden = false;
    badge.textContent = draft ? 'Taslak' : o.status === 'open' ? 'Açık' : o.status === 'closed' ? 'Kapalı' : o.status;
    badge.className = 'status-pill ' + (draft ? 'draft' : o.status === 'open' ? 'open' : 'closed');

    const rows = activeRows(o);
    const pendingCount = rows.filter((i) => i.status === 'pending').length;
    const sentCount = rows.filter((i) => ['in_kitchen', 'ready', 'served'].includes(i.status)).length;
    const flowHint = el('orderFlowHint');
    flowHint.hidden = pendingCount === 0;
    flowHint.className = 'order-flow-hint' + (sentCount > 0 ? ' is-extra' : '');
    flowHint.innerHTML = pendingCount
      ? `<i class="bi ${sentCount > 0 ? 'bi-plus-circle-fill' : 'bi-bell-fill'}"></i>` +
        `<span>${sentCount > 0 ? 'Ek sipariş alındı' : 'Sipariş alındı'} · ${pendingCount} kalem KDS ekranında</span>`
      : '';

    items.innerHTML = '';
    if (!rows.length) items.innerHTML = '<div class="pos-empty"><i class="bi bi-basket3"></i><p>Menüden ürün ekleyin.</p></div>';
    const editable = o.status === 'open';
    rows.forEach((i) => {
      const row = document.createElement('div');
      row.className = 'pos-item' + (i.status === 'pending' ? ' is-new' : '');
      row.innerHTML =
        `<div class="pi-info"><span class="pi-name">${escapeHtml(i.name)}</span>` +
        `<span class="pi-sub"><span class="pi-price">${fmt(i.unitPrice)}</span>` +
        `<span class="pi-badge s-${i.status}">${statusLabel(i.status)}${i.isComp ? ' · İkram' : ''}</span></span></div>` +
        `<div class="pi-right"><span class="pi-line">${fmt(i.lineTotal)}</span></div>`;
      if (editable && !draft) {
        const ctl = document.createElement('div');
        ctl.className = 'pi-ctl';
        ctl.innerHTML =
          `<button class="qbtn" data-a="dec">−</button><span class="qn">${i.quantity}</span>` +
          `<button class="qbtn" data-a="inc">+</button>` +
          `<button class="vbtn comp" data-a="comp" title="İkram"><i class="bi bi-gift"></i></button>` +
          `<button class="vbtn" data-a="void"><i class="bi bi-trash"></i></button>`;
        ctl.querySelector('[data-a=inc]').onclick = () => setQty(i.id, Number(i.quantity) + 1);
        ctl.querySelector('[data-a=dec]').onclick = () => Number(i.quantity) <= 1 ? voidItem(i.id) : setQty(i.id, Number(i.quantity) - 1);
        ctl.querySelector('[data-a=comp]').onclick = () => compItem(i.id);
        ctl.querySelector('[data-a=void]').onclick = () => voidItem(i.id);
        row.appendChild(ctl);
      }
      items.appendChild(row);
    });

    el('tDisc').textContent = '-' + fmt(o.discountTotal);
    el('tTax').textContent = fmt(o.taxTotal);
    const displayTotal = effectiveTotal(o);
    const displaySubtotal = Number(o.subtotal || 0) > 0 ? Number(o.subtotal || 0) : itemTotal(o);
    el('tSub').textContent = fmt(displaySubtotal);
    el('tTotal').textContent = fmt(displayTotal);
    el('payTotalBtn').textContent = fmt(displayTotal);
    foot.hidden = false;
    ['btnDiscount', 'btnPay', 'btnReceipt', 'btnQueuePrint'].forEach((b) => (el(b).disabled = !editable || draft || rows.length === 0));
    el('btnTransfer').disabled = !editable || draft || rows.length === 0;
    ['btnMerge', 'btnDetail'].forEach((b) => (el(b).disabled = !editable || draft));
    el('btnCancel').disabled = !editable || draft;
    const send = el('btnSendKitchen');
    send.disabled = !editable || draft || !pendingCount;
    send.classList.toggle('has-new', pendingCount > 0);
    send.innerHTML = pendingCount
      ? `<i class="bi bi-fire"></i> ${sentCount > 0 ? 'Ek siparişi hazırla' : 'Hazırlamaya al'} (${pendingCount})`
      : '<i class="bi bi-check2-circle"></i> KDS güncel';
    renderContext(); renderMenu();
  }

  function statusLabel(s) { return { pending: 'Alındı', in_kitchen: 'Hazırlanıyor', ready: 'Hazır', served: 'Servis', cancelled: 'İptal', void: 'İptal' }[s] || s; }
  function foodIcon(name) {
    const n = (name || '').toLocaleLowerCase('tr');
    const map = [
      [/(kahve|coffee|latte|espres|çay|tea)/, 'bi-cup-hot'], [/(ayran|süt|milk|kola|cola|soda|gazoz|meşrubat|smoothie|shake)/, 'bi-cup-straw'],
      [/(su|water)/, 'bi-droplet'], [/(bira|beer|şarap|wine|rakı|kokteyl|cocktail)/, 'bi-cup'],
      [/(salata|salad|meyve|sebze)/, 'bi-flower1'], [/(tatlı|dessert|baklava|künefe|sütlaç|dondurma|ice|kek)/, 'bi-cake2'],
      [/(çorba|soup)/, 'bi-cup-hot'],
      [/(burger|köfte|kofte|et|steak|biftek|tavuk|chicken|kanat|kebap|kebab|şiş|balık|fish|döner|dürüm|pide|lahmacun|pizza|makarna|pasta|sandviç|sandwich|tost|patates|fries)/, 'bi-egg-fried'],
    ];
    for (const [re, ico] of map) if (re.test(n)) return ico;
    return 'bi-basket3';
  }
  function catColor(hex) {
    let h = (hex || '').trim();
    if (!/^#?[0-9a-fA-F]{6}$/.test(h)) return { bg: 'var(--brand-tint)', fg: 'var(--brand)' };
    h = h.replace('#', '');
    const r = parseInt(h.slice(0, 2), 16), g = parseInt(h.slice(2, 4), 16), b = parseInt(h.slice(4, 6), 16);
    return { bg: `rgba(${r},${g},${b},0.14)`, fg: `rgb(${Math.round(r * 0.72)},${Math.round(g * 0.72)},${Math.round(b * 0.72)})` };
  }

  async function refreshTables() {
    try {
      const [tables, open] = await Promise.all([getJson('Tables'), getJson('OpenOrders')]);
      state.tables = tables || [];
      state.openOrders = open || [];
      state.openByTable = new Map();
      state.openOrders.forEach((o) => { if (o.tableId) state.openByTable.set(o.tableId, o); });
      if (!el('tablesView').hidden) renderTableCards();
    } catch (e) {   }
  }
  async function loadOrder(id) { try { state.order = await getJson('Order', { id }); showAdisyon(); } catch (e) { toast(e.message, false); } }
  function makeDraft(tableId, orderType) {
    return {
      isDraft: true,
      id: null,
      orderNo: null,
      tableId,
      orderType,
      status: 'open',
      guestCount: 1,
      subtotal: 0,
      discountTotal: 0,
      taxTotal: 0,
      total: 0,
      openedAt: new Date().toISOString(),
      items: []
    };
  }
  function openOrder(tableId) {
    state.order = makeDraft(tableId, 'dinein');
    showAdisyon();
  }
  async function openTakeaway() {
    state.sectionFilter = '__packages';
    state.order = makeDraft(null, 'takeaway');
    showAdisyon();
  }

  async function discardEmptyOrder() {
    const o = state.order;
    if (!o || isDraft(o) || o.status !== 'open') return;
    if (activeRows(o).length > 0 || Number(o.total || 0) > 0) return;
    try {
      await postJson('Cancel', { orderId: o.id, reason: 'Ürün girilmeden kapatıldı' });
      toast('Boş adisyon kapatıldı', true);
    } catch (e) { }
  }

  async function backToTables() {
    await discardEmptyOrder();
    state.order = null;
    await refreshTables();
    showTables();
  }

  async function openInitialRoute() {
    const params = new URLSearchParams(window.location.search);
    const orderId = params.get('orderId');
    const tableId = params.get('tableId');

    if (orderId) {
      await loadOrder(orderId);
      return true;
    }

    if (tableId) {
      const table = state.tables.find((t) => t.id === tableId && t.isActive);
      if (!table) {
        toast('Masa bulunamadı veya pasif.', false);
        return false;
      }

      const open = state.openByTable.get(tableId);
      if (open) await loadOrder(open.id);
      else await openOrder(tableId);
      return true;
    }

    return false;
  }

  async function addItem(menuItemId) {
    if (!state.order) return;
    const draft = isDraft(state.order);
    const draftOrder = draft ? { ...state.order } : null;
    let openedOrder = null;
    try {
      if (draft) {
        openedOrder = await postJson('Open', {
          tableId: state.order.tableId,
          orderType: state.order.orderType || 'dinein',
          guestCount: state.order.guestCount || 1
        });
        state.order = openedOrder;
      }
      const wasActive = (state.order.items || []).some((i) => ['in_kitchen', 'ready', 'served'].includes(i.status));
      state.order = await postJson('AddItem', { orderId: state.order.id, menuItemId, quantity: 1 });
      await refreshTables();
      renderOrder();
      toast(wasActive ? 'Ek sipariş KDS ekranına düştü' : 'Sipariş KDS ekranına düştü', true);
    }
    catch (e) {
      if (draft && openedOrder?.id) {
        try { await postJson('Cancel', { orderId: openedOrder.id, reason: 'Ürün eklenmeden kapatıldı' }); }
        catch (_) { }
      }
      if (draftOrder) state.order = draftOrder;
      await refreshTables();
      renderOrder();
      toast(e.message, false);
    }
  }
  async function setQty(itemId, quantity) { try { state.order = await postJson('SetQty', { itemId, quantity }); renderOrder(); } catch (e) { toast(e.message, false); } }
  async function voidItem(itemId, reason) {
    try {
      state.order = await postJson('VoidItem', { itemId, reason: reason || null });
      await refreshTables();
      renderOrder();
    } catch (e) { toast(e.message, false); }
  }
  async function compItem(itemId) {
    try {
      state.order = await postJson('CompItem', { itemId });
      await refreshTables();
      renderOrder();
      toast('Kalem ikram edildi', true);
    } catch (e) { toast(e.message, false); }
  }
  async function sendToKitchen() {
    if (!state.order) return;
    const pending = (state.order.items || []).filter((i) => i.status === 'pending');
    if (!pending.length) return;
    try {
      for (const i of pending) await postJson('ItemStatus', { itemId: i.id, status: 'in_kitchen' });
      state.order = await getJson('Order', { id: state.order.id });
      renderOrder();
      toast(`${pending.length} kalem mutfağa gönderildi`, true);
    } catch (e) { toast(e.message, false); }
  }
  async function applyDiscount() {
    if (!state.order) return;
    const raw = prompt('İndirim yüzdesi (%):', '10');
    if (raw === null) return;
    const value = Number(raw);
    if (!(value > 0)) return;
    try { state.order = await postJson('Discount', { orderId: state.order.id, type: 'percent', value, reason: 'POS indirimi' }); renderOrder(); toast('İndirim uygulandı', true); }
    catch (e) { toast(e.message, false); }
  }
  async function cancelOrder() {
    if (!state.order || !(await confirmAction('Adisyonu iptal etmek istediğinize emin misiniz?'))) return;
    try { await postJson('Cancel', { orderId: state.order.id, reason: 'POS iptal' }); state.order = null; await refreshTables(); showTables(); toast('Adisyon iptal edildi', true); }
    catch (e) { toast(e.message, false); }
  }
  async function showReceipt() {
    if (!state.order) return;
    try { const r = await getJson('Receipt', { id: state.order.id }); el('receiptText').textContent = r.plainText || ''; bs('receiptModal').show(); }
    catch (e) { toast(e.message, false); }
  }
  async function queuePrint() {
    if (!state.order) return;
    try { await postJson('QueueReceipt', { orderId: state.order.id }); toast('Yazdırma kuyruğa alındı', true); }
    catch (e) { toast(e.message, false); }
  }

  function orderSummaryLabel(order) {
    const name = tableName(order.tableId);
    return `#${order.orderNo} · ${name} · ${fmt(order.total)}`;
  }

  function fillTableSelect(select, options, emptyLabel) {
    select.innerHTML = '';
    if (emptyLabel) {
      const opt = document.createElement('option');
      opt.value = '';
      opt.textContent = emptyLabel;
      select.appendChild(opt);
    }
    options.forEach((t) => {
      const opt = document.createElement('option');
      opt.value = t.id;
      opt.textContent = t.name;
      select.appendChild(opt);
    });
  }

  function freeTables(excludeTableId) {
    return state.tables
      .filter((t) => t.isActive && t.id !== excludeTableId && !summaryHasItems(state.openByTable.get(t.id)))
      .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'tr'));
  }

  function openTransfer() {
    if (!state.order || isDraft(state.order)) return;
    const select = el('transferTableSelect');
    const tables = freeTables(state.order.tableId);
    fillTableSelect(select, tables, null);
    if (!tables.length) {
      select.innerHTML = '<option value="">Boş hedef masa yok</option>';
    }
    el('transferConfirm').disabled = !tables.length;
    bs('transferModal').show();
  }

  async function confirmTransfer() {
    if (!state.order) return;
    const toTableId = el('transferTableSelect').value;
    if (!toTableId) return;
    try {
      state.order = await postJson('Transfer', { orderId: state.order.id, toTableId });
      await refreshTables();
      bs('transferModal').hide();
      renderOrder();
      toast('Masa taşındı', true);
    } catch (e) { toast(e.message, false); }
  }

  function openMerge() {
    if (!state.order || isDraft(state.order)) return;
    const select = el('mergeOrderSelect');
    const orders = state.openOrders
      .filter((o) => o.id !== state.order.id && summaryHasItems(o))
      .sort((a, b) => Date.parse(b.openedAt) - Date.parse(a.openedAt));
    select.innerHTML = '';
    orders.forEach((o) => {
      const opt = document.createElement('option');
      opt.value = o.id;
      opt.textContent = orderSummaryLabel(o);
      select.appendChild(opt);
    });
    if (!orders.length) {
      select.innerHTML = '<option value="">Birleştirilecek açık hesap yok</option>';
    }
    el('mergeConfirm').disabled = !orders.length;
    bs('mergeModal').show();
  }

  async function confirmMerge() {
    if (!state.order) return;
    const sourceOrderId = el('mergeOrderSelect').value;
    if (!sourceOrderId) return;
    try {
      state.order = await postJson('Merge', { orderId: state.order.id, sourceOrderId });
      await refreshTables();
      bs('mergeModal').hide();
      renderOrder();
      toast('Hesaplar birleştirildi', true);
    } catch (e) { toast(e.message, false); }
  }

  function renderDetailModal() {
    const o = state.order;
    if (!o) return;
    const rows = activeRows(o);
    el('detailSummary').innerHTML =
      `<div><small>Adisyon</small><strong>${isDraft(o) ? 'Taslak' : '#' + o.orderNo}</strong></div>` +
      `<div><small>Masa</small><strong>${escapeHtml(tableName(o.tableId))}</strong></div>` +
      `<div><small>Kalem</small><strong>${rows.length}</strong></div>` +
      `<div><small>Toplam</small><strong>${fmt(effectiveTotal(o))}</strong></div>`;

    const items = el('detailItems');
    items.innerHTML = '';
    if (!rows.length) {
      items.innerHTML = '<div class="pos-empty compact"><i class="bi bi-receipt"></i><p>İşlem yapılacak kalem yok.</p></div>';
    } else {
      rows.forEach((i) => {
        const row = document.createElement('div');
        row.className = 'detail-item' + (i.isComp ? ' is-comp' : '');
        row.innerHTML =
          `<label class="detail-check"><input type="checkbox" value="${escapeHtml(i.id)}"><span></span></label>` +
          `<div class="detail-main"><strong>${escapeHtml(i.name)}</strong>` +
          `<small>${i.quantity} x ${fmt(i.unitPrice)} · ${statusLabel(i.status)}${i.isComp ? ' · İkram' : ''}</small></div>` +
          `<div class="detail-line">${fmt(i.lineTotal)}</div>` +
          `<div class="detail-actions">` +
          `<button type="button" data-a="comp" title="İkram"><i class="bi bi-gift"></i></button>` +
          `<button type="button" data-a="void" title="İptal"><i class="bi bi-trash"></i></button>` +
          `</div>`;
        row.querySelector('[data-a=comp]').onclick = async () => {
          if (!(await confirmAction('Seçili kalem ikram olarak işaretlensin mi?'))) return;
          await compItem(i.id);
          renderDetailModal();
        };
        row.querySelector('[data-a=void]').onclick = async () => {
          if (!(await confirmAction('Seçili kalem iptal edilsin mi?'))) return;
          const reason = prompt('İptal nedeni:', '');
          if (reason === null) return;
          await voidItem(i.id, reason);
          renderDetailModal();
        };
        items.appendChild(row);
      });
    }

    fillTableSelect(el('splitTableSelect'), freeTables(o.tableId), 'Masasız yeni adisyon');
  }

  function openDetail() {
    if (!state.order || isDraft(state.order)) return;
    renderDetailModal();
    bs('detailModal').show();
  }

  async function confirmSplit() {
    if (!state.order) return;
    const itemIds = [...el('detailItems').querySelectorAll('input[type=checkbox]:checked')].map((x) => x.value);
    if (!itemIds.length) {
      toast('Ayırmak için kalem seçin', false);
      return;
    }
    const sourceOrderId = state.order.id;
    const toTableId = el('splitTableSelect').value || null;
    try {
      const newOrder = await postJson('Split', { orderId: sourceOrderId, itemIds, toTableId });
      await refreshTables();
      state.order = newOrder;
      bs('detailModal').hide();
      renderOrder();
      toast('Seçili kalemler yeni adisyona ayrıldı', true);
    } catch (e) { toast(e.message, false); }
  }

  function updatePayPreview() {
    const total = state.order ? effectiveTotal(state.order) : 0;
    const amount = parseAmount(el('payAmount')?.value);
    if (el('payEntered')) el('payEntered').textContent = fmt(amount);
    if (el('payChange')) el('payChange').textContent = fmt(Math.max(0, amount - total));
    if (el('payConfirm')) el('payConfirm').disabled = !(amount > 0);
  }

  function renderQuickAmounts(total) {
    const wrap = el('payQuick'); wrap.innerHTML = '';
    const candidates = [total, 100, 200, 500, 1000]
      .filter((value, index, arr) => value >= total && arr.indexOf(value) === index)
      .slice(0, 5);
    const add = (label, val, exact) => {
      const b = document.createElement('button');
      b.type = 'button';
      b.className = 'pay-quick-btn' + (exact ? ' exact' : '');
      b.innerHTML = exact ? `<i class="bi bi-bullseye"></i><span>${label}</span>` : `<span>${label}</span>`;
      b.onclick = () => setPayAmount(val, false);
      wrap.appendChild(b);
    };
    add('Tam', total, true);
    candidates.filter((value) => value !== total).forEach((value) => add(fmt(value).replace(',00', ''), value, false));
  }

  function renderPaySummary(order) {
    const rows = activeRows(order);
    const source = tableName(order.tableId);
    const type = orderTypeLabel(order);
    el('payOrderMeta').textContent = `${source} · ${type} · ${rows.length} kalem`;
    el('payOrderTitle').textContent = isDraft(order) ? 'Taslak' : `#${order.orderNo}`;
    el('payOrderType').textContent = type;
    el('paySub').textContent = fmt(Number(order.subtotal || 0) > 0 ? Number(order.subtotal || 0) : itemTotal(order));
    el('payDiscount').textContent = '-' + fmt(order.discountTotal);
    el('payTax').textContent = fmt(order.taxTotal);
    el('payGrand').textContent = fmt(effectiveTotal(order));

    const list = el('payLineList');
    list.innerHTML = '';
    rows.forEach((item) => {
      const row = document.createElement('div');
      row.className = 'pay-line';
      row.innerHTML =
        `<span>${escapeHtml(item.quantity)}</span>` +
        `<div><strong>${escapeHtml(item.name)}</strong><small>${statusLabel(item.status)}</small></div>` +
        `<em>${fmt(item.lineTotal)}</em>`;
      list.appendChild(row);
    });
    if (!rows.length) list.innerHTML = '<div class="pay-empty">Tahsil edilecek kalem yok.</div>';
  }

  function fiscalTerminals() {
    return (state.fiscal?.terminals || []).filter((x) => x.isActive && ['payment', 'fiscal'].includes(x.terminalType));
  }

  function selectedMethod() {
    return (document.querySelector('input[name=pm]:checked') || {}).value || 'cash';
  }

  function renderTerminalSelect() {
    const wrap = el('payTerminalWrap');
    const select = el('payTerminal');
    const hint = el('payTerminalHint');
    if (!wrap || !select || !hint) return;
    const method = selectedMethod();
    const needsTerminal = method === 'card' || method === 'meal_voucher';
    wrap.hidden = !needsTerminal;
    select.innerHTML = '';
    const terminals = fiscalTerminals();
    terminals.forEach((terminal) => {
      const opt = document.createElement('option');
      opt.value = terminal.id;
      opt.textContent = `${terminal.name}${terminal.providerTerminalId ? ' · ' + terminal.providerTerminalId : ''}`;
      select.appendChild(opt);
    });
    select.disabled = !terminals.length;
    hint.textContent = !needsTerminal
      ? ''
      : terminals.length
        ? `${state.fiscal.paymentTerminalProvider || 'POS'} ile işlem gönderilecek`
        : 'Aktif POS cihazı yok. Ayarlar > Mali Entegrasyon ekranından cihaz ekleyin.';
  }

  function setPayBusy(busy) {
    const progress = el('payProgress');
    if (progress) progress.hidden = !busy;
    ['payConfirm', 'payExactBtn'].forEach((id) => { if (el(id)) el(id).disabled = busy || (id === 'payConfirm' && !(parseAmount(el('payAmount')?.value) > 0)); });
    document.querySelectorAll('input[name=pm], #payAmount, #payTerminal, #payKeypad button').forEach((node) => { node.disabled = busy; });
  }

  function openPay() {
    if (!state.order) return;
    const total = effectiveTotal(state.order);
    renderPaySummary(state.order);
    el('payDue').textContent = fmt(total);
    renderQuickAmounts(total);
    setPayAmount(total, false);
    renderTerminalSelect();
    setPayBusy(false);
    bs('payModal').show();
  }

  function handlePayKey(key) {
    const input = el('payAmount');
    if (!input || !state.order) return;
    const total = effectiveTotal(state.order);
    if (key === 'exact') { setPayAmount(total, false); return; }
    if (key === 'clear') { input.value = ''; state.payEntryTouched = true; updatePayPreview(); return; }
    if (key === 'back') { input.value = input.value.slice(0, -1); state.payEntryTouched = true; updatePayPreview(); return; }

    const current = state.payEntryTouched ? input.value : '';
    if (key === ',' && /[,.]/.test(current)) return;
    input.value = (current || (key === ',' ? '0' : '')) + key;
    state.payEntryTouched = true;
    updatePayPreview();
  }

  async function confirmPay() {
    if (!state.order) return;
    const amount = parseAmount(el('payAmount').value);
    const method = selectedMethod();
    if (!(amount > 0)) { toast('Geçerli tutar girin', false); return; }
    const change = amount - effectiveTotal(state.order);
    if (method !== 'cash' && change > 0.005) {
      toast('Kart ve yemek kartında kalan tutardan fazla tahsilat yapılamaz.', false);
      setPayAmount(effectiveTotal(state.order), false);
      return;
    }
    const terminalId = el('payTerminal')?.value || null;
    if ((method === 'card' || method === 'meal_voucher') && !terminalId) {
      toast('Kartlı tahsilat için POS cihazı seçin.', false);
      renderTerminalSelect();
      return;
    }
    try {
      setPayBusy(true);
      const result = await postJson('Pay', {
        orderId: state.order.id,
        method,
        amount,
        tip: 0,
        reference: null,
        terminalId,
        issueEInvoice: false,
        documentType: null,
        buyerName: null,
        buyerTaxNumber: null,
        buyerTaxOffice: null,
        buyerAddress: null,
        buyerCity: null,
        buyerEmail: null,
        notes: null
      });
      bs('payModal').hide();
      state.order = await getJson('Order', { id: state.order.id });
      await refreshTables();
      const msg = result?.userMessage || (state.order.status !== 'open' ? 'Ödeme tamamlandı' : 'Kısmi ödeme alındı');
      if (state.order.status !== 'open') { toast(change > 0.005 ? `${msg} · Para üstü ${fmt(change)}` : msg, true); state.order = null; showTables(); }
      else { renderOrder(); toast(msg, true); }
    } catch (e) { toast(e.message, false); }
    finally { setPayBusy(false); }
  }

  async function init() {
    const t = document.createElement('div'); t.id = 'posToast'; t.className = 'pos-toast'; document.body.appendChild(t);
    state.openOrders = D.openOrders || [];
    state.openOrders.forEach((o) => { if (o.tableId) state.openByTable.set(o.tableId, o); });

    renderCatRail();

    if (el('btnTakeaway')) el('btnTakeaway').onclick = openTakeaway;
    el('btnBackTables').onclick = backToTables;
    el('btnSendKitchen').onclick = sendToKitchen;
    el('btnDiscount').onclick = applyDiscount;
    el('btnReceipt').onclick = showReceipt;
    el('btnQueuePrint').onclick = queuePrint;
    el('btnCancel').onclick = cancelOrder;
    el('btnTransfer').onclick = openTransfer;
    el('transferConfirm').onclick = confirmTransfer;
    el('btnMerge').onclick = openMerge;
    el('mergeConfirm').onclick = confirmMerge;
    el('btnDetail').onclick = openDetail;
    el('splitConfirm').onclick = confirmSplit;
    el('btnPay').onclick = openPay;
    el('payConfirm').onclick = confirmPay;
    el('payExactBtn').onclick = () => { if (state.order) setPayAmount(effectiveTotal(state.order), false); };
    el('payAmount').addEventListener('input', () => { state.payEntryTouched = true; updatePayPreview(); });
    el('payKeypad').addEventListener('click', (e) => {
      const button = e.target.closest('[data-key]');
      if (button) handlePayKey(button.dataset.key);
    });
    document.querySelectorAll('input[name=pm]').forEach((input) => {
      input.addEventListener('change', () => {
        if (!state.order) return;
        const amount = parseAmount(el('payAmount').value);
        const total = effectiveTotal(state.order);
        if (input.value !== 'cash' && amount > total) setPayAmount(total, false);
        else updatePayPreview();
        renderTerminalSelect();
      });
    });
    el('menuSearch').addEventListener('input', (e) => { state.search = e.target.value; renderMenu(); });
    if (el('tableSearch')) el('tableSearch').addEventListener('input', (e) => { state.tableSearch = e.target.value; renderTableCards(); });
    document.querySelectorAll('[data-status]').forEach((button) => {
      button.onclick = () => {
        state.statusFilter = button.dataset.status;
        document.querySelectorAll('[data-status]').forEach((b) => b.classList.toggle('active', b === button));
        renderTableCards();
      };
    });

    try {
      const routed = await openInitialRoute();
      if (!routed) showTables();
    } catch (e) {
      showTables();
      toast(e.message, false);
    }

    setInterval(refreshTables, 15000);
  }
  document.addEventListener('DOMContentLoaded', init);
})();
