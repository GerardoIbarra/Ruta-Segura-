using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RutaSegura.Runtime.Core
{
    public enum GamePhase
    {
        Intro,
        Exploration,
        Incident,
        Detour,
        Outcome
    }

    /// <summary>
    /// Maquina de estados finita del ciclo principal de Ruta Segura:
    /// Intro -> Exploration -> Incident -> Detour -> Outcome.
    /// Coordina transiciones narrativas y reinicio limpio con [R].
    /// </summary>
    [DefaultExecutionOrder(-60)]
    [AddComponentMenu("Ruta Segura/Core/Game Loop Manager")]
    public class GameLoopManager : MonoBehaviour
    {
        public static GameLoopManager Instance { get; private set; }

        [Header("Fases de Partida")]
        [SerializeField] private float introDuration = 2.5f;
        [SerializeField] private GamePhase currentPhase = GamePhase.Intro;

        private float introTimer;

        public GamePhase CurrentPhase => currentPhase;
        public event Action<GamePhase> OnPhaseChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (currentPhase == GamePhase.Intro)
            {
                introTimer += Time.deltaTime;
                if (introTimer >= introDuration)
                {
                    SetPhase(GamePhase.Exploration);
                }
            }
            else if (currentPhase == GamePhase.Outcome)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RestartLevel();
                }
            }
        }

        public void SetPhase(GamePhase nextPhase)
        {
            if (nextPhase == currentPhase) return;
            if (currentPhase == GamePhase.Outcome) return;

            currentPhase = nextPhase;
            OnPhaseChanged?.Invoke(currentPhase);
            Debug.Log($"<color=cyan>[GAMELOOP]</color> Fase cambiada a: {currentPhase}");
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
