

# Documento Técnico de Producción (v8) — “Ruta Segura / Marea / Calle Abierta”



**Base del equipo:** Repositorio `msitarzewski/agency-agents`
**Motor Gráfico:** Unity (High Definition Render Pipeline - HDRP + DOTS/Entities)
**Locación Principal:** Metrópolis Latinoamericana (Guadalajara / CDMX)

## Nivel 0 — Origen del Proyecto y Contexto Sociopolítico (La Visión)

**La Inspiración Base:**
El concepto nace de una experiencia real en el centro de Guadalajara (8M, 2021). El protagonista intentaba realizar una tarea cotidiana (comprar flores para el cumpleaños de su hermana) mientras la ciudad comenzaba a blindarse con mallas y maderas. La experiencia de ser perfilado por la multitud —donde el simple hecho de cruzar por la zona equivocada podía detonar persecuciones— inspiró el sistema central del juego: **el peligro de estar en el lugar equivocado con el perfil equivocado.**

**Relevancia Contemporánea (Octubre 2026):**
El juego actúa como un reflejo del clima social global actual, inspirándose en conflictos reales que paralizan la vida urbana: la exigencia de agua limpia en Guadalajara, las crisis en los hospitales del IMSS en CDMX, la crisis de vivienda en España y las huelgas de educación en Francia. El juego expone al jugador al desafío de sobrevivir al colapso logístico que estas crisis generan en las urbes modernas.

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
* **🟡 Nivel Amarillo (Acción Directa / Perfilamiento):** Cortes viales severos. Comercios cerrando cortinas de acero y poniendo tablas. Riesgo de ser perfilado por autoridades o la misma marcha.
* **🔴 Nivel Rojo (Estallido):** Uso de gas lacrimógeno (penalización de ceguera). "Efecto Barrido" policial (Hazard de avalancha). El objetivo cambia a buscar refugio inmediato en zaguanes o negocios (ej. florerías).
* **🎉 Evento Masivo (Festejo Deportivo):** El "Falso Verde". Comienza festivo pero la marea humana arrastra al jugador y puede escalar a Rojo por riñas.

---

## Nivel 3 — Matemáticas del Juego y Optimización de IA

**3.1. Simulación de la "Marea Humana" (Vector / Flow Fields)**
Uso de un Grid 2D direccional (Unity DOTS/Burst) para simular multitudes. Entrar a una celda de alta densidad aplica una fuerza direccional (`AddForce`), obligando al jugador a moverse lateralmente para salir de la corriente.

**3.2. Modelo del Perfilamiento (Threat Score)**
La IA evalúa la amenaza del jugador ($T$) en una escala de 0 a 100:
$T = (Base_{vestimenta/perfil} + Mod_{celular}) \times Multiplicador_{postura} \times Entorno - Cobertura$
El Threat Score es contextual a la zona (ej. vestir ropa corporativa reduce amenaza policial, pero puede incrementarla en zonas de huelga).

**3.3. El "Efecto Barrido" (Nivel Rojo)**
La formación de barrido es un *Compound Hazard Object* (`BoxCollider`) que empuja o arresta todo a su paso, con un `Post-Processing Volume` vinculado (gas lacrimógeno).

---

## Nivel 4 — Interfaz Diegética (El Celular)

La UI existe en *World Space* en las manos del personaje.

* **RutaLibre (GPS):** Muestra congestión y bloqueos. Requiere señal.
* **Eco Urbano (Red Social):** Feed en vivo para anticipar cierres ("Están poniendo maderas en Av. Juárez").
* **Modo Avión (Stealth Digital):** Reduce consumo de batería y silencia alertas, pero desconecta de la red.

---

## Nivel 5 — Progresión y Karma Urbano

* **El Loadout Diario:** La elección de mochila o prendas modifica tu perfil desde el inicio.
* **Karma Urbano:** Navegar con "Empatía" permite que los comerciantes te dejen entrar a refugiarte cuando las cosas se salgan de control.

---

## Nivel 6 — Arquitectura del Repositorio

