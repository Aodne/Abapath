using UnityEngine;

// build sprites
public static class SpriteFactory
{
    static Sprite rounded, circle;
    static Font font;

    public static Sprite Rounded
    {
        get { if (rounded == null) rounded = MakeRoundedSquare(256, 64f); return rounded; }
    }

    public static Sprite Circle
    {
        get { if (circle == null) circle = MakeCircle(256); return circle; }
    }

    public static Font BuiltinFont
    {
        get { if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); return font; }
    }

    public static SpriteRenderer MakeBox(string name, Vector3 p, Vector2 size, Color c, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = p;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Rounded;
        sr.color = c;
        sr.sortingOrder = 0;
        return sr;
    }

    public static Transform MakeSprite(string name, Transform parent, Sprite sp, Vector3 localPos, Vector3 scale, Color c, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = c;
        sr.sortingOrder = order;
        return go.transform;
    }

    public static TextMesh MakeText(string name, Vector3 p, int fontSize, Color c, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = p;
        var tm = go.AddComponent<TextMesh>();
        tm.font = BuiltinFont;
        tm.fontSize = fontSize * GameConfig.FONT_SCALE;
        tm.characterSize = 0.1f / GameConfig.FONT_SCALE;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = c;
        var mr = go.GetComponent<MeshRenderer>();
        mr.material = BuiltinFont.material;
        mr.sortingOrder = 10;
        return tm;
    }

    // soft-edged circle 1 world unit across
    static Sprite MakeCircle(int size)
    {
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        float r = size / 2f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // rounded square 1 world unit across
    // rounded squaredev 1 world unit across
    static Sprite MakeRoundedSquare(int size, float radius)
    {
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        float half = size / 2f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(half - radius, half - radius);
                float d = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
