using UnityEngine;
using TMPro;

public class ExtractionPoint : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public TextMeshProUGUI uiText;
    [TextArea] public string promptMessage = "[F] - Extract";

    public string InteractionPrompt => promptMessage; //

    public void Interact(GameObject player)
    {
        GameLoopManager.instance.ExtractPlayer();
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
        if (uiText != null) uiText.gameObject.SetActive(false);
    }
}