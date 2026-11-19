using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Content.Interaction;
using TMPro;

public class ControlRemotoRobot : MonoBehaviour
{
    // =========================================
    // AVION
    // =========================================

    [Header("AVION")]
    [Tooltip("Objeto principal del avion que sera controlado.")]
    [SerializeField] private Transform robot;


    // =========================================
    // CONTROLES XR
    // =========================================

    [Header("CONTROLES XR")]

    [Tooltip("Palanca de encendido.")]
    [SerializeField] private XRLever lever;

    [Tooltip("Joystick para mover el avion.")]
    [SerializeField] private XRJoystick joystick;

    [Tooltip("Knob para controlar la rotacion.")]
    [SerializeField] private XRKnob knob;

    [Tooltip("Slider para controlar la velocidad.")]
    [SerializeField] private XRSlider slider;

    [Tooltip("Segundo knob para controlar la altura.")]
    [SerializeField] private XRKnob dialElevator;


    // =========================================
    // BATERIA / SOCKET
    // =========================================

    [Header("BATERIA / SOCKET")]

    [Tooltip("Socket donde se coloca la bateria.")]
    [SerializeField] private XRSocketInteractor batterySocket;

    [Tooltip("Bateria que acepta el socket.")]
    [SerializeField] private XRBaseInteractable batteryToAccept;


    // =========================================
    // MOVIMIENTO
    // =========================================

    [Header("MOVIMIENTO")]

    [Tooltip("Velocidad minima del avion.")]
    [SerializeField] private float minSpeed = 0.5f;

    [Tooltip("Velocidad maxima del avion.")]
    [SerializeField] private float maxSpeed = 5f;


    // =========================================
    // ROTACION
    // =========================================

    [Header("ROTACION")]

    [Tooltip("Angulo maximo de giro hacia cada lado.")]
    [SerializeField] private float maxRotationAngle = 90f;


    // =========================================
    // ELEVADOR
    // =========================================

    [Header("ELEVADOR / ALTURA")]

    [Tooltip("Altura maxima que puede subir el avion desde su posicion inicial.")]
    [SerializeField] private float alturaMaxima = 10f;

    [Tooltip("Invierte la direccion del Dial Elevator.")]
    [SerializeField] private bool invertirDialElevator = false;


    // =========================================
    // HELICE
    // =========================================

    [Header("HELICE")]

    [Tooltip("Objeto independiente de la helice.")]
    [SerializeField] private Transform helice;

    [Tooltip("Velocidad maxima de giro de la helice.")]
    [SerializeField] private float velocidadGiroHelice = 1500f;

    [Tooltip("Eje local sobre el que gira la helice.")]
    [SerializeField] private Vector3 ejeGiroHelice = Vector3.forward;


    // =========================================
    // TEXTOS
    // =========================================

    [Header("TEXTOS DE LOS CONTROLES")]

    [Tooltip("Texto que muestra ON / OFF.")]
    [SerializeField] private TMP_Text textoLever;

    [Tooltip("Texto del joystick.")]
    [SerializeField] private TMP_Text textoJoystick;

    [Tooltip("Texto que muestra el angulo de rotacion.")]
    [SerializeField] private TMP_Text textoRotacion;

    [Tooltip("Texto que muestra la altura.")]
    [SerializeField] private TMP_Text textoElevator;

    [Tooltip("Texto que muestra la velocidad.")]
    [SerializeField] private TMP_Text textoVelocidad;


    // =========================================
    // VALORES DE LOS CONTROLES
    // =========================================

    private Vector2 joystickValue = Vector2.zero;

    private float knobValue = 0.5f;

    private float sliderValue = 0f;

    private float elevatorValue = 0f;


    // =========================================
    // ESTADO DEL SISTEMA
    // =========================================

    private bool hasBattery = false;

    private bool leverActivated = false;


    // =========================================
    // MOVIMIENTO
    // =========================================

    private float currentSpeed;

    private Quaternion initialRobotRotation;

    private Vector3 visualCenterLocalOffset = Vector3.zero;

    private float initialAltitude;


    // =========================================
    // AWAKE
    // =========================================

    private void Awake()
    {
        minSpeed = Mathf.Max(0f, minSpeed);

        maxSpeed = Mathf.Max(
            minSpeed,
            maxSpeed
        );

        alturaMaxima = Mathf.Max(
            0f,
            alturaMaxima
        );

        currentSpeed = minSpeed;
    }


