// JavaScript para el panel de administración, CRUD de productos y gestión de pedidos
document.addEventListener("DOMContentLoaded", () => {
    // Si estamos en la página del dashboard, cargamos el inventario inicialmente
    if (document.getElementById("tabla-productos-body")) {
        cargarInventario();
        
        // Cargar pedidos también al cambiar de pestaña
        const pedidosTab = document.getElementById("pedidos-tab");
        if (pedidosTab) {
            pedidosTab.addEventListener("click", cargarPedidosAdmin);
        }

        // Cargar categorías al cambiar de pestaña
        const categoriasTab = document.getElementById("categorias-tab");
        if (categoriasTab) {
            categoriasTab.addEventListener("click", cargarCategorias);
        }
        
        // Agregar listener para guardar producto
        const formProd = document.getElementById("productoForm");
        if (formProd) {
            formProd.addEventListener("submit", guardarProducto);
        }

        // Agregar listener para guardar categoría
        const formCat = document.getElementById("categoriaForm");
        if (formCat) {
            formCat.addEventListener("submit", guardarCategoria);
        }

        // Agregar listener para cambiar estado de pedido
        const formPedido = document.getElementById("pedidoEstadoForm");
        if (formPedido) {
            formPedido.addEventListener("submit", guardarEstadoPedido);
        }

        // Agregar listener para vista previa en vivo de la URL de la imagen
        const imgUrlInput = document.getElementById("imagenUrl");
        if (imgUrlInput) {
            imgUrlInput.addEventListener("input", (e) => {
                const val = e.target.value.trim();
                const imgPrev = document.getElementById("imagenPreview");
                const prevContainer = document.getElementById("previewContainer");
                if (val) {
                    if (imgPrev) imgPrev.src = val;
                    if (prevContainer) prevContainer.classList.remove("d-none");
                } else {
                    if (imgPrev) imgPrev.src = "";
                    if (prevContainer) prevContainer.classList.add("d-none");
                }
            });
        }
    }
});

// ==========================================
// 1. SECCIÓN DE INVENTARIO (CRUD PRODUCTOS)
// ==========================================
let categoriasDisponibles = [];

function cargarInventario() {
    const tabla = document.getElementById("tabla-productos-body");
    if (!tabla) return;

    tabla.innerHTML = `
        <tr>
            <td colspan="7" class="text-center py-4">
                <div class="spinner-border text-success" role="status">
                    <span class="visually-hidden">Cargando...</span>
                </div>
                <p class="mt-2 text-muted small">Cargando inventario de productos...</p>
            </td>
        </tr>`;

    fetch("/api/AdminProductosApi")
        .then(res => {
            if (!res.ok) throw new Error("No se pudo obtener el inventario.");
            return res.json();
        })
        .then(data => {
            categoriasDisponibles = data.categorias;
            
            // Llenar el dropdown del modal
            const selectCategoria = document.getElementById("categoriaId");
            if (selectCategoria) {
                let options = '<option value="">Selecciona una categoría...</option>';
                data.categorias.forEach(cat => {
                    options += `<option value="${cat.id}">${cat.nombre}</option>`;
                });
                selectCategoria.innerHTML = options;
            }

            // Renderizar la tabla de productos
            renderizarTabla(data.productos);
        })
        .catch(err => {
            console.error("Error al cargar inventario:", err);
            tabla.innerHTML = `
                <tr>
                    <td colspan="7" class="text-center text-danger py-4">
                        <i class="bi bi-exclamation-triangle fs-3"></i>
                        <p class="mt-2">Error al cargar productos. Verifica tus credenciales de Administrador.</p>
                    </td>
                </tr>`;
        });
}

