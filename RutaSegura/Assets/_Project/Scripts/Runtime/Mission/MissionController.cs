using System;
using UnityEngine;

namespace RutaSegura.Runtime.Mission
{
    public enum MissionState
    {
        InProgress,
        Completed,
        Failed
    }

    /// <summary>
    /// Administrador de la misión «Las flores» según la Sección 3.4 del GDD.
    /// Registra el objetivo, el tiempo transcurrido y coordina el estado de la partida.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Mission/Mission Controller")]
    public class MissionController : MonoBehaviour
    {
        public static MissionController Instance { get; private set; }

        [Header("Datos de la Misión")]
        [SerializeField] private string missionTitle = "Misión 1: Las Flores";
        [TextArea(2, 4)]
        [SerializeField] private string missionObjective = "Comprar flores para la reunión familiar antes de que cierren las tiendas.";
        [SerializeField] private string destinationName = "Florería 'El Jazmín'";

        [Header("Estado")]
        [SerializeField] private MissionState currentState = MissionState.InProgress;
        [SerializeField] private float elapsedTime = 0f;

        public string MissionTitle => missionTitle;
        public string MissionObjective => missionObjective;
        public string DestinationName => destinationName;
        public MissionState CurrentState => currentState;
        public float ElapsedTime => elapsedTime;

        public event Action<MissionState> OnMissionStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (currentState == MissionState.InProgress)
            {
                elapsedTime += Time.deltaTime;
            }
        }

        public void CompleteMission()
        {
            if (currentState != MissionState.InProgress) return;

            currentState = MissionState.Completed;
            OnMissionStateChanged?.Invoke(currentState);
            Debug.Log($"<color=green>[MISIÓN]</color> ¡{missionTitle} completada con éxito en {elapsedTime:F1} segundos!");
        }

        public void FailMission()
        {
            if (currentState != MissionState.InProgress) return;

            currentState = MissionState.Failed;
            OnMissionStateChanged?.Invoke(currentState);
        }
    }
}
