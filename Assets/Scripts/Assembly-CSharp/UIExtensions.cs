using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UIExtensions
{
    private static List<RaycastResult> s_raycastResults = new List<RaycastResult>();
    public static bool IsPointerOverUIObject(this EventSystem eventSystem, Vector2 position)
    {
        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = position;
        eventSystem.RaycastAll(pointerEventData, s_raycastResults);
        bool result = s_raycastResults.Count > 0;
        s_raycastResults.Clear();
        return result;
    }

    public static IEnumerator PlayFadeInAnimation(this CanvasRenderer canvasRenderer, float time)
    {
        yield return canvasRenderer.PlayAlphaAnimation(time, 0f, 1f);
    }

    public static IEnumerator PlayFadeInAnimation(this CanvasGroup canvasGroup, float time)
    {
        yield return canvasGroup.PlayAlphaAnimation(time, 0f, 1f);
    }

    public static IEnumerator PlayFadeOutAnimation(this CanvasRenderer canvasRenderer, float time)
    {
        yield return canvasRenderer.PlayAlphaAnimation(time, 1f, 0f);
    }

    public static IEnumerator PlayFadeOutAnimation(this CanvasGroup canvasGroup, float time)
    {
        yield return canvasGroup.PlayAlphaAnimation(time, 1f, 0f);
    }

    public static IEnumerator PlayAlphaAnimation(this CanvasRenderer canvasRenderer, float time, float alpha0, float alpha1)
    {
        Action<float> setAlpha = delegate (float alpha)
        {
            canvasRenderer.SetAlpha(alpha);
        };
        yield return PlayAlphaAnimationInternal(time, alpha0, alpha1, setAlpha);
    }

    public static IEnumerator PlayAlphaAnimation(this CanvasGroup canvasGroup, float time, float alpha0, float alpha1)
    {
        Action<float> setAlpha = delegate (float alpha)
        {
            canvasGroup.alpha = alpha;
        };
        yield return PlayAlphaAnimationInternal(time, alpha0, alpha1, setAlpha);
    }

    private static IEnumerator PlayAlphaAnimationInternal(float time, float alpha0, float alpha1, Action<float> setAlpha)
    {
        int frameCount = (int)(time / Time.deltaTime);
        for (int i = 0; i < frameCount; i++)
        {
            float num = (float)i / (float)(frameCount - 1);
            float obj = alpha0 * (1f - num) + alpha1 * num;
            setAlpha(obj);
            yield return null;
        }
    }
}