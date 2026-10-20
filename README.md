# RUTA SEGURA
## Game Design Document — GDD v9.0

**Título provisional:** Ruta Segura  
**Títulos alternativos:** Marea / Calle Abierta  
**Género:** Simulación narrativa, navegación urbana, sigilo social y supervivencia no violenta  
**Perspectiva:** Primera persona  
**Motor:** Unity  
**Desarrollo:** Individual con asistencia de agentes de IA mediante Gemini / Antigravity  
**Estado:** Preproducción — definición del primer prototipo jugable  
**Ubicación de referencia:** Guadalajara, Jalisco, México  
**Versión del documento:** 9.0

---

# 1. Visión del proyecto

## 1.1. Concepto

Ruta Segura es un videojuego de navegación urbana y toma de decisiones en el que una actividad cotidiana se convierte en un desafío cuando el entorno social de una ciudad comienza a cambiar.

El jugador debe llegar a destinos concretos mientras interpreta señales del entorno, evalúa rutas, administra recursos limitados y responde a situaciones impredecibles.

La ciudad no es únicamente el escenario: es un sistema dinámico que modifica las opciones disponibles para el jugador.

La experiencia se centra en observar, anticipar, decidir y adaptarse. El jugador no está diseñado para dominar el entorno mediante la violencia, sino para comprenderlo y encontrar una manera segura de atravesarlo.

## 1.2. Premisa narrativa

La idea nace de una experiencia personal en el centro de Guadalajara durante una jornada de movilización social en 2021.

Una tarea sencilla —comprar flores para el cumpleaños de un familiar— se convierte en un problema cuando las calles comienzan a cerrarse, los comercios se protegen y el comportamiento de las personas cambia.

El protagonista no busca participar en el conflicto. Solo necesita llegar a su destino, pero descubre que su capacidad para hacerlo depende de interpretar correctamente el espacio y sus condiciones.

La ficción podrá ampliarse a diferentes contextos urbanos, crisis logísticas, manifestaciones, eventos masivos y otras situaciones que alteren la movilidad cotidiana.

Las situaciones inspiradas en acontecimientos reales deberán tratarse con contexto, investigación y sensibilidad, sin presentar a grupos sociales completos como inherentemente hostiles.

## 1.3. Objetivo de diseño

Crear una experiencia en la que el jugador piense:

*«Sé a dónde quiero llegar, pero ¿qué está pasando en la ciudad y cuál es la mejor manera de atravesarla?»*

El éxito del juego depende de que el jugador aprenda a interpretar el entorno, no de que memorice una ruta fija.

## 1.4. Pilares de diseño

### Pilar 1 — La ciudad comunica

El entorno proporciona información mediante sonidos, movimientos, cambios en los comercios, obstáculos, densidad de personas y comportamiento colectivo.

### Pilar 2 — La información es incompleta

El teléfono, las señales ambientales y la observación directa ofrecen información útil, pero ninguno garantiza una lectura perfecta de la situación.

### Pilar 3 — Las decisiones tienen consecuencias

La ruta elegida modifica el tiempo de llegada, la exposición al riesgo y los recursos disponibles.

### Pilar 4 — La violencia no es la solución principal

La supervivencia depende de la observación, el desplazamiento, la adaptación y la búsqueda de alternativas.

### Pilar 5 — El contexto importa

Una misma acción o característica puede tener consecuencias diferentes según la zona, el momento y las condiciones del entorno. El sistema no debe reducir grupos sociales a categorías rígidas de buenos y malos.

---

# 2. Experiencia del jugador

## 2.1. Fantasía principal

El jugador experimenta la vulnerabilidad de desplazarse por una ciudad que no puede controlar completamente.

No es un soldado ni un héroe de acción. Es una persona común que necesita cumplir una tarea y tomar decisiones razonables bajo presión.

## 2.2. Bucle principal de juego

