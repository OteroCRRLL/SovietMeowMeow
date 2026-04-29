using UnityEngine;

public class DroneController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform body; // El modelo 3D del Dron
    public Animator anim;

    [Header("Ajustes")]
    public float rotationSpeed = 8f;

    /// <summary>
    /// Rota suavemente el modelo 3D del dron para mirar hacia un punto en el espacio.
    /// A diferencia de los soldados que solo rotan en Y, el dron puede inclinarse hacia arriba/abajo (eje X).
    /// </summary>
    public void RotateTowardsPoint(Vector3 point)
    {
        if (body == null) return;

        Vector3 direction = (point - body.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            body.rotation = Quaternion.Slerp(body.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    /// <summary>
    /// Controla las animaciones del dron si tiene (ej: hélices girando más rápido al atacar).
    /// </summary>
    public void SetAnimationState(string state)
    {
        if (anim != null)
        {
            // Ejemplo: anim.Play(state); 
            // O anim.SetTrigger(state);
        }
    }
}
