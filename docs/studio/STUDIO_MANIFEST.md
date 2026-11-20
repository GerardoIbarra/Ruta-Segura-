# MANIFIESTO DE ESTUDIO — RUTA SEGURA
## Gobernanza de Desarrollo y Coordinación de los 5 Departamentos

> **Mandato Fundamental del Proyecto:**
> A partir de esta etapa, **todo diseño, implementación, revisión y toma de decisiones técnicas y artísticas en Ruta Segura se rige obligatoriamente por los 5 Departamentos de Estudio**, adaptados del framework *Claude Code Game Studios (CCGS)* para garantizar calidad comercial de PC en Steam y erradicar cualquier sensación de juego generado con IA genérica.

---

```
                                  ┌──────────────────────────────┐
                                  │      DIRECCIÓN DE JUEGO      │
                                  │   (GDD & Visión de Proyecto) │
                                  └──────────────┬───────────────┘
                                                 │
         ┌───────────────────┬───────────────────┼───────────────────┬───────────────────┐
         ▼                   ▼                   ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ DEPARTAMENTO 1: │ │ DEPARTAMENTO 2: │ │ DEPARTAMENTO 3: │ │ DEPARTAMENTO 4: │ │ DEPARTAMENTO 5: │
│  LEVEL DESIGN & │ │  INGENIERÍA &   │ │   DIRECCIÓN     │ │     AUDIO &     │ │  CONTROL DE     │
│   NAVEGACIÓN    │ │ MECÁNICAS UNITY │ │   NARRATIVA     │ │    ATMÓSFERA    │ │  CALIDAD & QA   │
└─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘
```

---

## DEPARTAMENTO 1: Level Design & Navegación Espacial
* **Liderazgo / Roles:** `level-designer`, `world-builder`, `technical-artist`.
* **Misión:** Diseñar y construir el espacio urbano de Guadalajara con escala métrica humana precisa, líneas de visión legibles, calles transitables y ritmo de escape.
* **Criterios de Calidad Obligatorios:**
  1. **Manzanas Sólidas y Continuas:** Prohibido usar planos aislados, biombos o muros delgados flotantes. Todo edificio debe tener volumen macizo (mínimo 7m de fondo) y fachadas continuas sin huecos.
  2. **Banqueta y Machuelo Real:** Cada calle debe contar con banqueta elevada de concreto (0.18m) y machuelo/guarnición que separe el paso peatonal de la calzada vehicular.
  3. **Identidad Tapatía:** Arquitectura regional de Guadalajara (zócalo de cantera dorada de Huentitán, cortinas metálicas comerciales, portones coloniales de madera, herrería de forja en balcones y farolas coloniales clásicas).
  4. **Navegación Visual:** El jugador debe orientarse mediante la arquitectura y la señalética oficial vial (*CALLE CERRADA*, *DESVÍO*, toldo de la Florería), no con flechas flotantes mágicas.

---

## DEPARTAMENTO 2: Ingeniería & Mecánicas Unity
* **Liderazgo / Roles:** `unity-specialist`, `unity-ui-specialist`, `unity-shader-specialist`, `gameplay-programmer`.
* **Misión:** Arquitectura de código C# en Unity 6 (URP), New Input System, controladores de personaje reactivos y sistemas de juego desacoplados.
* **Criterios de Calidad Obligatorios:**
  1. **Control de Cámara FPS Preciso:** Respuesta de ratón directa 1:1, sin retardo de suavizado ("mouse smoothing drift") ni inversión vertical invertida.
  2. **Locomoción y Físicas:** `CharacterController` configurado con escalonamiento natural (`stepOffset: 0.35m`) para subir y bajar banquetas sin tirones ni bloqueos.
  3. **Colisiones Integrales:** Todas las mallas urbanas (asfalto, banquetas, edificios) deben contar con `MeshCollider` o `BoxCollider` garantizado desde la instanciación.
  4. **URP & Iluminación:** Atardecer tapatío realista (17:42 hrs, luz cálida 3200K, sombras suaves y ACES tonemapping).

---

