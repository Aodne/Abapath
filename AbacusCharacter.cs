using System.Collections;
using UnityEngine;


public class AbacusCharacter : MonoBehaviour
{
    // bead positions 
    const float BEAM_X = -0.14f;
    const float HEAVEN_OFF = -0.355f, HEAVEN_ON = -0.205f;
    static readonly float[] EARTH_ON = { -0.075f, 0.025f, 0.125f, 0.225f };
    static readonly float[] EARTH_OFF = { 0.055f, 0.155f, 0.255f, 0.355f };
    static readonly Vector3 HEAVEN_SCALE = new Vector3(0.11f, 0.16f, 1f);
    static readonly Vector3 EARTH_SCALE = new Vector3(0.1f, 0.15f, 1f);

    static readonly Color FRAME_COLOR = new Color(0.45f, 0.28f, 0.12f);
    static readonly Color INNER_COLOR = new Color(0.99f, 0.92f, 0.72f);
    static readonly Color GHOST_FRAME = new Color(0.4f, 0.42f, 0.55f, 0.55f);
    static readonly Color GHOST_INNER = new Color(0.8f, 0.82f, 0.9f, 0.3f);
    static readonly Color HEAVEN_COLOR = new Color(0.9f, 0.28f, 0.22f);
    static readonly Color EARTH_COLOR = new Color(0.3f, 0.6f, 0.9f);

    SoundPlayer sound;
    SpriteRenderer body, inner;
    Transform[] eyeParts;
    Vector3[] eyeScales;
    Transform[] pupils;
    Vector3[] pupilBase;
    Transform[] heavenBeads = new Transform[3];
    Transform[,] earthBeads = new Transform[3, 4];
    bool isEmpty;
    Coroutine hopCo;

    public void Init(SoundPlayer s)
    {
        sound = s;
        Build();
        StartCoroutine(BlinkLoop());
    }

    void Build()
    {
        Sprite rounded = SpriteFactory.Rounded;
        Sprite circle = SpriteFactory.Circle;

        Color footColor = new Color(0.3f, 0.18f, 0.08f);
        SpriteFactory.MakeSprite("FootL", transform, rounded, new Vector3(-0.3f, -0.4f, 0), new Vector3(0.2f, 0.11f, 1f), footColor, 19);
        SpriteFactory.MakeSprite("FootR", transform, rounded, new Vector3(0.3f, -0.4f, 0), new Vector3(0.2f, 0.11f, 1f), footColor, 19);

        Transform frame = SpriteFactory.MakeSprite("Frame", transform, rounded, Vector3.zero, new Vector3(1.0f, 0.76f, 1f), FRAME_COLOR, 20);
        body = frame.GetComponent<SpriteRenderer>();
        Transform innerT = SpriteFactory.MakeSprite("Inner", transform, rounded, Vector3.zero, new Vector3(0.88f, 0.64f, 1f), INNER_COLOR, 21);
        inner = innerT.GetComponent<SpriteRenderer>();
        SpriteFactory.MakeSprite("Beam", transform, rounded, new Vector3(BEAM_X, 0, 0), new Vector3(0.04f, 0.6f, 1f), new Color(0.4f, 0.25f, 0.1f), 22);

        // rods and beads 
        float[] rowY = { 0.19f, 0f, -0.19f };
        for (int r = 0; r < 3; r++)
        {
            SpriteFactory.MakeSprite("Rod" + r, transform, rounded, new Vector3(0, rowY[r], 0), new Vector3(0.84f, 0.03f, 1f), new Color(0.35f, 0.35f, 0.4f), 22);
            heavenBeads[r] = SpriteFactory.MakeSprite("Heaven" + r, transform, circle, new Vector3(HEAVEN_OFF, rowY[r], 0), HEAVEN_SCALE, HEAVEN_COLOR, 23);
            for (int k = 0; k < 4; k++)
                earthBeads[r, k] = SpriteFactory.MakeSprite("Earth" + r + k, transform, circle, new Vector3(EARTH_OFF[k], rowY[r], 0), EARTH_SCALE, EARTH_COLOR, 23);
        }

        // eyes 
        var eyeL = SpriteFactory.MakeSprite("EyeL", transform, circle, new Vector3(-0.2f, 0.42f, 0), new Vector3(0.26f, 0.26f, 1f), Color.white, 24);
        var eyeR = SpriteFactory.MakeSprite("EyeR", transform, circle, new Vector3(0.2f, 0.42f, 0), new Vector3(0.26f, 0.26f, 1f), Color.white, 24);
        var pupL = SpriteFactory.MakeSprite("PupilL", transform, circle, new Vector3(-0.2f, 0.42f, 0), new Vector3(0.13f, 0.13f, 1f), new Color(0.1f, 0.1f, 0.15f), 25);
        var pupR = SpriteFactory.MakeSprite("PupilR", transform, circle, new Vector3(0.2f, 0.42f, 0), new Vector3(0.13f, 0.13f, 1f), new Color(0.1f, 0.1f, 0.15f), 25);

        eyeParts = new[] { eyeL, eyeR, pupL, pupR };
        eyeScales = new Vector3[eyeParts.Length];
        for (int i = 0; i < eyeParts.Length; i++) eyeScales[i] = eyeParts[i].localScale;
        pupils = new[] { pupL, pupR };
        pupilBase = new[] { pupL.localPosition, pupR.localPosition };
    }

