using UnityEngine;
using TMPro;

public class SoldierStateDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoldierBrain soldierBrain;
    [SerializeField] private TextMeshPro stateText;

    private Camera mainCamera;
    private HealthSystem healthSystem;
    private FactionIdentity factionIdentity;

    [Header("Colors")]
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color followColor = new Color(0.2f, 0.8f, 0.2f); // Verde azulado
    [SerializeField] private Color combatColor = Color.red;
    [SerializeField] private Color huntPlayerColor = new Color(1f, 0.5f, 0f); // Naranja
    [SerializeField] private Color reloadingColor = Color.yellow;
    [SerializeField] private Color deadColor = Color.gray;

    private void Start()
    {
        mainCamera = Camera.main;

        // Intentar buscar referencias si no están asignadas
        if (soldierBrain == null) soldierBrain = GetComponentInParent<SoldierBrain>();
        if (stateText == null) stateText = GetComponent<TextMeshPro>();

        // Conseguir referencias adicionales
        if (soldierBrain != null)
        {
            healthSystem = soldierBrain.GetComponent<HealthSystem>();
            factionIdentity = soldierBrain.GetComponent<FactionIdentity>();
        }
    }

    private void Update()
    {
        if (soldierBrain != null && stateText != null)
        {
            UpdateDisplay();
        }

        FaceCamera();
    }

    private void UpdateDisplay()
    {
        SoldierState currentState = soldierBrain.CurrentState;
        
        string factionStr = (factionIdentity != null) ? factionIdentity.myFaction.ToString() : "Unknown";
        string healthStr = (healthSystem != null) ? Mathf.CeilToInt(healthSystem.CurrentHealth).ToString() + "/" + Mathf.CeilToInt(healthSystem.maxHealth).ToString() : "---";

        // Formato final: [Facción]
        //               HP: 100/100
        //               ESTADO
        stateText.text = $"[{factionStr}]\nHP: {healthStr}\n{currentState.ToString()}";

        // Cambiar color según estado
        switch (currentState)
        {
            case SoldierState.Patrol:
                stateText.color = patrolColor;
                break;
            case SoldierState.FollowLeader:
                stateText.color = followColor;
                break;
            case SoldierState.Combat:
                stateText.color = combatColor;
                break;
            case SoldierState.HuntPlayer:
                stateText.color = huntPlayerColor;
                break;
            case SoldierState.Reloading:
                stateText.color = reloadingColor;
                break;
            case SoldierState.Dead:
                stateText.color = deadColor;
                stateText.text = $"[{factionStr}]\nHP: 0/{Mathf.CeilToInt(healthSystem?.maxHealth ?? 100)}\nDEAD";
                break;
        }
    }

    private void FaceCamera()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
}