1. **Preparar:** recibir un objetivo y revisar la información disponible.
2. **Observar:** identificar bloqueos, concentraciones de personas y señales de tensión.
3. **Evaluar:** comparar rutas y estimar sus riesgos.
4. **Actuar:** desplazarse, cambiar de ruta, esperar o buscar un refugio.
5. **Adaptarse:** responder a eventos que alteran las condiciones de la ciudad.
6. **Resolver:** alcanzar el destino o fracasar en el intento.
7. **Aprender:** comprender las consecuencias y utilizar esa experiencia en situaciones posteriores.

El bucle debe funcionar sin requerir gráficos avanzados, multitudes masivas ni sistemas de simulación complejos.

## 2.3. Condiciones de éxito

Una misión puede completarse cuando el jugador alcanza el destino o cumple una condición alternativa definida por el diseño.

El resultado puede considerar:

- Tiempo empleado.
- Exposición a situaciones peligrosas.
- Recursos consumidos.
- Decisiones tomadas.
- Objetivos secundarios completados.

No todas las misiones deben utilizar todas estas variables.

## 2.4. Condiciones de fracaso

El fracaso puede producirse cuando el jugador:

- Agota un recurso indispensable.
- Queda atrapado en una situación que no puede resolver.
- Supera un límite de exposición definido para la misión.
- No alcanza un objetivo antes de una condición temporal.
- Pierde acceso a una ruta necesaria sin encontrar una alternativa.

El fracaso debe ser comprensible. El juego tiene que proporcionar información suficiente para que el jugador pueda aprender de la decisión que tomó.

---

# 3. Alcance del primer prototipo

## 3.1. Objetivo del prototipo

Validar que el bucle de observación, navegación, cambio de condiciones y toma de decisiones produzca una experiencia interesante.

El prototipo no busca demostrar que todos los sistemas de la visión final ya están implementados.

## 3.2. Contenido incluido

- Una misión breve en una zona urbana ficticia inspirada en Guadalajara.
- Cámara en primera persona y movimiento básico.
- Dos rutas posibles hacia un destino.
- Obstáculos y señales ambientales.
- Un evento que modifica las condiciones de una ruta.
- Un sistema inicial de riesgo contextual.
- Una interfaz mínima para mostrar el objetivo y el resultado.
- Condiciones de éxito y fracaso.
- Una build ejecutable para pruebas.

## 3.3. Contenido excluido del primer prototipo

- Ciudad de mundo abierto.
- Multitudes de gran escala.
- Simulación física avanzada de corrientes humanas.
- Generación procedural de ciudades.
- Sistema persistente de reputación ciudadana.
- Inventario complejo de ropa y equipo.
- Narrativa ramificada extensa.
- Simulación completa de autoridades y grupos sociales.
- Efectos volumétricos avanzados.
- Sistemas de progresión entre múltiples capítulos.

Estas características podrán evaluarse en versiones posteriores, siempre que los resultados del prototipo justifiquen su incorporación.

## 3.4. Primera misión: «Las flores»

**Premisa:** el protagonista necesita comprar flores antes de llegar a una reunión familiar.

**Objetivo:** alcanzar una florería ubicada al otro lado de una zona urbana con condiciones cambiantes.

**Desarrollo:**

1. El jugador recibe el destino y una estimación inicial del trayecto.
2. Explora una calle con señales tempranas de actividad social.
3. Descubre que la ruta principal comienza a bloquearse.
4. Puede continuar, tomar una ruta alternativa o esperar y observar.
5. Un evento modifica la disponibilidad o el riesgo de una de las rutas.
6. El jugador decide cómo continuar.
7. La misión termina al alcanzar el destino o activar una condición de fracaso.

La misión debe poder completarse con geometría sencilla y un número reducido de personajes no jugables.

## 3.5. Criterios de aceptación

El prototipo se considera funcional cuando:

- El jugador puede iniciar y completar una misión.
- El movimiento y las colisiones funcionan correctamente.
- Existen dos rutas transitables.
- Un evento modifica el estado del entorno.
- El sistema de riesgo produce resultados consistentes.
- Las decisiones afectan al menos una variable observable.
- La misión puede finalizar mediante éxito o fracaso.
- La build se ejecuta sin depender del editor.
- No existen errores bloqueantes conocidos en el recorrido validado.
- Al menos algunos jugadores externos al desarrollo pueden explicar las señales observadas y las decisiones que tomaron.

