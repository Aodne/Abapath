using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// sin sounds
public class SoundPlayer : MonoBehaviour
{
    AudioSource sfx;
    Dictionary<int, AudioClip> cache = new Dictionary<int, AudioClip>();

    void Awake()
    {
        sfx = GetComponent<AudioSource>();
        if (sfx == null) sfx = gameObject.AddComponent<AudioSource>();
    }

    public void PlayTone(float freq, float dur, float vol = 0.4f)
    {
        int key = Mathf.RoundToInt(freq) * 1000 + Mathf.RoundToInt(dur * 1000f);
        AudioClip clip;
        if (!cache.TryGetValue(key, out clip))
        {
            const int rate = 44100;
            int n = (int)(rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float env = 1f - (float)i / n;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / rate) * env;
            }
            clip = AudioClip.Create("tone", n, 1, rate, false);
            clip.SetData(data, 0);
            cache[key] = clip;
        }
        sfx.PlayOneShot(clip, vol);
    }

    // plays quick notes
    public void Arp(params float[] freqs)
    {
        StartCoroutine(ArpRoutine(freqs));
    }

    IEnumerator ArpRoutine(float[] freqs)
    {
        foreach (float f in freqs)
        {
            PlayTone(f, 0.15f);
            yield return new WaitForSeconds(0.07f);
        }
    }

    // different sound for each
    public void PlayOpSound(TileKind kind, int before, int after, bool gambleWin, int stepNo)
    {
        if (kind == TileKind.Mul || (kind == TileKind.Gamble && gambleWin))
            Arp(523f, 784f, 1047f);                                                 // multiply chime
        else if (after > before)
            PlayTone(330f * Mathf.Pow(2f, Mathf.Min(stepNo, 12) / 12f), 0.12f);     // rising ping
        else if (after < before)
            Arp(392f, 294f);                                                        // falling ping
        else
            PlayTone(250f, 0.06f, 0.25f);
    }
}