    // beads
    public void UpdateAbacus(float value, bool snap)
    {
        if (isEmpty) return;
        int v = Mathf.Clamp(Mathf.RoundToInt(value), 0, 999);
        float k = snap ? 1f : 1f - Mathf.Exp(-16f * Time.deltaTime);
        for (int r = 0; r < 3; r++)
        {
            int d = r == 0 ? v / 100 : (r == 1 ? (v / 10) % 10 : v % 10);
            MoveBead(heavenBeads[r], d >= 5 ? HEAVEN_ON : HEAVEN_OFF, k);
            int e = d % 5;
            for (int i = 0; i < 4; i++) MoveBead(earthBeads[r, i], i < e ? EARTH_ON[i] : EARTH_OFF[i], k);
        }
    }

    void MoveBead(Transform b, float x, float k)
    {
        Vector3 p = b.localPosition;
        p.x = Mathf.Lerp(p.x, x, k);
        b.localPosition = p;
    }

    // empty = redemption
    public void SetEmpty(bool empty)
    {
        isEmpty = empty;
        for (int r = 0; r < 3; r++)
        {
            heavenBeads[r].gameObject.SetActive(!empty);
            if (!empty) heavenBeads[r].localScale = HEAVEN_SCALE;
            for (int i = 0; i < 4; i++)
            {
                earthBeads[r, i].gameObject.SetActive(!empty);
                if (!empty) earthBeads[r, i].localScale = EARTH_SCALE;
            }
        }
        body.color = empty ? GHOST_FRAME : FRAME_COLOR;
        inner.color = empty ? GHOST_INNER : INNER_COLOR;
    }

    // beads pop back onto the rods 
    public void Refill(float value)
    {
        SetEmpty(false);
        UpdateAbacus(value, true);
        float delay = 0f;
        for (int r = 0; r < 3; r++)
        {
            StartCoroutine(PopBead(heavenBeads[r], HEAVEN_SCALE, delay));
            delay += 0.05f;
            for (int i = 0; i < 4; i++)
            {
                StartCoroutine(PopBead(earthBeads[r, i], EARTH_SCALE, delay));
                delay += 0.05f;
            }
        }
    }