---

# 4. Sistemas de gameplay

## 4.1. Planificación de rutas

El jugador recibe información inicial sobre el destino y las rutas disponibles.

Cada ruta podrá describirse mediante:

- Distancia estimada.
- Tiempo aproximado.
- Estado de accesibilidad.
- Riesgo contextual.
- Información disponible y su antigüedad.

Durante el recorrido, las condiciones pueden cambiar.

La ruta aparentemente más rápida no debe ser siempre la mejor opción.

## 4.2. Lectura del entorno

El jugador podrá interpretar señales como:

- Comercios que comienzan a cerrar.
- Instalación de barreras.
- Aumento de la densidad de personas.
- Cambios en el ruido ambiental.
- Calles parcialmente bloqueadas.
- Alteraciones en el movimiento colectivo.
- Presencia de humo u otros efectos ambientales.

Las señales deben anticipar, cuando sea posible, cambios relevantes en la jugabilidad.

La información no debe ser completamente explícita: el jugador necesita interpretar lo que observa, pero el sistema debe mantener una lógica consistente.

## 4.3. Sigilo social

El sigilo social representa la capacidad de desplazarse sin atraer atención indeseada en determinadas circunstancias.

No equivale a invisibilidad ni a una puntuación moral.

Las consecuencias dependen de la situación, el comportamiento del entorno y las reglas específicas de cada misión.

Durante el prototipo, este concepto se validará de forma simplificada mediante exposición contextual y cambios en el nivel de riesgo. No se implementará todavía un sistema completo de reconocimiento individual.

## 4.4. Sistema de amenaza contextual

El sistema representa la exposición del jugador a situaciones potencialmente peligrosas.

Se propone una escala interna de 0 a 100:

- **0–24:** exposición baja.
- **25–49:** exposición moderada.
- **50–74:** exposición elevada.
- **75–100:** exposición crítica.

Estos umbrales son valores iniciales para pruebas, no parámetros definitivos.

Las variables consideradas podrán incluir:

- Estado de la zona.
- Proximidad a un peligro activo.
- Densidad de personas.
- Accesibilidad de rutas de salida.
- Acciones recientes del jugador.
- Disponibilidad de refugios.

El sistema deberá evitar atribuir peligrosidad a una persona únicamente por su vestimenta o pertenencia aparente a un grupo.

La amenaza representa condiciones de la situación, no una verdad objetiva sobre las personas presentes.

### Reglas de implementación

- El cálculo será determinista durante el prototipo.
- Los factores y umbrales estarán configurados de manera centralizada.
- Las transiciones deberán ser verificables mediante pruebas.
- Los cambios importantes deberán comunicarse al jugador mediante señales comprensibles.
- El sistema deberá permitir ajustes sin reescribir los componentes de movimiento o interfaz.

No se utilizará una fórmula definitiva hasta que existan pruebas suficientes para determinar qué variables producen decisiones interesantes.

## 4.5. Estados de tensión del entorno

### Verde — Actividad estable

El entorno presenta condiciones relativamente previsibles.

El desafío principal consiste en navegar, reconocer obstáculos y planificar el trayecto.

### Amarillo — Tensión creciente

Aumentan las restricciones de movilidad o la incertidumbre.

Pueden aparecer bloqueos, cambios en las rutas o señales de que una situación está evolucionando.

### Rojo — Peligro inmediato

Una condición del entorno obliga al jugador a priorizar la seguridad y reconsiderar su objetivo.

Puede ser necesario abandonar la ruta, buscar refugio o esperar una oportunidad para continuar.

### Evento masivo — Condiciones especiales

Celebraciones, concentraciones o eventos urbanos pueden generar densidad elevada, cambios de dirección y rutas congestionadas.

Este estado no implica automáticamente hostilidad.

Los estados describen condiciones del entorno y podrán coexistir con eventos específicos según las reglas de cada escenario.

## 4.6. Eventos dinámicos

Los eventos modifican las condiciones de una misión.

Ejemplos:

