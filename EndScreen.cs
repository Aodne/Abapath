using System.Collections;
using UnityEngine;

//  final score screen
public class EndScreen : MonoBehaviour
{
    HudView hud;
    SoundPlayer sound;
    Confetti confetti;

    TextMesh title, scoreTm, bestTm, hint;
    Coroutine co;

    public void Init(HudView h, SoundPlayer s, Confetti c)
    {
        hud = h;
        sound = s;
        confetti = c;
    }
    //bust not confetti ;(
    public void Show(int score, int best, bool newBest, bool busted)
    {
        co = StartCoroutine(Run(score, best, newBest, busted));
    }

    public void Clear()
    {
        if (co != null) { StopCoroutine(co); co = null; }
        if (title != null) Destroy(title.gameObject);
        if (scoreTm != null) Destroy(scoreTm.gameObject);
        if (bestTm != null) Destroy(bestTm.gameObject);
        if (hint != null) Destroy(hint.gameObject);
        confetti.Clear();
        hud.SetPanel(false, 1f, 1f, 0f);
    }

    IEnumerator Run(int score, int best, bool newBest, bool busted)
    {
        hud.SetPanel(true, 10f, 6.4f, 0.85f);
        title = SpriteFactory.MakeText("EndTitle", new Vector3(0, 2.5f, 0), 54, busted ? new Color(1f, 0.5f, 0.5f) : Color.white, null);
        scoreTm = SpriteFactory.MakeText("EndScore", new Vector3(0, 1.0f, 0), 160, GameConfig.GOLD, null);
        bestTm = SpriteFactory.MakeText("EndBest", new Vector3(0, -0.9f, 0), 44, new Color(0.7f, 0.85f, 1f), null);
        hint = SpriteFactory.MakeText("EndHint", new Vector3(0, -2.2f, 0), 34, new Color(0.75f, 0.75f, 0.85f), null);
        title.GetComponent<MeshRenderer>().sortingOrder = 40;
        scoreTm.GetComponent<MeshRenderer>().sortingOrder = 40;
        bestTm.GetComponent<MeshRenderer>().sortingOrder = 40;
        hint.GetComponent<MeshRenderer>().sortingOrder = 40;

        title.text = busted ? "BUSTED OUT  -  FINAL SCORE" : "FINAL SCORE";
        scoreTm.text = "0";
        bestTm.text = "";
        hint.text = "";
        hud.Message = "GAME OVER";
        StartCoroutine(Juice.PopText(title.transform));
        if (!busted) confetti.Rain(4f);

        // count the final score up
        float dur = Mathf.Clamp(score / 400f, 0.8f, 2f);
        float nextTick = 0f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            float e = 1f - (1f - k) * (1f - k);
            scoreTm.text = Mathf.RoundToInt(score * e).ToString();
            if (t >= nextTick)
            {
                sound.PlayTone(450f + 60f * Mathf.Floor(k * 12f), 0.04f, 0.2f);
                nextTick = t + 0.05f;
            }
            yield return null;
        }
        scoreTm.text = score.ToString();
        StartCoroutine(Juice.PopText(scoreTm.transform));

        if (busted)
        {
            sound.Arp(392f, 330f, 262f);
        }
        else
        {
            sound.Arp(523f, 659f, 784f, 1047f, 1319f);
            confetti.Burst(90);
        }

        yield return new WaitForSeconds(0.3f);
        bestTm.text = newBest && score > 0 ? "NEW BEST!" : "BEST  " + best;
        bestTm.color = newBest && score > 0 ? GameConfig.GOLD : new Color(0.7f, 0.85f, 1f);
        StartCoroutine(Juice.PopText(bestTm.transform));
        hint.text = "Press SPACE to play again";
        co = null;
    }
}
