using System.Collections;
using UnityEngine;


public class HudView : MonoBehaviour
{
    static readonly string[] MYSTERY_OPTS = { "+3", "-5", "+12", "+7", "-2", "+9", "-6", "+1", "+4" };
    static readonly string[] COIN_OPTS = { "x2", GameConfig.DIV + "2" };

    SoundPlayer sound;
    CameraRig rig;
    Coroutine bannerCo;

    TextMesh scoreText, infoText, targetText, totalText, messageText, modText, counterText, bannerText, bankText;
    SpriteRenderer panelSr, bankBg, tutBg;

    // score counting up
    int scoreGoal;
    float shownScore;

    float displayVal, bumpT;
    int displayGoal, totalShown, realTotal, realTarget;
    bool flashing;
    Color flashColor;

    public bool BannerVisible { get; private set; }

    public bool SkipRequested { get; set; }

    public float DisplayValue { get { return displayVal; } }

    public string Message
    {
        get { return messageText.text; }
        set { messageText.text = value; }
    }

    public void Init(SoundPlayer s, CameraRig r)
    {
        sound = s;
        rig = r;

        scoreText = SpriteFactory.MakeText("Score", new Vector3(0, 6.9f, 0), 60, GameConfig.SCORE_COLOR, null);
        infoText = SpriteFactory.MakeText("Info", new Vector3(0, 6.1f, 0), 34, Color.white, null);
        targetText = SpriteFactory.MakeText("Target", new Vector3(0, 5.3f, 0), 56, new Color(0.7f, 0.85f, 1f), null);
        totalText = SpriteFactory.MakeText("Total", new Vector3(0, 4.3f, 0), 90, Color.white, null);

        modText = SpriteFactory.MakeText("Modifiers", new Vector3(-4.8f, 4.3f, 0), 30, new Color(1f, 0.6f, 0.25f), null);
        counterText = SpriteFactory.MakeText("Counters", new Vector3(4.8f, 4.3f, 0), 44, Color.white, null);

        bankBg = SpriteFactory.MakeBox("BankButton", new Vector3(0, -5f, 0), new Vector2(4.6f, 1.3f), new Color(0.95f, 0.8f, 0.2f), null);
        bankText = SpriteFactory.MakeText("BankLabel", new Vector3(0, -5f, 0), 34, Color.black, null);
        bankText.text = "BANK  [Space]";

        tutBg = SpriteFactory.MakeBox("TutorialButton", new Vector3(-6.2f, 6.9f, 0), new Vector2(3.4f, 0.8f), new Color(0.35f, 0.55f, 0.9f), null);
        SpriteFactory.MakeText("TutorialLabel", new Vector3(-6.2f, 6.9f, 0), 26, Color.white, null).text = "TUTORIAL [T]";

        messageText = SpriteFactory.MakeText("Message", new Vector3(0, -6.3f, 0), 36, Color.white, null);

        SpriteFactory.MakeText("Legend", new Vector3(0, -7.2f, 0), 24, new Color(0.7f, 0.7f, 0.8f), null).text =
            "purple ? = random +/-    orange = coin flip: x2 OR " + GameConfig.DIV + "2 (rounds down)    brown X = rock";

        panelSr = SpriteFactory.MakeBox("Panel", new Vector3(0, 0.2f, 0), new Vector2(10f, 3f), new Color(0f, 0f, 0f, 0.8f), null);
        panelSr.sortingOrder = 39;
        panelSr.gameObject.SetActive(false);

        bannerText = SpriteFactory.MakeText("Banner", new Vector3(0, 0.2f, 0), 40, new Color(1f, 0.95f, 0.6f), null);
        bannerText.GetComponent<MeshRenderer>().sortingOrder = 40;
        bannerText.text = "";

        ResetScore();
    }

    // call every frame
    public void Tick()
    {
        if (shownScore != scoreGoal)
        {
            shownScore = Mathf.MoveTowards(shownScore, scoreGoal, Mathf.Max(1f, Mathf.Abs(scoreGoal - shownScore) * 6f * Time.deltaTime));
            RefreshScoreText();
        }
        UpdateTotalDisplay();
    }

