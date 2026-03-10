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
            GameLoopManager.instance.DeployToWarzone();
        }
        else
        {
            GameLoopManager.instance.ExtractPlayer();
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