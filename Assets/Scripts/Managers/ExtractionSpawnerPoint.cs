using UnityEngine;

/// <summary>
/// Marca un punto donde puede aparecer el ExtractionPoint (humo rojo) al empezar el nivel.
/// Puede haber cualquier cantidad en la escena: LevelManager elige uno al azar cada día.
/// </summary>
public class ExtractionSpawnerPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
