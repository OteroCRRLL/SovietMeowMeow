using UnityEngine;
using TMPro;

public class Bed : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public TextMeshProUGUI uiText;
    [TextArea] public string promptMessage = "[E] - Dormir y Avanzar Día";

    public string InteractionPrompt => promptMessage;

    public void Interact(GameObject player)
    {
        if (GameManager.instance != null)
        {
            if (!GameManager.instance.hasDeployedToday)
            {
                Debug.Log("Aún no has ido de misión hoy. ¡Ve a la puerta para desplegarte!");
            }
            else
            {
                bool survived = GameManager.instance.CompleteDay();
                
                if (survived)
                {
                    Debug.Log("Has dormido. Es un nuevo día.");
                    
                    // Guardado automático al ir a dormir
                    GameManager.instance.SaveGame();
                    
                    // Recargamos el Hub para que se instancie el mensaje del LevelAnnouncer "Home - Day X"
                    if (SceneController.instance != null)
                    {
                        SceneController.instance.LoadScene("Hub");
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Hub");
                    }
                }
            }
        }
    }

    public void OnHoverEnter()
    {
        if (uiText != null)
        {
            if (GameManager.instance != null && !GameManager.instance.hasDeployedToday)
            {
                uiText.text = "Aún no has trabajado hoy.";
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
