(() => {
    'use strict';
    const shell = document.querySelector('[data-gd2-demo]');
    if (!shell || shell.dataset.valid !== 'true') return;
    const token = shell.dataset.paymentToken;
    // Remove bearer capability from visible URL/history after the page loads.
    try { window.history.replaceState(null, '', window.location.pathname); } catch (_) { }
    const url = shell.dataset.paymentSubmit;
    const expiryMs = Date.parse(shell.dataset.expiresAt || '');
    const pay = document.getElementById('demo-pay');
    const fail = document.getElementById('demo-fail');
    const result = document.getElementById('demo-result');
    const actions = document.getElementById('demo-actions');
    const countdown = document.getElementById('demo-countdown');
    let busy = false;
    let finished = false;
    const show = (message, success) => {
        result.hidden = false;
        result.textContent = message;
        result.className = 'demo-result ' + (success ? 'demo-result-success' : 'demo-result-error');
    };
    const tick = () => {
        if (finished) return;
        const seconds = Math.max(0, Math.ceil((expiryMs - Date.now()) / 1000));
        countdown.textContent = `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`;
        if (seconds <= 0) {
            finished = true;
            actions.hidden = true;
            show('Phiên đã hết hạn. Vui lòng tạo lần thanh toán mới.', false);
        }
    };
    const submit = async (outcome) => {
        if (busy || finished) return;
        busy = true;
        pay.disabled = fail.disabled = true;
        show('Đang gửi kết quả mô phỏng...', true);
        try {
            const response = await fetch(url, {
                method: 'POST', credentials: 'omit', cache: 'no-store',
                headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                body: JSON.stringify({ token, outcome })
            });
            const payload = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(payload.error || 'Không thể xác nhận giao dịch.');
            finished = true;
            actions.hidden = true;
            show(outcome === 'PAID'
                ? 'Đã xác nhận thanh toán DEMO! Trang thanh toán trên máy tính sẽ tự cập nhật.'
                : 'Đã mô phỏng thanh toán thất bại. Trở lại trang máy tính để thử lại.', outcome === 'PAID');
            // Never write a token to console, URL navigation, or analytics.
        } catch (err) {
            show(err.message || 'Lỗi kết nối. Vui lòng thử lại.', false);
            if (Date.now() < expiryMs) pay.disabled = fail.disabled = false;
        } finally {
            busy = false;
        }
    };
    pay.addEventListener('click', () => submit('PAID'));
    fail.addEventListener('click', () => submit('FAILED'));
    tick();
    setInterval(tick, 1000);
})();