function renderizarTabla(productos) {
    const tabla = document.getElementById("tabla-productos-body");
    if (!tabla) return;

    if (productos.length === 0) {
        tabla.innerHTML = `
            <tr>
                <td colspan="7" class="text-center py-4 text-muted">
                    No hay productos registrados en la base de datos.
                </td>
            </tr>`;
        return;
    }

    let html = "";
    productos.forEach(prod => {
        html += `
            <tr class="${prod.activo ? '' : 'table-light text-muted'} align-middle">
                <td><strong>#${prod.id}</strong></td>
                <td>
                    <div class="d-flex align-items-center gap-2">
                        <img src="${prod.imagenUrl || 'https://images.unsplash.com/photo-1542838132-92c53300491e?w=50&q=80'}" alt="${prod.nombre}" style="width: 40px; height: 40px; object-fit: cover; border-radius: 4px;" onerror="this.src='https://images.unsplash.com/photo-1542838132-92c53300491e?w=50&q=80'">
                        <div>
                            <div class="fw-bold text-dark">${prod.nombre}</div>
                            <div class="text-muted small text-truncate" style="max-width: 250px;">${prod.descripcion || 'Sin descripción'}</div>
                        </div>
                    </div>
                </td>
                <td><span class="badge bg-secondary">${prod.nombreCategoria}</span></td>
                <td class="fw-bold">$${prod.precio.toFixed(2)}</td>
                <td>
                    <span class="badge ${prod.stock > 10 ? 'bg-success' : 'bg-warning'} px-2 py-1">
                        ${prod.stock} pzas
                    </span>
                </td>
                <td>
                    <span class="badge ${prod.activo ? 'bg-success bg-opacity-10 text-success' : 'bg-danger bg-opacity-10 text-danger'} px-2 py-1">
                        ${prod.activo ? 'Activo' : 'Inactivo'}
                    </span>
                </td>
                <td>
                    <div class="d-flex gap-2">
                        <button class="btn btn-sm btn-outline-primary" onclick="abrirModalEditar(${prod.id})" title="Editar producto">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="eliminarProducto(${prod.id})" title="Eliminar (desactivar)">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    });

    tabla.innerHTML = html;
}

window.abrirModalCrear = function() {
    document.getElementById("productoForm").reset();
    document.getElementById("productoId").value = "";
    document.getElementById("modalProductoTitulo").textContent = "Nuevo Producto";
    
    // Limpiar campo de archivo e imagen de vista previa
    const imgFile = document.getElementById("imagenFile");
    if (imgFile) imgFile.value = "";
    const prevContainer = document.getElementById("previewContainer");
    if (prevContainer) prevContainer.classList.add("d-none");
    const imgPrev = document.getElementById("imagenPreview");
    if (imgPrev) imgPrev.src = "";
    
    const modalEl = document.getElementById("modalProducto");
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();

    // Attach event listener for auto-upload
    const imgFileEl = document.getElementById("imagenFile");
    if (imgFileEl && !imgFileEl.dataset.listenerAttached) {
        imgFileEl.addEventListener("change", function() {
            if (this.files && this.files.length > 0) {
                window.subirImagenProducto();
            }
        });
        imgFileEl.dataset.listenerAttached = "true";
    }
};

window.abrirModalEditar = function(id) {
    fetch(`/api/AdminProductosApi/${id}`)
        .then(res => {
            if (!res.ok) throw new Error("No se pudo obtener el producto.");
            return res.json();
        })
        .then(prod => {
            document.getElementById("productoId").value = prod.id;
            document.getElementById("nombre").value = prod.nombre;
            document.getElementById("descripcion").value = prod.descripcion || "";
            document.getElementById("precio").value = prod.precio;
            document.getElementById("stock").value = prod.stock;
            document.getElementById("imagenUrl").value = prod.imagenUrl || "";
            document.getElementById("categoriaId").value = prod.categoriaId;
            document.getElementById("activo").checked = prod.activo;

            // Limpiar selector de archivo local
            const imgFile = document.getElementById("imagenFile");
            if (imgFile) imgFile.value = "";

            // Mostrar vista previa si existe imagen
            const prevContainer = document.getElementById("previewContainer");
            const imgPrev = document.getElementById("imagenPreview");
            if (prod.imagenUrl) {
                if (imgPrev) imgPrev.src = prod.imagenUrl;
                if (prevContainer) prevContainer.classList.remove("d-none");
            } else {
                if (imgPrev) imgPrev.src = "";
                if (prevContainer) prevContainer.classList.add("d-none");
            }

            document.getElementById("modalProductoTitulo").textContent = "Editar Producto #" + prod.id;
            
            const modalEl = document.getElementById("modalProducto");
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();

            // Attach event listener for auto-upload
            const imgFileEl = document.getElementById("imagenFile");
            if (imgFileEl && !imgFileEl.dataset.listenerAttached) {
                imgFileEl.addEventListener("change", function() {
                    if (this.files && this.files.length > 0) {
                        window.subirImagenProducto();
                    }
                });
                imgFileEl.dataset.listenerAttached = "true";
            }
        })
        .catch(err => {
            console.error("Error al cargar producto para edición:", err);
            mostrarNotificacionAdmin("No se pudo cargar el producto.", "danger");
        });
};

function guardarProducto(e) {
    e.preventDefault();

    // Validar usando RequiredFieldValidator
    const validator = new RequiredFieldValidator("productoForm");
    if (!validator.validate()) {
        return;
    }

    const id = document.getElementById("productoId").value;
    const nombre = document.getElementById("nombre").value;
    const descripcion = document.getElementById("descripcion").value;
    const precio = parseFloat(document.getElementById("precio").value);
    const stock = parseInt(document.getElementById("stock").value);
    const imagenUrl = document.getElementById("imagenUrl").value;
    const categoriaId = parseInt(document.getElementById("categoriaId").value);
    const activo = document.getElementById("activo").checked;

    if (!nombre || isNaN(precio) || isNaN(stock) || isNaN(categoriaId)) {
        mostrarNotificacionAdmin("Por favor llena todos los campos obligatorios.", "warning");
        return;
    }

    const payload = {
        nombre,
        descripcion,
        precio,
        stock,
        imagenUrl,
        categoriaId,
        activo
    };

    const isEdit = id !== "";
    const url = isEdit ? `/api/AdminProductosApi/${id}` : "/api/AdminProductosApi";
    const method = isEdit ? "PUT" : "POST";

    fetch(url, {
        method: method,
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
    })
    .then(res => {
        if (!res.ok) throw new Error("Error al guardar el producto.");
        return res.json();
    })
    .then(result => {
        mostrarNotificacionAdmin(result.mensaje, "success");
        
        const modalEl = document.getElementById("modalProducto");
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.hide();

        cargarInventario();
    })
    .catch(err => {
        console.error("Error al guardar:", err);
        mostrarNotificacionAdmin("Ocurrió un error al guardar el producto.", "danger");
    });
}

window.eliminarProducto = function(id) {
    if (!confirm(`¿Estás seguro de desactivar el Producto #${id}? Se ocultará del catálogo público sin borrar los datos históricos.`)) {
        return;
    }

    fetch(`/api/AdminProductosApi/${id}`, {
        method: "DELETE"
    })
    .then(res => {
        if (!res.ok) throw new Error("No se pudo desactivar el producto.");
        return res.json();
    })
    .then(result => {
        mostrarNotificacionAdmin(result.mensaje, "warning");
        cargarInventario();
    })
    .catch(err => {
        console.error("Error al eliminar producto:", err);
        mostrarNotificacionAdmin("No se pudo desactivar el producto.", "danger");
    });
};

// ==========================================
// 2. SECCIÓN DE GESTIÓN DE PEDIDOS
// ==========================================

function cargarPedidosAdmin() {
    const tabla = document.getElementById("tabla-pedidos-body");
    if (!tabla) return;

    tabla.innerHTML = `
        <tr>
            <td colspan="7" class="text-center py-4">
                <div class="spinner-border text-success" role="status">
                    <span class="visually-hidden">Cargando...</span>
                </div>
                <p class="mt-2 text-muted small">Cargando pedidos de clientes...</p>
            </td>
        </tr>`;

    fetch("/api/AdminPedidosApi")
        .then(res => {
            if (!res.ok) throw new Error("No se pudieron cargar los pedidos.");
            return res.json();
        })
        .then(pedidos => {
            renderizarTablaPedidos(pedidos);
        })
        .catch(err => {
            console.error("Error al cargar pedidos:", err);
            tabla.innerHTML = `
                <tr>
                    <td colspan="7" class="text-center text-danger py-4">
                        <i class="bi bi-exclamation-triangle fs-3"></i>
                        <p class="mt-2">Error al cargar pedidos en el servidor.</p>
                    </td>
                </tr>`;
        });
}

function renderizarTablaPedidos(pedidos) {
    const tabla = document.getElementById("tabla-pedidos-body");
    if (!tabla) return;

    if (pedidos.length === 0) {
        tabla.innerHTML = `
            <tr>
                <td colspan="7" class="text-center py-4 text-muted">
                    No se han registrado pedidos en el sistema.
                </td>
            </tr>`;
        return;
    }

    let html = "";
    pedidos.forEach(p => {
        let badgeColor = "bg-warning text-dark";
        if (p.estado === "Preparando") badgeColor = "bg-info text-white";
        else if (p.estado === "En Camino" || p.estado === "Enviado") badgeColor = "bg-primary text-white";
        else if (p.estado === "Entregado") badgeColor = "bg-success text-white";
        else if (p.estado === "Cancelado") badgeColor = "bg-danger text-white";

        const fecha = new Date(p.fechaPedido).toLocaleDateString('es-MX', {
            day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
        });

        html += `
            <tr class="align-middle">
                <td><strong>#${p.id}</strong></td>
                <td>
                    <div class="fw-bold text-dark">${p.nombreCliente}</div>
                    <div class="text-muted small"><i class="bi bi-phone"></i> ${p.telefonoCliente}</div>
                </td>
                <td><span class="small text-muted">${fecha}</span></td>
                <td><span class="badge bg-secondary">${p.metodoPago}</span></td>
                <td><span class="badge ${badgeColor} px-2 py-1">${p.estado}</span></td>
                <td class="fw-bold text-success">$${p.total.toFixed(2)}</td>
                <td>
                    <button class="btn btn-sm btn-success fw-bold" onclick="abrirModalPedido(${p.id})">
                        <i class="bi bi-gear-fill"></i> Gestionar
                    </button>
                </td>
            </tr>
        `;
    });

    tabla.innerHTML = html;
}

window.abrirModalPedido = function(id) {
    fetch(`/api/AdminPedidosApi/${id}`)
        .then(res => {
            if (!res.ok) throw new Error("No se pudo obtener el detalle del pedido.");
            return res.json();
        })
        .then(data => {
            document.getElementById("pedidoIdAdmin").value = data.id;
            document.getElementById("detPedidoIdAdmin").textContent = `#${data.id}`;
            document.getElementById("detPedidoCliente").textContent = data.nombreCliente;
            document.getElementById("detPedidoTelefono").textContent = data.telefonoCliente;
            document.getElementById("detPedidoDireccion").textContent = data.direccionEnvio;
            document.getElementById("detPedidoTotal").textContent = `$${data.total.toFixed(2)}`;
            document.getElementById("estadoNuevo").value = data.estado;
            document.getElementById("notasCambio").value = "";

            // Listar artículos
            const listaArticulos = document.getElementById("pedidoArticulosListaAdmin");
            let artHtml = "";
            data.detalles.forEach(art => {
                artHtml += `
                    <div class="d-flex justify-content-between align-items-center mb-2 border-bottom pb-2 small">
                        <span><strong>${art.cantidad}x</strong> ${art.nombreProducto}</span>
                        <span class="text-muted fw-bold">$${art.subtotal.toFixed(2)}</span>
                    </div>
                `;
            });
            listaArticulos.innerHTML = artHtml;

            // Listar bitácora histórica
            const bitacora = document.getElementById("pedidoBitacoraAdmin");
            let bitHtml = "";
            data.historial.forEach(h => {
                const hFecha = new Date(h.fechaCambio).toLocaleTimeString('es-MX', {
                    hour: '2-digit', minute: '2-digit'
                });
                bitHtml += `
                    <div class="list-group-item bg-transparent py-2">
                        <div class="d-flex justify-content-between align-items-center">
                            <span class="badge bg-secondary bg-opacity-10 text-secondary small">${h.estadoNuevo}</span>
                            <span class="text-muted small" style="font-size: 0.7rem;">${hFecha}</span>
                        </div>
                        <p class="m-0 text-muted" style="font-size: 0.72rem;">${h.notas || 'Sin notas.'}</p>
                    </div>
                `;
            });
            bitacora.innerHTML = bitHtml;

            const modalEl = document.getElementById("modalPedidoAdmin");
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        })
        .catch(err => {
            console.error("Error al cargar pedido:", err);
            mostrarNotificacionAdmin("No se pudo obtener el detalle del pedido.", "danger");
        });
};

function guardarEstadoPedido(e) {
    e.preventDefault();

    // Validar usando RequiredFieldValidator
    const validator = new RequiredFieldValidator("pedidoEstadoForm");
    if (!validator.validate()) {
        return;
    }

    const id = document.getElementById("pedidoIdAdmin").value;
    const estadoNuevo = document.getElementById("estadoNuevo").value;
    const notas = document.getElementById("notasCambio").value.trim();

    if (!estadoNuevo) {
        mostrarNotificacionAdmin("Por favor selecciona un estado válido.", "warning");
        return;
    }

    fetch(`/api/AdminPedidosApi/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ estadoNuevo, notas })
    })
    .then(res => {
        if (!res.ok) return res.json().then(err => { throw new Error(err.mensaje); });
        return res.json();
    })
    .then(result => {
        mostrarNotificacionAdmin(result.mensaje, "success");
        
        const modalEl = document.getElementById("modalPedidoAdmin");
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.hide();

        cargarPedidosAdmin();
    })
    .catch(err => {
        console.error("Error al actualizar estado:", err);
        mostrarNotificacionAdmin(err.message || "Error al actualizar estado.", "danger");
    });
}

// ==========================================
// 3. TOAST DE NOTIFICACIONES
// ==========================================
function mostrarNotificacionAdmin(mensaje, tipo = "success") {
    const toast = document.createElement("div");
    toast.className = `alert alert-${tipo} border-0 shadow-lg text-white fade-in-up`;
    toast.style.position = "fixed";
    toast.style.top = "20px";
    toast.style.right = "20px";
    toast.style.zIndex = "2500";
    toast.style.borderRadius = "12px";
    toast.style.padding = "12px 24px";
    toast.style.fontWeight = "600";
    toast.style.display = "flex";
    toast.style.alignItems = "center";
    toast.style.gap = "10px";

    if (tipo === "success") toast.style.backgroundColor = "#27ae60";
    else if (tipo === "warning") toast.style.backgroundColor = "#e67e22";
    else if (tipo === "danger") toast.style.backgroundColor = "#c0392b";
    else toast.style.backgroundColor = "#2980b9";

    toast.innerHTML = `
        <span>${mensaje}</span>
        <button type="button" class="btn-close btn-close-white ms-2" onclick="this.parentElement.remove()"></button>
    `;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.transition = "opacity 0.5s ease";
        toast.style.opacity = "0";
        setTimeout(() => toast.remove(), 500);
    }, 4000);
}

// ==========================================
// 4. SECCIÓN DE CATEGORÍAS (CRUD CATEGORÍAS)
// ==========================================

function cargarCategorias() {
    const tabla = document.getElementById("tabla-categorias-body");
    if (!tabla) return;

    tabla.innerHTML = `
        <tr>
            <td colspan="6" class="text-center py-4">
                <div class="spinner-border text-success" role="status">
                    <span class="visually-hidden">Cargando...</span>
                </div>
                <p class="mt-2 text-muted small">Cargando catálogo de categorías...</p>
            </td>
        </tr>`;

    fetch("/api/AdminCategoriasApi")
        .then(res => {
            if (!res.ok) throw new Error("No se pudieron cargar las categorías.");
            return res.json();
        })
        .then(data => {
            renderizarTablaCategorias(data);
        })
        .catch(err => {
            console.error("Error al cargar categorías:", err);
            tabla.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-danger py-4">
                        <i class="bi bi-exclamation-triangle fs-3"></i>
                        <p class="mt-2">Error al cargar categorías en el servidor.</p>
                    </td>
                </tr>`;
        });
}