    public void SetInfo(string s) { infoText.text = s; }
    public void SetTarget(string s) { targetText.text = s; }
    public void SetModifierLabel(string s) { modText.text = s; }

    public void SetBank(string text, Color c)
    {
        bankText.text = text;
        bankBg.color = c;
    }

    public bool IsOverBank(Vector3 w)
    {
        return Mathf.Abs(w.x) < 2.3f && Mathf.Abs(w.y + 5f) < 0.65f;
    }
    public bool IsOverTutorial(Vector3 w)
    {
        return Mathf.Abs(w.x + 6.2f) < 1.7f && Mathf.Abs(w.y - 6.9f) < 0.4f;
    }

    // timer
    public void RefreshCounters(RoundModifiers mods, bool planning, float planLeft, float timeLeft, int movesLeft)
    {
        string s = "";
        bool warn = false;
        if (mods.timer)
        {
            if (planning) s += "PLAN " + Mathf.Max(0, Mathf.CeilToInt(planLeft));
            else
            {
                s += "TIME " + Mathf.Max(0, Mathf.CeilToInt(timeLeft));
                if (timeLeft <= 5f) warn = true;
            }
        }
        if (mods.moves)
        {
            s += (s.Length > 0 ? "\n" : "") + "MOVES " + movesLeft;
            if (movesLeft <= 3) warn = true;
        }
        counterText.text = s;
        if (warn) counterText.color = new Color(1f, 0.35f, 0.35f);
        else if (planning) counterText.color = new Color(0.5f, 0.85f, 1f);
        else counterText.color = Color.white;
    }

    public void ResetScore()
    {
        scoreGoal = 0;
        shownScore = 0;
        scoreText.color = GameConfig.SCORE_COLOR;
        RefreshScoreText();
    }

    public void SetScoreGoal(int goal) { scoreGoal = goal; }

    void RefreshScoreText()
    {
        scoreText.text = "SCORE " + Mathf.RoundToInt(shownScore);
    }

 
    public void ResetTotal(int total, int target)
    {
        realTotal = total;
        realTarget = target;
        displayVal = total;
        displayGoal = total;
        totalShown = total;
        totalText.text = total.ToString();
        totalText.transform.localScale = Vector3.one;
        totalText.transform.rotation = Quaternion.identity;
        flashing = false;
        UpdateTotalColor();
    }

    //  real total hange 
    public void SetRealTotal(int total, int target)
    {
        realTotal = total;
        realTarget = target;
    }

    public void SetDisplayGoal(int goal, Color flash)
    {
        displayGoal = goal;
        flashColor = flash;
        bumpT = 0f;
        flashing = true;
    }

