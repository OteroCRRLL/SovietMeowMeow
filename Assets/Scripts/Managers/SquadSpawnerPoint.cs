using UnityEngine;

public class SquadSpawnerPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(2, 2, 2));
    }
}