## DEPARTAMENTO 3: Dirección Narrativa & Contexto Tapatío
* **Liderazgo / Roles:** `narrative-director`, `writer`.
* **Misión:** Guiar la experiencia emocional, la inmersión de supervivencia no violenta y las alertas diegéticas del smartphone (*Eco Urbano* y *RutaLibre*).
* **Criterios de Calidad Obligatorios:**
  1. **PROHIBICIÓN ESTRICTA DE EMOJIS (ANTI-IA):** Queda terminantemente prohibido el uso de emoticones Unicode (`📢`, `📱`, `⚠️`, `✓`, `🔴`, etc.) en cualquier texto, notificación, HUD, log o señalética. Steam y los jugadores perciben los emojis como "AI slop" de baja calidad.
  2. **Tono Diegético y Periodístico:** Las alertas del smartphone deben redactarse con estilo sobrio de red vecinal / alerta urbana real:
     - *Correcto:* `[ECO URBANO - ALERTA VIAL]: Cierre inmediato de Av. Juárez por contingente a la altura de Galeana. Tome rutas alternas.`
     - *Incorrecto (AI Slop):* `📢 ¡Atención! ⚠️ ¡La calle está cerrada! Corre por tu vida! 🏃‍♂️`
  3. **Jerga y Geografía Real de Guadalajara:** Nombres oficiales de calles (Juárez, Galeana, Pedro Moreno, Colón), referencias verosímiles a instituciones locales (SIAPA, CFE, Policía de Guadalajara) y respeto al GDD.

---

## DEPARTAMENTO 4: Sonido, Tensión & Atmósfera Acústica
* **Liderazgo / Roles:** `audio-director`, `sound-designer`.
* **Misión:** Diseñar la tensión fisiológica y la atmósfera sonora diegética de una ciudad en contingencia social.
* **Criterios de Calidad Obligatorios:**
  1. **Biometría Sonora Reactiva:** El sonido del latido cardíaco debe acelerarse y cambiar de timbre en sincronía directa con el nivel de amenaza (`Tranquilo` 68 BPM ➔ `Nervioso` 95 BPM ➔ `Alerta` 115 BPM ➔ `Pánico` 140+ BPM).
  2. **Paisaje Sonoro Urbano (Soundscape):** Ecos distantes de helicópteros de vigilancia, sirenas de patrulla a lo lejos, el rumor apagado de consignas de protesta rebotando entre los muros de cantera y el crujido de pasos sobre el asfalto.
  3. **Audio No Violento:** La tensión se genera por anticipación, respiración agitada y atmósfera opresiva, sin recurrir a efectos genéricos de juego de disparos.

---

## DEPARTAMENTO 5: Control de Calidad, Físicas & Rendimiento (QA)
* **Liderazgo / Roles:** `qa-lead`, `qa-tester`, `performance-analyst`.
* **Misión:** Probar rigurosamente cada build, auditar colisiones, validar el bucle de juego y asegurar 60+ FPS estables.
* **Criterios de Calidad Obligatorios:**
  1. **Zero Collision Leaks:** El jugador nunca debe poder atravesar fachadas, quedar atrapado en geometrías ni caer al vacío infinito.
  2. **Bucle de Misión Completo y Probado:** Inicio ➔ Caminata por Av. Juárez ➔ Detección de Bloqueo ➔ Spike de Tensión ➔ Alerta de Eco Urbano ➔ Desvío por Galeana ➔ Llegada a Florería El Jazmín ➔ Pantalla de Victoria.
  3. **Fórmula de Fracaso:** Si el estrés alcanza el 100%, la pantalla de colapso por pánico debe activarse correctamente y permitir reinicio instantáneo con `[ R ]`.
  4. **Perfilado de Rendimiento:** Sin caídas bruscas de fotogramas, mallas combinadas para bajo conteo de draw calls en Unity.

---

## PROTOCOLO DE TRABAJO EN CADA TURNO
Cada vez que se introduzca una nueva característica, cambio de mapa, arte o mecánica, el agente coordinador y los subagentes deben reportar el impacto y la aprobación desde los 5 departamentos:

| Departamento | Responsable | Validación Requerida |
| :--- | :--- | :--- |
| **1. Level Design** | `level-designer` | ¿La geometría es sólida, proporcionada y transitable? |
| **2. Ingeniería Unity** | `unity-specialist` | ¿El código C# compila limpio y sin bugs en Unity 6? |
| **3. Narrativa** | `narrative-director` | ¿Cero emojis? ¿Texto diegético y ambientado en Guadalajara? |
| **4. Audio** | `audio-director` | ¿El ritmo cardíaco y la atmósfera acústica responden al estado de juego? |
| **5. QA & Performance** | `qa-lead` | ¿Se probó la build? ¿Colisiones y 60 FPS garantizados? |
