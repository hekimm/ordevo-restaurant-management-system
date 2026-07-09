(function () {
  'use strict';

  const codeMessages = new Map([
    ['branch.required', 'Bu işlem için aktif bir şube seçili olmalı.'],
    ['identity.invalid_credentials', 'Giriş bilgileri uyuşmuyor. Lütfen tekrar deneyin.'],
    ['auth.invalid_credentials', 'Giriş bilgileri uyuşmuyor. Lütfen tekrar deneyin.'],
    ['auth.unauthorized', 'Bu işlem için yeniden giriş yapmanız gerekiyor.'],
    ['auth.forbidden', 'Bu işlem için yetkiniz yok.'],
    ['user.name_required', 'Personel adını kontrol edip tekrar deneyin.'],
    ['user.pin_invalid', 'PIN 6 haneli rakamlardan oluşmalı.'],
    ['user.no_roles', 'Personel rolü seçilemedi. Lütfen ayarları kontrol edin.'],
    ['user.invalid_roles', 'Seçilen personel rolü kullanılamıyor.'],
    ['user.not_found', 'Personel kaydı bulunamadı.'],
    ['order.not_found', 'Adisyon bulunamadı veya artık açık değil.'],
    ['order.item_not_found', 'Seçilen adisyon kalemi bulunamadı.'],
    ['order.table_busy', 'Seçilen masada açık bir adisyon var.'],
    ['order.invalid_item', 'Bu ürün şu anda siparişe eklenemiyor.'],
    ['order.invalid_qty', 'Adet bilgisi geçerli değil.'],
    ['order.split_empty', 'Ayırmak için en az bir kalem seçin.'],
    ['fiscal.terminal.required', 'Kartlı tahsilat için POS cihazı seçin.'],
    ['fiscal.terminal.not_found', 'Seçilen POS cihazı bulunamadı veya pasif.'],
    ['fiscal.terminal.failed', 'POS cihazı işlemi tamamlayamadı. Ödeme kaydedilmedi.'],
    ['fiscal.terminal.unreachable', 'POS cihazından yanıt alınamadı. Ödeme kaydedilmedi.'],
    ['einvoice.provider', 'e-Belge sağlayıcısından yanıt alınamadı. Belge daha sonra tekrar gönderilebilir.'],
    ['kds.order_closed', 'Bu adisyon kapalı olduğu için mutfak durumu değiştirilemez.'],
    ['validation.failed', 'Bilgileri kontrol edip tekrar deneyin.'],
    ['not_found', 'Kayıt bulunamadı.'],
    ['conflict', 'Bu işlem mevcut durum nedeniyle tamamlanamadı.'],
  ]);

  function statusMessage(status) {
    switch (Number(status || 0)) {
      case 0:
        return 'Sistemle bağlantı kurulamadı. Lütfen bağlantınızı kontrol edin.';
      case 400:
      case 422:
        return 'Bilgileri kontrol edip tekrar deneyin.';
      case 401:
        return 'Oturumunuz sona ermiş olabilir. Lütfen tekrar giriş yapın.';
      case 403:
        return 'Bu işlem için yetkiniz yok.';
      case 404:
        return 'Aradığınız kayıt bulunamadı.';
      case 409:
        return 'Bu işlem mevcut durum nedeniyle tamamlanamadı.';
      default:
        return Number(status) >= 500
          ? 'İşlem şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.'
          : 'İşlem tamamlanamadı. Lütfen tekrar deneyin.';
    }
  }

  function looksTechnical(value) {
    const text = String(value || '').trim();
    return !text
      || text.length > 180
      || /(^[a-z0-9_.-]+$)|(\bat\s+\w+\.)|(\bline\s+\d+\b)/i.test(text)
      || /exception|stack|trace|ora-|sql|dapper|system\.|\/api\//i.test(text)
      || /^(bad request|unauthorized|forbidden|not found|conflict|internal server error)$/i.test(text);
  }

  function friendlyError(raw, status) {
    if (!raw) return statusMessage(status);
    const value = String(raw).trim();
    const firstPart = value.split(' - ', 1)[0];
    const direct = codeMessages.get(firstPart.toLowerCase());
    if (direct) return direct;
    for (const [code, message] of codeMessages.entries()) {
      if (value.toLowerCase().includes(code)) return message;
    }
    return looksTechnical(value) ? statusMessage(status) : value;
  }

  function ensureToastZone() {
    let zone = document.querySelector('.ordevo-toast-zone');
    if (zone) return zone;
    zone = document.createElement('div');
    zone.className = 'ordevo-toast-zone';
    zone.setAttribute('aria-live', 'polite');
    zone.setAttribute('aria-atomic', 'true');
    document.body.appendChild(zone);
    return zone;
  }

  function toast(message, type) {
    if (!message || !document.body) return;
    const zone = ensureToastZone();
    const kind = type === 'error' ? 'error' : 'success';
    const item = document.createElement('div');
    item.className = `ordevo-toast is-${kind}`;
    item.innerHTML =
      `<i class="bi ${kind === 'error' ? 'bi-exclamation-triangle-fill' : 'bi-check-circle-fill'}"></i>` +
      `<span></span>` +
      `<button type="button" aria-label="Kapat"><i class="bi bi-x-lg"></i></button>`;
    item.querySelector('span').textContent = message;
    item.querySelector('button').onclick = () => item.remove();
    zone.appendChild(item);
    requestAnimationFrame(() => item.classList.add('show'));
    setTimeout(() => {
      item.classList.remove('show');
      setTimeout(() => item.remove(), 180);
    }, kind === 'error' ? 5200 : 3400);
  }

  function confirmDialog(message) {
    return new Promise((resolve) => {
      const backdrop = document.createElement('div');
      backdrop.className = 'ordevo-confirm-backdrop';
      backdrop.innerHTML =
        `<section class="ordevo-confirm" role="dialog" aria-modal="true" aria-labelledby="ordevoConfirmTitle">` +
        `<div class="ordevo-confirm-icon"><i class="bi bi-question-lg"></i></div>` +
        `<div class="ordevo-confirm-copy">` +
        `<h2 id="ordevoConfirmTitle">Onay gerekiyor</h2>` +
        `<p></p>` +
        `</div>` +
        `<div class="ordevo-confirm-actions">` +
        `<button type="button" class="btn btn-light" data-confirm-cancel>Vazgeç</button>` +
        `<button type="button" class="btn btn-dark" data-confirm-ok>Onayla</button>` +
        `</div>` +
        `</section>`;
      backdrop.querySelector('p').textContent = message;

      const close = (answer) => {
        backdrop.classList.remove('show');
        setTimeout(() => {
          backdrop.remove();
          resolve(answer);
        }, 160);
      };

      backdrop.querySelector('[data-confirm-cancel]').onclick = () => close(false);
      backdrop.querySelector('[data-confirm-ok]').onclick = () => close(true);
      backdrop.addEventListener('click', (event) => {
        if (event.target === backdrop) close(false);
      });
      document.addEventListener('keydown', function onKey(event) {
        if (!backdrop.isConnected) {
          document.removeEventListener('keydown', onKey);
          return;
        }
        if (event.key === 'Escape') {
          document.removeEventListener('keydown', onKey);
          close(false);
        }
      });

      document.body.appendChild(backdrop);
      requestAnimationFrame(() => {
        backdrop.classList.add('show');
        backdrop.querySelector('[data-confirm-ok]').focus();
      });
    });
  }

  function replayConfirmedAction(trigger) {
    trigger.dataset.confirmed = 'true';
    if (trigger instanceof HTMLAnchorElement && trigger.href) {
      window.location.href = trigger.href;
      return;
    }

    const form = trigger.form;
    if (form) {
      if (typeof form.requestSubmit === 'function') {
        form.requestSubmit(trigger);
      } else {
        form.submit();
      }
      setTimeout(() => delete trigger.dataset.confirmed, 0);
      return;
    }

    trigger.click();
    setTimeout(() => delete trigger.dataset.confirmed, 0);
  }

  document.addEventListener('click', async (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const trigger = target?.closest('[data-confirm]');
    if (!trigger || trigger.dataset.confirmed === 'true') return;

    const message = trigger.getAttribute('data-confirm');
    if (!message) return;
    event.preventDefault();
    event.stopImmediatePropagation();

    if (await confirmDialog(message)) replayConfirmedAction(trigger);
  });

  function applyListFilters(targetSelector) {
    if (!targetSelector) return;
    const rows = Array.from(document.querySelectorAll(targetSelector));
    const search = document.querySelector(`[data-list-filter="${targetSelector}"]`);
    const status = document.querySelector(`[data-status-filter="${targetSelector}"].active`);
    const query = (search?.value || '').trim().toLocaleLowerCase('tr');
    const statusValue = status?.getAttribute('data-filter-value') || 'all';

    for (const row of rows) {
      const text = (row.getAttribute('data-filter-text') || row.textContent || '').toLocaleLowerCase('tr');
      const rowStatus = row.getAttribute('data-status') || 'all';
      const matchesText = !query || text.includes(query);
      const matchesStatus = statusValue === 'all' || rowStatus === statusValue;
      row.hidden = !(matchesText && matchesStatus);
    }
  }

  document.addEventListener('input', (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const input = target?.closest('[data-list-filter]');
    if (!input) return;
    applyListFilters(input.getAttribute('data-list-filter'));
  });

  document.addEventListener('click', (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const button = target?.closest('[data-status-filter]');
    if (!button) return;
    const targetSelector = button.getAttribute('data-status-filter');
    for (const peer of document.querySelectorAll(`[data-status-filter="${targetSelector}"]`)) {
      peer.classList.toggle('active', peer === button);
    }
    applyListFilters(targetSelector);
  });

  document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-toast-message]').forEach((node) => {
      toast(node.getAttribute('data-toast-message'), node.getAttribute('data-toast-type'));
    });
  });

  window.OrdevoUI = Object.assign(window.OrdevoUI || {}, {
    confirm: confirmDialog,
    friendlyError,
    toast,
  });
})();
