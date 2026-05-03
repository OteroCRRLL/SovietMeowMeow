using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DayDirector : MonoBehaviour
{
    public static DayDirector instance;

    [Header("Configuración por Días")]
    public List<DayConfig> dayConfigs = new List<DayConfig>();

    [Header("Prefabs de Enemigos")]
    public GameObject soldierPrefab;
    public GameObject dronePrefab;
    public GameObject tankPrefab;

    [Header("Ajustes Generales")]
    public float mapCenterPatrolRadius = 40f;
    public Transform mapCenterPoint; // Objeto vacío en el centro del mapa para calcular inserciones

    private DayConfig currentConfig;
    private float reinforcementTimer = 0f;
    private float droneRespawnTimer = 0f;

    private List<SquadManager> activeSquads = new List<SquadManager>();
    private List<DroneBrain> activeDrones = new List<DroneBrain>();
    private List<TankBrain> activeTanks = new List<TankBrain>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Invoke("InitializeDay", 1f); // Esperar un segundo para asegurar que el GameManager y NavMesh estén listos
    }

    private void InitializeDay()
    {
        if (GameManager.instance == null) return;

        int currentDay = GameManager.instance.currentDay;
        currentConfig = dayConfigs.Find(c => c.dayNumber == currentDay);

        if (currentConfig == null && dayConfigs.Count > 0)
        {
            currentConfig = dayConfigs[dayConfigs.Count - 1]; // Fallback al último día si no hay más
        }

        if (currentConfig != null)
        {
            SpawnInitialForces();
        }
    }

    private void SpawnInitialForces()
    {
        // 1. Tanques (Solo inicio)
        List<TankSpawnerPoint> tankPoints = new List<TankSpawnerPoint>();
        for (int i = 0; i < currentConfig.initialTanks; i++)
        {
            FactionType fac = DetermineFaction(i, currentConfig.initialTanks);
            TankSpawnerPoint sp = GetAndRemoveSpawnPoint(ref tankPoints);
            if (sp != null) SpawnTank(fac, sp);
        }

        // 2. Drones (Inicio)
        List<DroneSpawnerPoint> dronePoints = new List<DroneSpawnerPoint>();
        for (int i = 0; i < currentConfig.initialDrones; i++)
        {
            FactionType fac = DetermineFaction(i, currentConfig.initialDrones);
            DroneSpawnerPoint sp = GetAndRemoveSpawnPoint(ref dronePoints);
            if (sp != null) SpawnDrone(fac, sp);
        }

        // 3. Escuadras (Inicio)
        List<SquadSpawnerPoint> squadPoints = new List<SquadSpawnerPoint>();
        for (int i = 0; i < currentConfig.initialSquads; i++)
        {
            FactionType fac = DetermineFaction(i, currentConfig.initialSquads);
            SquadSpawnerPoint sp = GetAndRemoveSpawnPoint(ref squadPoints);
            if (sp != null) SpawnSquad(fac, false, sp); // false = Patrulla normal, no son refuerzos
        }
    }

    private void Update()
    {
        if (currentConfig == null) return;

        CleanDeadEntities();

        // Lógica de Respawn de Drones
        if (activeDrones.Count < currentConfig.maxActiveDrones)
        {
            droneRespawnTimer -= Time.deltaTime;
            if (droneRespawnTimer <= 0f)
            {
                FactionType fac = DetermineFaction(Random.Range(0, 10), 10); // Random
                List<DroneSpawnerPoint> dronePoints = new List<DroneSpawnerPoint>();
                DroneSpawnerPoint sp = GetAndRemoveSpawnPoint(ref dronePoints);
                if (sp != null) SpawnDrone(fac, sp);
                droneRespawnTimer = currentConfig.droneRespawnTime;
            }
        }
        else
        {
            droneRespawnTimer = currentConfig.droneRespawnTime;
        }

        // Lógica de Refuerzos de Infantería
        int totalSoldiers = GetTotalActiveSoldiers();
        if (totalSoldiers < currentConfig.maxActiveSoldiers)
        {
            reinforcementTimer -= Time.deltaTime;
            if (reinforcementTimer <= 0f)
            {
                CheckForReinforcements();
                reinforcementTimer = currentConfig.reinforcementCooldown;
            }
        }
        else
        {
            reinforcementTimer = currentConfig.reinforcementCooldown;
        }
    }

    private void CheckForReinforcements()
    {
        int totalSoldiers = GetTotalActiveSoldiers();

        if (totalSoldiers < currentConfig.maxActiveSoldiers)
        {
            List<SquadSpawnerPoint> squadPoints = new List<SquadSpawnerPoint>();
            for (int i = 0; i < currentConfig.reinforcementSquads; i++)
            {
                // Si la suma va a exceder el máximo, no spawneamos más escuadras
                if (totalSoldiers + currentConfig.minSoldiersPerSquad > currentConfig.maxActiveSoldiers) break;

                FactionType fac = DetermineFaction(Random.Range(0, 10), 10); // Random
                SquadSpawnerPoint sp = GetAndRemoveSpawnPoint(ref squadPoints);
                if (sp != null) SpawnSquad(fac, true, sp); // true = Vienen de refuerzo (van al centro primero)
                
                totalSoldiers += currentConfig.maxSoldiersPerSquad; // Estimación para el siguiente loop
            }
        }
    }

    private void CleanDeadEntities()
    {
        activeSquads.RemoveAll(s => s == null || s.members.Count == 0);
        activeDrones.RemoveAll(d => d == null || d.CurrentState == DroneState.Dead);
        activeTanks.RemoveAll(t => t == null || t.CurrentState == TankState.Dead);
    }

    private int GetTotalActiveSoldiers()
    {
        int count = 0;
        foreach (var squad in activeSquads)
        {
            if (squad != null) count += squad.members.Count;
        }
        return count;
    }

    // --- REGLA DE FACCIONES ---
    private FactionType DetermineFaction(int index, int totalRequested)
    {
        // Obligar mínimo a 1 Soviet y 1 MeowMeow si se piden 2 o más
        if (totalRequested >= 2)
        {
            if (index == 0) return FactionType.Soviet;
            if (index == 1) return FactionType.MeowMeow;
        }
        
        return Random.value > 0.5f ? FactionType.Soviet : FactionType.MeowMeow;
    }

    private T GetAndRemoveSpawnPoint<T>(ref List<T> availablePoints) where T : MonoBehaviour
    {
        if (availablePoints == null || availablePoints.Count == 0)
        {
            T[] points = FindObjectsOfType<T>();
            if (points.Length == 0) return null;

            availablePoints = new List<T>(points);

            // Shuffle
            for (int i = 0; i < availablePoints.Count; i++)
            {
                T temp = availablePoints[i];
                int randomIndex = Random.Range(i, availablePoints.Count);
                availablePoints[i] = availablePoints[randomIndex];
                availablePoints[randomIndex] = temp;
            }
        }

        T selected = availablePoints[0];
        availablePoints.RemoveAt(0);
        return selected;
    }

    // --- FUNCIONES DE SPAWN ---
    private void SpawnTank(FactionType faction, TankSpawnerPoint spawner)
    {
        if (spawner == null) return;
        Transform spawnPoint = spawner.transform;
        GameObject tank = Instantiate(tankPrefab, spawnPoint.position, spawnPoint.rotation);
        
        FactionIdentity id = tank.GetComponent<FactionIdentity>();
        if (id != null) id.myFaction = faction;

        TankBrain brain = tank.GetComponent<TankBrain>();
        if (brain != null) 
        {
            // Pasarle los waypoints del spawner al cerebro del tanque
            brain.waypoints = spawner.waypoints;
            activeTanks.Add(brain);
        }
    }

    private void SpawnDrone(FactionType faction, DroneSpawnerPoint spawner)
    {
        if (spawner == null) return;
        Transform spawnPoint = spawner.transform;
        GameObject drone = Instantiate(dronePrefab, spawnPoint.position, spawnPoint.rotation);

        FactionIdentity id = drone.GetComponent<FactionIdentity>();
        if (id != null) id.myFaction = faction;

        DroneBrain brain = drone.GetComponent<DroneBrain>();
        if (brain != null) activeDrones.Add(brain);
    }

    private void SpawnSquad(FactionType faction, bool isReinforcement, SquadSpawnerPoint spawner)
    {
        if (spawner == null) return;
        Transform spawnPoint = spawner.transform;
        
        int soldierCount = Random.Range(currentConfig.minSoldiersPerSquad, currentConfig.maxSoldiersPerSquad + 1);
        
        GameObject squadObj = new GameObject("Squad_" + faction.ToString());
        SquadManager squadManager = squadObj.AddComponent<SquadManager>();
        
        List<SoldierBrain> spawnedSoldiers = new List<SoldierBrain>();

        for (int i = 0; i < soldierCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * 3f;
            Vector3 spawnPos = spawnPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject soldierObj = Instantiate(soldierPrefab, spawnPos, spawnPoint.rotation);
            soldierObj.name = faction.ToString() + "_Soldier_" + i;
            
            FactionIdentity factionIdentity = soldierObj.GetComponent<FactionIdentity>();
            if (factionIdentity != null) factionIdentity.myFaction = faction;

            SoldierBrain brain = soldierObj.GetComponent<SoldierBrain>();
            if (brain != null)
            {
                brain.squadManager = squadManager;
                brain.patrolRadius = mapCenterPatrolRadius; 
                spawnedSoldiers.Add(brain);
            }
        }

        squadManager.InitializeSquad(spawnedSoldiers);
        activeSquads.Add(squadManager);

        if (isReinforcement)
        {
            // Calcular un punto hacia el centro del mapa
            Vector3 targetCenter = mapCenterPoint != null ? mapCenterPoint.position : Vector3.zero;
            Vector3 dirToCenter = (targetCenter - spawnPoint.position).normalized;
            float distToCenter = Vector3.Distance(spawnPoint.position, targetCenter);
            
            // Que caminen una distancia moderada hacia el centro antes de empezar a patrullar a lo loco
            Vector3 insertionPoint = spawnPoint.position + dirToCenter * (distToCenter * 0.4f);
            
            if (NavMesh.SamplePosition(insertionPoint, out NavMeshHit hitCenter, 10f, NavMesh.AllAreas))
            {
                squadManager.StartInsertion(hitCenter.position);
            }
        }
    }
}
