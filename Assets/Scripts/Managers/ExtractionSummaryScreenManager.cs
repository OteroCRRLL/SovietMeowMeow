using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cartel a pantalla completa mostrado justo al extraer, con el desglose de la
/// puntuación conseguida (base + bonus por combate) y a cuánto dinero se traduce.
/// Vive dentro de _SYSTEMS (como prefab anidado ExtractionSummaryPanel), así que
/// se vuelve a registrar como singleton cada vez que se carga una escena.
/// </summary>
public class ExtractionSummaryScreenManager : MonoBehaviour
{
    public static ExtractionSummaryScreenManager instance;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public Image backgroundImage;
    public TextMeshProUGUI messageText;

    [Header("Timing")]
    public float fadeInDuration = 1f;
    public float displayDuration = 13f;
    public float fadeOutDuration = 1f;

    [Header("Contenido")]
    [Tooltip("Usa {base}, {bonus}, {total} y {money} como placeholders del desglose.")]
    [TextArea(8, 20)]
    public string messageTemplate = "FOOTAGE LOGGED\n\nBase footage: {base} views\nCombat bonus: {bonus} views\n\nTotal: {total} views\n\nPaid out: ${money}";
    public Sprite background;

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

    public void Show(int baseScore, int bonusScore, int totalScore, float moneyEarned, Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        string message = messageTemplate
            .Replace("{base}", baseScore.ToString())
            .Replace("{bonus}", bonusScore.ToString())
            .Replace("{total}", totalScore.ToString())
            .Replace("{money}", moneyEarned.ToString("0"));

        StartCoroutine(ShowRoutine(message, onComplete));
    }

    private IEnumerator ShowRoutine(string message, Action onComplete)
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
