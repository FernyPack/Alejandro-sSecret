using System.Collections;

public interface IScreenFader
{
    bool IsFading { get; }

    IEnumerator FadeOut(float duration);
    IEnumerator FadeIn(float duration);
}
