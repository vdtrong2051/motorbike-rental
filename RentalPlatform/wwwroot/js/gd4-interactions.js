/* GD4 progressive enhancement; all server forms work without this script. */
(() => {
  'use strict';
  const ready = fn => document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', fn) : fn();
  ready(() => {
    // Filter remains a normal GET form: collapsed on mobile, expanded on desktop.
    const filter = document.querySelector('[data-gd4-filter]');
    if (filter) {
      const media = window.matchMedia('(min-width: 768px)');
      const update = () => { filter.open = media.matches; };
      update();
      if (media.addEventListener) media.addEventListener('change', update);
      else media.addListener(update);
    }

    // Make invalid date ordering obvious without changing the submitted field names.
    document.querySelectorAll('form[data-gd4-date-range]').forEach(form => {
      const start = form.querySelector('[name="NgayNhanXe"]');
      const end = form.querySelector('[name="NgayTraXe"]');
      if (!start || !end) return;
      const validate = () => {
        end.setCustomValidity(start.value && end.value && end.value <= start.value
          ? 'Ngày trả xe phải sau ngày nhận xe.' : '');
      };
      start.addEventListener('input', validate);
      end.addEventListener('input', validate);
      form.addEventListener('submit', validate);
    });

    // Mark current public link for screen readers; no server navigation changes.
    const pathname = window.location.pathname.replace(/\/$/, '') || '/';
    document.querySelectorAll('.gd4-public-nav a[href],.gd4-sidebar a[href]').forEach(a => {
      try { const target = new URL(a.href, window.location.origin);
        if (target.pathname.replace(/\/$/, '') === pathname) a.setAttribute('aria-current', 'page');
      } catch { /* non-URL href */ }
    });

    // When a real POST is submitted, avoid accidental double-click; no JS-only routing.
    document.querySelectorAll('form[method="post"]').forEach(form => {
      form.addEventListener('submit', evt => {
        if (evt.defaultPrevented || !form.checkValidity()) return;
        const submitter = evt.submitter;
        if (!submitter || submitter.dataset.gd4Busy === '1') {
          if (submitter?.dataset.gd4Busy === '1') evt.preventDefault();
          return;
        }
        // Defers disabling until after ASP.NET/Bootstrap submit listeners.
        setTimeout(() => { submitter.dataset.gd4Busy = '1'; submitter.setAttribute('aria-busy','true'); }, 0);
      });
    });

    // Dynamic checkout collection uses numbered names; validate each row in place.
    document.querySelectorAll('.checkout-item').forEach(card => {
      const a = card.querySelector('.rental-start');
      const b = card.querySelector('.rental-end');
      if (!a || !b) return;
      const validate = () => b.setCustomValidity(a.value && b.value && b.value <= a.value
        ? 'Thời gian trả xe phải sau thời gian nhận.' : '');
      a.addEventListener('input', validate); b.addEventListener('input', validate);
    });

    // Focus the summary on server validation errors, if any; preserve entered values.
    const errors = document.querySelector('.validation-summary-errors,[data-valmsg-summary="true"] ul li');
    if (errors) {
      const main = document.getElementById('main-content');
      if (main) main.focus({ preventScroll: true });
    }
  });
})();
