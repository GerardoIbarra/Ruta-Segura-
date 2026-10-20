
El hecho de que el juego nazca de una experiencia real tuya en Guadalajara —intentando hacer algo tan cotidiano como comprarle flores a tu hermana para su cumpleaños mientras la ciudad se blinda con maderas y la tensión sube— es exactamente el tipo de narrativa que los *publishers* y los jugadores buscan. Demuestra que las mecánicas (el *Threat Score*, el evitar ciertas calles, el encapsulamiento) no son inventos de un diseñador aburrido, sino situaciones de supervivencia urbana que los latinoamericanos vivimos constantemente.

Y conectarlo con la realidad actual de este **octubre de 2026** (el problema del agua sucia en GDL, la crisis del IMSS en CDMX, y las réplicas globales en España y Francia) eleva el juego de una simple anécdota local a una **experiencia universal sobre el colapso de las urbes modernas**.

Aquí tienes el **GDD v7**, donde he integrado esta historia como el pilar fundamental del proyecto (Nivel 0).

---

# Documento Técnico de Producción (v7) — “Ruta Segura / Marea / Calle Abierta”

**Base del equipo:** Repositorio `msitarzewski/agency-agents`
**Motor Gráfico:** Unity (High Definition Render Pipeline - HDRP + DOTS/Entities)
**Locación Principal:** Metrópolis Latinoamericana (Guadalajara / CDMX)

## Nivel 0 — Origen del Proyecto y Contexto Sociopolítico (La Visión)

**La Inspiración Base:**
El concepto nace de una experiencia real en el centro de Guadalajara (8M, 2021). El protagonista intentaba realizar una tarea cotidiana (comprar flores para el cumpleaños de su hermana) mientras la ciudad comenzaba a blindarse con mallas y maderas. La experiencia de ser perfilado por la multitud —donde el simple hecho de ser un hombre cruzando por la zona equivocada podía detonar agresiones y persecuciones— inspiró el sistema central del juego: **el peligro de estar en el lugar equivocado con el perfil equivocado.**

**Relevancia Contemporánea (Octubre 2026):**
El juego actúa como un reflejo del clima social global actual. Los niveles y bloqueos se inspiran en conflictos reales que paralizan la vida urbana:

* **Locales:** Las manifestaciones recientes en Guadalajara por la exigencia de agua limpia y los bloqueos en CDMX por las crisis en los hospitales del IMSS.
* **Globales:** Ecos de la crisis de vivienda en España y las huelgas de educación en Francia.
El juego no juzga las causas sociales; simplemente expone al jugador al desafío de sobrevivir al colapso logístico que estas generan.

## Resumen Ejecutivo

Un videojuego de navegación urbana, lectura del entorno y sigilo social. El jugador debe cumplir objetivos cotidianos en un mapa urbano vivo cuyas reglas cambian por tensión social. La violencia no es una herramienta; el juego exige observación táctica, empatía espacial y mimetismo en un contexto hiperrealista.

---

## Nivel 1 — Diseño del Gameplay y Mecánicas Base

1. **Planificación (Riesgo vs. Recompensa):** Trazar rutas y evaluar el *Loadout* antes de salir de casa.
2. **Navegación Táctica:** Reconocer *telegraphing* visual y sonoro (humo, instalación de maderas en comercios, vallas, cristales rotos).
3. **Sigilo Social:** Aplicar técnicas de mimetismo para evadir perfilamiento policial, de manifestantes o encapsulamientos.
4. **Impacto:** Evaluación de tiempo, nivel de batería del celular y reputación ciudadana.

---

## Nivel 2 — Estados de Tensión y Escenarios Dinámicos

* **🟢 Nivel Verde (Marcha Pacífica):** Multitud estructurada. Reto logístico ("Laberinto de Vallas"). Navegación lenta pero segura.
* **🟡 Nivel Amarillo (Acción Directa / Perfilamiento):** Cortes viales severos. Comercios cerrando cortinas de acero y poniendo tablas. Riesgo de ser perfilado (por autoridades o por la misma marcha, dependiendo de tu apariencia/género/vestimenta).
* **🔴 Nivel Rojo (Estallido):** Uso de gas lacrimógeno (penalización de ceguera). "Efecto Barrido" policial (Hazard de avalancha). El objetivo cambia a buscar refugio inmediato en zaguanes o negocios.
* **🎉 Evento Masivo (Festejo Deportivo):** El "Falso Verde". Comienza festivo pero la marea humana arrastra al jugador y puede escalar a Rojo por riñas.

---

## Nivel 3 — Matemáticas del Juego y Optimización de IA

**3.1. Simulación de la "Marea Humana" (Vector / Flow Fields)**
Uso de un Grid 2D direccional (Unity DOTS/Burst) para simular multitudes sin colapsar el CPU.

* **Mecánica de arrastre:** Entrar a una celda de alta densidad aplica una fuerza direccional (`AddForce`), obligando al jugador a moverse lateralmente para salir de la corriente.

**3.2. Modelo del Perfilamiento (Threat Score)**
La IA (tanto policial como grupos radicales) evalúa la amenaza del jugador ($T$) en una escala de 0 a 100:
$T = (Base_{vestimenta/perfil} + Mod_{celular}) \times Multiplicador_{postura} \times Entorno - Cobertura$

* *El Threat Score es contextual:* Vestir de oficinista te vuelve invisible para la policía, pero te puede hacer blanco de hostilidad si intentas cruzar un bloqueo sindical. Ser hombre en una marcha separatista eleva el $T$ al máximo automáticamente en esa zona específica.

**3.3. El "Efecto Barrido" (Nivel Rojo)**
La formación de barrido es un *Compound Hazard Object* (`BoxCollider`) que empuja o arresta todo a su paso, con un `Post-Processing Volume` vinculado (gas lacrimógeno).

---

## Nivel 4 — Interfaz Diegética (El Celular)

La UI existe en *World Space* en las manos del personaje.

* **RutaLibre (GPS):** Muestra congestión y bloqueos. Requiere señal.
* **Eco Urbano (Red Social):** Feed en vivo para anticipar cierres ("Están poniendo maderas en Av. Juárez", "No hay paso por el IMSS").
* **Modo Avión (Stealth Digital):** Reduce consumo y silencia alertas, pero desconecta al jugador de la información en vivo.

---

## Nivel 5 — Progresión y Karma Urbano

* **El Loadout Diario:** La elección de mochila o prendas modifica tu perfil desde el inicio.
* **Karma Urbano:** Navegar con "Empatía" permite que los comerciantes (como los de la florería) te dejen entrar a refugiarte cuando las cosas se salgan de control.

---

## Nivel 6 — Arquitectura del Repositorio

* **Estructura Git:** `.gitignore` estricto para Unity. Git LFS activado para texturas y modelos OSM.
* **Serialización:** Asset Serialization en `Force Text` para evitar corrupción de escenas.
* **Ramas:** Integraciones en `develop`, versiones jugables en `main`.

---

## Nivel 7 — Estructura del Equipo (10 Agentes)

* **Técnico y Diseño:** Unity Architect (Marea Humana DOTS), Unity Shader Graph Artist (VFX/Gas), Game Designer (Threat Score), Level Designer (Grayboxing de calles).
* **Operaciones:** Project Shepherd (Orquestación), Jira Workflow Steward (Git/Jira), UI Designer (Celular), Blender Addon Engineer (Topografía OSM).
* **Calidad:** Reality Checker (Precisión sociopolítica y respeto al contexto), Performance Benchmarker (Optimización de FPS).