    IEnumerator PopBead(Transform b, Vector3 baseScale, float delay)
    {
        b.localScale = Vector3.zero;
        yield return new WaitForSeconds(delay);
        if (b == null) yield break;
        sound.PlayTone(500f + delay * 600f, 0.05f, 0.2f);
        const float dur = 0.2f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            if (b == null) yield break;
            float k = t / dur;
            float s = k < 0.6f ? Mathf.Lerp(0f, 1.4f, k / 0.6f) : Mathf.Lerp(1.4f, 1f, (k - 0.6f) / 0.4f);
            b.localScale = baseScale * s;
            yield return null;
        }
        if (b != null) b.localScale = baseScale;
    }

    // movement
    public void ResetTo(Vector3 p, bool empty)
    {
        StopHop();
        transform.position = p;
        transform.localScale = Vector3.one;
        SetEmpty(empty);
        LookAt(Vector2Int.zero);
    }

    // jump to a spot
    public void SnapTo(Vector3 p)
    {
        StopHop();
        transform.position = p;
    }

    public void HopTo(Vector3 dest)
    {
        if (hopCo != null) StopCoroutine(hopCo);
        hopCo = StartCoroutine(HopRoutine(dest));
    }

    IEnumerator HopRoutine(Vector3 b)
    {
        Vector3 a = transform.position;
        const float dur = 0.09f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            Vector3 p = Vector3.Lerp(a, b, k);
            p.y += 0.25f * Mathf.Sin(k * Mathf.PI);          //  hop
            transform.position = p;
            float s = Mathf.Sin(k * Mathf.PI) * 0.12f;        // squash and stretch
            transform.localScale = new Vector3(1f - s, 1f + s, 1f);
            yield return null;
        }
        transform.position = b;
        transform.localScale = Vector3.one;
        hopCo = null;
    }

    public void StopHop()
    {
        if (hopCo != null) { StopCoroutine(hopCo); hopCo = null; }
        transform.localScale = Vector3.one;
    }

    // bumped into a wall or a visited tile
    public void Bonk(Vector2Int dir, Vector3 basePos)
    {
        LookAt(dir);
        sound.PlayTone(150f, 0.08f, 0.3f);
        if (hopCo == null) StartCoroutine(BonkRoutine(dir, basePos));
    }

    IEnumerator BonkRoutine(Vector2Int d, Vector3 basePos)
    {
        Vector3 dir = new Vector3(d.x, d.y, 0);
        const float dur = 0.12f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            if (hopCo != null) yield break;   // a real move started
            transform.position = basePos + dir * 0.15f * Mathf.Sin(t / dur * Mathf.PI);
            yield return null;
        }
        if (hopCo == null) transform.position = basePos;
    }

 
    public void Squash()
    {
        transform.localScale = new Vector3(1.35f, 0.55f, 1f);
        transform.position += Vector3.down * 0.2f;
        body.color = new Color(0.8f, 0.2f, 0.2f);
    }

    public void Celebrate(Vector3 basePos)
    {
        StartCoroutine(CelebrateRoutine(basePos));
    }

    IEnumerator CelebrateRoutine(Vector3 basePos)
    {
        const float dur = 0.22f;
        for (int j = 0; j < 3; j++)
        {
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                transform.position = basePos + Vector3.up * 0.45f * Mathf.Sin(k * Mathf.PI);
                float s = Mathf.Sin(k * Mathf.PI) * 0.15f;
                transform.localScale = new Vector3(1f - s, 1f + s, 1f);
                yield return null;
            }
        }
        transform.position = basePos;
        transform.localScale = Vector3.one;
    }

    public void LookAt(Vector2Int d)
    {
        Vector3 off = new Vector3(d.x, d.y, 0) * 0.035f;
        for (int i = 0; i < pupils.Length; i++) pupils[i].localPosition = pupilBase[i] + off;
    }

    IEnumerator BlinkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            for (int i = 0; i < eyeParts.Length; i++)
                eyeParts[i].localScale = new Vector3(eyeScales[i].x, eyeScales[i].y * 0.1f, 1f);
            yield return new WaitForSeconds(0.1f);
            for (int i = 0; i < eyeParts.Length; i++)
                eyeParts[i].localScale = eyeScales[i];
        }
    }
}
