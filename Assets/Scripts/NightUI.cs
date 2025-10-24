using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script OPCIONAL para mostrar el número de noche actual en la UI del juego
/// Coloca este componente en la escena PoliNights
/// </summary>
public class NightUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text que mostrará el número de noche (ej: 'Noche 1')")]
    public TextMeshProUGUI nightText;

    [Header("Display Settings")]
    [Tooltip("Formato del texto. {0} será reemplazado por el número")]
    public string textFormat = "NOCHE {0}";

    [Tooltip("Mostrar en la esquina superior")]
    public bool showInCorner = true;

    [Header("Animation (Opcional)")]
    [Tooltip("Animar el texto al aparecer")]
    public bool animateOnStart = true;

    [Tooltip("Duración de la animación de entrada")]
    public float animationDuration = 1f;

    [Header("Style")]
    public Color nightColor = Color.white;
    public int fontSize = 36;

    void Start()
    {
        // Si no hay referencia, crear UI automáticamente
        if (nightText == null)
        {
            CreateNightUI();
        }

        UpdateNightDisplay();

        if (animateOnStart)
        {
            StartCoroutine(AnimateEntry());
        }
    }

    void CreateNightUI()
    {
        // Buscar o crear Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("NightUI_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Crear texto
        GameObject textObj = new GameObject("NightText");
        textObj.transform.SetParent(canvas.transform, false);

        nightText = textObj.AddComponent<TextMeshProUGUI>();
        nightText.fontSize = fontSize;
        nightText.color = nightColor;
        nightText.alignment = TextAlignmentOptions.Center;
        nightText.fontStyle = FontStyles.Bold;

        // Configurar posición
        RectTransform rectTransform = nightText.GetComponent<RectTransform>();

        if (showInCorner)
        {
            // Esquina superior derecha
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-20, -20);
            rectTransform.sizeDelta = new Vector2(300, 80);
            nightText.alignment = TextAlignmentOptions.TopRight;
        }
        else
        {
            // Centro superior
            rectTransform.anchorMin = new Vector2(0.5f, 1);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = new Vector2(0, -20);
            rectTransform.sizeDelta = new Vector2(400, 80);
            nightText.alignment = TextAlignmentOptions.Top;
        }

        Debug.Log("NightUI creada automáticamente");
    }

    void UpdateNightDisplay()
    {
        if (nightText == null) return;

        // Obtener número de noche del NightSystem
        if (NightSystem.Instance != null)
        {
            int currentNight = NightSystem.Instance.GetCurrentNight();
            nightText.text = string.Format(textFormat, currentNight);

            Debug.Log($"Mostrando: {nightText.text}");
        }
        else
        {
            nightText.text = string.Format(textFormat, 1);
            Debug.LogWarning("NightSystem no encontrado, mostrando noche 1");
        }
    }

    System.Collections.IEnumerator AnimateEntry()
    {
        if (nightText == null) yield break;

        // Iniciar invisible
        Color originalColor = nightText.color;
        Color transparent = originalColor;
        transparent.a = 0f;
        nightText.color = transparent;

        // Empezar con escala pequeña
        Vector3 originalScale = nightText.transform.localScale;
        nightText.transform.localScale = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Smooth ease out
            t = 1f - Mathf.Pow(1f - t, 3f);

            // Animar alpha
            Color c = nightText.color;
            c.a = Mathf.Lerp(0f, originalColor.a, t);
            nightText.color = c;

            // Animar escala
            nightText.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);

            yield return null;
        }

        // Asegurar valores finales
        nightText.color = originalColor;
        nightText.transform.localScale = originalScale;
    }

    // Método público para actualizar manualmente
    public void RefreshDisplay()
    {
        UpdateNightDisplay();
    }

    // Para cuando el jugador avanza de noche
    public void OnNightChanged()
    {
        UpdateNightDisplay();

        if (animateOnStart)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateEntry());
        }
    }

    void OnValidate()
    {
        // Actualizar en el editor cuando cambien valores
        if (nightText != null)
        {
            nightText.fontSize = fontSize;
            nightText.color = nightColor;
        }
    }

    [ContextMenu("Force Update Display")]
    void ForceUpdate()
    {
        UpdateNightDisplay();
    }
}