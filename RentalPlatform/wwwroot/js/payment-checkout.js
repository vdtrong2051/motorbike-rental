(() => {
    'use strict';
    const root = document.querySelector('[data-gd2-payment]');
    if (!root || root.dataset.pending !== 'true') return;
    const statusUrl = root.dataset.statusUrl;
    const expiryMs = Date.parse(root.dataset.expiresAt || '');
    const timer = document.getElementById('gd2-timer');
    const note = document.getElementById('gd2-status-message');
    const retry = document.getElementById('gd2-retry');
    let stopped = false;
    let busy = false;
    const tick = () => {
        if (!timer || !Number.isFinite(expiryMs)) return;
        const seconds = Math.max(0, Math.ceil((expiryMs - Date.now()) / 1000));
        timer.textContent = `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`;
        if (seconds === 0 && note && retry) {
            note.className = 'gd2-message gd2-warning';
            note.textContent = 'Phiên demo đã hết hạn. Tạo giao dịch mới để tiếp tục.';
            retry.hidden = false;
            const qr = root.querySelector('.gd2-qr-frame');
            if (qr) qr.hidden = true;
        }
    };
    const check = async () => {
        if (busy || stopped || document.hidden) return;
        busy = true;
        try {
            const response = await fetch(statusUrl, { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'application/json' } });
            if (response.status === 401 || response.status === 403) {
                stopped = true;
                if (note) note.textContent = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
                return;
            }
            if (!response.ok) return;
            const data = await response.json();
            if (data.paid || ['ThatBai', 'DaHoanTien', 'ChoHoanTien'].includes(data.status) || data.orderStatus === 'DaHuy') {
                stopped = true;
                window.location.reload();
            }
        } catch (_) { /* A temporary network issue is not evidence of payment success. */ }
        finally { busy = false; }
    };
    tick();
    setInterval(tick, 1000);
    setInterval(check, 3000);
    window.addEventListener('focus', check);
})();