    void UpdateTotalColor()
    {
        if (realTotal > realTarget) totalText.color = new Color(1f, 0.25f, 0.25f);
        else if (realTotal == realTarget) totalText.color = new Color(0.3f, 1f, 0.4f);
        else totalText.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.2f), Mathf.Clamp01((float)realTotal / realTarget));
    }

    void UpdateTotalDisplay()
    {
        float diff = displayGoal - displayVal;
        if (Mathf.Abs(diff) > 0.5f) displayVal += diff * (1f - Mathf.Exp(-14f * Time.deltaTime));
        else displayVal = displayGoal;

        int v = Mathf.RoundToInt(displayVal);
        if (v != totalShown)
        {
            totalShown = v;
            totalText.text = v.ToString();
        }

        if (flashing)
        {
            Transform tr = totalText.transform;
            bumpT += Time.deltaTime;
            float k = Mathf.Clamp01(bumpT / 0.4f);
            tr.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(k * Mathf.PI));
            tr.rotation = Quaternion.Euler(0, 0, Mathf.Sin(bumpT * 40f) * 6f * (1f - k));
            totalText.color = flashColor;
            if (k >= 1f)
            {
                flashing = false;
                tr.localScale = Vector3.one;
                tr.rotation = Quaternion.identity;
                UpdateTotalColor();
            }
        }
    }

    
    public void SetPanel(bool visible, float w, float h, float alpha)
    {
        panelSr.gameObject.SetActive(visible);
        panelSr.transform.localScale = new Vector3(w, h, 1f);
        panelSr.color = new Color(0f, 0f, 0f, alpha);
    }

    
    public IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds && !SkipRequested; t += Time.deltaTime) yield return null;
    }

    
    public void ShowBanner(string s, float duration)
    {
        if (bannerCo != null) StopCoroutine(bannerCo);
        bannerCo = StartCoroutine(BannerRoutine(s, duration));
    }

    
    public void HideBanner(bool keepPanel)
    {
        if (bannerCo != null) { StopCoroutine(bannerCo); bannerCo = null; }
        bannerText.text = "";
        BannerVisible = false;
        if (!keepPanel) SetPanel(false, 1f, 1f, 0f);
    }

    IEnumerator BannerRoutine(string s, float duration)
    {
        BannerVisible = true;
        bannerText.text = s;
        int lines = 1;
        foreach (char ch in s) if (ch == '\n') lines++;
        SetPanel(true, 11.5f, lines * 0.62f + 1f, 0.8f);

        Color c = new Color(1f, 0.95f, 0.6f, 1f);
        float fade = Mathf.Min(0.5f, duration * 0.3f);
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float sc = t < 0.25f
                ? Mathf.Lerp(0.3f, 1.1f, t / 0.25f)
                : Mathf.Lerp(1.1f, 1f, Mathf.Clamp01((t - 0.25f) / 0.2f));
            bannerText.transform.localScale = Vector3.one * sc;
            float a = t < duration - fade ? 1f : 1f - (t - (duration - fade)) / fade;
            c.a = a;
            bannerText.color = c;
            panelSr.color = new Color(0f, 0f, 0f, 0.8f * a);
            yield return null;
        }
        bannerText.text = "";
        BannerVisible = false;
        bannerCo = null;
        SetPanel(false, 1f, 1f, 0f);
    }

   
    public IEnumerator ShowBigText(string text, int size, Color c, float panelW, float panelH, float panelAlpha, float hold)
    {
        SetPanel(true, panelW, panelH, panelAlpha);
        TextMesh tm = SpriteFactory.MakeText("BigText", new Vector3(0, 0.2f, 0), size, c, null);
        tm.GetComponent<MeshRenderer>().sortingOrder = 40;
        tm.text = text;
        StartCoroutine(Juice.PopText(tm.transform));
        yield return StartCoroutine(Wait(hold));
        Destroy(tm.gameObject);
        SetPanel(false, 1f, 1f, 0f);
    }

   
    TextMesh SpawnPopup(Vector3 worldPos, Color c, int size)
    {
        var tm = SpriteFactory.MakeText("Popup", worldPos, size, c, null);
        tm.GetComponent<MeshRenderer>().sortingOrder = 30;
        tm.transform.localScale = Vector3.one * 0.01f;
        return tm;
    }

 
    public void ShowTileResult(Vector3 worldPos, TileKind kind, string opText, Color oc, bool spins)
    {
        TextMesh pop = SpawnPopup(worldPos, Color.white, 80);
        StartCoroutine(PopupRoutine(pop, kind, opText, oc, spins));
    }

    public void ShowGo()
    {
        TextMesh go = SpawnPopup(new Vector3(0, 0.2f, 0), new Color(0.4f, 1f, 0.55f), 120);
        go.text = "GO!";
        StartCoroutine(PopupLife(go));
    }

    IEnumerator PopupRoutine(TextMesh pop, TileKind kind, string opText, Color oc, bool spins)
    {
        if (spins)
        {
            pop.text = "?";
            yield return StartCoroutine(SpinPopup(pop, kind == TileKind.Mystery ? MYSTERY_OPTS : COIN_OPTS, GameConfig.SPIN_TIME));
        }
        if (pop == null) yield break;
        pop.text = opText;
        pop.color = oc;
        yield return StartCoroutine(PopupLife(pop));
    }

    
    IEnumerator SpinPopup(TextMesh tm, string[] opts, float dur)
    {
        float t = 0f, next = 0f, gap = 0.04f;
        int i = 0;
        tm.transform.localScale = Vector3.one;
        while (t < dur)
        {
            if (tm == null) yield break;
            if (t >= next)
            {
                tm.text = opts[i % opts.Length];
                i++;
                next = t + gap;
                gap *= 1.12f;
                sound.PlayTone(480f + (i % 4) * 90f, 0.04f, 0.2f);
            }
            tm.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 50f) * 8f);
            t += Time.deltaTime;
            yield return null;
        }
        if (tm != null) tm.transform.rotation = Quaternion.identity;
    }

   
    IEnumerator PopupLife(TextMesh tm)
    {
        Transform tr = tm.transform;
        Vector3 start = tr.position;
        Color c = tm.color;
        const float dur = 0.9f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            if (tm == null) yield break;
            float k = t / dur;
            float s = k < 0.15f
                ? Mathf.Sin(k / 0.15f * Mathf.PI * 0.5f) * 1.4f
                : Mathf.Lerp(1.4f, 1f, Mathf.Clamp01((k - 0.15f) / 0.15f));
            tr.localScale = Vector3.one * s;
            tr.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 30f) * 8f * (1f - k));
            tr.position = start + Vector3.up * (0.6f * k);
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            tm.color = c;
            yield return null;
        }
        if (tm != null) Destroy(tm.gameObject);
    }

   
    public IEnumerator CalcSequence(string[] rows, Color[] cols, int gained, int finalScore)
    {
        int n = rows.Length;
        const float spacing = 1.0f;
        SetPanel(true, 10f, n * spacing + 1f, 0.8f);

        var tms = new TextMesh[n];
        for (int i = 0; i < n; i++)
        {
            float y = 0.2f + ((n - 1) / 2f - i) * spacing;
            tms[i] = SpriteFactory.MakeText("CalcRow", new Vector3(0, y, 0), i == n - 1 ? 96 : 54, cols[i], null);
            tms[i].GetComponent<MeshRenderer>().sortingOrder = 40;
            tms[i].text = "";
        }

        for (int i = 0; i < n - 1; i++)
        {
            tms[i].text = rows[i];
            StartCoroutine(Juice.PopText(tms[i].transform));
            sound.PlayTone(440f + i * 110f, 0.1f);
            if (rows[i].Contains("EXACT"))
            {
                rig.Shake();
                sound.Arp(784f, 1047f);
            }
            yield return StartCoroutine(Wait(0.35f));
        }

        yield return StartCoroutine(CountUpRow(tms[n - 1], gained));
        sound.Arp(659f, 784f, 1047f);
        yield return StartCoroutine(Wait(0.25f));

     
        if (gained > 0)
        {
            Transform tr = tms[n - 1].transform;
            Vector3 from = tr.position;
            Vector3 to = scoreText.transform.position;
            const float flyTime = 0.25f;
            for (float t = 0; t < flyTime && !SkipRequested; t += Time.deltaTime)
            {
                float k = t / flyTime;
                tr.position = Vector3.Lerp(from, to, k * k);
                tr.localScale = Vector3.one * (1f - 0.7f * k);
                yield return null;
            }
        }

        scoreGoal = finalScore;
        StartCoroutine(Juice.Pop(scoreText.transform));

        for (int i = 0; i < n; i++) Destroy(tms[i].gameObject);
        SetPanel(false, 1f, 1f, 0f);
    }

    IEnumerator CountUpRow(TextMesh tm, int value)
    {
        const float dur = 0.55f;
        float nextTick = 0f;
        for (float t = 0; t < dur && !SkipRequested; t += Time.deltaTime)
        {
            float k = t / dur;
            float e = 1f - (1f - k) * (1f - k);
            tm.text = "= " + Mathf.RoundToInt(value * e);
            tm.transform.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(t * 30f));
            if (t >= nextTick)
            {
                sound.PlayTone(500f + 50f * Mathf.Floor(k * 14f), 0.04f, 0.2f);
                nextTick = t + 0.05f;
            }
            yield return null;
        }
        tm.text = "= " + value;
        tm.transform.localScale = Vector3.one;
        StartCoroutine(Juice.PopText(tm.transform));
    }
}