* **Estructura Git:** `.gitignore` estricto para Unity. Git LFS activado para texturas y modelos OSM.
* **Serialización:** Asset Serialization en `Force Text` para evitar corrupción de escenas.
* **Ramas:** Integraciones en `develop`, versiones jugables en `main`.

---

## Nivel 7 — Equipamiento, Instalaciones y Entregables por Agente

Para la ejecución del **Sprint 1 (Prototipo 0.1)**, el equipo de 10 agentes requiere la siguiente infraestructura y deberá producir estos archivos exactos:

### 🛠️ Núcleo Técnico y de Diseño (Constructores)

**1. Unity Architect**

* **Software/Instalaciones:** Unity Editor (versión LTS actual), paquete *Entities* (DOTS) y *Burst Compiler*.
* **Archivos Entregables:**
* `PlayerController.cs`: Movimiento en primera persona y control de inercia.
* `CrowdFlowSystem.cs`: Lectura del Grid 2D y aplicación de fuerza direccional (Marea Humana).
* `PhoneController.cs`: Lógica de activación de la UI diegética y drenaje de batería.



**2. Unity Shader Graph Artist**

* **Software/Instalaciones:** Unity HDRP configurado con volúmenes locales.
* **Archivos Entregables:**
* `TearGas_VFX.prefab`: Humo volumétrico reactivo a la luz.
* `WetAsphalt_Mat.mat`: Material reflectante para asfalto mojado.
* `PoliceStrobe_Light.prefab`: Sistema de luces estroboscópicas optimizadas.



**3. Game Designer**

* **Software/Instalaciones:** Microsoft Excel o Google Sheets.
* **Archivos Entregables:**
* `ThreatScore_Balance.xlsx`: Matriz matemática de valores de amenaza (ropa, postura, acciones).



**4. Level Designer**

* **Software/Instalaciones:** Unity con paquete *ProBuilder*.
* **Archivos Entregables:**
* `Scene_01_GDL_Juarez.unity`: Ensamblaje principal (graybox).
* `SafeZone_Floreria.prefab`: Volumen interactivo de refugio que reduce el *Threat Score* a cero.



### 🎒 Operaciones y Arte (Soporte)

**5. Project Shepherd**

* **Software/Instalaciones:** Acceso administrador al Project Board (GitHub/Jira).
* **Archivos Entregables:** El GDD v8 y el control de alcance (aprobación/rechazo de features no planeadas).

**6. Jira Workflow Steward**

* **Software/Instalaciones:** Acceso root al repositorio de GitHub, Git CLI, Git LFS.
* **Archivos Entregables:**
* `.gitignore` para Unity.
* `.gitattributes` para gestión de binarios y modelos 3D.
* Configuración de reglas de protección en la rama `main`.



**7. UI Designer**

* **Software/Instalaciones:** Figma o Adobe XD.
* **Archivos Entregables:**
* `UI_RutaLibre_Map.png`: Mapa GPS base.
* `UI_EcoUrbano_Feed.png`: Diseño del feed social.
* Íconos vectoriales del estado de batería (SVG).



**8. Blender Addon Engineer**

* **Software/Instalaciones:** Blender 3.x+ con el Addon *OpenStreetMap (OSM)*.
* **Archivos Entregables:**
* `GDL_Juarez_Mesh.fbx`: Topografía limpia y escalada (1m = 1 unidad Unity) de la zona centro, con colisiones básicas.



### 🔍 División de Calidad (Validación)

**9. Reality Checker**

* **Software/Instalaciones:** Base de datos documental (referencias de video/fotografía de marchas reales, 8M 2021, crisis de 2026).
* **Archivos Entregables:**
* *Checklist de Contexto Cultural*: Documento de validación de materiales (ej. vallas de madera/acero estilo LatAm, equipo antimotines correcto).



**10. Performance Benchmarker**

* **Software/Instalaciones:** Unity Profiler, Frame Debugger.
* **Archivos Entregables:**
* *Reporte de Estabilidad*: Validación escrita del rendimiento (ej. mantenimiento de FPS estables con 50+ NPCs y volúmenes de gas activos).
