namespace RutaSegura.Runtime.Threat
{
    /// <summary>
    /// Estados de tensión del entorno según la Sección 4.4 y 4.5 del GDD.
    /// </summary>
    public enum ThreatLevel
    {
        Low = 0,        // 0 - 24: Verde (Estable / Previsible)
        Moderate = 1,   // 25 - 49: Amarillo (Tensión creciente / Advertencia)
        High = 2,       // 50 - 74: Naranja (Riesgo elevado / Rutas comprometidas)
        Critical = 3    // 75 - 100: Rojo (Peligro inmediato / Priorizar refugio)
    }
}
