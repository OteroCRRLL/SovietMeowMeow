using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cartel de conclusión a pantalla completa que se muestra al terminar el ciclo de 3 días,
/// con un texto/imagen distinto según si se cumplió la cuota o no.
/// Vive dentro de _SYSTEMS (como prefab anidado OutroPanel), así que se vuelve a
/// registrar como singleton cada vez que se carga una escena.
/// </summary>
public class OutroScreenManager : MonoBehaviour
{
    public static OutroScreenManager instance;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public Image backgroundImage;
    public TextMeshProUGUI messageText;

    [Header("Timing")]
    public float fadeInDuration = 1f;
    public float displayDuration = 13f;
    public float fadeOutDuration = 1f;

    [Header("Quota Achieved")]
    [TextArea(3, 10)]
    public string quotaAchievedMessage = "QUOTA MET\n\nThe Party is satisfied... for now.";
    public Sprite quotaAchievedBackground;

    [Header("Quota Failed")]
    [TextArea(3, 10)]
    public string quotaFailedMessage = "QUOTA NOT MET\n\nYou've been fired, comrade.";
    public Sprite quotaFailedBackground;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void Show(bool quotaAchieved, Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        string message = quotaAchieved ? quotaAchievedMessage : quotaFailedMessage;
        Sprite background = quotaAchieved ? quotaAchievedBackground : quotaFailedBackground;
        StartCoroutine(ShowRoutine(message, background, onComplete));
    }

    private IEnumerator ShowRoutine(string message, Sprite background, Action onComplete)
    {
        Time.timeScale = 0f;

        if (messageText != null) messageText.text = message;
        if (backgroundImage != null && background != null) backgroundImage.sprite = background;

        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(displayDuration);

        Time.timeScale = 1f;

        onComplete?.Invoke();

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}
