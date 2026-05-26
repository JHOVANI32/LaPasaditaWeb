// JavaScript para el panel de administración, CRUD de productos y gestión de pedidos
document.addEventListener("DOMContentLoaded", () => {
    // Si estamos en la página del dashboard, cargamos el inventario inicialmente
    if (document.getElementById("tabla-productos-body")) {
        if (typeof cargarInventario === 'function') {
            cargarInventario();
        }
    }

    // Manejar apertura de pestañas por hash en la URL
    const activarPestanaPorHash = () => {
        try {
            if (window.location.hash) {
                const targetTab = document.querySelector(`[data-bs-target="${window.location.hash}"]`);
                if (targetTab) {
                    // Quitar clase active de todas las pestañas y paneles
                    document.querySelectorAll(".nav-link[data-bs-toggle='tab']").forEach(t => {
                        t.classList.remove("active");
                        t.setAttribute("aria-selected", "false");
                    });
                    document.querySelectorAll(".tab-pane").forEach(p => p.classList.remove("show", "active"));

                    // Añadir clase active a la pestaña objetivo
                    targetTab.classList.add("active");
                    targetTab.setAttribute("aria-selected", "true");
                    const targetPane = document.querySelector(window.location.hash);
                    if (targetPane) {
                        targetPane.classList.add("show", "active");
                    }

                    // Cargar los datos correspondientes manualmente
                    if (window.location.hash === '#promociones-pane' && typeof cargarPromociones === 'function') {
                        cargarPromociones();
                    } else if (window.location.hash === '#categorias-pane' && typeof cargarCategorias === 'function') {
                        cargarCategorias();
                    } else if (window.location.hash === '#pedidos-pane' && typeof cargarPedidosAdmin === 'function') {
                        cargarPedidosAdmin();
                    } else if (window.location.hash === '#configuracion-pane' && typeof cargarConfiguracion === 'function') {
                        cargarConfiguracion();
                    }
                }
            }
        } catch(e) {
            console.error("Error al activar pestaña por hash:", e);
        }
    };
    
    activarPestanaPorHash();
    window.addEventListener("hashchange", activarPestanaPorHash);

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
        // Cargar configuración al cambiar de pestaña
        const configuracionTab = document.getElementById("configuracion-tab");
        if (configuracionTab) {
            configuracionTab.addEventListener("click", cargarConfiguracion);
        }

        // Cargar promociones al cambiar de pestaña
        const promocionesTab = document.getElementById("ofertas-tab"); // NOTA: El id es ofertas-tab
        if (promocionesTab) {
            promocionesTab.addEventListener("click", cargarPromociones);
        }

        // Cargar campañas al cambiar de pestaña
        const campanasTab = document.getElementById("campanas-tab");
        if (campanasTab) {
            campanasTab.addEventListener("click", cargarCampanas);
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

        // Agregar listener para guardar configuración
        const formConfig = document.getElementById("configuracionForm");
        if (formConfig) {
            formConfig.addEventListener("submit", guardarConfiguracion);
        }

        // Agregar listener para guardar promoción
        const formPromocion = document.getElementById("promocionForm");
        if (formPromocion) {
            formPromocion.addEventListener("submit", guardarPromocion);
        }

        // Agregar listener para guardar campaña
        const formCampana = document.getElementById("form-campana");
        if (formCampana) {
            formCampana.addEventListener("submit", guardarCampana);
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

        // Agregar listener para vista previa en vivo del Logo de la tienda
        const logoUrlInput = document.getElementById("configLogoUrl");
        if (logoUrlInput) {
            logoUrlInput.addEventListener("input", (e) => {
                const val = e.target.value.trim();
                const imgEl = document.getElementById("logoPreviewImg");
                const placeholder = document.getElementById("logoPreviewPlaceholder");
                if (val) {
                    if (imgEl) { imgEl.src = val; imgEl.style.display = "block"; }
                    if (placeholder) placeholder.style.display = "none";
                } else {
                    if (imgEl) { imgEl.src = ""; imgEl.style.display = "none"; }
                    if (placeholder) placeholder.style.display = "block";
                }
            });
        }
    // Listener for local logo file upload
    const logoFileInput = document.getElementById('configLogoFile');
    if (logoFileInput) {
        logoFileInput.addEventListener('change', function() {
            const file = this.files[0];
            if (!file) return;
            const formData = new FormData();
            formData.append('logoFile', file);
            fetch('/api/AdminConfiguracionApi/upload-logo', {
                method: 'POST',
                body: formData
            })
            .then(res => {
                if (!res.ok) throw new Error('Error al subir el logo.');
                return res.json();
            })
            .then(data => {
                const urlInput = document.getElementById('configLogoUrl');
                if (urlInput) {
                    urlInput.value = data.logoUrl;
                    const event = new Event('input', { bubbles: true });
                    urlInput.dispatchEvent(event);
                }
            })
            .catch(err => {
                console.error(err);
                alert('No se pudo subir el logo. Verifica el archivo y permisos.');
            });
        });
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

// ═══════════════════════════════════════════════════════════
// GESTIÓN DE CAMPAÑAS DE CUPONES
// ═══════════════════════════════════════════════════════════

window.cargarCampanas = function() {
    const tbody = document.getElementById("tabla-campanas-body");
    if (!tbody) return;

    fetch("/api/AdminCampanasApi")
        .then(res => res.json())
        .then(data => {
            tbody.innerHTML = "";
            if (data.length === 0) {
                tbody.innerHTML = `<tr><td colspan="9" class="text-center py-4 text-muted">No hay campañas registradas.</td></tr>`;
                return;
            }

            data.forEach(campana => {
                const tr = document.createElement("tr");
                const badgeActivo = campana.activo 
                    ? `<span class="badge bg-success">Activa</span>` 
                    : `<span class="badge bg-secondary">Inactiva</span>`;
                
                let recompensaTxt = campana.tipoRecompensa === "Fijo"
                    ? `${campana.valorRecompensaFija}%`
                    : `Sorpresa (${campana.valoresSorpresa})`;

                tr.innerHTML = `
                    <td class="fw-bold">#${campana.id}</td>
                    <td>${campana.titulo}</td>
                    <td>$${campana.montoMinimo.toFixed(2)}</td>
                    <td>${recompensaTxt}</td>
                    <td>${campana.limiteEarlyBird > 0 ? campana.limiteEarlyBird : 'Sin Límite'}</td>
                    <td>${campana.limiteDiario > 0 ? campana.limiteDiario : 'Sin Límite'}</td>
                    <td>${campana.cuponesGeneradosHoy}</td>
                    <td>${badgeActivo}</td>
                    <td class="text-end">
                        <button class="btn btn-sm ${campana.activo ? 'btn-outline-secondary' : 'btn-outline-success'}" onclick="toggleCampanaActivo(${campana.id})" title="${campana.activo ? 'Desactivar' : 'Activar'}">
                            <i class="bi ${campana.activo ? 'bi-pause-circle' : 'bi-play-circle'}"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-primary ms-1" onclick="editarCampana(${campana.id})" title="Editar">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger ms-1" onclick="eliminarCampana(${campana.id})" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        })
        .catch(err => {
            console.error("Error al cargar campañas:", err);
            tbody.innerHTML = `<tr><td colspan="9" class="text-center py-4 text-danger">Error al cargar datos.</td></tr>`;
        });
};

window.abrirModalCampana = function() {
    document.getElementById("form-campana").reset();
    document.getElementById("campanaId").value = "";
    document.getElementById("campanaActivo").checked = true;
    window.toggleRecompensaCampos();
    new bootstrap.Modal(document.getElementById("modalCampana")).show();
};

window.editarCampana = function(id) {
    fetch(`/api/AdminCampanasApi`)
        .then(res => res.json())
        .then(data => {
            const campana = data.find(c => c.id === id);
            if (!campana) return;

            document.getElementById("campanaId").value = campana.id;
            document.getElementById("campanaTitulo").value = campana.titulo;
            document.getElementById("campanaActivo").checked = campana.activo;
            document.getElementById("campanaMonto").value = campana.montoMinimo;
            document.getElementById("campanaLimiteDiario").value = campana.limiteDiario;
            document.getElementById("campanaEarlyBird").value = campana.limiteEarlyBird;
            document.getElementById("campanaTipoRecompensa").value = campana.tipoRecompensa;
            
            if (campana.tipoRecompensa === "Fijo") {
                document.getElementById("campanaValorFijo").value = campana.valorRecompensaFija;
            } else {
                document.getElementById("campanaValoresSorpresa").value = campana.valoresSorpresa;
            }
            
            document.getElementById("campanaMensajeBanner").value = campana.mensajeBanner || "";
            
            window.toggleRecompensaCampos();
            new bootstrap.Modal(document.getElementById("modalCampana")).show();
        })
        .catch(err => console.error("Error:", err));
};

window.toggleRecompensaCampos = function() {
    const tipo = document.getElementById("campanaTipoRecompensa").value;
    if (tipo === "Fijo") {
        document.getElementById("campoFijo").classList.remove("d-none");
        document.getElementById("campoSorpresa").classList.add("d-none");
    } else {
        document.getElementById("campoFijo").classList.add("d-none");
        document.getElementById("campoSorpresa").classList.remove("d-none");
    }
};

window.guardarCampana = function(e) {
    e.preventDefault();
    const id = document.getElementById("campanaId").value;
    const tipo = document.getElementById("campanaTipoRecompensa").value;
    
    const campana = {
        titulo: document.getElementById("campanaTitulo").value,
        activo: document.getElementById("campanaActivo").checked,
        montoMinimo: parseFloat(document.getElementById("campanaMonto").value),
        tipoRecompensa: tipo,
        limiteDiario: parseInt(document.getElementById("campanaLimiteDiario").value),
        limiteEarlyBird: parseInt(document.getElementById("campanaEarlyBird").value),
        mensajeBanner: document.getElementById("campanaMensajeBanner").value
    };

    if (tipo === "Fijo") {
        campana.valorRecompensaFija = parseFloat(document.getElementById("campanaValorFijo").value);
    } else {
        campana.valoresSorpresa = document.getElementById("campanaValoresSorpresa").value;
    }

    if (id) {
        campana.id = parseInt(id);
        fetch(`/api/AdminCampanasApi/${id}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(campana)
        })
        .then(res => {
            if (res.ok) {
                mostrarNotificacionAdmin("Campaña actualizada", "success");
                bootstrap.Modal.getInstance(document.getElementById("modalCampana")).hide();
                cargarCampanas();
            } else {
                mostrarNotificacionAdmin("Error al actualizar", "danger");
            }
        });
    } else {
        fetch("/api/AdminCampanasApi", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(campana)
        })
        .then(res => {
            if (res.ok) {
                mostrarNotificacionAdmin("Campaña creada", "success");
                bootstrap.Modal.getInstance(document.getElementById("modalCampana")).hide();
                cargarCampanas();
            } else {
                mostrarNotificacionAdmin("Error al crear", "danger");
            }
        });
    }
};

window.toggleCampanaActivo = function(id) {
    fetch(`/api/AdminCampanasApi/toggle/${id}`, { method: "PUT" })
        .then(res => res.json())
        .then(data => {
            mostrarNotificacionAdmin(data.message, "success");
            cargarCampanas();
        });
};

window.eliminarCampana = function(id) {
    if (confirm("¿Estás seguro de eliminar esta campaña? Esta acción no se puede deshacer.")) {
        fetch(`/api/AdminCampanasApi/${id}`, { method: "DELETE" })
            .then(res => {
                if (res.ok) {
                    mostrarNotificacionAdmin("Campaña eliminada", "success");
                    cargarCampanas();
                } else {
                    mostrarNotificacionAdmin("Error al eliminar", "danger");
                }
            });
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

// ==========================================
// 5. SECCIÓN DE CONFIGURACIÓN DE LA TIENDA
// ==========================================

function cargarConfiguracion() {
    fetch("/api/AdminConfiguracionApi")
        .then(res => {
            if (!res.ok) throw new Error("No se pudo cargar la configuración.");
            return res.json();
        })
        .then(config => {
            document.getElementById("configNombre").value = config.nombreTienda || "";
            document.getElementById("configCostoEnvio").value = config.costoEnvioBase !== undefined ? config.costoEnvioBase : 15;
            document.getElementById("configTelefono").value = config.telefonoContacto || "";
            document.getElementById("configEmail").value = config.emailContacto || "";
            document.getElementById("configDireccion").value = config.direccionFisica || "";
            document.getElementById("configHorario").value = config.horarioAtencion || "";
            document.getElementById("configSmtpEmail").value = config.smtpEmail || "";
            document.getElementById("configSmtpPassword").value = config.smtpPassword || "";
            document.getElementById("configSmtpHost").value = config.smtpHost || "";
            document.getElementById("configSmtpPort").value = config.smtpPort || "";

            // Logo
            const logoVal = config.logoUrl || "";
            document.getElementById("configLogoUrl").value = logoVal;
            const imgEl = document.getElementById("logoPreviewImg");
            const placeholder = document.getElementById("logoPreviewPlaceholder");
            if (logoVal) {
                if (imgEl) { imgEl.src = logoVal; imgEl.style.display = "block"; }
                if (placeholder) placeholder.style.display = "none";
            } else {
                if (imgEl) { imgEl.src = ""; imgEl.style.display = "none"; }
                if (placeholder) placeholder.style.display = "block";
            }
        })
        .catch(err => {
            console.error("Error al cargar config:", err);
            mostrarNotificacionAdmin("No se pudo obtener la configuración.", "warning");
        });
}

function guardarConfiguracion(e) {
    e.preventDefault();

    const payload = {
        nombreTienda: document.getElementById("configNombre").value,
        costoEnvioBase: parseFloat(document.getElementById("configCostoEnvio").value),
        telefonoContacto: document.getElementById("configTelefono").value,
        emailContacto: document.getElementById("configEmail").value,
        direccionFisica: document.getElementById("configDireccion").value,
        horarioAtencion: document.getElementById("configHorario").value,
        smtpEmail: document.getElementById("configSmtpEmail").value,
        smtpPassword: document.getElementById("configSmtpPassword").value,
        smtpHost: document.getElementById("configSmtpHost").value,
        smtpPort: parseInt(document.getElementById("configSmtpPort").value) || 587,
        logoUrl: document.getElementById("configLogoUrl").value
    };

    if (!payload.nombreTienda || isNaN(payload.costoEnvioBase)) {
        mostrarNotificacionAdmin("Nombre de tienda y Costo de envío son obligatorios.", "warning");
        return;
    }

    fetch("/api/AdminConfiguracionApi", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
    .then(res => {
        if (!res.ok) throw new Error("Error al guardar la configuración.");
        return res.json();
    })
    .then(result => {
        mostrarNotificacionAdmin(result.mensaje || "Configuración guardada correctamente.", "success");
    })
    .catch(err => {
        console.error("Error al guardar config:", err);
        mostrarNotificacionAdmin("Error al guardar configuración.", "danger");
    });
}

// ==========================================
// 6. SECCIÓN DE CARGA MASIVA (EXCEL)
// ==========================================

window.abrirModalCargaMasiva = function() {
    document.getElementById("formCargaMasiva").reset();
    document.getElementById("progresoCargaMasiva").classList.add("d-none");
    document.getElementById("btnSubirExcel").disabled = false;
    
    const modalEl = document.getElementById("modalCargaMasiva");
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
};

window.subirArchivoExcel = function() {
    const fileInput = document.getElementById("archivoExcel");
    if (!fileInput || fileInput.files.length === 0) {
        mostrarNotificacionAdmin("Por favor selecciona un archivo Excel primero.", "warning");
        return;
    }

    const file = fileInput.files[0];
    if (!file.name.endsWith('.xlsx')) {
        mostrarNotificacionAdmin("El archivo debe tener extensión .xlsx", "warning");
        return;
    }

    const formData = new FormData();
    formData.append("archivoExcel", file);

    const btn = document.getElementById("btnSubirExcel");
    const progreso = document.getElementById("progresoCargaMasiva");
    
    btn.disabled = true;
    progreso.classList.remove("d-none");

    fetch("/api/AdminProductosApi/carga-masiva", {
        method: "POST",
        body: formData
    })
    .then(res => {
        if (!res.ok) return res.json().then(err => { throw new Error(err.mensaje || "Error al procesar el archivo Excel."); });
        return res.json();
    })
    .then(data => {
        mostrarNotificacionAdmin(data.mensaje, "success");
        
        const modalEl = document.getElementById("modalCargaMasiva");
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.hide();
        
        cargarInventario();
    })
    .catch(err => {
        console.error("Error al subir excel:", err);
        mostrarNotificacionAdmin(err.message || "Ocurrió un error al procesar el archivo.", "danger");
    })
    .finally(() => {
        btn.disabled = false;
        progreso.classList.add("d-none");
    });
};

// ==========================================
// 5. SECCIÓN DE PROMOCIONES Y OFERTAS
// ==========================================
function cargarPromociones() {
    const tabla = document.getElementById("tablaPromocionesCuerpo");
    if (!tabla) return;

    tabla.innerHTML = `
        <tr>
            <td colspan="6" class="text-center py-4">
                <div class="spinner-border text-success" role="status">
                    <span class="visually-hidden">Cargando...</span>
                </div>
                <p class="mt-2 text-muted small">Cargando ofertas y promociones...</p>
            </td>
        </tr>`;

    fetch("/api/AdminPromocionesApi")
        .then(res => res.json())
        .then(data => {
            tabla.innerHTML = "";
            if (data.length === 0) {
                tabla.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-muted small">No hay promociones registradas.</td></tr>`;
                return;
            }

            data.forEach(promo => {
                let badgeClass = "bg-secondary";
                if (promo.estado === "Activa") badgeClass = "bg-success";
                if (promo.estado === "Programada") badgeClass = "bg-info text-dark";
                if (promo.estado === "Expirada") badgeClass = "bg-danger";

                const vigencia = `Del ${new Date(promo.fechaInicio).toLocaleDateString()} al ${new Date(promo.fechaFin).toLocaleDateString()}`;

                const tr = document.createElement("tr");
                tr.innerHTML = `
                    <td class="fw-bold">${promo.titulo}</td>
                    <td class="text-muted small">${promo.nombreProducto}</td>
                    <td class="text-danger fw-bold">-${promo.descuentoPorcentaje}%</td>
                    <td class="small text-muted">${vigencia}</td>
                    <td class="text-center"><span class="badge ${badgeClass} rounded-pill">${promo.estado}</span></td>
                    <td class="text-end">
                        <button class="btn btn-sm btn-outline-danger ms-1" onclick="eliminarPromocion(${promo.id})" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                `;
                tabla.appendChild(tr);
            });
        })
        .catch(err => {
            console.error("Error al cargar promociones:", err);
            tabla.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-danger small">Error al cargar promociones.</td></tr>`;
        });
}

window.abrirModalCrearPromocion = function() {
    document.getElementById("promocionForm").reset();
    document.getElementById("promocionId").value = "0";

    // Llenar el select de productos con los disponibles de la tabla principal
    const selectProd = document.getElementById("promoProductoId");
    selectProd.innerHTML = '<option value="">Todos los productos</option>';
    
    // Obtenemos los productos actuales consultando de nuevo si hace falta o de una variable
    fetch("/api/AdminProductosApi")
        .then(res => res.json())
        .then(data => {
            if (data.productos) {
                data.productos.forEach(p => {
                    selectProd.innerHTML += `<option value="${p.id}">${p.nombre}</option>`;
                });
            }
        });

    const modalEl = document.getElementById("modalPromocion");
    const modal = new bootstrap.Modal(modalEl);
    modal.show();
};

function guardarPromocion(e) {
    e.preventDefault();

    const promoId = document.getElementById("promocionId").value;
    const prodIdVal = document.getElementById("promoProductoId").value;
    
    const promocion = {
        titulo: document.getElementById("promoTitulo").value.trim(),
        descripcion: document.getElementById("promoDescripcion").value.trim(),
        productoId: prodIdVal ? parseInt(prodIdVal) : null,
        descuentoPorcentaje: parseFloat(document.getElementById("promoDescuento").value),
        fechaInicio: document.getElementById("promoFechaInicio").value,
        fechaFin: document.getElementById("promoFechaFin").value,
        activo: document.getElementById("promoActivo").checked
    };

    fetch("/api/AdminPromocionesApi", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(promocion)
    })
    .then(async res => {
        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || "Error al crear promoción.");
        }
        return res.json();
    })
    .then(data => {
        mostrarNotificacionAdmin("Oferta creada exitosamente.", "success");
        const modal = bootstrap.Modal.getInstance(document.getElementById("modalPromocion"));
        modal.hide();
        cargarPromociones();
    })
    .catch(err => {
        console.error("Error:", err);
        mostrarNotificacionAdmin(err.message, "danger");
    });
}

window.eliminarPromocion = function(id) {
    if (confirm("¿Estás seguro de eliminar esta oferta?")) {
        fetch(`/api/AdminPromocionesApi/${id}`, { method: "DELETE" })
            .then(res => {
                if (res.ok) {
                    mostrarNotificacionAdmin("Oferta eliminada.", "success");
                    cargarPromociones();
                } else {
                    mostrarNotificacionAdmin("Error al eliminar la oferta.", "danger");
                }
            })
            .catch(err => console.error(err));
    }
};

// ==========================================
// 8. SECCIÓN DE REPORTES PDF
// ==========================================

window.descargarReportePdf = function(periodo) {
    mostrarNotificacionAdmin("Generando reporte, por favor espera...", "info");

    // Obtener configuración (logo) y datos de ventas en paralelo
    Promise.all([
        fetch("/api/AdminConfiguracionApi").then(r => r.ok ? r.json() : null).catch(() => null),
        fetch(`/api/ReportesApi/Ventas?periodo=${periodo}`).then(r => { if (!r.ok) throw new Error("Error"); return r.json(); })
    ])
    .then(([config, data]) => {
        const logoUrl = config && config.logoUrl ? config.logoUrl : "";
        const nombreTienda = config && config.nombreTienda ? config.nombreTienda : "Abarrotes La Pasadita";

        // Construir un HTML temporal para el reporte
        const container = document.createElement("div");
        container.style.padding = "20px";
        container.style.fontFamily = "Arial, sans-serif";
        container.style.color = "#333";
        container.style.backgroundColor = "#fff";

        // Construir filas de la tabla
        let filas = "";
        if (data.ventas && data.ventas.length > 0) {
            data.ventas.forEach(v => {
                const fecha = new Date(v.fechaPedido).toLocaleString("es-MX");
                const total = new Intl.NumberFormat("es-MX", { style: "currency", currency: "MXN" }).format(v.total);
                filas += `
                    <tr style="border-bottom: 1px solid #ddd;">
                        <td style="padding: 8px; text-align: center;">${v.id}</td>
                        <td style="padding: 8px;">${fecha}</td>
                        <td style="padding: 8px;">${v.nombreCliente}</td>
                        <td style="padding: 8px;">${v.metodoPago}</td>
                        <td style="padding: 8px; text-align: center;"><span style="padding: 3px 8px; border-radius: 12px; font-size: 11px; background-color: #e8f8f0; color: #27ae60; font-weight: bold;">${v.estado}</span></td>
                        <td style="padding: 8px; font-weight: bold; text-align: right;">${total}</td>
                    </tr>
                `;
            });
        } else {
            filas = `<tr><td colspan="6" style="padding: 15px; text-align: center;">No hay ventas registradas en este periodo.</td></tr>`;
        }

        const totalGlobal = new Intl.NumberFormat("es-MX", { style: "currency", currency: "MXN" }).format(data.totalIngresos);
        const fechaGen = new Date(data.fechaGeneracion).toLocaleString("es-MX");

        // Construir cabecera del PDF: logo + nombre o solo nombre
        let cabeceraHtml = "";
        if (logoUrl) {
            cabeceraHtml = `
                <div style="text-align: center; margin-bottom: 25px; border-bottom: 3px solid #27ae60; padding-bottom: 15px;">
                    <img src="${logoUrl}" alt="Logo" style="max-height: 80px; max-width: 250px; margin-bottom: 10px;" crossorigin="anonymous" />
                    <h2 style="color: #27ae60; margin: 5px 0; font-size: 24px; letter-spacing: 1px;">${nombreTienda}</h2>
                    <h4 style="margin-top: 0; color: #555; font-weight: 500;">${data.titulo}</h4>
                    <p style="font-size: 11px; color: #888; margin-top: 5px;">Generado el: ${fechaGen}</p>
                </div>
            `;
        } else {
            cabeceraHtml = `
                <div style="text-align: center; margin-bottom: 25px; border-bottom: 3px solid #27ae60; padding-bottom: 15px;">
                    <h2 style="color: #27ae60; margin-bottom: 5px; font-size: 28px; letter-spacing: 1px;">${nombreTienda}</h2>
                    <h4 style="margin-top: 0; color: #555; font-weight: 500;">${data.titulo}</h4>
                    <p style="font-size: 11px; color: #888; margin-top: 5px;">Generado el: ${fechaGen}</p>
                </div>
            `;
        }

        container.innerHTML = `
            ${cabeceraHtml}
            
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 25px; background-color: #f8f9fa; padding: 15px 20px; border-radius: 8px; border-left: 5px solid #27ae60;">
                <div style="font-size: 14px; color: #555;"><strong>Total de Pedidos:</strong> <span style="font-size: 16px; font-weight: bold; color: #333;">${data.totalPedidos}</span></div>
                <div style="font-size: 15px; color: #555;"><strong>Ingresos Totales:</strong> <span style="font-size: 20px; font-weight: bold; color: #27ae60;">${totalGlobal}</span></div>
            </div>

            <table style="width: 100%; border-collapse: collapse; font-size: 12px; margin-bottom: 20px;">
                <thead style="background-color: #27ae60; color: white;">
                    <tr>
                        <th style="padding: 10px; text-align: center; border-radius: 4px 0 0 4px;">Folio</th>
                        <th style="padding: 10px; text-align: left;">Fecha</th>
                        <th style="padding: 10px; text-align: left;">Cliente</th>
                        <th style="padding: 10px; text-align: left;">Pago</th>
                        <th style="padding: 10px; text-align: center;">Estado</th>
                        <th style="padding: 10px; text-align: right; border-radius: 0 4px 4px 0;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${filas}
                </tbody>
            </table>
            
            <div style="margin-top: 40px; text-align: center; font-size: 10px; color: #aaa; border-top: 1px solid #eee; padding-top: 15px;">
                Este documento es un reporte administrativo generado automáticamente para ${nombreTienda} y no tiene validez fiscal.
            </div>
        `;

        // Opciones optimizadas para html2pdf
        const opt = {
            margin:       12,
            filename:     `reporte_ventas_${periodo}.pdf`,
            image:        { type: 'jpeg', quality: 0.98 },
            html2canvas:  { scale: 2, useCORS: true, scrollY: 0, scrollX: 0 },
            jsPDF:        { unit: 'mm', format: 'a4', orientation: 'portrait' }
        };

        // Generar y descargar
        html2pdf().set(opt).from(container).save().then(() => {
            mostrarNotificacionAdmin("Reporte PDF descargado con éxito.", "success");
        }).catch(err => {
            console.error(err);
            mostrarNotificacionAdmin("Error al descargar el PDF.", "danger");
        });

    })
    .catch(err => {
        console.error(err);
        mostrarNotificacionAdmin("Error al generar el reporte. Intenta de nuevo.", "danger");
    });
};
