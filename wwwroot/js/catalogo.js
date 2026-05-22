// Funciones globales para el catálogo y carrito silencioso
document.addEventListener("DOMContentLoaded", () => {
    // Inicializar elementos clave
    inicializarTokenInvitado();
    cargarCatalogo();
    configurarEventosCarrito();
});

// 1. Manejo del Token del Carrito Silencioso (Invitados)
function inicializarTokenInvitado() {
    // Si ya existe un token en localStorage, no hacemos nada
    if (localStorage.getItem("lapasadita_guest_token")) {
        cargarCarrito();
        return;
    }

    // Si no existe, solicitamos uno nuevo de forma asíncrona a nuestra API
    fetch("/api/CarritoApi/token", {
        method: "POST",
        headers: { "Content-Type": "application/json" }
    })
    .then(response => {
        if (!response.ok) throw new Error("No se pudo obtener el token.");
        return response.json();
    })
    .then(data => {
        localStorage.setItem("lapasadita_guest_token", data.token);
        cargarCarrito(); // Carga el carrito vacío inicializado
    })
    .catch(error => console.error("Error al inicializar sesión de invitado:", error));
}

// Retorna el token de invitado o null si no se ha inicializado
function obtenerTokenInvitado() {
    return localStorage.getItem("lapasadita_guest_token");
}

// Retorna el ID de usuario si está logueado en la ventana (definido en el layout)
function obtenerUsuarioId() {
    return window.UsuarioId || null;
}

// 2. Cargar Catálogo (Categorías, Productos e Info Tienda) vía AJAX
let listadoProductos = []; // Guardar productos en memoria para filtros rápidos locales

function cargarCatalogo() {
    const contenedorProductos = document.getElementById("contenedor-productos");
    const contenedorFiltros = document.getElementById("contenedor-filtros");
    const nombreTiendaElements = document.querySelectorAll(".info-nombre-tienda");
    const horarioTiendaElements = document.querySelectorAll(".info-horario-tienda");

    if (contenedorProductos) {
        contenedorProductos.innerHTML = `
            <div class="col-12 text-center py-5">
                <div class="spinner-border text-success" role="status">
                    <span class="visually-hidden">Cargando catálogo...</span>
                </div>
                <p class="mt-2 text-muted">Trayendo productos frescos de Axtla...</p>
            </div>`;
    }

    fetch("/api/CatalogoApi")
        .then(res => {
            if (!res.ok) throw new Error("Error al obtener catálogo");
            return res.json();
        })
        .then(data => {
            // Actualizar información general de la tienda
            if (data.configuracion) {
                nombreTiendaElements.forEach(el => el.textContent = data.configuracion.nombreTienda);
                horarioTiendaElements.forEach(el => el.textContent = data.configuracion.horarioAtencion);
            }

            // Guardar en memoria
            listadoProductos = data.productos;

            // Renderizar filtros de categoría
            renderizarFiltros(data.categorias);

            // Renderizar productos
            renderizarProductos(data.productos);
        })
        .catch(err => {
            console.error("Error al cargar catálogo:", err);
            if (contenedorProductos) {
                contenedorProductos.innerHTML = `
                    <div class="col-12 text-center py-5">
                        <div class="alert alert-danger">
                            No se pudo cargar el catálogo de productos. Por favor intenta más tarde.
                        </div>
                    </div>`;
            }
        });
}

// Renderizar dinámicamente los botones de filtro por categoría
function renderizarFiltros(categorias) {
    const contenedorFiltros = document.getElementById("contenedor-filtros");
    if (!contenedorFiltros) return;

    let html = `<button class="filter-btn active" onclick="filtrarCategoria(0, this)">Todos</button>`;
    
    categorias.forEach(cat => {
        html += `<button class="filter-btn" onclick="filtrarCategoria(${cat.id}, this)">${cat.nombre}</button>`;
    });

    contenedorFiltros.innerHTML = html;
}