- Una calle queda bloqueada.
- Una ruta pierde accesibilidad.
- Una concentración de personas cambia de dirección.
- Un comercio habilita temporalmente un refugio.
- La información del teléfono queda desactualizada.

Para el primer prototipo solo se implementará un evento dinámico principal.

Los eventos deberán tener condiciones de activación, consecuencias y reglas de finalización explícitas.

## 4.7. Movimiento y multitudes

El movimiento en primera persona deberá proporcionar control preciso y predecible.

La simulación de multitudes se desarrollará por etapas:

1. Obstáculos estáticos y rutas alternativas.
2. NPC individuales con trayectorias controladas.
3. Grupos con comportamientos compartidos.
4. Evaluación de navegación basada en densidad y flujo.
5. Simulación de multitudes de mayor escala, si las pruebas lo justifican.

La implementación inicial no utilizará fuerzas arbitrarias que retiren el control del jugador sin comunicarlo.

Los sistemas de flujo humano, grids direccionales, DOTS y Burst quedan sujetos a validación técnica y mediciones de rendimiento.

---

# 5. Teléfono diegético e información

El teléfono del protagonista funciona como una herramienta dentro del mundo del juego.

## 5.1. RutaLibre — Navegación

Funciones previstas:

- Mostrar el destino.
- Consultar rutas disponibles.
- Mostrar bloqueos conocidos.
- Representar estimaciones de tiempo y riesgo.

La información del mapa puede estar incompleta o desactualizada.

## 5.2. Eco Urbano — Información social

Feed ficticio de información contextual.

Puede proporcionar avisos sobre cierres, cambios de movilidad y condiciones de una zona.

Los mensajes no deben considerarse automáticamente verdaderos. Su utilidad dependerá de la antigüedad, el contexto y la posibilidad de contrastarlos con el entorno.

## 5.3. Batería y conectividad

La batería y la conectividad serán recursos opcionales del diseño completo.

En el prototipo solo se incorporarán si aportan decisiones reales al bucle principal.

El modo avión podrá reducir consumo o notificaciones, pero también limitará las funciones que requieren conectividad.

No se añadirá una mecánica de batería únicamente por aumentar la cantidad de sistemas.

---

# 6. Refugios, empatía y progresión

## 6.1. Refugios

Los refugios son espacios que permiten interrumpir el desplazamiento y reconsiderar las opciones disponibles.

Pueden incluir comercios, zaguanes y otros lugares accesibles según la situación.

Su disponibilidad depende del contexto y de las reglas de la misión.

Un refugio no debe eliminar automáticamente todo el riesgo. Puede proporcionar protección temporal o una oportunidad para esperar.

## 6.2. Empatía y relaciones urbanas

El concepto de empatía urbana podrá influir en interacciones futuras.

Las relaciones con personajes y establecimientos deberán basarse en acciones contextualizadas, no en un sistema simplista de karma que clasifique todas las decisiones como buenas o malas.

## 6.3. Progresión futura

Las versiones posteriores podrán incorporar:

- Diferentes objetivos cotidianos.
- Nuevas zonas urbanas.
- Recursos y preparación previa.
- Consecuencias persistentes.
- Relaciones con personajes.
- Variaciones de escenarios.
- Diferentes condiciones iniciales.

Estos sistemas no forman parte de los criterios de aceptación del primer prototipo.

---

# 7. Dirección audiovisual

## 7.1. Dirección visual

La identidad visual busca representar una ciudad latinoamericana reconocible.

La prioridad será la legibilidad espacial: el jugador debe poder distinguir rutas, obstáculos, refugios y señales relevantes.

La fidelidad gráfica se incrementará cuando el diseño de la misión esté validado.

## 7.2. Audio

El sonido tendrá una función informativa y emocional.

Podrá comunicar:

- Cambios en la densidad del entorno.
- Cierres y bloqueos.
- Alteraciones en el comportamiento colectivo.
- Proximidad de determinados eventos.
- Cambios en las condiciones de seguridad.

La información sonora deberá complementar, no sustituir por completo, las señales visuales.

## 7.3. Atmósfera

