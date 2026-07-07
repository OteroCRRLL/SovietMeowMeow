using UnityEngine;

public class PauseControlsPanel : MonoBehaviour
{
    [Tooltip("Objetos que se activan al mostrar los controles (texto de instrucciones, botón de volver, panel de fondo si lo hay...).")]
    public GameObject[] showWhenControlsOpen;

    [Tooltip("Objetos que se desactivan al mostrar los controles (los botones normales del menú).")]
    public GameObject[] hideWhenControlsOpen;

    private void OnEnable()
    {
        HideControls();
    }

    public void ShowControls()
    {
        SetActive(showWhenControlsOpen, true);
        SetActive(hideWhenControlsOpen, false);
    }

    public void HideControls()
    {
        SetActive(showWhenControlsOpen, false);
        SetActive(hideWhenControlsOpen, true);
    }

    private void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null) obj.SetActive(active);
        }
    }
}
