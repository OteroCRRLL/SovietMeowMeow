using UnityEngine;

[System.Serializable]
public class DayConfig
{
    public int dayNumber;
    
    [Header("Fuerzas Iniciales")]
    public int initialSquads;
    public int minSoldiersPerSquad = 2;
    public int maxSoldiersPerSquad = 4;
    public int initialDrones;
    public int initialTanks;

    [Header("Límites del Mapa (Ecosistema)")]
    public int maxActiveSoldiers = 15;
    public int maxActiveDrones = 2;

    [Header("Refuerzos")]
    public float reinforcementCooldown = 45f;
    public int reinforcementSquads = 1;
    public float droneRespawnTime = 60f;
}