La tensión deberá surgir de la combinación de incertidumbre, espacio, sonido y decisiones.

No se buscará mantener una intensidad máxima constante. Los momentos de calma permitirán al jugador procesar la información y tomar decisiones.

---

# 8. Arquitectura técnica provisional

## 8.1. Principios

- Modularidad.
- Responsabilidades explícitas.
- Dependencias controladas.
- Configuración separada de la lógica.
- Pruebas automatizadas donde aporten valor.
- Integración frecuente.
- Complejidad proporcional a las necesidades demostradas.

## 8.2. Componentes iniciales

Los nombres son provisionales y deberán validarse antes de la implementación.

- `PlayerController`: movimiento y orientación.
- `RouteDefinition`: configuración de rutas.
- `ThreatSystem`: cálculo y transiciones del riesgo.
- `WorldEventController`: eventos del entorno.
- `MissionController`: estado y resultado de la misión.
- `MissionHUD`: información presentada al jugador.

Los sistemas de gameplay no deberán depender directamente de componentes visuales de la interfaz.

## 8.3. Motor y renderizado

Unity será el motor de desarrollo.

El render pipeline, la versión exacta del editor y los paquetes se definirán tras verificar el entorno y los requisitos de la plataforma objetivo.

HDRP podrá utilizarse si sus capacidades justifican el coste técnico y de rendimiento. No se considera un requisito inamovible del prototipo.

## 8.4. Optimización

La optimización se basará en mediciones reproducibles.

DOTS, Entities, Burst, flow fields y otras soluciones avanzadas se evaluarán cuando exista una carga de trabajo que justifique su adopción.

No se impondrá una cifra de NPC ni un objetivo de FPS sin definir antes la plataforma, resolución, calidad gráfica y presupuesto de CPU/GPU.

---

# 9. Desarrollo asistido por agentes de IA

## 9.1. Entorno de trabajo

Gemini / Antigravity será el entorno principal de orquestación de agentes.

GitHub será la fuente de verdad para el código, la documentación, las tareas y el historial de cambios.

Los agentes actuarán como colaboradores especializados; sus resultados requerirán revisión e integración.

## 9.2. Roles

### Coordinación técnica

Descompone tareas, identifica dependencias, define contratos y mantiene el alcance del sprint.

### Gameplay

Implementa movimiento, interacción, rutas y objetivos.

### Sistemas

Implementa el cálculo del riesgo, las transiciones de estado y los eventos.

### Entorno

Prepara la escena, la geometría inicial, los recursos y la configuración visual asignada.

### QA

Verifica criterios de aceptación, ejecuta pruebas y documenta errores reproducibles.

Estos roles podrán combinarse en una misma sesión de trabajo. La cantidad de agentes no es un objetivo de producción.

## 9.3. Requisitos de cada tarea

Toda tarea debe especificar:

- Objetivo.
- Alcance y exclusiones.
- Archivos que puede modificar.
- Dependencias.
- Contratos de integración.
- Criterios de aceptación.
- Pruebas requeridas.
- Evidencia de verificación.

Los agentes no deberán afirmar que una compilación o prueba ha pasado si no existe evidencia de su ejecución.

## 9.4. Control de cambios

- Cada funcionalidad se desarrollará en una rama aislada.
- Los cambios se integrarán mediante pull requests.
- No se permitirán modificaciones directas a `main` por agentes.
- Las escenas y prefabs compartidos tendrán propietarios de cambios definidos.
- Las modificaciones arquitectónicas requerirán aprobación.
- Los errores detectados deberán documentarse y reproducirse antes de declarar una tarea terminada.

---

# 10. Git y organización del repositorio

## 10.1. Ramas

- `main`: última versión estable y reproducible.
- `feat/*`: funcionalidades.
- `fix/*`: correcciones.
- `test/*`: pruebas o validaciones específicas.

Las ramas de trabajo serán de corta duración.

## 10.2. Gestión de archivos

Se versionarán los assets fuente, scripts, escenas, prefabs, configuraciones y archivos `.meta` necesarios para reconstruir el proyecto.

Los archivos generados por Unity se excluirán mediante `.gitignore`.

