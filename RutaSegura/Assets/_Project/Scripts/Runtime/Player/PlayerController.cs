using UnityEngine;
using UnityEngine.InputSystem;

namespace RutaSegura.Runtime.Player
{
    /// <summary>
    /// Controlador de locomocion y vista en primera persona para Ruta Segura.
    /// Disenado para una experiencia de desplazamiento urbano realista, precisa y estable.
    /// Requiere un CharacterController y una camara hija asignada.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Ruta Segura/Player/Player Controller")]
    public class PlayerController : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Camara en primera persona (hija del jugador)")]
        [SerializeField] private Camera playerCamera;
        
        [Tooltip("Accion de entrada opcional o auto-generada")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Velocidades de Movimiento (m/s)")]
        [Tooltip("Velocidad de caminata cotidiana")]
        [SerializeField] private float walkSpeed = 3.5f;

        [Tooltip("Velocidad de trote rapido / carrera bajo tension")]
        [SerializeField] private float sprintSpeed = 6.0f;

        [Tooltip("Velocidad de paso cauto / agachado para pasar desapercibido")]
        [SerializeField] private float cautiousSpeed = 1.8f;

        [Header("Fisica y Gravedad")]
        [SerializeField] private float gravity = -18.0f;

        [Header("Sensibilidad y Calibracion de Camara")]
        [Tooltip("Sensibilidad general del raton (calibracion estandar FPS)")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float lookSensitivity = 0.08f;

        [Tooltip("Multiplicador vertical")]
        [Range(0.3f, 1.0f)]
        [SerializeField] private float verticalSensitivityMultiplier = 0.75f;

        [Tooltip("Limite de inclinacion vertical")]
        [Range(50f, 85f)]
        [SerializeField] private float verticalLookLimit = 75.0f;

        [Tooltip("Suavizado de camara (desactivado por defecto para respuesta inmediata 1:1)")]
        [SerializeField] private bool smoothCamera = false;

        [Range(10f, 60f)]
        [SerializeField] private float cameraSmoothSpeed = 35.0f;

        [SerializeField] private bool invertY = false;

        [Header("Estados Actuales (Lectura)")]
        [SerializeField] private bool isGrounded;
        [SerializeField] private bool isSprinting;
        [SerializeField] private bool isCautious;
        [SerializeField] private float currentSpeed;

        // Componentes internos
        private CharacterController characterController;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction crouchAction;

        private Vector2 moveInput;
        private Vector2 lookInput;
        private Vector2 currentLookVector;
        private float verticalVelocity = -2f;
        private float cameraPitch = 0.0f;

        public float CurrentSpeed => currentSpeed;
        public bool IsSprinting => isSprinting;
        public bool IsCautious => isCautious;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            // Asegurar escalonamiento comodo de banqueta (18cm machuelo)
            if (characterController != null)
            {
                characterController.stepOffset = 0.35f;
                characterController.slopeLimit = 45.0f;
            }

            SetupInput();
        }

        private void SetupInput()
        {
            if (inputActions != null)
            {
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    moveAction = playerMap.FindAction("Move");
                    lookAction = playerMap.FindAction("Look");
                    sprintAction = playerMap.FindAction("Sprint");
                    crouchAction = playerMap.FindAction("Crouch");
                }
            }
        }

        private void OnEnable()
        {
            if (inputActions != null)
            {
                inputActions.Enable();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (inputActions != null)
            {
                inputActions.Disable();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            ReadInput();
            HandleLook();
            HandleMovement();
        }

        private void ReadInput()
        {
            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }
            else if (Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;
                if (Keyboard.current.wKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed) y -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                moveInput = new Vector2(x, y).normalized;
            }

            if (lookAction != null)
            {
                lookInput = lookAction.ReadValue<Vector2>();
            }
            else if (Mouse.current != null)
            {
                lookInput = Mouse.current.delta.ReadValue();
            }

            if (sprintAction != null)
            {
                isSprinting = sprintAction.IsPressed();
            }
            else if (Keyboard.current != null)
            {
                isSprinting = Keyboard.current.leftShiftKey.isPressed;
            }

            if (crouchAction != null)
            {
                isCautious = crouchAction.IsPressed();
            }
            else if (Keyboard.current != null)
            {
                isCautious = Keyboard.current.cKey.isPressed || Keyboard.current.leftCtrlKey.isPressed;
            }
        }

        private void HandleLook()
        {
            if (playerCamera == null) return;

            // Pausar giro de camara cuando el telefono esta abierto
            if (UI.PhoneHUD.Instance != null && UI.PhoneHUD.Instance.IsOpen)
            {
                return;
            }

            if (smoothCamera)
            {
                currentLookVector = Vector2.Lerp(currentLookVector, lookInput, cameraSmoothSpeed * Time.deltaTime);
            }
            else
            {
                currentLookVector = lookInput;
            }

            float mouseX = currentLookVector.x * lookSensitivity;
            float mouseY = currentLookVector.y * lookSensitivity * verticalSensitivityMultiplier;

            // Rotacion horizontal directa
            transform.Rotate(Vector3.up * mouseX);

            // Inclinacion vertical estandar: mouse arriba -> mira hacia arriba
            cameraPitch -= mouseY * (invertY ? -1f : 1f);
            cameraPitch = Mathf.Clamp(cameraPitch, -verticalLookLimit, verticalLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            isGrounded = characterController.isGrounded;

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
                verticalVelocity = Mathf.Max(verticalVelocity, -25.0f);
            }

            float targetSpeed = walkSpeed;
            if (isCautious)
            {
                targetSpeed = cautiousSpeed;
            }
            else if (isSprinting && moveInput.y > 0)
            {
                targetSpeed = sprintSpeed;
            }

            currentSpeed = moveInput.sqrMagnitude > 0.01f ? targetSpeed : 0f;

            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            Vector3 movement = moveDirection.normalized * currentSpeed;
            movement.y = verticalVelocity;

            characterController.Move(movement * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * 1.2f);
        }
    }
}