// Renderizar tarjetas de productos usando Bootstrap 5
function renderizarProductos(productos) {
    const contenedor = document.getElementById("contenedor-productos");
    if (!contenedor) return;

    if (productos.length === 0) {
        contenedor.innerHTML = `
            <div class="col-12 text-center py-5">
                <p class="text-muted fs-5">No se encontraron productos disponibles en este momento.</p>
            </div>`;
        return;
    }

    let html = "";
    const promociones = window.promocionesActivas || [];

    productos.forEach(prod => {
        // En un proyecto real se usaría la imagen guardada. Si es null o placeholder, ponemos una genérica.
        const imgUrl = prod.imagenUrl || "/images/productos/default-grocery.png";
        
        let descuento = 0;
        let promoActiva = promociones.find(p => p.productoId === prod.id);
        if (!promoActiva) {
            promoActiva = promociones.find(p => p.productoId === null);
        }
        
        let precioHtml = `<span class="product-price">$${prod.precio.toFixed(2)}</span>`;
        let badgePromo = '';
        
        if (promoActiva) {
            descuento = promoActiva.descuentoPorcentaje;
            const precioRebajado = prod.precio - (prod.precio * (descuento / 100));
            precioHtml = `
                <div class="d-flex flex-column">
                    <span class="text-muted text-decoration-line-through small" style="font-size: 0.8rem;">$${prod.precio.toFixed(2)}</span>
                    <span class="product-price text-danger" style="font-size: 1.15rem;">$${precioRebajado.toFixed(2)}</span>
                </div>
            `;
            badgePromo = `<div class="position-absolute top-0 start-0 bg-danger text-white px-2 py-1 m-2 fw-bold rounded shadow-sm" style="font-size: 0.75rem; z-index: 2;">-${descuento}% OFERTA</div>`;
        }

        html += `
            <div class="col-12 col-sm-6 col-md-4 col-lg-3 mb-4 fade-in-up">
                <div class="product-card position-relative h-100 d-flex flex-column">
                    ${badgePromo}
                    <div class="product-img-container" style="flex-shrink: 0;">
                        <img src="${imgUrl}" alt="${prod.nombre}" class="product-img" onerror="this.src='https://images.unsplash.com/photo-1542838132-92c53300491e?w=500&q=80'">
                    </div>
                    <div class="product-body d-flex flex-column flex-grow-1">
                        <span class="product-category">ID: #${prod.id}</span>
                        <h5 class="product-title" title="${prod.nombre}">${prod.nombre}</h5>
                        <p class="text-muted small mb-2 text-truncate" style="max-width: 100%; flex-grow: 1;">${prod.descripcion || 'Sin descripción'}</p>
                        <div class="product-price-row mt-auto d-flex justify-content-between align-items-end">
                            ${precioHtml}
                            <button class="btn-add-cart" onclick="agregarProducto(${prod.id})" title="Agregar al carrito">
                                +
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    });

    contenedor.innerHTML = html;
}

// Filtrar productos localmente
window.filtrarCategoria = function(categoriaId, btnElement) {
    // Cambiar clase activa en los botones de filtro
    document.querySelectorAll(".filter-btn").forEach(btn => btn.classList.remove("active"));
    btnElement.classList.add("active");

    if (categoriaId === 0) {
        renderizarProductos(listadoProductos);
    } else {
        const filtrados = listadoProductos.filter(p => p.categoriaId === categoriaId);
        renderizarProductos(filtrados);
    }
};

// Buscador de productos en tiempo real mediante API AJAX
let timeoutBusqueda = null;
window.buscarProductos = function(busqueda) {
    clearTimeout(timeoutBusqueda);

    // Esperar 300ms después de que el usuario deje de escribir (debouncing)
    timeoutBusqueda = setTimeout(() => {
        // Obtener la categoría seleccionada actualmente
        const btnActivo = document.querySelector(".filter-btn.active");
        // Extraer id de la categoría del atributo onclick del botón activo
        let categoriaId = 0;
        if (btnActivo) {
            const match = btnActivo.getAttribute("onclick").match(/\d+/);
            if (match) categoriaId = parseInt(match[0]);
        }

        let url = `/api/CatalogoApi/buscar?q=${encodeURIComponent(busqueda)}`;
        if (categoriaId > 0) {
            url += `&categoriaId=${categoriaId}`;
        }

        fetch(url)
            .then(res => res.json())
            .then(productos => {
                renderizarProductos(productos);
            })
            .catch(err => console.error("Error al buscar productos:", err));
    }, 300);
};


// 3. Operaciones del Carrito (AJAX)
window.agregarProducto = function(productoId, cantidad = 1) {
    const token = obtenerTokenInvitado();
    const usuarioId = obtenerUsuarioId();

    const data = {
        token: token,
        usuarioId: usuarioId,
        productoId: productoId,
        cantidad: cantidad
    };

    fetch("/api/CarritoApi/agregar", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
    })
    .then(res => {
        if (!res.ok) {
            return res.json().then(err => { throw new Error(err.mensaje || "Error al agregar producto"); });
        }
        return res.json();
    })
    .then(result => {
        // Mostrar alerta o notificación sutil al usuario (micro-interacción)
        mostrarNotificacion(result.mensaje, "success");
        cargarCarrito();
        abrirCarrito();
    })
    .catch(err => {
        mostrarNotificacion(err.message, "danger");
    });
};

window.cambiarCantidad = function(productoId, nuevaCantidad) {
    if (nuevaCantidad <= 0) {
        eliminarDelCarrito(productoId);
        return;
    }

    const token = obtenerTokenInvitado();
    const usuarioId = obtenerUsuarioId();

    fetch("/api/CarritoApi/actualizar", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            token,
            usuarioId,
            productoId,
            cantidad: nuevaCantidad
        })
    })
    .then(res => {
        if (!res.ok) {
            return res.json().then(err => { throw new Error(err.mensaje || "Error al actualizar"); });
        }
        return res.json();
    })
    .then(() => {
        cargarCarrito();
    })
    .catch(err => {
        mostrarNotificacion(err.message, "danger");
    });
};

window.eliminarDelCarrito = function(productoId) {
    const token = obtenerTokenInvitado();
    const usuarioId = obtenerUsuarioId();

    fetch("/api/CarritoApi/eliminar", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            token,
            usuarioId,
            productoId,
            cantidad: 0 // No importa para eliminar
        })
    })
    .then(res => {
        if (!res.ok) throw new Error("No se pudo eliminar el producto.");
        return res.json();
    })
    .then(result => {
        mostrarNotificacion(result.mensaje, "warning");
        cargarCarrito();
    })
    .catch(err => console.error("Error al eliminar del carrito:", err));
};

function cargarCarrito() {
    const token = obtenerTokenInvitado();
    const usuarioId = obtenerUsuarioId();

    if (!token && !usuarioId) return;

    let url = `/api/CarritoApi?`;
    if (usuarioId) {
        url += `usuarioId=${usuarioId}`;
    } else {
        url += `token=${token}`;
    }

    fetch(url)
        .then(res => res.json())
        .then(items => {
            renderizarCarrito(items);
        })
        .catch(err => console.error("Error al cargar carrito:", err));
}

function renderizarCarrito(items) {
    const listaCarrito = document.getElementById("lista-carrito");
    const countCarrito = document.getElementById("count-carrito");
    const totalCarrito = document.getElementById("total-carrito");

    if (!listaCarrito) return;

    // Calcular totales locales
    let totalItems = 0;
    let totalPrecio = 0;

    if (items.length === 0) {
        listaCarrito.innerHTML = `
            <div class="text-center py-5 text-muted">
                <i class="bi bi-cart-x fs-1"></i>
                <p class="mt-2">Tu carrito silencioso está vacío.</p>
                <button class="btn btn-outline-success btn-sm mt-2" onclick="cerrarCarrito()">Comenzar a comprar</button>
            </div>`;
        if (countCarrito) countCarrito.textContent = "0";
        if (totalCarrito) totalCarrito.textContent = "$0.00";
        return;
    }

    let html = "";
    items.forEach(item => {
        totalItems += item.cantidad;
        totalPrecio += item.subtotal;

        html += `
            <div class="cart-item">
                <img src="${item.imagenUrl || 'https://images.unsplash.com/photo-1542838132-92c53300491e?w=100&q=80'}" alt="${item.nombreProducto}" class="cart-item-img" onerror="this.src='https://images.unsplash.com/photo-1542838132-92c53300491e?w=100&q=80'">
                <div class="cart-item-info">
                    <h6 class="cart-item-title">${item.nombreProducto}</h6>
                    <div class="cart-item-price">$${item.precioUnitario.toFixed(2)} c/u</div>
                    <div class="cart-item-qty-row">
                        <button class="qty-btn" onclick="cambiarCantidad(${item.productoId}, ${item.cantidad - 1})">-</button>
                        <span class="qty-val">${item.cantidad}</span>
                        <button class="qty-btn" onclick="cambiarCantidad(${item.productoId}, ${item.cantidad + 1})">+</button>
                        <button class="btn-remove-item ms-3" onclick="eliminarDelCarrito(${item.productoId})">
                            Eliminar
                        </button>
                    </div>
                </div>
            </div>
        `;
    });

    listaCarrito.innerHTML = html;
    if (countCarrito) countCarrito.textContent = totalItems;
    if (totalCarrito) totalCarrito.textContent = `$${totalPrecio.toFixed(2)}`;
}

// 4. Lógica de Interfaz del Sidebar Deslizante
function configurarEventosCarrito() {
    const trigger = document.getElementById("btn-abrir-carrito");
    const closeBtn = document.getElementById("btn-cerrar-carrito");
    const overlay = document.getElementById("cart-overlay");

    if (trigger) trigger.addEventListener("click", abrirCarrito);
    if (closeBtn) closeBtn.addEventListener("click", cerrarCarrito);
    if (overlay) overlay.addEventListener("click", cerrarCarrito);
}

window.abrirCarrito = function() {
    const sidebar = document.getElementById("cart-sidebar");
    const overlay = document.getElementById("cart-overlay");
    if (sidebar) sidebar.classList.add("open");
    if (overlay) overlay.classList.add("open");
};

window.cerrarCarrito = function() {
    const sidebar = document.getElementById("cart-sidebar");
    const overlay = document.getElementById("cart-overlay");
    if (sidebar) sidebar.classList.remove("open");
    if (overlay) overlay.classList.remove("open");
};

// 5. Notificaciones Micro-interactivas (Alertas dinámicas)
function mostrarNotificacion(mensaje, tipo = "success") {
    // Remover notificaciones anteriores si existen
    const viejas = document.querySelectorAll(".notif-toast");
    viejas.forEach(n => n.remove());

    // Crear el Toast contenedor
    const toast = document.createElement("div");
    toast.className = `notif-toast alert alert-${tipo} border-0 shadow-lg text-white fade-in-up`;
    toast.style.position = "fixed";
    toast.style.bottom = "20px";
    toast.style.left = "20px";
    toast.style.zIndex = "2000";
    toast.style.borderRadius = "12px";
    toast.style.padding = "12px 24px";
    toast.style.fontWeight = "600";
    toast.style.display = "flex";
    toast.style.alignItems = "center";
    toast.style.gap = "10px";

    // Colores según tipo
    if (tipo === "success") toast.style.backgroundColor = "#27ae60";
    else if (tipo === "warning") toast.style.backgroundColor = "#e67e22";
    else if (tipo === "danger") toast.style.backgroundColor = "#c0392b";
    else toast.style.backgroundColor = "#2980b9";

    toast.innerHTML = `
        <span>${mensaje}</span>
        <button type="button" class="btn-close btn-close-white ms-2" style="font-size: 0.75rem;" onclick="this.parentElement.remove()"></button>
    `;

    document.body.appendChild(toast);

    // Auto-eliminar después de 4 segundos
    setTimeout(() => {
        toast.style.transition = "opacity 0.5s ease";
        toast.style.opacity = "0";
        setTimeout(() => toast.remove(), 500);
    }, 4000);
}
