// Theme Manager
window.themeManager = {
    init: function () {
        const savedTheme = localStorage.getItem('theme') || 'light';
        this.setTheme(savedTheme);
        
        // Watch for system theme changes if auto
        if (savedTheme === 'auto') {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
                this.applyTheme(e.matches ? 'dark' : 'light');
            });
        }
    },
    
    setTheme: function (theme) {
        if (theme === 'auto') {
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            this.applyTheme(prefersDark ? 'dark' : 'light');
        } else {
            this.applyTheme(theme);
        }
        localStorage.setItem('theme', theme);
    },
    
    applyTheme: function (theme) {
        if (theme === 'dark') {
            document.documentElement.setAttribute('data-theme', 'dark');
        } else {
            document.documentElement.removeAttribute('data-theme');
        }
        
        // Smooth transition
        document.documentElement.style.transition = 'background 0.3s ease, color 0.3s ease';
    },
    
    toggleTheme: function () {
        const currentTheme = localStorage.getItem('theme') || 'light';
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        this.setTheme(newTheme);
        return newTheme;
    },
    
    getCurrentTheme: function () {
        return localStorage.getItem('theme') || 'light';
    }
};

// Navbar scroll effect
window.navbarManager = {
    init: function () {
        const navbar = document.querySelector('.modern-navbar');
        if (!navbar) return;
        
        let lastScroll = 0;
        window.addEventListener('scroll', () => {
            const currentScroll = window.pageYOffset;
            
            if (currentScroll > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
            
            lastScroll = currentScroll;
        });
    },
    
    toggleMobileMenu: function () {
        const collapse = document.querySelector('.navbar-collapse');
        const backdrop = document.querySelector('.navbar-backdrop');
        
        if (collapse && backdrop) {
            const isShowing = collapse.classList.contains('show');
            
            if (isShowing) {
                collapse.classList.remove('show');
                backdrop.classList.remove('show');
                document.body.style.overflow = '';
            } else {
                collapse.classList.add('show');
                backdrop.classList.add('show');
                document.body.style.overflow = 'hidden';
            }
        }
    },
    
    closeMobileMenu: function () {
        const collapse = document.querySelector('.navbar-collapse');
        const backdrop = document.querySelector('.navbar-backdrop');
        
        if (collapse && backdrop) {
            collapse.classList.remove('show');
            backdrop.classList.remove('show');
            document.body.style.overflow = '';
        }
    }
};

// Dropdown manager
window.dropdownManager = {
    init: function () {
        document.addEventListener('click', (e) => {
            const toggle = e.target.closest('[data-bs-toggle="dropdown"]');
            
            if (toggle) {
                e.preventDefault();
                const menuId = toggle.getAttribute('aria-controls') || 'userDropdown';
                const menu = document.getElementById(menuId);
                
                if (menu) {
                    const isShowing = menu.classList.contains('show');
                    
                    // Close all dropdowns first
                    document.querySelectorAll('.dropdown-menu.show').forEach(m => {
                        m.classList.remove('show');
                    });
                    
                    if (!isShowing) {
                        menu.classList.add('show');
                    }
                }
            } else if (!e.target.closest('.dropdown-menu')) {
                // Click outside - close all dropdowns
                document.querySelectorAll('.dropdown-menu.show').forEach(m => {
                    m.classList.remove('show');
                });
            }
        });
    }
};

// Simple Bootstrap Dropdown initialization
window.initializeDropdowns = function() {
    // Just let Bootstrap handle it naturally
    console.log('Dropdowns ready - Bootstrap will handle them automatically');
};

// Initialize on load
document.addEventListener('DOMContentLoaded', () => {
    themeManager.init();
    navbarManager.init();
});

// Smooth scroll animations
window.addEventListener('DOMContentLoaded', () => {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-fade-in');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);
    
    document.querySelectorAll('.card, .stat-card').forEach(el => {
        observer.observe(el);
    });
});
