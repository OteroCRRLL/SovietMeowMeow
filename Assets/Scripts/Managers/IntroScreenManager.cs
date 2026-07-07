using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cartel de introducción a pantalla completa que se muestra al empezar una partida nueva.
/// Tiene dos páginas consecutivas dentro del mismo fundido: primero el lore/reflexión,
/// luego las instrucciones de la misión.
/// Vive dentro de _SYSTEMS (como prefab anidado IntroPanel), así que se vuelve a
/// registrar como singleton cada vez que se carga una escena.
/// </summary>
public class IntroScreenManager : MonoBehaviour
{
    public static IntroScreenManager instance;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public Image backgroundImage;
    public TextMeshProUGUI messageText;

    [Header("Timing")]
    public float fadeInDuration = 1f;
    public float displayDuration = 13f;
    public float pageTransitionDuration = 0.5f;
    public float fadeOutDuration = 1f;

    [Header("Page 1 - Lore")]
    [TextArea(8, 20)]
    public string loreMessage = "How many times have you scrolled past twenty seconds of war footage before swiping to the next clip?";
    public Sprite loreBackground;

    [Header("Page 2 - Instructions")]
    [TextArea(8, 20)]
    public string instructionsMessage = "WAR CORRESPONDENT";
    public Sprite instructionsBackground;

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

    public void Show(Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(ShowRoutine(onComplete));
    }

    private IEnumerator ShowRoutine(Action onComplete)
    {
        Time.timeScale = 0f;
        canvasGroup.blocksRaycasts = true;

        // Página 1: lore / reflexión
        SetContent(loreMessage, loreBackground);
        yield return FadeCanvas(0f, 1f, fadeInDuration);
        yield return new WaitForSecondsRealtime(displayDuration);

        // Transición a página 2: instrucciones
        yield return FadeCanvas(1f, 0f, pageTransitionDuration);
        SetContent(instructionsMessage, instructionsBackground);
        yield return FadeCanvas(0f, 1f, pageTransitionDuration);
        yield return new WaitForSecondsRealtime(displayDuration);

        Time.timeScale = 1f;

        onComplete?.Invoke();

        yield return FadeCanvas(1f, 0f, fadeOutDuration);
        canvasGroup.blocksRaycasts = false;
    }

    private void SetContent(string message, Sprite background)
    {
        if (messageText != null) messageText.text = message;
        if (backgroundImage != null && background != null) backgroundImage.sprite = background;
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
