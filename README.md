# Ruta Segura

> **Videojuego de simulación narrativa, navegación urbana, sigilo social y supervivencia no violenta en primera persona.**

Inspirado en acontecimientos urbanos reales en Guadalajara, Jalisco, México. El jugador experimenta la vulnerabilidad de una persona común que necesita atravesar una ciudad en constante cambio para cumplir un objetivo cotidiano sin recurrir a la violencia.

---

## 📌 Ficha Técnica

| Parámetro | Detalle |
|---|---|
| **Estado actual** | Fase 0 — Preproducción y preparación del prototipo |
| **Motor previsto** | Unity (versión recomendada: Unity 6 o 2022.3 LTS) |
| **Render Pipeline** | Universal Render Pipeline (URP) |
| **Perspectiva** | Primera persona |
| **Documento de Diseño** | [Game Design Document (GDD v9.0)](docs/GDD.md) |

---

## 🎯 Primer Hito: Prototipo Vertical («Las flores»)

El objetivo inmediato es validar la hipótesis jugable mediante una misión contenida:

1. **Escenario:** Recorrido urbano básico con dos rutas transitables hacia una florería.
2. **Dinámica:** Un evento ambiental modifica la accesibilidad o el riesgo de una de las calles.
3. **Mecánicas:**
   - Movimiento en primera persona e interpretación de señales visuales y sonoras.
   - Sistema de amenaza contextual (0 a 100).
   - Teléfono diegético con mapa (*RutaLibre*) y feed local (*Eco Urbano*).
   - Condiciones claras de éxito o fracaso.

---

## 📂 Estructura del Repositorio

```text
Ruta-Segura-/
├── .gitignore          # Reglas de exclusión para Unity (Library, Temp, Builds, etc.)
├── README.md           # Visión general y guía del repositorio
├── docs/               # Documentación de diseño y arquitectura
│   └── GDD.md          # Game Design Document completo (v9.0)
└── Assets/             # Archivos y assets del proyecto Unity (al inicializar)
    └── _Project/       # Código, escenas y configuraciones propias
        ├── Scripts/
        ├── Scenes/
        ├── Prefabs/
        └── Settings/
```

---

## 📖 Documentación

Para consultar los pilares de diseño, la arquitectura de sistemas, la dirección artística y el desglose de fases de producción, revisa el documento maestro:

👉 **[Leer el Game Design Document completo](docs/GDD.md)**

---

## 🛠️ Guía para Colaboradores y Agentes de IA

Este proyecto sigue una metodología de desarrollo modular asistido por IA:
- **Ramas aisladas:** Todo trabajo se realiza en ramas `feat/*`, `fix/*` o `test/*`.
- **Integración continua:** No se fusiona código en `main` sin validación de compilación y pruebas.
- **Disciplina de alcance:** Se construye poco, se prueba con jugadores y se aprende antes de añadir nuevos sistemas.