    // =========================================
    // START
    // =========================================

    private void Start()
    {
        // -----------------------------------------
        // AVION
        // -----------------------------------------

        if (robot != null)
        {
            initialRobotRotation = robot.rotation;

            visualCenterLocalOffset =
                GetLocalCenterOffset(robot);

            // Guardar altura inicial
            initialAltitude = robot.position.y;
        }


        // -----------------------------------------
        // JOYSTICK
        // -----------------------------------------

        if (joystick != null)
        {
            joystickValue = new Vector2(
                joystick.value.x,
                joystick.value.y
            );
        }


        // -----------------------------------------
        // KNOB ROTACION
        // -----------------------------------------

        if (knob != null)
        {
            knobValue = knob.value;
        }


        // -----------------------------------------
        // SLIDER VELOCIDAD
        // -----------------------------------------

        if (slider != null)
        {
            sliderValue =
                Mathf.Clamp01(slider.value);
        }

        UpdateSpeed(sliderValue);


        // -----------------------------------------
        // DIAL ELEVATOR
        // -----------------------------------------

        if (dialElevator != null)
        {
            elevatorValue =
                Mathf.Clamp01(
                    dialElevator.value
                );
        }


        // -----------------------------------------
        // BATERIA
        // -----------------------------------------

        hasBattery = false;

        if (
            batterySocket != null &&
            batterySocket.hasSelection
        )
        {
            if (
                batteryToAccept == null ||
                batterySocket.interactablesSelected.Contains(
                    batteryToAccept
                )
            )
            {
                hasBattery = true;
            }
        }


        // -----------------------------------------
        // LEVER
        // -----------------------------------------

        if (lever != null)
        {
            // Detectar si ya estaba en ON
            leverActivated = lever.value;
        }
        else
        {
            leverActivated = false;
        }


        // Actualizar textos al comenzar
        ActualizarTextos();
    }


    // =========================================
    // ON ENABLE
    // =========================================

    private void OnEnable()
    {
        // -----------------------------------------
        // LEVER
        // -----------------------------------------

        if (lever != null)
        {
            lever.onLeverActivate.AddListener(
                OnLeverActivate
            );

            lever.onLeverDeactivate.AddListener(
                OnLeverDeactivate
            );
        }


        // -----------------------------------------
        // JOYSTICK
        // -----------------------------------------

        if (joystick != null)
        {
            joystick.onValueChangeX.AddListener(
                OnJoystickValueChangeX
            );

            joystick.onValueChangeY.AddListener(
                OnJoystickValueChangeY
            );
        }


        // -----------------------------------------
        // KNOB ROTACION
        // -----------------------------------------

        if (knob != null)
        {
            knob.onValueChange.AddListener(
                OnKnobValueChange
            );
        }


        // -----------------------------------------
        // SLIDER
        // -----------------------------------------

        if (slider != null)
        {
            slider.onValueChange.AddListener(
                OnSliderValueChange
            );
        }


        // -----------------------------------------
        // DIAL ELEVATOR
        // -----------------------------------------

        if (dialElevator != null)
        {
            dialElevator.onValueChange.AddListener(
                OnElevatorValueChange
            );
        }


        // -----------------------------------------
        // BATERIA
        // -----------------------------------------

        if (batterySocket != null)
        {
            batterySocket.selectEntered.AddListener(
                OnBatteryInserted
            );

            batterySocket.selectExited.AddListener(
                OnBatteryRemoved
            );
        }
    }


    // =========================================
    // ON DISABLE
    // =========================================

