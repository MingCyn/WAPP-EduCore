/* EduCore fx.js: pointer spotlight, button ripple, nav scroll state.
   Plain vanilla JS, no dependencies. Load once per page, before </body>. fx.js file */

(function () {
    'use strict';

    var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Cards that get the mouse-following glow (sets --mx / --my on the element)
    var SPOT = [
        '.course-card', '.path-card', '.stat-box', '.step',            // landing
        '.course', '.card', '.stat', '.gr-stat', '.gr-card', '.panel', // student
        '.lesson-row', '.access-card', '.q-card', '.tutor-card',
        '.fm-card', '.fm-box', '.fx-spot'                              // forum + opt-in
    ].join(',');

    // Buttons that get the click ripple
    var RIPPLE = [
        '.btn-primary', '.btn-primary-large', '.btn-secondary',
        '.btn', '.btn-start', '.btn-solid', '.btn-ghost',
        '.fm-btn', '.cl-btn-solid', '#chatSend'
    ].join(',');

    var raf = null, lastEvt = null;

    function paintSpotlight() {
        raf = null;
        if (!lastEvt) return;
        var el = lastEvt.target.closest ? lastEvt.target.closest(SPOT) : null;
        if (!el) return;
        var r = el.getBoundingClientRect();
        el.style.setProperty('--mx', (lastEvt.clientX - r.left) + 'px');
        el.style.setProperty('--my', (lastEvt.clientY - r.top) + 'px');
    }

    if (!reduce) {
        document.addEventListener('pointermove', function (e) {
            if (e.pointerType === 'touch') return;
            lastEvt = e;
            if (!raf) raf = requestAnimationFrame(paintSpotlight);   // one write per frame
        }, { passive: true });
    }

    document.addEventListener('pointerdown', function (e) {
        if (reduce || !e.target.closest) return;
        var btn = e.target.closest(RIPPLE);
        if (!btn || btn.disabled) return;

        var r = btn.getBoundingClientRect();
        var size = Math.max(r.width, r.height) * 2;
        var dot = document.createElement('span');
        dot.className = 'fx-ripple';
        dot.style.width = dot.style.height = size + 'px';
        dot.style.left = (e.clientX - r.left - size / 2) + 'px';
        dot.style.top = (e.clientY - r.top - size / 2) + 'px';

        if (getComputedStyle(btn).position === 'static') btn.style.position = 'relative';
        btn.style.overflow = 'hidden';
        btn.appendChild(dot);
        dot.addEventListener('animationend', function () { dot.remove(); });
    });

    // Landing nav: stronger glass once the page scrolls
    var nav = document.querySelector('.landing-nav');
    if (nav) {
        var onScroll = function () { nav.classList.toggle('is-scrolled', window.scrollY > 12); };
        onScroll();
        window.addEventListener('scroll', onScroll, { passive: true });
    }
})();