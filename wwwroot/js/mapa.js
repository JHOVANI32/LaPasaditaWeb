document.addEventListener("DOMContentLoaded", () => {
    // Coordenadas de Axtla de Terrazas (Ejemplo, puede cambiarse)
    const lat = 21.4333;
    const lng = -98.8667;
    
    // Inicializar el mapa
    const map = L.map('mapa-tienda').setView([lat, lng], 15);

    // Cargar los "tiles" de OpenStreetMap
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(map);

    // Crear un ícono personalizado de Bootstrap (usando HTML en un DivIcon de Leaflet)
    const customIcon = L.divIcon({
        className: 'custom-pin',
        html: `<div style="background-color: var(--bs-danger); color: white; border-radius: 50%; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; box-shadow: 0 4px 8px rgba(0,0,0,0.3); border: 2px solid white;">
                  <i class="bi bi-shop fs-5"></i>
               </div>`,
        iconSize: [40, 40],
        iconAnchor: [20, 40], // El punto inferior apunta al lugar exacto
        popupAnchor: [0, -40] // Dónde se abre el popup
    });

    // Agregar marcador
    L.marker([lat, lng], { icon: customIcon }).addTo(map)
        .bindPopup('<b>Abarrotes La Pasadita</b><br>¡Visítanos para las mejores ofertas!')
        .openPopup();
});