    private void OnDisable()
    {
        // -----------------------------------------
        // LEVER
        // -----------------------------------------

        if (lever != null)
        {
            lever.onLeverActivate.RemoveListener(
                OnLeverActivate
            );

            lever.onLeverDeactivate.RemoveListener(
                OnLeverDeactivate
            );
        }


        // -----------------------------------------
        // JOYSTICK
        // -----------------------------------------

        if (joystick != null)
        {
            joystick.onValueChangeX.RemoveListener(
                OnJoystickValueChangeX
            );

            joystick.onValueChangeY.RemoveListener(
                OnJoystickValueChangeY
            );
        }


        // -----------------------------------------
        // KNOB ROTACION
        // -----------------------------------------

        if (knob != null)
        {
            knob.onValueChange.RemoveListener(
                OnKnobValueChange
            );
        }


        // -----------------------------------------
        // SLIDER
        // -----------------------------------------

        if (slider != null)
        {
            slider.onValueChange.RemoveListener(
                OnSliderValueChange
            );
        }


        // -----------------------------------------
        // DIAL ELEVATOR
        // -----------------------------------------

        if (dialElevator != null)
        {
            dialElevator.onValueChange.RemoveListener(
                OnElevatorValueChange
            );
        }


        // -----------------------------------------
        // BATERIA
        // -----------------------------------------

        if (batterySocket != null)
        {
            batterySocket.selectEntered.RemoveListener(
                OnBatteryInserted
            );

            batterySocket.selectExited.RemoveListener(
                OnBatteryRemoved
            );
        }
    }


    // =========================================
    // UPDATE
    // =========================================

    private void Update()
    {
        // Mantener actualizado el estado de la palanca
        if (lever != null)
        {
            leverActivated = lever.value;
        }


        // -----------------------------------------
        // CONTROLES
        // -----------------------------------------

        if (CanControlRobot())
        {
            // Girar helice
            GirarHelice();

            if (robot != null)
            {
                // Movimiento horizontal
                MoveRobot();

                // Rotacion
                RotateRobot();

                // Altura
                MoveElevator();
            }
        }


        // -----------------------------------------
        // TEXTOS
        // -----------------------------------------

        ActualizarTextos();
    }


    // =========================================
    // ESTADO DEL SISTEMA
    // =========================================

    private bool CanControlRobot()
    {
        return hasBattery && leverActivated;
    }


    // =========================================
    // CENTRO VISUAL DEL AVION
    // =========================================

    private Vector3 GetLocalCenterOffset(
        Transform target
    )
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return Vector3.zero;

