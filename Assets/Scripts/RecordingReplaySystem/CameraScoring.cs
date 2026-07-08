using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CameraScoring : MonoBehaviour
{
    public static CameraScoring instance;

    [Header("Configuracin de Grabacin")]
    public float rayDistance = 50f;
    public int pointsPerTarget = 100;
    public List<string> targetTags = new List<string>();

    [Header("Bonus por Combate")]
    [Tooltip("Multiplicador extra aplicado a la puntuación de un objetivo mientras está activamente en combate (disparando, apuntando, en kamikaze...). 1 = +100% de bonus.")]
    public float combatBonusMultiplier = 1f;

    [Header("Bonus por Proximidad")]
    [Tooltip("Multiplicador extra en la distancia mínima (pegado al objetivo). 1 = +100% de bonus. El bonus baja de forma lineal hasta 0 en rayDistance.")]
    public float proximityBonusMultiplier = 1f;

    [Header("UI")]
    public TextMeshProUGUI viewsText;

    private int currentScore = 0;
    private int currentBaseScore = 0;
    private int currentCombatBonusScore = 0;
    private int currentProximityBonusScore = 0;
    private Dictionary<GameObject, float> recordedObjectsTime = new Dictionary<GameObject, float>();
    private Camera mainCamera;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        mainCamera = GetComponent<Camera>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Start()
    {
        FindViewsTextUI();
        UpdateUI();
    }

    private void FindViewsTextUI()
    {
        if (viewsText == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                TextMeshProUGUI[] texts = player.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI t in texts)
                {
                    if (t.gameObject.name == "Views" || t.gameObject.name.Contains("Views"))
                    {
                        viewsText = t;
                        break;
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (ReplayManager.instance != null && ReplayManager.instance.IsRecordingGlobal)
        {
            ScanForContent();
        }
    }

    private void ScanForContent()
    {
        // Deteccin usando OverlapSphere en vez de un simple Raycast hacia el centro.
        Collider[] colliders = Physics.OverlapSphere(transform.position, rayDistance);
        HashSet<GameObject> currentlyVisible = new HashSet<GameObject>();

        foreach (Collider col in colliders)
        {
            FactionIdentity faction = col.GetComponentInParent<FactionIdentity>();
            if (faction != null && faction.myFaction != FactionType.Player) // Solo enemigos o neutrales, no el jugador
            {
                Vector3 targetPos = col.bounds.center;
                Vector3 viewportPos = mainCamera.WorldToViewportPoint(targetPos);

                // Comprobar si est en el frustum (en pantalla)
                if (viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
                {
                    // Comprobar que no hay obstculos bloqueando la visin
                    Vector3 dirToTarget = targetPos - transform.position;
                    float distanceToTarget = dirToTarget.magnitude;

                    if (Physics.Raycast(transform.position, dirToTarget.normalized, out RaycastHit hit, distanceToTarget, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.root == col.transform.root)
                        {
                            currentlyVisible.Add(col.gameObject);
                        }
                        else
                        {
                            // A veces el raycast golpea la propia cámara o al jugador si la cámara está dentro de su collider
                            if (hit.transform.root == transform.root)
                            {
                                // Se lanza otro raycast ignorando al jugador
                                RaycastHit[] hits = Physics.RaycastAll(transform.position, dirToTarget.normalized, distanceToTarget, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                                
                                foreach (RaycastHit h in hits)
                                {
                                    if (h.transform.root == transform.root) continue;
                                    
                                    if (h.transform.root == col.transform.root)
                                    {
                                        currentlyVisible.Add(col.gameObject);
                                    }
                                    break; // El primer objeto que no sea la propia cámara/jugador determina si hay visión o no
                                }
                            }
                        }
                    }
                    else
                    {
                        // Si el raycast no golpea nada pero el objeto est ah, se considera visible
                        currentlyVisible.Add(col.gameObject);
                    }
                }
            }
        }

        bool updateScoreUI = false;

        // Sumar tiempo a los objetos detectados
        foreach (GameObject obj in currentlyVisible)
        {
            if (!recordedObjectsTime.ContainsKey(obj))
            {
                recordedObjectsTime[obj] = 0f;
            }

            if (recordedObjectsTime[obj] < 10f)
            {
                recordedObjectsTime[obj] += Time.deltaTime;
                if (recordedObjectsTime[obj] > 10f) recordedObjectsTime[obj] = 10f;
                updateScoreUI = true;
            }
        }

        if (updateScoreUI)
        {
            CalculateScore();
        }
    }

    private void CalculateScore()
    {
        int newBaseScore = 0;
        int newCombatBonusScore = 0;
        int newProximityBonusScore = 0;

        foreach (var kvp in recordedObjectsTime)
        {
            GameObject obj = kvp.Key;
            float timeVisible = kvp.Value;

            // Se necesita un mnimo de 2 segundos para dar puntuacin
            if (timeVisible < 2f) continue;
            if (obj == null) continue;

            int baseValue = (int)(pointsPerTarget * timeVisible);
            newBaseScore += baseValue;

            // Bonus por grabar el objetivo mientras está activamente en combate,
            // en vez de solo grabar cantidad de enemigos parados en pantalla.
            if (IsInCombat(obj))
            {
                newCombatBonusScore += (int)(baseValue * combatBonusMultiplier);
            }

            // Bonus por grabar de cerca en vez de a salvo desde lejos: máximo pegado al objetivo,
            // baja de forma lineal hasta 0 en rayDistance.
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            float proximityFactor = Mathf.Clamp01(1f - (distance / rayDistance));
            if (proximityFactor > 0f)
            {
                newProximityBonusScore += (int)(baseValue * proximityBonusMultiplier * proximityFactor);
            }
        }

        int newScore = newBaseScore + newCombatBonusScore + newProximityBonusScore;

        if (newScore != currentScore)
        {
            currentScore = newScore;
            currentBaseScore = newBaseScore;
            currentCombatBonusScore = newCombatBonusScore;
            currentProximityBonusScore = newProximityBonusScore;
            UpdateUI();
        }
    }

    /// <summary>
    /// Comprueba si el objetivo grabado está activamente en combate (disparando, apuntando,
    /// persiguiendo, en kamikaze...) leyendo el estado público de su IA, sin tocar esos scripts.
    /// </summary>
    private bool IsInCombat(GameObject obj)
    {
        SoldierBrain soldier = obj.GetComponentInParent<SoldierBrain>();
        if (soldier != null)
        {
            return soldier.CurrentState == SoldierState.Combat || soldier.CurrentState == SoldierState.HuntPlayer;
        }

        DroneBrain drone = obj.GetComponentInParent<DroneBrain>();
        if (drone != null)
        {
            return drone.CurrentState == DroneState.Locking || drone.CurrentState == DroneState.Kamikaze;
        }

        TankBrain tank = obj.GetComponentInParent<TankBrain>();
        if (tank != null)
        {
            return tank.CurrentState == TankState.Lock || tank.CurrentState == TankState.Shoot;
        }

        return false;
    }

    private void UpdateUI()
    {
        if (viewsText == null) FindViewsTextUI();

        if (viewsText != null)
        {
            viewsText.text = $"Views: {currentScore}";
        }
    }

    public void ShowFinalScore()
    {
        Debug.Log($"--- EXTRACCIN COMPLETADA --- \n VISITAS DEL VDEO TOTALES: {currentScore}");
    }

    public void ResetScore()
    {
        currentScore = 0;
        currentBaseScore = 0;
        currentCombatBonusScore = 0;
        currentProximityBonusScore = 0;
        recordedObjectsTime.Clear();
        UpdateUI();
        Debug.Log("Puntuacin reiniciada para un nuevo despliegue.");
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public int GetBaseScore()
    {
        return currentBaseScore;
    }

    public int GetCombatBonusScore()
    {
        return currentCombatBonusScore;
    }

    public int GetProximityBonusScore()
    {
        return currentProximityBonusScore;
    }
}