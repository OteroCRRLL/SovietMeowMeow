using UnityEngine;

public class TankSpawnerPoint : MonoBehaviour
{
    [Tooltip("Asigna aquí los waypoints por los que quieres que patrulle el tanque generado en este punto")]
    public Transform[] waypoints;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, new Vector3(4, 3, 6));

        // Dibujar líneas entre los waypoints para ver la ruta en el editor
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawSphere(waypoints[i].position, 0.5f);
                    
                    if (i < waypoints.Length - 1 && waypoints[i+1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
                    }
                }
            }
            
            // Cerrar el loop de la ruta
            if (waypoints.Length > 1 && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
            {
                Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
            }
        }
    }
}
