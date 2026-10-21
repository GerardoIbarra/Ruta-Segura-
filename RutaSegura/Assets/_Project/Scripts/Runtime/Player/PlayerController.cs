using UnityEngine;
using UnityEngine.InputSystem;

namespace RutaSegura.Runtime.Player
{
    /// <summary>
    /// Controlador de locomoción y vista en primera persona para Ruta Segura.
    /// Diseñado para una experiencia de desplazamiento urbano realista y vulnerable.
    /// Requiere un CharacterController y una cámara hija asignada.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Ruta Segura/Player/Player Controller")]
    public class PlayerController : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Cámara en primera persona (hija del jugador)")]
        [SerializeField] private Camera playerCamera;
        
        [Tooltip("Acción de entrada opcional o auto-generada")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Velocidades de Movimiento (m/s)")]
        [Tooltip("Velocidad de caminata cotidiana")]
        [SerializeField] private float walkSpeed = 3.5f;

        [Tooltip("Velocidad de trote rápido / carrera bajo tensión")]
        [SerializeField] private float sprintSpeed = 6.0f;

        [Tooltip("Velocidad de paso cauto / agachado para pasar desapercibido")]
        [SerializeField] private float cautiousSpeed = 1.8f;

        [Header("Física y Gravedad")]
        [SerializeField] private float gravity = -18.0f;
        [SerializeField] private float groundCheckOffset = -0.1f;

        [Header("Sensibilidad y Calibración de Cámara")]
        [Tooltip("Sensibilidad general del ratón")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float lookSensitivity = 0.06f;

        [Tooltip("Multiplicador vertical (reduce la tendencia a subir/bajar de golpe)")]
        [Range(0.3f, 1.0f)]
        [SerializeField] private float verticalSensitivityMultiplier = 0.6f;

        [Tooltip("Límite de inclinación vertical hacia arriba/abajo en grados")]
        [Range(50f, 85f)]
        [SerializeField] private float verticalLookLimit = 70.0f;

        [Tooltip("Suavizado cinemático de cámara para evitar saltos bruscos")]
        [SerializeField] private bool smoothCamera = true;

        [Range(5f, 30f)]
        [SerializeField] private float cameraSmoothSpeed = 18.0f;

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
        private float verticalVelocity;
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

            // Calibrar sensibilidad si venía con el valor por defecto anterior (0.2f)
            if (lookSensitivity > 0.15f)
            {
                lookSensitivity = 0.06f;
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
            // Lectura mediante Input Actions si están disponibles, con fallback directo al teclado/ratón
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

            // Pausar giro de cámara con el ratón cuando el teléfono está abierto e interactuando
            if (UI.PhoneHUD.Instance != null && UI.PhoneHUD.Instance.IsOpen)
            {
                return;
            }

            // Suavizado de la entrada del mouse para evitar sobresaltos
            if (smoothCamera)
            {
                currentLookVector = Vector2.Lerp(currentLookVector, lookInput, cameraSmoothSpeed * Time.deltaTime);
            }
            else
            {
                currentLookVector = lookInput;
            }

            // Sensibilidad diferenciada: el eje vertical es más estable para no apuntar al cielo al caminar
            float mouseX = currentLookVector.x * lookSensitivity;
            float mouseY = currentLookVector.y * lookSensitivity * verticalSensitivityMultiplier * (invertY ? 1f : -1f);

            // Rotación horizontal (gira el cuerpo en Y)
            transform.Rotate(Vector3.up * mouseX);

            // Rotación vertical (inclina solo la cabeza/cámara en X con límites acotados)
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -verticalLookLimit, verticalLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            isGrounded = characterController.isGrounded;

            if (isGrounded && verticalVelocity < 0f)
            {
                // Pequeña fuerza hacia abajo para mantenerse pegado al suelo en desniveles
                verticalVelocity = -2f;
            }

            // Determinar velocidad según estado
            float targetSpeed = walkSpeed;
            if (isCautious)
            {
                targetSpeed = cautiousSpeed;
            }
            else if (isSprinting && moveInput.y > 0) // Solo sprint hacia adelante
            {
                targetSpeed = sprintSpeed;
            }

            currentSpeed = moveInput.sqrMagnitude > 0.01f ? targetSpeed : 0f;

            // Dirección en relación a la orientación del jugador
            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            Vector3 movement = moveDirection.normalized * currentSpeed;

            // Aplicar gravedad
            verticalVelocity += gravity * Time.deltaTime;
            movement.y = verticalVelocity;

            // Mover con CharacterController
            characterController.Move(movement * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            // Muestra en la vista de escena la orientación del frente
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * 1.2f);
        }
    }
}
