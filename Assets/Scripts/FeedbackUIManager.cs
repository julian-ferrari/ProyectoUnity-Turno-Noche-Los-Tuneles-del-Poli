using UnityEngine;
using UnityEngine.UI;

public class FeedbackUIManager : MonoBehaviour
{
    public static FeedbackUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Text feedbackText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeSpeed = 3f;

    private float timer = 0f;
    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (isShowing)
        {
            timer += Time.deltaTime;

            if (timer >= displayDuration)
            {
                HideMessage();
            }
        }
    }

    public void ShowMessage(string message, float duration = -1f)
    {
        if (feedbackText == null) return;

        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        isShowing = true;
        timer = 0f;

        if (duration > 0)
        {
            displayDuration = duration;
        }

        Debug.Log("Mensaje mostrado: " + message);
    }

    void HideMessage()
    {
        isShowing = false;

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }

        canvasGroup.alpha = 0f;
    }
}