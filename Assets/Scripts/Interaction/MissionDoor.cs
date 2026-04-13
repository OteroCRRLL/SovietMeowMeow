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
            uiText.text = promptMessage;
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