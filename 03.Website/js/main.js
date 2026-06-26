/* ============================================================
   CJ.Plug Website - Main JavaScript
   Pure vanilla JS, no framework dependencies
   ============================================================ */

(function () {
    'use strict';

    // ----- DOM References -----
    const navbar = document.getElementById('navbar');
    const mobileToggle = document.getElementById('mobileToggle');
    const navLinks = document.getElementById('navLinks');
    const allNavLinks = navLinks.querySelectorAll('a');

    // ----- Navbar Scroll Effect -----
    function onScroll() {
        if (window.scrollY > 10) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    }

    // ----- Mobile Menu Toggle -----
    function toggleMobileMenu() {
        mobileToggle.classList.toggle('active');
        navLinks.classList.toggle('active');
        document.body.style.overflow = navLinks.classList.contains('active') ? 'hidden' : '';
    }

    function closeMobileMenu() {
        mobileToggle.classList.remove('active');
        navLinks.classList.remove('active');
        document.body.style.overflow = '';
    }

    mobileToggle.addEventListener('click', toggleMobileMenu);

    allNavLinks.forEach(function (link) {
        link.addEventListener('click', closeMobileMenu);
    });

    // ----- Scroll Animation (Intersection Observer) -----
    var animatedElements = document.querySelectorAll('[data-animate]');

    if ('IntersectionObserver' in window) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-visible');
                    observer.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.15,
            rootMargin: '0px 0px -40px 0px'
        });

        animatedElements.forEach(function (el) {
            observer.observe(el);
        });
    } else {
        // Fallback: show all immediately
        animatedElements.forEach(function (el) {
            el.classList.add('animate-visible');
        });
    }

    // ----- Feature Card Mouse Glow Effect -----
    var featureCards = document.querySelectorAll('.feature-card');

    featureCards.forEach(function (card) {
        card.addEventListener('mousemove', function (e) {
            var rect = card.getBoundingClientRect();
            var x = ((e.clientX - rect.left) / rect.width) * 100;
            var y = ((e.clientY - rect.top) / rect.height) * 100;
            card.style.setProperty('--mouse-x', x + '%');
            card.style.setProperty('--mouse-y', y + '%');
        });
    });

    // ----- Smooth Scroll for Anchor Links (with offset) -----
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener('click', function (e) {
            var targetId = this.getAttribute('href');
            if (targetId === '#') return;
            var target = document.querySelector(targetId);
            if (target) {
                e.preventDefault();
                var offset = navbar.offsetHeight + 16;
                var targetPosition = target.getBoundingClientRect().top + window.pageYOffset - offset;
                window.scrollTo({
                    top: targetPosition,
                    behavior: 'smooth'
                });
            }
        });
    });

    // ----- Active Nav Link Highlight on Scroll -----
    var sections = document.querySelectorAll('section[id]');

    function updateActiveNavLink() {
        var scrollPos = window.scrollY + navbar.offsetHeight + 80;

        sections.forEach(function (section) {
            var top = section.offsetTop;
            var bottom = top + section.offsetHeight;

            if (scrollPos >= top && scrollPos < bottom) {
                var id = section.getAttribute('id');
                allNavLinks.forEach(function (link) {
                    link.style.color = '';
                    if (link.getAttribute('href') === '#' + id) {
                        link.style.color = 'var(--color-primary-light)';
                    }
                });
            }
        });
    }

    // ----- Combined Scroll Handler (throttled) -----
    var ticking = false;
    function scrollHandler() {
        if (!ticking) {
            window.requestAnimationFrame(function () {
                onScroll();
                updateActiveNavLink();
                ticking = false;
            });
            ticking = true;
        }
    }

    window.addEventListener('scroll', scrollHandler, { passive: true });

    // ----- Initial call -----
    onScroll();

    // ----- Lightbox -----
    var lightbox = document.getElementById('lightbox');
    var lightboxImg = document.getElementById('lightboxImg');

    window.openLightbox = function (src) {
        lightboxImg.src = src;
        lightbox.classList.add('active');
        document.body.style.overflow = 'hidden';
    };

    window.closeLightbox = function () {
        lightbox.classList.remove('active');
        lightboxImg.src = '';
        document.body.style.overflow = '';
    };

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && lightbox.classList.contains('active')) {
            closeLightbox();
        }
    });

})();
