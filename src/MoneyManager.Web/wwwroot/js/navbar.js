// Collapses the Bootstrap navbar menu on mobile after navigation.
// This prevents the menu from staying open when changing pages.

(function () {
  function collapseNavbar() {
    try {
      var collapseEl = document.getElementById('navbarNav');
      if (!collapseEl) return;

      // Only collapse if it's currently shown.
      if (!collapseEl.classList.contains('show')) return;

      // Bootstrap 5 collapse API
      if (window.bootstrap && window.bootstrap.Collapse) {
        var instance = window.bootstrap.Collapse.getOrCreateInstance(collapseEl, { toggle: false });
        instance.hide();
      } else {
        // Fallback: remove 'show' class
        collapseEl.classList.remove('show');
      }
    } catch {
      // ignore
    }
  }

  window.moneyManager = window.moneyManager || {};
  window.moneyManager.navbar = window.moneyManager.navbar || {};
  window.moneyManager.navbar.collapse = collapseNavbar;
})();
