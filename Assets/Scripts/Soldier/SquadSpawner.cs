using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SquadSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject soldierPrefab;
    public int minSoldiers = 2;
    public int maxSoldiers = 5;
    public FactionType squadFaction = FactionType.Soviet;
    
    [Header("Patrol Settings")]
    public float patrolRadius = 40f;

    private void Start()
    {
        SpawnSquad();
    }

    private void SpawnSquad()
    {
        if (soldierPrefab == null)
        {
            Debug.LogWarning("SquadSpawner: No soldier prefab assigned!");
            return;
        }

        int soldierCount = Random.Range(minSoldiers, maxSoldiers + 1);
        
        // Crear un objeto para organizar el escuadrón
        GameObject squadObj = new GameObject("Squad_" + squadFaction.ToString() + "_" + gameObject.name);
        SquadManager squadManager = squadObj.AddComponent<SquadManager>();
        
        List<SoldierBrain> spawnedSoldiers = new List<SoldierBrain>();

        for (int i = 0; i < soldierCount; i++)
        {
            // Instanciar cerca del spawner
            Vector2 randomCircle = Random.insideUnitCircle * 3f;
            Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Asegurar que caen en el NavMesh
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject soldierObj = Instantiate(soldierPrefab, spawnPos, transform.rotation);
            soldierObj.name = squadFaction.ToString() + "_Soldier_" + i;
            
            // Asignar facción
            FactionIdentity factionIdentity = soldierObj.GetComponent<FactionIdentity>();
            if (factionIdentity != null)
            {
                factionIdentity.myFaction = squadFaction;
            }

            SoldierBrain brain = soldierObj.GetComponent<SoldierBrain>();
            if (brain != null)
            {
                brain.squadManager = squadManager;
                brain.patrolRadius = patrolRadius; // Pasar el radio de patrulla
                spawnedSoldiers.Add(brain);
            }
        }

        // Inicializar el escuadrón
        squadManager.InitializeSquad(spawnedSoldiers);
        
        // Destruir el spawner ya que no se necesita más
        Destroy(gameObject);
    }
}