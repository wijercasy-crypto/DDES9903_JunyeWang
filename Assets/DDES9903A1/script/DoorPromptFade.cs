using System.Collections;
using UnityEngine;

public class DoorPromptFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (promptCanvasGroup == null)
        {
            Debug.LogError("DoorPromptFade: CanvasGroup Œ¥…Ë÷√°£", this);
            enabled = false;
            return;
        }

        promptCanvasGroup.alpha = 0f;
        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;
    }

    public void ShowPrompt()
    {
        StartFade(1f);
    }

    public void HidePrompt()
    {
        StartFade(0f);
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeTo(targetAlpha));
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = promptCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);

            promptCanvasGroup.alpha =
                Mathf.Lerp(startAlpha, targetAlpha, progress);

            yield return null;
        }

        promptCanvasGroup.alpha = targetAlpha;
        promptCanvasGroup.interactable = targetAlpha > 0.99f;
        promptCanvasGroup.blocksRaycasts = targetAlpha > 0.99f;

        fadeCoroutine = null;
    }
}