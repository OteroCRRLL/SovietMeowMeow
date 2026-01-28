using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshTest : MonoBehaviour
{
    [SerializeField]
    private Camera Camera;
    private NavMeshAgent Agent;

    private RaycastHit[] Hits = new RaycastHit[1];
    // Start is called before the first frame update
    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0))
        { 
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.RaycastNonAlloc(ray, Hits) > 0)
            {
                Agent.SetDestination(Hits[0].point);
            }
        }
    }
}
