document.addEventListener("DOMContentLoaded", () => {
    cargarRecetaDelDia();
});

function cargarRecetaDelDia() {
    fetch("https://www.themealdb.com/api/json/v1/1/random.php")
        .then(res => res.json())
        .then(data => {
            if (!data.meals || data.meals.length === 0) return;
            const meal = data.meals[0];
            
            // Ocultar spinner
            document.getElementById("receta-spinner").classList.add("d-none");
            
            // Renderizar datos
            const imgEl = document.getElementById("receta-img");
            imgEl.src = meal.strMealThumb;
            imgEl.classList.remove("d-none");
            
            // Recopilar ingredientes y medidas
            const ingredientes = [];
            for (let i = 1; i <= 10; i++) {
                const ing = meal[`strIngredient${i}`];
                const measure = meal[`strMeasure${i}`];
                if (ing && ing.trim() !== "") {
                    ingredientes.push({ ing: ing.trim(), med: (measure || "").trim() });
                }
            }

            // Preparar el texto a traducir (uniendo con un separador especial)
            const textos = [
                meal.strMeal, 
                meal.strCategory, 
                meal.strArea, 
                ...ingredientes.map(i => i.ing)
            ];
            const textoCombinado = textos.join(" | ");

            // Llamar a la API de traducción (MyMemory)
            fetch(`https://api.mymemory.translated.net/get?q=${encodeURIComponent(textoCombinado)}&langpair=en|es`)
                .then(r => r.json())
                .then(transData => {
                    let traducidos = textos;
                    if (transData && transData.responseData && transData.responseData.translatedText) {
                        traducidos = transData.responseData.translatedText.split(" | ");
                    }
                    
                    // Asegurar que tengamos suficientes elementos (por si falló el split)
                    if (traducidos.length < 3) traducidos = textos;

                    const tituloEs = traducidos[0] || meal.strMeal;
                    const categoriaEs = traducidos[1] || meal.strCategory;
                    const areaEs = traducidos[2] || meal.strArea;

                    document.getElementById("receta-titulo").textContent = tituloEs;
                    document.getElementById("receta-categoria").innerHTML = `<i class="bi bi-tag-fill"></i> ${categoriaEs} | ${areaEs}`;

                    if (meal.strMeal) {
                        const link = document.getElementById("receta-link");
                        const queryBusqueda = encodeURIComponent(tituloEs + " receta en español");
                        link.href = "https://www.youtube.com/results?search_query=" + queryBusqueda;
                        link.classList.remove("d-none");
                    }

                    // Ingredientes
                    const listEl = document.getElementById("receta-ingredientes");
                    listEl.innerHTML = "";
                    for (let i = 0; i < ingredientes.length; i++) {
                        const ingTraducido = traducidos[3 + i] || ingredientes[i].ing;
                        const li = document.createElement("li");
                        li.className = "col-6 mb-2";
                        li.innerHTML = `<i class="bi bi-check2-circle text-success me-1"></i> <strong>${ingTraducido}</strong> (${ingredientes[i].med})`;
                        listEl.appendChild(li);
                    }
                })
                .catch(err => {
                    console.error("Error al traducir:", err);
                    // Fallback a inglés si falla la traducción
                    document.getElementById("receta-titulo").textContent = meal.strMeal;
                    document.getElementById("receta-categoria").innerHTML = `<i class="bi bi-tag-fill"></i> ${meal.strCategory} | ${meal.strArea}`;
                    
                    if (meal.strMeal) {
                        const link = document.getElementById("receta-link");
                        const queryBusqueda = encodeURIComponent(meal.strMeal + " receta en español");
                        link.href = "https://www.youtube.com/results?search_query=" + queryBusqueda;
                        link.classList.remove("d-none");
                    }

                    const listEl = document.getElementById("receta-ingredientes");
                    listEl.innerHTML = "";
                    ingredientes.forEach(item => {
                        const li = document.createElement("li");
                        li.className = "col-6 mb-2";
                        li.innerHTML = `<i class="bi bi-check2-circle text-success me-1"></i> <strong>${item.ing}</strong> (${item.med})`;
                        listEl.appendChild(li);
                    });
                });
        })
        .catch(err => {
            console.error("Error cargando receta:", err);
            document.getElementById("receta-spinner").innerHTML = `<span class="text-danger small">No se pudo cargar la receta.</span>`;
        });
}
