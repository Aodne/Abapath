using System.Collections;
using UnityEngine;

// reusable visual effects for popping and bouncing things
public static class Juice
{
    // quick grow-and-settle bump (tiles, score)
    public static IEnumerator Pop(Transform tr)
    {
        Vector3 baseScale = tr.localScale;
        for (float t = 0; t < 0.15f; t += Time.deltaTime)
        {
            if (tr == null) yield break;
            tr.localScale = baseScale * (1f + 0.25f * Mathf.Sin(t / 0.15f * Mathf.PI));
            yield return null;
        }
        if (tr != null) tr.localScale = baseScale;
    }

    // bigger overshoot pop for text that appears (banners, score rows)
    public static IEnumerator PopText(Transform tr)
    {
        const float dur = 0.2f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            if (tr == null) yield break;
            float k = t / dur;
            float s = k < 0.5f ? Mathf.Lerp(0.2f, 1.35f, k / 0.5f) : Mathf.Lerp(1.35f, 1f, (k - 0.5f) / 0.5f);
            tr.localScale = Vector3.one * s;
            yield return null;
        }
        if (tr != null) tr.localScale = Vector3.one;
    }
}
