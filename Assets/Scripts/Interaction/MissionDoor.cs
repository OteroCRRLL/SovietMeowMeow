using UnityEngine;
using TMPro;

public class MissionDoor : MonoBehaviour, IInteractable
{
    [Header("Mission Settings")]
    public bool isDeploying = true;

    [Header("UI")]
    public TextMeshProUGUI uiText;
    [TextArea] public string promptMessage = "[E] - Usar Puerta";

    public string InteractionPrompt => promptMessage;

    public void Interact(GameObject player)
    {
        if (isDeploying)
        {
            if (GameManager.instance != null && GameManager.instance.hasDeployedToday)
            {
                Debug.Log("Misión Bloqueada: Ya has ido de misión hoy. Debes ir a dormir a la cama.");
                return;
            }

            if (HubManager.instance != null)
            {
                HubManager.instance.DeployToDay();
            }
            else
            {
                Debug.LogWarning("Falta el HubManager en la escena.");
            }
        }
        else
        {
            if (LevelManager.instance != null)
            {
                LevelManager.instance.ExtractPlayer();
            }
            else
            {
                Debug.LogWarning("Falta el LevelManager en la escena.");
            }
        }
    }

    public void OnHoverEnter()
    {
        if (uiText != null)
        {
            if (isDeploying && GameManager.instance != null && GameManager.instance.hasDeployedToday)
            {
                uiText.text = "Ya has desplegado. Ve a dormir.";
            }
            else
            {
                uiText.text = promptMessage;
            }
            uiText.gameObject.SetActive(true);
        }
    }

    public void OnHoverExit()
    {
        if (uiText != null)
        {
            uiText.gameObject.SetActive(false);
        }
    }
}