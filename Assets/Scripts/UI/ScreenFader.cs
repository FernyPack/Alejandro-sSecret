using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour, IScreenFader
{
    [Header("Fade Visuals")]
    [Tooltip("The full screen Image that covers the screen. You can leave it turned OFF in the Hierarchy while editing.")]
    public Image fadeImage;

    [Tooltip("The color the screen fades to. Black by default.")]
    public Color fadeColor = Color.black;

    [Tooltip("Shape of the fade. Straight line = even speed. Curve = soft start and end.")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Start Settings")]
    [Tooltip("ON: the game starts with a clear screen. OFF: the game starts black.")]
    public bool startTransparent = true;

    private CanvasGroup canvasGroup;

    public bool IsFading { get; private set; }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (fadeImage != null)
            fadeImage.color = fadeColor;

        canvasGroup.alpha          = startTransparent ? 0f : 1f;
        canvasGroup.blocksRaycasts = !startTransparent;
        canvasGroup.interactable   = false;

        SetImageVisible(!startTransparent);

        ServiceLocator.Register<IScreenFader>(this);
    }

    public IEnumerator FadeOut(float duration)
    {
        return FadeTo(1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        return FadeTo(0f, duration);
    }

    private void SetImageVisible(bool visible)
    {
        if (fadeImage != null)
            fadeImage.gameObject.SetActive(visible);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        IsFading = true;
        SetImageVisible(true);
        canvasGroup.blocksRaycasts = true;

        float startAlpha = canvasGroup.alpha;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha          = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;
        SetImageVisible(targetAlpha > 0f);
        IsFading = false;
    }
}
