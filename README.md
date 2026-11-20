# Ruta Segura

> **Videojuego de simulación narrativa, navegación urbana, sigilo social y supervivencia no violenta en primera persona.**

Inspirado en acontecimientos urbanos reales en Guadalajara, Jalisco, México. El jugador experimenta la vulnerabilidad de una persona común que necesita atravesar una ciudad en constante cambio para cumplir un objetivo cotidiano sin recurrir a la violencia.

---

## 📌 Ficha Técnica

| Parámetro | Detalle |
|---|---|
| **Estado actual** | Fase 1 — Vertical Slice jugable («Las flores») |
| **Motor** | Unity 6 (`6000.0.6f1 LTS`) |
| **Render Pipeline** | Universal Render Pipeline (URP) |
| **Perspectiva** | Primera persona (New Input System) |
| **Pipeline 3D** | Blender 5.2.2 LTS (modelado procedural urbano) |
| **Documento de Diseño** | [Game Design Document (GDD v9.0)](docs/GDD.md) |
| **Gobernanza de Estudio** | [Manifiesto de 5 Departamentos CCGS](docs/studio/STUDIO_MANIFEST.md) |

---

## 🏛️ Gobernanza de Estudio — Los 5 Departamentos

El proyecto opera bajo una estructura de estudio profesional inspirada en *Claude Code Game Studios (CCGS)* para garantizar calidad comercial de Steam y erradicar cualquier estética genérica o "AI slop":

1. **Level Design & Navegación:** Manzanas urbanas sólidas, escala métrica real, banquetas integradas y arquitectura histórica de Guadalajara (cantera, cortinas metálicas, balcones de forja).
2. **Ingeniería Unity:** C# en Unity 6, controladores FPS con respuesta 1:1, físicas estables, eventos dinámicos y HUD desacoplado.
3. **Dirección Narrativa (Anti-IA estricto):** Cero emojis Unicode, inmersión periodística y diegética (*Eco Urbano*), tensión social realista de Guadalajara.
4. **Audio & Atmósfera:** Soundscape diegético (sirenas lejanas, consignas, pasos) y pulso cardíaco reactivo según el nivel de amenaza.
5. **Control de Calidad (QA):** Certificación de colisiones, bucle de misión completo y rendimiento fluido a 60+ FPS.

👉 **[Consultar el Manifiesto de Estudio Completo](docs/studio/STUDIO_MANIFEST.md)**

---

## 🎯 Primer Hito: Vertical Slice («Las flores»)

Validación jugable en el Centro Histórico de Guadalajara (Av. Juárez y Calle Galeana):

1. **Escenario:** Manzanas continuas de Guadalajara con calzada de asfalto, banquetas con machuelo y dos rutas hacia la meta.
2. **Evento Dinámico:** Cierre de Av. Juárez por contingente social con valla antidisturbios y señalética vial oficial.
3. **Mecánicas Clave:**
   - Movimiento en primera persona e inspección visual urbana.
   - Sistema de amenaza contextual (0 a 100: *Tranquilo*, *Nervioso*, *Alerta*, *Pánico*).
   - Monitor biométrico cardíaco inferior izquierdo con ECG y cálculo de BPM.
   - Smartphone diegético (*RutaLibre* y *Eco Urbano*) con tecla `[TAB]`.
   - Marcador AR proyectado en asfalto hacia la ruta alterna (*Calle Galeana*).
   - Destino meta: **Florería El Jazmín**.

---

## 📂 Estructura del Repositorio

```text
Ruta-Segura-/
├── .agents/                    # Agentes especializados y skills de estudio (CCGS + Agency)
│   ├── game-development/ccgs/  # Departamentos técnicos (Unity, Level Design, Audio, QA)
│   └── ccgs-skills/            # Habilidades de coordinación y vertical slice
├── docs/                       # Documentación
│   ├── GDD.md                  # Game Design Document completo (v9.0)
│   └── studio/                 # Manifiesto de estudio y guías técnicas Unity
├── tools/                      # Herramientas de automatización en Blender y datos OSM
└── RutaSegura/                 # Proyecto Unity 6 URP
    └── Assets/
        └── _Project/           # Código, modelos, materiales y escenas
            ├── Scripts/        # Runtime y herramientas de Editor
            ├── Models/         # Arquitectura y props (FBX)
            ├── Materials/      # Paleta URP Lit tapatía
            └── Scenes/         # SampleScene.unity
```
