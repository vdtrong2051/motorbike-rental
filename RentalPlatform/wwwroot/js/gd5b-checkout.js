/* GD5-B: checkout feedback only; server is the authoritative availability checker. */
(() => {
  const form = document.getElementById('gd5b-checkout-form');
  if (!form) return;
  const cards = [...form.querySelectorAll('.checkout-item')];
  const submit = form.querySelector('button[type="submit"]');
  let pending = 0;
  let generation = 0;
  const check = async () => {
    const mine = ++generation;
    submit.disabled = true;
    const results = await Promise.all(cards.map(async card => {
      const start = card.querySelector('.rental-start')?.value;
      const end = card.querySelector('.rental-end')?.value;
      const output = card.querySelector('.gd5b-check-message');
      const vehicleId = card.dataset.vehicleId;
      if (!start || !end || new Date(end) <= new Date(start)) {
        if (output) output.textContent = 'Khoảng ngày giờ không hợp lệ.';
        return false;
      }
      const url = new URL(form.dataset.availabilityUrl,location.origin);
      url.searchParams.set('vehicleId',vehicleId);
      url.searchParams.set('start',start);
      url.searchParams.set('end',end);
      try {
        const res = await fetch(url, {credentials:'same-origin'});
        const body = await res.json();
        if (mine !== generation) return false;
        if (output) { output.textContent = body.message || 'Không xác định'; output.className = body.available ? 'gd5b-check-message small text-success mt-2' : 'gd5b-check-message small text-danger mt-2'; }
        return !!body.available;
      } catch (_) {
        if (output && mine === generation) output.textContent = 'Chưa kiểm tra được lịch, vui lòng thử lại.';
        return false;
      }
    }));
    if (mine === generation) submit.disabled = !results.every(Boolean);
  };
  let debounce;
  cards.forEach(card => card.querySelectorAll('.rental-start,.rental-end').forEach(input =>
    input.addEventListener('change', () => { clearTimeout(debounce); debounce=setTimeout(check,250); })));
  form.addEventListener('submit', event => { if (submit.disabled) event.preventDefault(); });
  check();
})();
