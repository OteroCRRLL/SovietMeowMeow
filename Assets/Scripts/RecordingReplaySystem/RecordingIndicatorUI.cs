using UnityEngine;

public class RecordingIndicatorUI : MonoBehaviour
{
    private void Awake()
    {
        if (ReplayManager.instance != null)
        {
            ReplayManager.instance.LinkIndicatorUI(gameObject);
        }
        else
        {
            // Si el manager no existe en la escena, el objeto se autooculta por seguridad.
            gameObject.SetActive(false);
        }
    }
}