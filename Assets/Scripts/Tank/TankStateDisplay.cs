using UnityEngine;
using TMPro;

public class TankStateDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("El script TankBrain del tanque para leer su estado.")]
    [SerializeField] private TankBrain tankBrain;
    
    [Tooltip("El componente TextMeshPro que mostrará el estado.")]
    [SerializeField] private TextMeshPro stateText;

    [Header("Colors")]
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color lockColor = Color.yellow;
    [SerializeField] private Color shootColor = Color.red;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        // Si no se asignan en el inspector, intenta encontrarlos automáticamente
        if (tankBrain == null)
        {
            tankBrain = GetComponentInParent<TankBrain>();
        }

        if (stateText == null)
        {
            stateText = GetComponent<TextMeshPro>();
        }
    }

    private void Update()
    {
        if (tankBrain != null && stateText != null)
        {
            UpdateStateDisplay();
        }

        FaceCamera();
    }

    private void UpdateStateDisplay()
    {
        TankState currentState = tankBrain.CurrentState;
        
        // Actualizar el texto
        stateText.text = currentState.ToString();

        // Actualizar el color según el estado
        switch (currentState)
        {
            case TankState.Patrol:
                stateText.color = patrolColor;
                break;
            case TankState.Lock:
                stateText.color = lockColor;
                break;
            case TankState.Shoot:
                stateText.color = shootColor;
                break;
        }
    }

    private void FaceCamera()
    {
        if (mainCamera != null)
        {
            // Efecto Billboard: Hacer que el texto siempre mire hacia la cámara
            // Multiplicar por Vector3.forward/up de la cámara evita que el texto se vea al revés
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
}