Git LFS se utilizará cuando el tamaño y la naturaleza de los assets lo requieran.

Las configuraciones del editor se mantendrán de forma consistente entre colaboradores y agentes.

## 10.3. Validación de integración

Antes de integrar cambios se verificará, según corresponda:

- Compilación de scripts.
- Pruebas Edit Mode.
- Pruebas Play Mode.
- Integridad de referencias.
- Apertura de la escena principal.
- Ejecución del recorrido de la misión.
- Generación de una build ejecutable.

---

# 11. Plan de producción

## Fase 0 — Preparación

- Verificar el repositorio.
- Definir la versión de Unity.
- Establecer la estructura del proyecto.
- Crear las convenciones de código y Git.
- Aprobar la arquitectura mínima.
- Definir los criterios del prototipo.

**Resultado:** proyecto preparado para comenzar el desarrollo.

## Fase 1 — Prototipo de movimiento

- Crear una escena básica.
- Implementar movimiento en primera persona.
- Configurar colisiones.
- Crear una interacción con el destino.
- Verificar una ejecución completa.

**Resultado:** personaje controlable y entorno transitable.

## Fase 2 — Navegación y riesgo

- Añadir dos rutas.
- Implementar estados de amenaza.
- Incorporar un evento dinámico.
- Registrar consecuencias observables.
- Añadir pruebas de lógica.

**Resultado:** decisiones de navegación funcionales.

## Fase 3 — Primera misión

- Construir el recorrido de «Las flores».
- Incorporar señales ambientales.
- Añadir interfaz mínima.
- Configurar condiciones de éxito y fracaso.

**Resultado:** misión completa de principio a fin.

## Fase 4 — Validación

- Generar una build.
- Ejecutar pruebas funcionales.
- Observar a jugadores.
- Recoger comentarios.
- Corregir errores.
- Documentar resultados y limitaciones.

**Resultado:** decisión fundamentada sobre la siguiente iteración.

## Criterio para continuar

Solo se ampliará el alcance cuando el prototipo permita evaluar de forma razonable el bucle principal.

Si los jugadores no comprenden las señales, no perciben diferencias entre rutas o no entienden las consecuencias de sus decisiones, se priorizarán esas deficiencias antes de incorporar más sistemas.

---

# 12. Riesgos y decisiones pendientes

| Riesgo | Mitigación |
|---|---|
| Sobrealcance técnico | Limitar la primera misión y posponer sistemas avanzados. |
| Código generado incompatible | Interfaces explícitas, pruebas y revisión de cambios. |
| Conflictos en escenas y prefabs | Propiedad de archivos y trabajo aislado. |
| Complejidad de la simulación | Empezar con comportamientos deterministas. |
| Amenaza poco comprensible | Probar señales, consecuencias y consistencia con jugadores. |
| Rendimiento insuficiente | Perfilar en el hardware objetivo antes de optimizar. |
| Dependencia excesiva de agentes | Mantener documentación, pruebas y control humano. |
| Representación social simplista | Investigación contextual y revisión narrativa. |

Las siguientes decisiones deben quedar documentadas antes de que afecten al código:

- Versión de Unity y render pipeline.
- Plataforma objetivo.
- Controles y requisitos de accesibilidad.
- Presupuesto técnico de rendimiento.
- Nivel de fidelidad visual del prototipo.
- Comportamiento exacto de la primera misión.
- Variables iniciales del sistema de amenaza.

---

# 13. Definición de éxito del proyecto

El éxito inicial de Ruta Segura no se medirá por el número de sistemas implementados, agentes utilizados o assets producidos.

Se medirá por la capacidad de entregar una experiencia funcional que permita comprobar la hipótesis central del juego:

**¿Puede una ciudad cambiante, observada desde la perspectiva de una persona común, generar tensión y decisiones interesantes sin depender de la violencia como mecánica principal?**

Si el prototipo demuestra que sí, el proyecto tendrá una base sólida para ampliar sus sistemas, su narrativa y su ambición técnica.

Hasta entonces, la prioridad es construir poco, probarlo y aprender.
