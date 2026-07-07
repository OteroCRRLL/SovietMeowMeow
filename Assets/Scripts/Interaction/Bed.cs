using UnityEngine;
using TMPro;

public class Bed : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public TextMeshProUGUI uiText;
    [TextArea] public string promptMessage = "[E] - Sleep and Advance Day";

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

                    // Recarga el Hub para que se instancie el mensaje del LevelAnnouncer "Home - Day X"
                    System.Action goToHub = () =>
                    {
                        if (SceneController.instance != null)
                        {
                            SceneController.instance.LoadScene("Hub");
                        }
                        else
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene("Hub");
                        }
                    };

                    // Si se acaba de cumplir la cuota del ciclo de 3 días, se muestra el cartel antes de volver al Hub.
                    if (GameManager.instance.quotaJustAchieved && OutroScreenManager.instance != null)
                    {
                        OutroScreenManager.instance.Show(true, goToHub);
                    }
                    else
                    {
                        goToHub();
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
                uiText.text = "You haven't worked today yet.";
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
