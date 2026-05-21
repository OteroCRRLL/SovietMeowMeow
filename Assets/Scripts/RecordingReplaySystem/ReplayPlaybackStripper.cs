using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Se añade a clones de replay antes de activarlos para evitar que NavMeshAgent registre agentes sin NavMesh.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class ReplayPlaybackStripper : MonoBehaviour
{
    private void Awake()
    {
        foreach (NavMeshAgent agent in GetComponentsInChildren<NavMeshAgent>(true))
        {
            if (agent != null)
            {
                Destroy(agent);
            }
        }
    }
}
