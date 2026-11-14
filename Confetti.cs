using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// confetti!
public class Confetti : MonoBehaviour
{
    class Piece
    {
        public Transform tr;
        public Vector3 vel;
        public float spin;
    }

    static readonly Color[] COLORS =
    {
        new Color(1f, 0.85f, 0.3f), new Color(1f, 0.4f, 0.4f), new Color(0.4f, 0.8f, 1f),
        new Color(0.5f, 1f, 0.6f), new Color(1f, 0.6f, 0.9f), Color.white
    };

    List<Piece> pieces = new List<Piece>();
    Coroutine rainCo;
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (pieces.Count > 0) UpdatePieces();
    }

    // a burst from the middle of the screen
    public void Burst(int count)
    {
        for (int i = 0; i < count; i++)
            Spawn(new Vector3(Random.Range(-1.5f, 1.5f), 0f, 0f),
                  new Vector3(Random.Range(-8f, 8f), Random.Range(5f, 14f), 0f));
    }

    // a big burst
    public void Rain(float seconds)
    {
        if (rainCo != null) StopCoroutine(rainCo);
        rainCo = StartCoroutine(RainRoutine(seconds));
    }

    // stops the rain and removes every piece
    public void Clear()
    {
        if (rainCo != null) { StopCoroutine(rainCo); rainCo = null; }
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i].tr != null) Destroy(pieces[i].tr.gameObject);
        pieces.Clear();
    }

    IEnumerator RainRoutine(float seconds)
    {
        Burst(100);
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        for (float t = 0; t < seconds; t += 0.06f)
        {
            for (int i = 0; i < 4; i++)
                Spawn(new Vector3(Random.Range(-halfW, halfW), halfH + 0.5f, 0f),
                      new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-3f, -0.5f), 0f));
            yield return new WaitForSeconds(0.06f);
        }
        rainCo = null;
    }

    void Spawn(Vector3 p, Vector3 v)
    {
        if (pieces.Count > 450) return;
        var go = new GameObject("Confetti");
        go.transform.position = p;
        go.transform.localScale = new Vector3(Random.Range(0.14f, 0.3f), Random.Range(0.08f, 0.16f), 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Rounded;
        sr.color = COLORS[Random.Range(0, COLORS.Length)];
        sr.sortingOrder = 50;
        var piece = new Piece();
        piece.tr = go.transform;
        piece.vel = v;
        piece.spin = Random.Range(-540f, 540f);
        pieces.Add(piece);
    }

    void UpdatePieces()
    {
        float floorY = -cam.orthographicSize - 1f;
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            Piece p = pieces[i];
            if (p.tr == null) { pieces.RemoveAt(i); continue; }
            p.vel.y = Mathf.Max(p.vel.y - 12f * Time.deltaTime, -3.5f);   // gravity
            p.vel.x *= 1f - 0.8f * Time.deltaTime;
            p.tr.position += p.vel * Time.deltaTime;
            p.tr.Rotate(0f, 0f, p.spin * Time.deltaTime);
            if (p.tr.position.y < floorY)
            {
                Destroy(p.tr.gameObject);
                pieces.RemoveAt(i);
            }
        }
    }
}
