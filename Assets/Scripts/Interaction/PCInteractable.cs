using UnityEngine;
using TMPro;

public class PCInteractable : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public TextMeshProUGUI uiText;
    [SerializeField] private string interactionPrompt = "[F] - Usar PC";
    public string InteractionPrompt => interactionPrompt;

    [Header("Referencias")]
    [Tooltip("Arrastra aquí el objeto que tiene el script PCUIManager")]
    public PCUIManager pcUIManager;
    
    [Tooltip("Arrastra aquí el objeto 'Pc_Camera' que es hijo de este PC")]
    public Transform pcCameraTransform;

    public void Interact(GameObject interactor)
    {
        if (pcUIManager != null)
        {
            if (pcCameraTransform != null)
            {
                // Ocultar el texto de interacción al abrir el PC
                if (uiText != null)
                {
                    uiText.gameObject.SetActive(false);
                }
                
                pcUIManager.OpenPC(interactor, pcCameraTransform);
            }
            else
            {
                Debug.LogError("PCInteractable: Falta asignar la Pc_Camera en el inspector.");
            }
        }
        else
        {
            Debug.LogWarning("PCInteractable: Falta asignar el PCUIManager en el inspector.");
        }
    }

    public void OnHoverEnter()
    {
        if (uiText != null)
        {
            uiText.text = interactionPrompt;
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