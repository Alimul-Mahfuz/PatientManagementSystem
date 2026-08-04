// Auto-initialise any element with class "pms-select2" as a Select2 widget.
// Use jQuery since Select2 depends on it; runs after DOM is ready.
$(function () {
    $('.pms-select2').select2({
        theme: 'bootstrap-5',
        width: '100%',
        placeholder: function () {
            return $(this).data('placeholder') || '— Select —';
        },
        allowClear: true,
        minimumResultsForSearch: 0  // always show search box (even with few items)
    });
});

// Sidebar toggle: desktop collapses to icon rail; mobile slides off-canvas.
(function () {
  const STORAGE_KEY = 'pms.sidebarCollapsed';
  const body = document.body;
  const toggle = document.getElementById('pmsSidebarToggle');
  const backdrop = document.getElementById('pmsSidebarBackdrop');
  const isMobile = () => window.matchMedia('(max-width: 767.98px)').matches;

  function applyState() {
    const collapsed = localStorage.getItem(STORAGE_KEY) === '1';
    if (collapsed && !isMobile()) {
      body.classList.add('pms-sidebar-collapsed');
      if (toggle) toggle.setAttribute('aria-expanded', 'false');
    } else {
      body.classList.remove('pms-sidebar-collapsed');
      if (toggle) toggle.setAttribute('aria-expanded', 'true');
    }
  }

  if (toggle) {
    toggle.addEventListener('click', function () {
      if (isMobile()) {
        body.classList.toggle('pms-sidebar-open');
        const open = body.classList.contains('pms-sidebar-open');
        toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
      } else {
        const willCollapse = !body.classList.contains('pms-sidebar-collapsed');
        body.classList.toggle('pms-sidebar-collapsed', willCollapse);
        localStorage.setItem(STORAGE_KEY, willCollapse ? '1' : '0');
        toggle.setAttribute('aria-expanded', willCollapse ? 'false' : 'true');
      }
    });
  }

  if (backdrop) {
    backdrop.addEventListener('click', function () {
      body.classList.remove('pms-sidebar-open');
      if (toggle) toggle.setAttribute('aria-expanded', 'false');
    });
  }

  // Close mobile sidebar on navigate / resize to desktop.
  window.addEventListener('resize', function () {
    if (!isMobile()) {
      body.classList.remove('pms-sidebar-open');
      applyState();
    } else {
      body.classList.remove('pms-sidebar-collapsed');
    }
  });

  applyState();
})();