        Bounds bounds =
            renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(
                renderers[i].bounds
            );
        }

        return target.InverseTransformPoint(
            bounds.center
        );
    }


    // =========================================
    // LEVER
    // =========================================

    private void OnLeverActivate()
    {
        leverActivated = true;
    }


    private void OnLeverDeactivate()
    {
        leverActivated = false;

        joystickValue = Vector2.zero;
    }


    // =========================================
    // JOYSTICK
    // =========================================

    private void OnJoystickValueChangeX(
        float value
    )
    {
        joystickValue.x = value;
    }


    private void OnJoystickValueChangeY(
        float value
    )
    {
        joystickValue.y = value;
    }


    // =========================================
    // MOVIMIENTO HORIZONTAL
    // =========================================

    private void MoveRobot()
    {
        Vector3 movement =
            new Vector3(
                joystickValue.x,
                0f,
                joystickValue.y
            );

        robot.position +=
            movement *
            currentSpeed *
            Time.deltaTime;
    }


    // =========================================
    // KNOB DE ROTACION
    // =========================================

    private void OnKnobValueChange(
        float value
    )
    {
        knobValue =
            Mathf.Clamp01(value);
    }


    private void RotateRobot()
    {
        float angle =
            Mathf.Lerp(
                -maxRotationAngle,
                maxRotationAngle,
                knobValue
            );

        Quaternion targetRotation =
            initialRobotRotation *
            Quaternion.Euler(
                0f,
                angle,
                0f
            );

        Vector3 visualCenterBefore =
            robot.TransformPoint(
                visualCenterLocalOffset
            );

        robot.rotation =
            targetRotation;

        Vector3 visualCenterAfter =
            robot.TransformPoint(
                visualCenterLocalOffset
            );

        robot.position +=
            visualCenterBefore -
            visualCenterAfter;
    }


    // =========================================
    // SLIDER DE VELOCIDAD
    // =========================================

    private void OnSliderValueChange(
        float value
    )
    {
        sliderValue =
            Mathf.Clamp01(value);

        UpdateSpeed(sliderValue);
    }


    private void UpdateSpeed(
        float value
    )
    {
        /*
         * IMPORTANTE:
         *
         * En tu XRSlider:
         *
         * ARRIBA = value 0 = MAXIMA VELOCIDAD
         * ABAJO  = value 1 = MINIMA VELOCIDAD
         *
         * Por eso invertimos el valor.
         */

        float invertedValue =
            1f - Mathf.Clamp01(value);

        currentSpeed =
            Mathf.Lerp(
                minSpeed,
                maxSpeed,
                invertedValue
            );
    }


    // =========================================
    // DIAL ELEVATOR
    // =========================================

    private void OnElevatorValueChange(
        float value
    )
    {
        elevatorValue =
            Mathf.Clamp01(value);
    }


    private void MoveElevator()
    {
        if (robot == null)
            return;

        float value =
            Mathf.Clamp01(elevatorValue);

        // Invertir direccion si se desea
        if (invertirDialElevator)
        {
            value = 1f - value;
        }

        /*
         * 0%   = altura inicial
         * 50%  = mitad de altura
         * 100% = altura maxima
         */

        float targetAltitude =
            initialAltitude +
            (alturaMaxima * value);

        Vector3 posicion =
            robot.position;

        posicion.y =
            targetAltitude;

        robot.position =
            posicion;
    }


    // =========================================
    // HELICE
    // =========================================

    private void GirarHelice()
    {
        if (helice == null)
            return;

        if (maxSpeed <= 0f)
            return;

        // La velocidad de giro depende
        // de la velocidad del avion.

        float factorVelocidad =
            Mathf.Clamp01(
                currentSpeed / maxSpeed
            );

        float velocidadGiro =
            velocidadGiroHelice *
            factorVelocidad;

        helice.Rotate(
            ejeGiroHelice.normalized,
            velocidadGiro *
            Time.deltaTime,
            Space.Self
        );
    }


    // =========================================
    // BATERIA INSERTADA
    // =========================================

    private void OnBatteryInserted(
        SelectEnterEventArgs args
    )
    {
        if (
            batteryToAccept == null ||
            args.interactableObject ==
            batteryToAccept
        )
        {
            hasBattery = true;

            // Revisar inmediatamente
            // si el lever ya estaba en ON.

            if (lever != null)
            {
                leverActivated =
                    lever.value;
            }
        }
    }


    // =========================================
    // BATERIA REMOVIDA
    // =========================================

    private void OnBatteryRemoved(
        SelectExitEventArgs args
    )
    {
        if (
            batteryToAccept == null ||
            args.interactableObject ==
            batteryToAccept
        )
        {
            hasBattery = false;

            leverActivated = false;

            joystickValue =
                Vector2.zero;
        }
    }


    // =========================================
    // TEXTOS
    // =========================================

    private void ActualizarTextos()
    {
        // -----------------------------------------
        // LEVER
        // -----------------------------------------

        if (textoLever != null)
        {
            textoLever.text =
                leverActivated
                ? "ON"
                : "OFF";
        }


        // -----------------------------------------
        // JOYSTICK
        // -----------------------------------------

        if (textoJoystick != null)
        {
            textoJoystick.text =
                "Movimiento";
        }


        // -----------------------------------------
        // ROTACION
        // -----------------------------------------

        if (textoRotacion != null)
        {
            float angulo =
                Mathf.Lerp(
                    -maxRotationAngle,
                    maxRotationAngle,
                    knobValue
                );

            textoRotacion.text =
                "Rotacion: " +
                angulo.ToString("0") +
                "°";
        }


        // -----------------------------------------
        // ALTURA
        // -----------------------------------------

        if (
            textoElevator != null &&
            robot != null
        )
        {
            float altura =
                robot.position.y;

            textoElevator.text =
                "Altura: " +
                altura.ToString("0.0") +
                " mts";
        }


        // -----------------------------------------
        // VELOCIDAD
        // -----------------------------------------

        if (textoVelocidad != null)
        {
            textoVelocidad.text =
                "Velocidad: " +
                currentSpeed.ToString("0.0") +
                " s";
        }
    }


    // =========================================
    // ACTIVACION MANUAL
    // =========================================

    public void ActivarControl()
    {
        hasBattery = true;

        if (lever != null)
        {
            leverActivated =
                lever.value;
        }
    }


    // =========================================
    // DESACTIVAR
    // =========================================

    public void DesactivarControl()
    {
        hasBattery = false;

        leverActivated = false;

        joystickValue =
            Vector2.zero;
    }
}