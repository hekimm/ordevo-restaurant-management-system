(function () {
  'use strict';

  const el = (id) => document.getElementById(id);
  const antiforgery = () => (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
  const HUB = window.KDS_HUB || { base: '', token: '' };
  const state = {
    tickets: stampTickets(window.KDS_DATA || []),
    station: null,
    busy: false,
    muted: localStorage.getItem('kdsMuted') === '1',
    prevPending: null,
    noticeTimer: null,
    undo: null,
    undoTimer: null,
    suppressArrival: false,
  };

  async function call(method, handler, body, query) {
    const qs = new URLSearchParams(Object.assign({ handler }, query || {})).toString();
    const opts = { method, headers: { RequestVerificationToken: antiforgery() } };
    if (body !== undefined) {
      opts.headers['Content-Type'] = 'application/json';
      opts.body = JSON.stringify(body);
    }
    const res = await fetch(`/kitchen?${qs}`, opts);
    const text = await res.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { data = null; }
    if (!res.ok) throw new Error(friendlyError(data && data.error, res.status));
    return data;
  }

  const getJson = (h, q) => call('GET', h, undefined, q);
  const postJson = (h, b) => call('POST', h, b || {});
  function friendlyError(message, status) {
    return window.OrdevoUI?.friendlyError
      ? window.OrdevoUI.friendlyError(message, status)
      : 'İşlem tamamlanamadı. Lütfen tekrar deneyin.';
  }
  const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const statusLabels = { pending: 'Yeni', in_kitchen: 'Ocakta', ready: 'Hazır', served: 'Serviste' };

  function stampTickets(tickets) {
    const syncedAt = Date.now();
    return (tickets || []).map((ticket) => Object.assign({}, ticket, {
      _syncedAt: syncedAt,
      items: (ticket.items || []).map((item) => Object.assign({}, item, { _syncedAt: syncedAt })),
    }));
  }

  function activeItems(t) {
    return (t.items || []).filter((i) => !['served', 'void', 'cancelled'].includes(i.status));
  }

  function newItems(t) { return activeItems(t).filter((i) => i.status === 'pending'); }
  function cookingItems(t) { return activeItems(t).filter((i) => i.status === 'in_kitchen'); }
  function readyItems(t) { return activeItems(t).filter((i) => i.status === 'ready'); }
  function itemsWithStatus(t, status) { return activeItems(t).filter((i) => i.status === status); }
  function pendingCount(t) { return newItems(t).length; }
  function hasAdditionalPending(t) { return newItems(t).some((i) => i.isAdditional); }
  function hasAnyAdditional(t) { return activeItems(t).some((i) => i.isAdditional); }

  function secondsSince(value) {
    const parsed = Date.parse(value);
    if (Number.isNaN(parsed)) return 0;
    return Math.max(0, Math.floor((Date.now() - parsed) / 1000));
  }

  function numericSeconds(value) {
    const n = Number(value);
    return Number.isFinite(n) && n >= 0 ? n : null;
  }

  function clockSeconds(seconds) {
    const s = Math.max(0, Math.floor(Number(seconds) || 0));
    return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
  }

  function elapsedSeconds(entity, fallbackTime) {
    const base = numericSeconds(entity && (entity.elapsedSeconds ?? entity.elapsed_seconds));
    if (base !== null) {
      const syncedAt = numericSeconds(entity._syncedAt) || Date.now();
      return Math.max(0, Math.floor(base + ((Date.now() - syncedAt) / 1000)));
    }
    return secondsSince(fallbackTime);
  }

  function shortTime(value) {
    const parsed = Date.parse(value);
    if (Number.isNaN(parsed)) return '--:--';
    return new Date(parsed).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
  }

  function oldestActiveItem(t) {
    return activeItems(t).reduce((oldest, item) => {
      if (!oldest) return item;
      const itemSeconds = elapsedSeconds(item, item.createdAt || item.created_at);
      const oldestSeconds = elapsedSeconds(oldest, oldest.createdAt || oldest.created_at);
      return itemSeconds > oldestSeconds ? item : oldest;
    }, null);
  }

  function ticketElapsedSeconds(t) {
    const item = oldestActiveItem(t);
    if (item) return elapsedSeconds(item, item.createdAt || item.created_at);
    return elapsedSeconds(t, t.openedAt || t.opened_at);
  }

  function ticketStage(t) {
    const items = activeItems(t);
    if (items.some((i) => i.status === 'pending')) return 'received';
    if (items.length && items.every((i) => i.status === 'ready')) return 'ready';
    return 'cooking';
  }

  function ageTone(t) {
    const minutes = Math.floor(ticketElapsedSeconds(t) / 60);
    if (minutes >= 15) return 'late';
    if (minutes >= 8) return 'warn';
    return 'fresh';
  }

  function stageLabel(stage, ticket) {
    if (stage === 'received') return hasAdditionalPending(ticket) ? 'Ek sipariş geldi' : 'Yeni sipariş';
    if (stage === 'ready') return 'Servise hazır';
    return 'Ocakta';
  }

  function statusLabel(status) {
    return statusLabels[status] || status;
  }

  function sourceLabel(t) {
    return t.tableName ? t.tableName : 'Paket';
  }

  let audioCtx;
  function beep() {
    if (state.muted) return;
    try {
      audioCtx = audioCtx || new (window.AudioContext || window.webkitAudioContext)();
      const hit = (freq, at, gain) => {
        const osc = audioCtx.createOscillator();
        const vol = audioCtx.createGain();
        osc.type = 'sine';
        osc.frequency.value = freq;
        osc.connect(vol);
        vol.connect(audioCtx.destination);
        vol.gain.setValueAtTime(0.001, audioCtx.currentTime + at);
        vol.gain.exponentialRampToValueAtTime(gain, audioCtx.currentTime + at + 0.015);
        vol.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + at + 0.18);
        osc.start(audioCtx.currentTime + at);
        osc.stop(audioCtx.currentTime + at + 0.2);
      };
      hit(784, 0, 0.16);
      hit(1046, 0.16, 0.2);
    } catch (e) {
       
    }
  }

  function showArrival(ticket, count, isExtra) {
    const box = el('kdsArrivalNotice');
    if (!box) return;
    el('kdsArrivalTitle').textContent = isExtra ? 'Ek sipariş geldi' : 'Sipariş alındı';
    el('kdsArrivalText').textContent = `${sourceLabel(ticket)} · ${count} ürün`;
    box.classList.toggle('extra', isExtra);
    box.hidden = false;
    box.classList.add('show');
    clearTimeout(state.noticeTimer);
    state.noticeTimer = setTimeout(() => {
      box.classList.remove('show');
      box.hidden = true;
    }, 5200);
  }

  function updateMuteBtn() {
    const btn = el('kdsMute');
    btn.innerHTML = `<i class="bi ${state.muted ? 'bi-volume-mute-fill' : 'bi-volume-up-fill'}"></i>`;
    btn.classList.toggle('off', state.muted);
    btn.setAttribute('aria-label', state.muted ? 'Sesli uyarı kapalı' : 'Sesli uyarı açık');
  }

  function stationTabs() {
    const set = new Set();
    state.tickets.forEach((t) => (t.items || []).forEach((i) => { if (i.station) set.add(i.station); }));
    if (state.station) set.add(state.station);
    const tabs = el('stationTabs');
    tabs.innerHTML = '';

    const make = (value, label) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'kds-tab' + (state.station === value ? ' active' : '');
      button.textContent = label;
      button.onclick = () => {
        state.station = value;
        render();
      };
      return button;
    };

    tabs.appendChild(make(null, 'Tüm mutfak'));
    [...set].sort().forEach((station) => {
      tabs.appendChild(make(station, station.charAt(0).toUpperCase() + station.slice(1)));
    });
  }

  function itemBadge(item) {
    if (item.status === 'pending') return item.isAdditional ? 'EK' : 'YENİ';
    if (item.status === 'ready') return 'HAZIR';
    if (item.status === 'in_kitchen') return 'OCAKTA';
    return item.status.toUpperCase();
  }

  function quantity(value) {
    const n = Number(value || 0);
    return Number.isInteger(n) ? String(n) : n.toLocaleString('tr-TR', { maximumFractionDigits: 2 });
  }

  function changeFor(item, to) {
    return {
      itemId: item.orderItemId,
      itemName: item.itemName,
      from: item.status,
      to,
    };
  }

  function transitionSummary(changes) {
    const groups = new Map();
    changes.forEach((change) => {
      const key = `${change.from}>${change.to}`;
      const group = groups.get(key) || { from: change.from, to: change.to, count: 0 };
      group.count += 1;
      groups.set(key, group);
    });
    return [...groups.values()]
      .map((group) => `${group.count} ürün ${statusLabel(group.from)} → ${statusLabel(group.to)}`)
      .join(', ');
  }

  function actionTitle(action) {
    if (action === 'start') return 'Mutfağa alındı';
    if (action === 'ready') return 'Hazır işaretlendi';
    if (action === 'serve') return 'Servise çıkarıldı';
    return 'Durum güncellendi';
  }

  function changesForAction(ticket, action) {
    if (action === 'start') return newItems(ticket).map((item) => changeFor(item, 'in_kitchen'));
    if (action === 'ready') return cookingItems(ticket).map((item) => changeFor(item, 'ready'));
    if (action === 'serve') return readyItems(ticket).map((item) => changeFor(item, 'served'));
    return [];
  }

  function orderedItems(ticket) {
    return [
      ...newItems(ticket),
      ...cookingItems(ticket),
      ...readyItems(ticket),
    ];
  }

  function stagePills(ticket) {
    const parts = [
      ['pending', 'Yeni', newItems(ticket).length],
      ['cooking', 'Ocakta', cookingItems(ticket).length],
      ['ready', 'Hazır', readyItems(ticket).length],
    ].filter((part) => part[2] > 0);

    return parts.map(([cls, label, count]) =>
      `<span class="kds-stage-pill ${cls}"><em>${count}</em>${label}</span>`).join('');
  }

  function itemRow(item) {
    const cls = ['kds-item', `is-${item.status}`];
    if (item.isAdditional) cls.push('is-extra');
    return `<li class="${cls.join(' ')}">` +
      `<span class="kds-qty">${quantity(item.quantity)}</span>` +
      `<div class="kds-item-main">` +
        `<div class="kds-item-line"><strong>${esc(item.itemName)}</strong><span>${itemBadge(item)}</span></div>` +
        (item.modifiers ? `<div class="kds-mod">${esc(item.modifiers)}</div>` : '') +
        (item.note ? `<div class="kds-note"><i class="bi bi-exclamation-triangle-fill"></i>${esc(item.note)}</div>` : '') +
        (item.station ? `<div class="kds-station">${esc(item.station)}</div>` : '') +
      `</div>` +
    `</li>`;
  }

  function ticketCard(ticket) {
    const stage = ticketStage(ticket);
    const tone = ageTone(ticket);
    const items = activeItems(ticket);
    const extra = hasAdditionalPending(ticket);
    const anyExtra = hasAnyAdditional(ticket);
    const card = document.createElement('article');
    card.className = `kds-ticket-card stage-${stage} tone-${tone}${anyExtra ? ' has-extra' : ''}`;
    card.dataset.orderId = ticket.orderId;

    const action = stage === 'received'
      ? { key: 'start', icon: 'bi-play-fill', label: extra ? `Ek Siparişi Al` : `Mutfağa Al` }
      : stage === 'cooking'
        ? { key: 'ready', icon: 'bi-check2-circle', label: 'Hazır' }
        : { key: 'serve', icon: 'bi-check2-all', label: 'Servise Çıkar' };

    const rows = orderedItems(ticket);
    card.innerHTML =
      `<header class="kds-ticket-head">` +
        `<div class="kds-ticket-main">` +
          `<div class="kds-source-line">` +
            `<strong>${esc(sourceLabel(ticket))} <small>#${ticket.orderNo}</small></strong>` +
            `<span>${ticket.tableName ? 'Masa' : 'Paket'}</span>` +
            (anyExtra ? '<em>EK</em>' : '') +
          `</div>` +
          `<div class="kds-ticket-meta">` +
            `<span>${shortTime(ticket.openedAt)}</span>` +
            `<span>${items.length} ürün</span>` +
          `</div>` +
        `</div>` +
        `<div class="kds-clock">` +
          `<span>Bekleme</span>` +
          `<time class="kds-timer" data-order-id="${esc(ticket.orderId)}">${clockSeconds(ticketElapsedSeconds(ticket))}</time>` +
        `</div>` +
      `</header>` +
      `<div class="kds-status-row">` +
        `<strong>${stageLabel(stage, ticket)}</strong>` +
        `<div class="kds-stage-pills">${stagePills(ticket)}</div>` +
        (tone === 'warn' ? '<em>8 dk+</em>' : tone === 'late' ? '<em>15 dk+</em>' : '') +
      `</div>` +
      `<ul class="kds-item-list kds-ticket-items">${rows.map(itemRow).join('')}</ul>` +
      `<footer class="kds-ticket-actions">` +
        `<button type="button" class="kds-primary-action" data-action="${action.key}"><i class="bi ${action.icon}"></i>${action.label}</button>` +
      `</footer>`;

    card.querySelectorAll('[data-action]').forEach((button) => {
      button.onclick = () => handleAction(ticket, button.dataset.action);
    });
    return card;
  }

  function filteredTickets() {
    return state.tickets
      .map((ticket) => state.station
        ? Object.assign({}, ticket, { items: (ticket.items || []).filter((item) => item.station === state.station) })
        : ticket)
      .filter((ticket) => activeItems(ticket).length)
      .sort((a, b) => ticketElapsedSeconds(b) - ticketElapsedSeconds(a));
  }

  function render() {
    const tickets = filteredTickets();
    const grid = el('kdsGrid');
    grid.innerHTML = '';
    tickets.forEach((ticket) => grid.appendChild(ticketCard(ticket)));

    el('kdsEmpty').hidden = tickets.length > 0;
    el('kdsGrid').hidden = tickets.length === 0;
    const pendingTotal = tickets.reduce((sum, t) => sum + pendingCount(t), 0);
    el('statCount').innerHTML = `<i class="bi bi-receipt"></i>${tickets.length} masa/paket`;
    el('statPending').innerHTML = `<i class="bi bi-bell-fill"></i>${pendingTotal} yeni`;
    el('statOldest').innerHTML = `<i class="bi bi-hourglass-split"></i>${tickets.length ? `${clockSeconds(ticketElapsedSeconds(tickets[0]))} bekleme` : '--:--'}`;

    const cur = new Map(tickets.map((ticket) => [ticket.orderId, {
      count: pendingCount(ticket),
      extra: hasAdditionalPending(ticket),
      ticket,
    }]));
    if (state.prevPending && !state.suppressArrival) {
      let arrived = null;
      cur.forEach((info, id) => {
        const previous = state.prevPending.get(id);
        const prevCount = previous ? previous.count : 0;
        if (info.count > prevCount) {
          arrived = { ticket: info.ticket, count: info.count - prevCount, extra: info.extra };
        }
      });
      if (arrived) {
        showArrival(arrived.ticket, arrived.count, arrived.extra);
        beep();
      }
    }
    state.suppressArrival = false;
    state.prevPending = cur;
  }

  function tickClocks() {
    const visibleTickets = new Map(filteredTickets().map((ticket) => [ticket.orderId, ticket]));
    document.querySelectorAll('.kds-timer').forEach((node) => {
      const ticket = visibleTickets.get(node.getAttribute('data-order-id'));
      node.textContent = ticket ? clockSeconds(ticketElapsedSeconds(ticket)) : '--:--';
    });
  }

  async function setStatus(itemId, status) {
    await postJson('ItemStatus', { itemId, status });
  }

  function hideToast() {
    const box = el('kdsUndoToast');
    if (!box) return;
    clearTimeout(state.undoTimer);
    state.undo = null;
    box.hidden = true;
    box.classList.remove('show', 'is-error', 'is-info');
  }

  function showToast(title, text, changes, kind) {
    const box = el('kdsUndoToast');
    if (!box) return;
    const undoable = Array.isArray(changes) && changes.length > 0;
    state.undo = undoable ? { changes } : null;
    el('kdsUndoTitle').textContent = title;
    el('kdsUndoText').textContent = text;
    el('kdsUndoBtn').hidden = !undoable;
    box.classList.toggle('is-error', kind === 'error');
    box.classList.toggle('is-info', kind !== 'error');
    box.hidden = false;
    requestAnimationFrame(() => box.classList.add('show'));
    clearTimeout(state.undoTimer);
    state.undoTimer = setTimeout(hideToast, undoable ? 9000 : 5200);
  }

  async function applyChanges(changes, title, undoable) {
    if (!changes.length) return;
    for (const change of changes) await setStatus(change.itemId, change.to);
    await refresh({ suppressArrival: true });
    showToast(title, transitionSummary(changes), undoable ? changes : null, 'info');
  }

  async function undoLastChange() {
    if (state.busy) return;
    const snapshot = state.undo;
    if (!snapshot || !snapshot.changes.length) return;

    state.busy = true;
    try {
      const undoChanges = snapshot.changes.map((change) => ({
        itemId: change.itemId,
        itemName: change.itemName,
        from: change.to,
        to: change.from,
      }));
      await applyChanges(undoChanges, 'Geri alındı', false);
    } catch (e) {
      showToast('Geri alma başarısız', e.message, null, 'error');
    } finally {
      state.busy = false;
    }
  }

  async function handleAction(ticket, action) {
    if (state.busy) return;
    const changes = changesForAction(ticket, action);
    if (!changes.length) return;

    state.busy = true;
    try {
      await applyChanges(changes, actionTitle(action), true);
    } catch (e) {
      showToast('Durum değiştirilemedi', e.message, null, 'error');
    } finally {
      state.busy = false;
    }
  }

  let refreshTimer = null;
  function debouncedRefresh() {
    clearTimeout(refreshTimer);
    refreshTimer = setTimeout(refresh, 250);
  }

  async function refresh(options) {
    try {
      state.suppressArrival = Boolean(options && options.suppressArrival);
      const data = await getJson('Board');
      state.tickets = stampTickets(data || []);
      stationTabs();
      render();
    } catch (e) {
      state.suppressArrival = false;
      setLive('Yenileme hatası', 'off');
    }
  }

  function setLive(text, cls) {
    const live = el('kdsLive');
    el('kdsLiveText').textContent = text;
    live.className = 'kds-live ' + (cls || '');
  }

  async function connectRealtime() {
    if (!window.signalR || !HUB.base || !HUB.token) {
      setLive('Periyodik yenileme', 'off');
      return false;
    }

    let connected = 0;
    const build = (path, eventName) => {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${HUB.base}${path}`, { accessTokenFactory: () => HUB.token })
        .withAutomaticReconnect()
        .build();

      connection.on(eventName, debouncedRefresh);
      connection.onreconnected(() => { setLive('Canlı', 'on'); debouncedRefresh(); });
      connection.onclose(() => setLive('Bağlantı bekliyor', 'off'));
      return connection.start()
        .then(() => { connected += 1; setLive('Canlı', 'on'); })
        .catch(() => {});
    };

    await Promise.all([
      build('/hubs/kds', 'ticketChanged'),
      build('/hubs/orders', 'orderChanged'),
    ]);
    return connected > 0;
  }

  async function init() {
    updateMuteBtn();
    el('kdsMute').onclick = () => {
      state.muted = !state.muted;
      localStorage.setItem('kdsMuted', state.muted ? '1' : '0');
      updateMuteBtn();
    };
    el('kdsUndoBtn').onclick = undoLastChange;
    el('kdsUndoClose').onclick = hideToast;
    stationTabs();
    render();
    setInterval(tickClocks, 1000);
    const live = await connectRealtime();
    setInterval(refresh, live ? 15000 : 5000);
  }

  document.addEventListener('DOMContentLoaded', init);
})();
