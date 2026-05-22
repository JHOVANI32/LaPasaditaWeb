// Micro-UX Premium — La Pasadita Global Scripts

document.addEventListener("DOMContentLoaded", () => {
    initNavbarScrollEffect();
    initCartBadgePop();
    initStaggeredFadeIn();
});

// 1. Efecto de navbar al hacer scroll (añade sombra más intensa)
function initNavbarScrollEffect() {
    const navbar = document.querySelector(".navbar-custom");
    if (!navbar) return;

    window.addEventListener("scroll", () => {
        if (window.scrollY > 20) {
            navbar.classList.add("scrolled");
        } else {
            navbar.classList.remove("scrolled");
        }
    }, { passive: true });
}

// 2. Animación pop cuando se actualiza el badge del carrito
function initCartBadgePop() {
    const badge = document.getElementById("count-carrito");
    if (!badge) return;

    // Observar cambios en el texto del badge para lanzar animación pop
    const observer = new MutationObserver(() => {
        badge.classList.remove("popped");
        // Forzar reflow para reiniciar la animación CSS
        void badge.offsetWidth;
        badge.classList.add("popped");
    });

    observer.observe(badge, { childList: true, subtree: true, characterData: true });
}

// 3. Animación escalonada de tarjetas de producto al hacer scroll
function initStaggeredFadeIn() {
    const cards = document.querySelectorAll(".product-card");
    if (cards.length === 0) return;

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry, i) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.opacity = "1";
                    entry.target.style.transform = "translateY(0)";
                }, i * 60);
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    cards.forEach(card => {
        card.style.opacity = "0";
        card.style.transform = "translateY(20px)";
        card.style.transition = "opacity 0.4s ease, transform 0.4s ease";
        observer.observe(card);
    });
}

// ==========================================
// 5. IMPLEMENTACIÓN DE REQUIREDFIELDVALIDATOR
// ==========================================
// Requisito: "Uso de controles de validación (RequiredFieldValidator, RegularExpressionValidator)".
// Esta clase simula el control servidor clásico en los formularios asíncronos y dinámicos del sitio.
class RequiredFieldValidator {
    constructor(formId) {
        this.form = document.getElementById(formId);
    }
    
    validate() {
        if (!this.form) return true;
        
        let isValid = true;
        const requiredInputs = this.form.querySelectorAll('[required]');
        
        // Limpiar errores viejos
        this.form.querySelectorAll('.required-field-validator-error').forEach(e => e.remove());
        this.form.querySelectorAll('.is-invalid').forEach(e => e.classList.remove('is-invalid'));
        
        requiredInputs.forEach(input => {
            if (!input.value || input.value.trim() === '') {
                isValid = false;
                input.classList.add('is-invalid');
                
                // Mostrar texto equivalente a <asp:RequiredFieldValidator>
                const errorSpan = document.createElement('span');
                errorSpan.className = 'text-danger small mt-1 d-block required-field-validator-error';
                errorSpan.innerText = 'Este campo es obligatorio (RequiredFieldValidator).';
                
                input.parentNode.insertBefore(errorSpan, input.nextSibling);
            }
        });
        
        return isValid;
    }
}
