/* GD5-B: Public availability, no customer-identifying booking data. */
(() => {
  const root = document.getElementById('gd5b-availability');
  if (!root) return;
  const vehicleId = root.dataset.vehicleId;
  const start = document.getElementById('gd5b-start');
  const end = document.getElementById('gd5b-end');
  const message = document.getElementById('gd5b-message');
  const forms = [...document.querySelectorAll('form[action*="/Cart/Add"]')];
  const buttons = forms.map(f => f.querySelector('button[type="submit"],button:not([type])')).filter(Boolean);
  const search = new URL(root.dataset.checkUrl, window.location.origin);
  let requestId = 0;
  function updateHidden() {
    for (const form of forms) {
      for (const [name, value] of [['ngayNhanXe', start.value], ['ngayTraXe', end.value]]) {
        let input = form.querySelector(`input[name="${name}"]`);
        if (!input) { input = document.createElement('input'); input.type='hidden'; input.name=name; form.appendChild(input); }
        input.value=value;
      }
    }
  }
  async function check() {
    updateHidden();
    const current = ++requestId;
    if (!start.value || !end.value || new Date(end.value) <= new Date(start.value)) {
      message.textContent = 'Chọn thời gian nhận và trả hợp lệ.';
      buttons.forEach(b => b.disabled=true);
      return;
    }
    buttons.forEach(b => b.disabled=true);
    message.textContent = 'Đang kiểm tra lịch...';
    search.searchParams.set('vehicleId',vehicleId);
    search.searchParams.set('start',start.value);
    search.searchParams.set('end',end.value);
    try {
      const response = await fetch(search.toString(), { credentials:'same-origin' });
      const result = await response.json();
      if (current !== requestId) return;
      message.textContent = result.message || 'Không thể kiểm tra lịch.';
      message.className = result.available ? 'small text-success mt-2' : 'small text-danger mt-2';
      buttons.forEach(b => b.disabled=!result.available);
    } catch (_) {
      if (current !== requestId) return;
      message.textContent = 'Không kết nối được hệ thống lịch. Vui lòng thử lại.';
      buttons.forEach(b => b.disabled=true);
    }
  }
  start.addEventListener('change',check);
  end.addEventListener('change',check);
  if (window.FullCalendar) {
    const calendar = new FullCalendar.Calendar(document.getElementById('gd5b-calendar'), {
      initialView:'dayGridMonth', locale:'vi', firstDay:1, height:'auto',
      headerToolbar:{left:'prev,next today',center:'title',right:'dayGridMonth,timeGridWeek'},
      eventTimeFormat:{hour:'2-digit',minute:'2-digit',hour12:false},
      events:{url:root.dataset.eventsUrl,extraParams:{vehicleId}},
      eventClick:(info)=>info.jsEvent.preventDefault()
    });
    calendar.render();
  }
  check();
})();
