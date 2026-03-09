using UnityEngine;
using TMPro;

public class MissionDoor : MonoBehaviour, IInteractable
{
    [Header("Mission Settings")]
    [Tooltip("Activa esto si la puerta lleva a la Warzone. Desactívalo si lleva a la Base.")]
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