/**
 * MoneyKey List Interop
 * Swipe gestures, haptic feedback, animation coordination.
 * Fas A – Phase A
 */
window.listInterop = (function () {

    // WeakMap: element → { handlers, dotnetRef }
    const _registry = new WeakMap();

    function initSwipe(element, dotnetRef) {
        if (!element || _registry.has(element)) return;

        let startX = 0, startY = 0, lastX = 0;
        let tracking = false;

        const THRESHOLD = 72;   // px to trigger action
        const MAX_SLIDE = 94;   // max translation
        const SPRING = 'transform 0.32s cubic-bezier(0.34, 1.56, 0.64, 1)';

        const leftBg = element.parentElement?.querySelector('.swipe-bg-left');
        const rightBg = element.parentElement?.querySelector('.swipe-bg-right');

        function slide(x, animated) {
            element.style.transition = animated ? SPRING : 'none';
            element.style.transform = `translateX(${x}px)`;
        }

        function showBg(dx) {
            if (leftBg) leftBg.style.opacity = dx > 0 ? Math.min(1, dx / THRESHOLD) : 0;
            if (rightBg) rightBg.style.opacity = dx < 0 ? Math.min(1, -dx / THRESHOLD) : 0;
        }

        function reset(animated = true) {
            slide(0, animated);
            showBg(0);
        }

        function onTouchStart(e) {
            if (e.touches.length !== 1) return;
            startX = e.touches[0].clientX;
            startY = e.touches[0].clientY;
            lastX = startX;
            tracking = false;
        }

        function onTouchMove(e) {
            if (e.touches.length !== 1) return;
            const dx = e.touches[0].clientX - startX;
            const dy = e.touches[0].clientY - startY;
            lastX = e.touches[0].clientX;

            if (!tracking) {
                if (Math.abs(dx) < 4 && Math.abs(dy) < 4) return;
                if (Math.abs(dy) > Math.abs(dx)) return; // vertical scroll priority
                tracking = true;
            }

            e.preventDefault();
            const clamped = Math.max(-MAX_SLIDE, Math.min(MAX_SLIDE, dx));
            slide(clamped, false);
            showBg(dx);
        }

        function onTouchEnd() {
            if (!tracking) return;
            tracking = false;

            const dx = lastX - startX;
            reset(true);

            if (dx > THRESHOLD) {
                triggerHaptic(30);
                dotnetRef.invokeMethodAsync('OnSwipeRight');
            } else if (dx < -THRESHOLD) {
                triggerHaptic(30);
                dotnetRef.invokeMethodAsync('OnSwipeLeft');
            }
        }

        element.addEventListener('touchstart', onTouchStart, { passive: true });
        element.addEventListener('touchmove', onTouchMove, { passive: false });
        element.addEventListener('touchend', onTouchEnd, { passive: true });
        element.addEventListener('touchcancel', onTouchEnd, { passive: true });

        _registry.set(element, { onTouchStart, onTouchMove, onTouchEnd });
    }

    function disposeSwipe(element) {
        if (!element || !_registry.has(element)) return;
        const h = _registry.get(element);
        element.removeEventListener('touchstart', h.onTouchStart);
        element.removeEventListener('touchmove', h.onTouchMove);
        element.removeEventListener('touchend', h.onTouchEnd);
        element.removeEventListener('touchcancel', h.onTouchEnd);
        _registry.delete(element);
    }

    function triggerHaptic(duration = 45) {
        if ('vibrate' in navigator) navigator.vibrate(duration);
    }

    /**
     * Trigger the bounce animation on a checkbox element.
     * Uses class-remove → reflow → class-add to ensure re-trigger.
     */
    function bounceCheckbox(element) {
        if (!element) return;
        element.classList.remove('is-bouncing');
        void element.offsetWidth; // force reflow
        element.classList.add('is-bouncing');
        element.addEventListener('animationend', () => {
            element.classList.remove('is-bouncing');
        }, { once: true });
    }

    /**
     * Trigger ripple on checkbox.
     */
    function rippleCheckbox(element) {
        if (!element) return;
        element.classList.remove('is-rippling');
        void element.offsetWidth;
        element.classList.add('is-rippling');
        element.addEventListener('animationend', () => {
            element.classList.remove('is-rippling');
        }, { once: true });
    }

    /**
     * Animate item reset (for packing list reset-all).
     * Staggers each element.
     */
    function animateReset(elements) {
        if (!elements?.length) return;
        elements.forEach((el, i) => {
            setTimeout(() => {
                el.classList.remove('item-resetting');
                void el.offsetWidth;
                el.classList.add('item-resetting');
                el.addEventListener('animationend', () => {
                    el.classList.remove('item-resetting');
                }, { once: true });
            }, i * 45);
        });
    }

    return { initSwipe, disposeSwipe, triggerHaptic, bounceCheckbox, rippleCheckbox, animateReset };
})();