function renderizarTablaCategorias(categorias) {
    const tabla = document.getElementById("tabla-categorias-body");
    if (!tabla) return;

    if (categorias.length === 0) {
        tabla.innerHTML = `
            <tr>
                <td colspan="6" class="text-center py-4 text-muted">
                    No hay categorías registradas en la base de datos.
                </td>
            </tr>`;
        return;
    }

    let html = "";
    categorias.forEach(cat => {
        html += `
            <tr class="${cat.activo ? '' : 'table-light text-muted'} align-middle">
                <td><strong>#${cat.id}</strong></td>
                <td><span class="fw-bold text-dark">${cat.nombre}</span></td>
                <td><span class="small text-muted">${cat.descripcion || 'Sin descripción'}</span></td>
                <td>
                    <span class="badge bg-info text-white px-2 py-1">
                        ${cat.cantProductos} prod.
                    </span>
                </td>
                <td>
                    <span class="badge ${cat.activo ? 'bg-success bg-opacity-10 text-success' : 'bg-danger bg-opacity-10 text-danger'} px-2 py-1">
                        ${cat.activo ? 'Activo' : 'Inactivo'}
                    </span>
                </td>
                <td>
                    <div class="d-flex gap-2">
                        <button class="btn btn-sm btn-outline-primary" onclick="abrirModalEditarCategoria(${cat.id})" title="Editar categoría">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="eliminarCategoria(${cat.id})" title="Desactivar categoría">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    });

    tabla.innerHTML = html;
}

window.abrirModalCrearCategoria = function() {
    document.getElementById("categoriaForm").reset();
    document.getElementById("categoriaIdField").value = "";
    document.getElementById("modalCategoriaTitulo").textContent = "Nueva Categoría";
    
    const modalEl = document.getElementById("modalCategoria");
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
};

window.abrirModalEditarCategoria = function(id) {
    fetch(`/api/AdminCategoriasApi/${id}`)
        .then(res => {
            if (!res.ok) throw new Error("No se pudo obtener la categoría.");
            return res.json();
        })
        .then(cat => {
            document.getElementById("categoriaIdField").value = cat.id;
            document.getElementById("categoriaNombre").value = cat.nombre;
            document.getElementById("categoriaDescripcion").value = cat.descripcion || "";
            document.getElementById("categoriaActivo").checked = cat.activo;

            document.getElementById("modalCategoriaTitulo").textContent = "Editar Categoría #" + cat.id;
            
            const modalEl = document.getElementById("modalCategoria");
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        })
        .catch(err => {
            console.error("Error al cargar categoría para edición:", err);
            mostrarNotificacionAdmin("No se pudo cargar la categoría.", "danger");
        });
};

function guardarCategoria(e) {
    e.preventDefault();

    // Validar usando RequiredFieldValidator
    const validator = new RequiredFieldValidator("categoriaForm");
    if (!validator.validate()) {
        return;
    }

    const id = document.getElementById("categoriaIdField").value;
    const nombre = document.getElementById("categoriaNombre").value.trim();
    const descripcion = document.getElementById("categoriaDescripcion").value.trim();
    const activo = document.getElementById("categoriaActivo").checked;

    if (!nombre) {
        mostrarNotificacionAdmin("El nombre de la categoría es obligatorio.", "warning");
        return;
    }

    const payload = {
        nombre,
        descripcion,
        activo
    };

    const isEdit = id !== "";
    const url = isEdit ? `/api/AdminCategoriasApi/${id}` : "/api/AdminCategoriasApi";
    const method = isEdit ? "PUT" : "POST";

    fetch(url, {
        method: method,
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
    })
    .then(res => {
        if (!res.ok) return res.json().then(err => { throw new Error(err.mensaje || "Error al guardar la categoría."); });
        return res.json();
    })
    .then(result => {
        mostrarNotificacionAdmin(result.mensaje, "success");
        
        const modalEl = document.getElementById("modalCategoria");
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.hide();

        cargarCategorias();
        
        // Si hay una lista de productos activa, recargar el catálogo/inventario para actualizar la lista desplegable de categorías en el formulario de productos
        if (typeof cargarInventario === "function") {
            cargarInventario();
        }
    })
    .catch(err => {
        console.error("Error al guardar categoría:", err);
        mostrarNotificacionAdmin(err.message || "Ocurrió un error al guardar la categoría.", "danger");
    });
}

window.eliminarCategoria = function(id) {
    if (!confirm(`¿Estás seguro de desactivar la Categoría #${id}? Los productos asociados seguirán existiendo, pero la categoría se ocultará para nuevas asignaciones.`)) {
        return;
    }

    fetch(`/api/AdminCategoriasApi/${id}`, {
        method: "DELETE"
    })
    .then(res => {
        if (!res.ok) throw new Error("No se pudo desactivar la categoría.");
        return res.json();
    })
    .then(result => {
        mostrarNotificacionAdmin(result.mensaje, "warning");
        cargarCategorias();
        if (typeof cargarInventario === "function") {
            cargarInventario();
        }
    })
    .catch(err => {
        console.error("Error al eliminar categoría:", err);
        mostrarNotificacionAdmin("No se pudo desactivar la categoría.", "danger");
    });
};

window.subirImagenProducto = function() {
    const fileInput = document.getElementById("imagenFile");
    if (!fileInput || fileInput.files.length === 0) {
        mostrarNotificacionAdmin("Por favor selecciona un archivo de imagen primero.", "warning");
        return;
    }

    const file = fileInput.files[0];
    const formData = new FormData();
    formData.append("imagenFile", file);

    const btn = document.getElementById("btnSubirImagen");
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Subiendo...`;

    fetch("/api/AdminProductosApi/upload", {
        method: "POST",
        body: formData
    })
    .then(res => {
        if (!res.ok) return res.json().then(err => { throw new Error(err.mensaje || "Error al subir la imagen."); });
        return res.json();
    })
    .then(data => {
        mostrarNotificacionAdmin("Imagen subida con éxito.", "success");
        
        // Asignar ruta al input de URL e inicializar la vista previa
        document.getElementById("imagenUrl").value = data.rutaImagen;
        
        const imgPrev = document.getElementById("imagenPreview");
        if (imgPrev) imgPrev.src = data.rutaImagen;
        const prevContainer = document.getElementById("previewContainer");
        if (prevContainer) prevContainer.classList.remove("d-none");
    })
    .catch(err => {
        console.error("Error al subir imagen:", err);
        mostrarNotificacionAdmin(err.message || "Ocurrió un error al subir la imagen.", "danger");
    })
    .finally(() => {
        btn.disabled = false;
        btn.innerHTML = originalText;
    });
};
