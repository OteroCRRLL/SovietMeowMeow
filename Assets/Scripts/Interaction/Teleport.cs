using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class TeleportObject : MonoBehaviour, IInteractable
{
    [Header("Teleport Settings")]
    public Transform destinationPoint;

    [Header("UI")]
    public TextMeshProUGUI uiText;
    [TextArea] public string promptMessage = "[E] - Teleport";

    public string InteractionPrompt => promptMessage;

    public void Interact(GameObject player)
    {
        if (destinationPoint == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = destinationPoint.position;
        player.transform.rotation = destinationPoint.rotation;

        if (cc != null) cc.enabled = true;

        Debug.Log("Teleport");

    }

    //Visual logic (when raycast hovers object)
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
