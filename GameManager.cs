using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    const int N = GameConfig.N;

    // Scene objects
    //[Header("Scene objects (optional, empty = created automatically)")]
    [SerializeField] CameraRig rig;
    [SerializeField] SoundPlayer sound;
    [SerializeField] Confetti confetti;
    [SerializeField] HudView hud;
    [SerializeField] BoardView board;
    [SerializeField] AbacusCharacter abacus;

    EndScreen endScreen;

    // puzzle
    BoardGenerator generator = new BoardGenerator();
    PathSolver solver = new PathSolver();
    RoundModifiers mods = new RoundModifiers();
    BoardData boardData;

    // run state
    int round, target, total, score, streak, steps, best;
    Vector2Int pos;
    float restartLock;
    bool roundActive, gameOver, animating;

    // redemption and bust
    bool redemption;
    int redemptionsLeft;

    // redemption rework
    const float REDEEM_FLASH = 1.5f;          // seconds the gold path is shown
    const float REDEEM_TIME = 5f;             // seconds to run it
    const bool BANK_REDEEM_DEADLY = false;    // true = stepping off in a bonus round also ends the run
    const int BANK_REDEEMS_PER_RUN = 3;       // set to 999 for unlimited bonus rounds

    bool bankRedeem, showingPath;
    int bankRedeemsLeft, bonusPoints, redemIdx;
    List<Vector2Int> redemPath = new List<Vector2Int>();

    // round clocks
    bool planning;
    float planLeft, timeLeft;
    int movesLeft, shownSec;

    // gold route (dev mode, tutorial, redemption flash)
    List<Vector2Int> bestPath = new List<Vector2Int>();
    bool[,] onBest = new bool[N, N];

    int PathCount { get { return generator.PathCount; } }

    // tutorial
    bool tutorial;
    int tutPage;

    static readonly string[] TUT_INTRO =
    {
        "TUTORIAL\nLand your total EXACTLY on the target.\nYou start at the bottom left tile.\n(Leaving resets your current run. ESC exits.)",
        "CONTROLS\nArrows / WASD: move one tile\nSPACE / Enter / click BANK: bank your total\nSPACE / click: continue or skip animations",
        "TILES\n+N adds    -N subtracts (never below 0)\nxN multiplies\npurple ? = 20% chance to complete the round,\n else a random + or -\norange = coin flip: x2 or " + GameConfig.DIV + "2\nbrown X = rock, you can't enter",
        "YOUR TURN\nGold tiles are the shortest route to the target.\nStep on them and watch your total.\nGo OVER the target and it's a BUST."
    };

    static readonly string[] TUT_OUTRO =
    {
        "SCORING\nBanked round = total x streak x keep%\nThe closer you bank to the target, the more you keep.\nStreak goes up by 1 each round you survive.",
        "EXACT HIT\nLand exactly on the target: keep 100%\nand your STREAK gets MULTIPLIED.\nHarder boards (fewer routes) multiply more.",
        "BUST AND REDEMPTION\nOver the target = 0 points, streak resets.\nThen a Redemption round: the winning path flashes gold for 1.5s,\nthen you have 5s to run it from memory. Step off and you're out.\nBank short and you get a bonus round to win back the points you lost.",
        "ROUND TWISTS\nTIMER: limited time per round\nMOVE LIMIT: limited moves\nROCKS: blocked tiles\nFOG: far tiles are hidden\nA banner tells you which are active.\n\nGood luck!"
    };

    void Start()
    {
        Application.targetFrameRate = 60;

        // use the scene object if you made one, otherwise build it here
        if (rig == null) rig = gameObject.AddComponent<CameraRig>();
        if (sound == null) sound = gameObject.AddComponent<SoundPlayer>();
        if (confetti == null) confetti = gameObject.AddComponent<Confetti>();

        if (hud == null) hud = gameObject.AddComponent<HudView>();
        hud.Init(sound, rig);

        endScreen = gameObject.AddComponent<EndScreen>();
        endScreen.Init(hud, sound, confetti);

        if (board == null) board = gameObject.AddComponent<BoardView>();

        if (abacus == null) abacus = new GameObject("Abacus").AddComponent<AbacusCharacter>();
        abacus.Init(sound);

        streak = 0;   // becomes 1 when round 1 starts
        redemptionsLeft = GameConfig.REDEMPTIONS_PER_RUN;
        bankRedeemsLeft = BANK_REDEEMS_PER_RUN;
        StartRound();
    }

    // ---------------- tutorial ----------------

    void StartTutorial()
    {
        StopAllCoroutines();
        hud.HideBanner(false);
        hud.SetPanel(false, 1f, 1f, 0f);
        endScreen.Clear();

        tutorial = true;
        tutPage = 0;
        gameOver = false;
        animating = false;
        redemption = false;
        showingPath = false;
        bankRedeem = false;
        round = 1;
        streak = 1;
        score = 0;
        target = 25;
        hud.ResetScore();

        BuildTutorialBoard();
        ShowTutPage();
    }

    void BuildTutorialBoard()
    {
        mods.ResetHistory();
        mods.PickFor(1);
        mods.timer = false; mods.moves = false; mods.rocks = false; mods.fog = false;

        total = GameConfig.START_TOTAL;
        steps = 0;
        boardData = generator.Generate(1, target, false, mods);

        System.Array.Clear(onBest, 0, onBest.Length);
        bestPath = solver.ShortestPath(boardData, target, 999);
        foreach (Vector2Int c in bestPath) onBest[c.x, c.y] = true;

        board.Build(boardData);
        pos = new Vector2Int(0, 0);
        abacus.ResetTo(board.CellToWorld(0, 0), false);
        hud.ResetTotal(total, target);
        abacus.UpdateAbacus(hud.DisplayValue, true);

        planning = false;
        roundActive = true;
        hud.SetModifierLabel("TUTORIAL");
        RefreshCounters();
        RefreshAll();
    }

    void ShowTutPage()
    {
        if (tutPage < TUT_INTRO.Length)
            hud.ShowBanner(TUT_INTRO[tutPage] + "\n\n[SPACE to continue]", 9999f);
        else
        {
            hud.HideBanner(false);
            hud.Message = "Move with arrows/WASD. Follow the gold tiles to " + target + "!";
        }
    }

    void ExitTutorial()
    {
        StopAllCoroutines();
        hud.HideBanner(false);
        hud.SetPanel(false, 1f, 1f, 0f);
        tutorial = false;
        animating = false;
        Restart();   // fresh real run
    }

    string TutorialHint(TileKind k)
    {
        string s;
        switch (k)
        {
            case TileKind.Add: s = "ADD tile: your total goes up."; break;
            case TileKind.Sub: s = "SUBTRACT tile: your total goes down."; break;
            case TileKind.Mul: s = "MULTIPLY tile: big jumps, careful not to overshoot!"; break;
            case TileKind.Mystery: s = "MYSTERY tile: a random amount is added or subtracted."; break;
            default: s = "COIN FLIP: 50% double, 50% half."; break;
        }
        if (steps >= 2) s += "  Press SPACE any time to bank early.";
        return s;
    }

    IEnumerator WaitForContinue()
    {
        yield return null;
        while (!(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
            yield return null;
    }

    IEnumerator TutorialEnd(bool bust, string prefix)
    {
        bool exact = !bust && total == target;

        // bust or stuck: explain and retry the same lesson
        if (bust || (!exact && prefix != ""))
        {
            hud.Message = bust ? "BUST! Over the target = 0 points. Let's try again."
                               : prefix + "Let's try again.";
            sound.PlayTone(110f, 0.5f, 0.6f);
            rig.Shake();
            abacus.Squash();
            yield return StartCoroutine(hud.ShowBigText(bust ? "BUST  +0" : "TRY AGAIN", 100, new Color(1f, 0.4f, 0.4f), 8.5f, 3f, 0.8f, 1.6f));
            animating = false;
            BuildTutorialBoard();
            hud.Message = "Try again! Follow the gold tiles to " + target + ".";
            yield break;
        }

        abacus.Celebrate(board.CellToWorld(pos.x, pos.y));
        if (exact)
        {
            confetti.Burst(60);
            sound.Arp(523f, 659f, 784f, 1047f);
            yield return StartCoroutine(hud.ShowBigText("EXACT HIT!", 100, GameConfig.GOLD, 9f, 3f, 0.85f, 1.8f));
        }
        else
        {
            int pctInt = Mathf.RoundToInt(ScoreRules.KeepPct(total, target) * 100f);
            hud.Message = "You banked early and kept " + pctInt + "%. An exact hit keeps 100%!";
            yield return StartCoroutine(hud.ShowBigText("BANKED  " + pctInt + "%", 90, new Color(0.6f, 0.85f, 1f), 9f, 3f, 0.85f, 2f));
        }

        foreach (string page in TUT_OUTRO)
        {
            hud.ShowBanner(page + "\n\n[SPACE to continue]", 9999f);
            yield return StartCoroutine(WaitForContinue());
        }
        hud.HideBanner(false);
        ExitTutorial();
    }


    void StartRound(bool redeem = false)
    {
        redemption = redeem;
        showingPath = false;
        if (!redeem)
        {
            bankRedeem = false;
            round++;
            streak++;
            target = 25 + 10 * (round - 1);
        }
        total = GameConfig.START_TOTAL;
        steps = 0;

        if (redeem)
        {
            mods.SetRedemption();
            mods.timer = true;   // redemption always runs on the clock
        }
        else mods.PickFor(round);

        boardData = generator.Generate(round, target, redemption, mods);
        if (redeem)
        {
            // the path has to exist, so retry a few times if needed
            for (int i = 0; i < 8 && !generator.Solvable; i++)
                boardData = generator.Generate(round, target, redemption, mods);
        }
        else if (!generator.Solvable && (mods.moves || mods.rocks))
        {
            // the twist made the board impossible, drop it and try again
            mods.moves = false;
            mods.rocks = false;
            boardData = generator.Generate(round, target, redemption, mods);
        }
        if (!generator.Solvable) Debug.LogWarning("Round " + round + ": could not build a solvable board!");

        // gold route: dev mode, or the redemption path
        System.Array.Clear(onBest, 0, onBest.Length);
        bestPath = new List<Vector2Int>();
        if (GameConfig.devSecret || redeem)
            bestPath = solver.ShortestPath(boardData, target, mods.moves ? GameConfig.MOVE_LIMIT : 999);

        if (GameConfig.devSecret && !redeem)
        {
            string log = "";
            foreach (Vector2Int c in bestPath)
            {
                onBest[c.x, c.y] = true;
                log += "(" + c.x + "," + c.y + ") ";
            }
            Debug.Log("[DEV] shortest route = " + bestPath.Count + " moves: " + log);
        }

        // the path the player has to follow in redemption (never includes the start tile)
        redemPath = new List<Vector2Int>(bestPath);
        if (redemPath.Count > 0 && redemPath[0] == Vector2Int.zero) redemPath.RemoveAt(0);
        redemIdx = 0;

        board.Build(boardData);
        pos = new Vector2Int(0, 0);

        abacus.ResetTo(board.CellToWorld(0, 0), redeem);   // empty and ghostly for redemption
        hud.ResetTotal(total, target);
        abacus.UpdateAbacus(hud.DisplayValue, true);

        planning = mods.timer && GameConfig.PLAN_TIME > 0f && !redeem;
        planLeft = GameConfig.PLAN_TIME;
        timeLeft = redeem ? REDEEM_TIME : GameConfig.TIME_LIMIT;
        movesLeft = GameConfig.MOVE_LIMIT;
        shownSec = -1;

        roundActive = true;
        hud.SetModifierLabel(redemption ? (bankRedeem ? "BOUNCE BACK ROUND" : "REDEMPTION") : mods.HudLabel());
        RefreshCounters();
        RefreshAll();

        // no banner in redemption, it would cover the flashing path
        if (redeem) hud.HideBanner(false);
        else hud.ShowBanner(BuildBannerText(), GameConfig.BANNER_TIME);

        if (redeem)
            StartCoroutine(RedemptionFlash());
        else if (planning)
            hud.Message = "PLAN! Study the board. Your first move (or " + (int)GameConfig.PLAN_TIME + "s) starts the clock.";
        else
            hud.Message = "Arrows/WASD move, SPACE banks (closer = keep more). Land on " + target + "!";

        if (GameConfig.devSecret && !redeem)
            hud.Message += "\n[DEV] gold = shortest route (" + (bestPath.Count > 0 ? bestPath.Count + " moves" : "none") + "), routes: " + PathCount + (PathCount >= 3 ? "+" : "");
    }

    IEnumerator RedemptionFlash()
    {
        showingPath = true;
        hud.Message = "MEMORIZE THE GOLD PATH!";
        foreach (Vector2Int c in redemPath) onBest[c.x, c.y] = true;
        RefreshAll();
        sound.PlayTone(520f, 0.1f);

        yield return new WaitForSeconds(REDEEM_FLASH);

        System.Array.Clear(onBest, 0, onBest.Length);   // hide it again
        showingPath = false;
        RefreshAll();
        hud.ShowGo();
        sound.PlayTone(660f, 0.1f);
        sound.PlayTone(880f, 0.15f);

        bool kills = !bankRedeem || BANK_REDEEM_DEADLY;
        hud.Message = (bankRedeem ? "BOUNCE BACK ROUND: " : "REDEMPTION: ") + "run the path from memory in " + (int)REDEEM_TIME + "s. "
                    + (kills ? "Step off it and you're out!" : "Step off it and you lose the bonus.");
    }

    string BuildBannerText()
    {
        if (redemption)
            return (bankRedeem ? "BOUNCE BACK ROUND" : "REDEMPTION ROUND") + "\nMemorize the gold path, then run it from memory";

        string s = round == GameConfig.ROUNDS ? "FINAL ROUND" : "ROUND " + round;
        if (!mods.Any)
            return round == 1 ? s + "\nLand exactly on " + target + "!\nBank closer to the target = keep more" : s;
        return s + mods.BannerLines();
    }

    void Update()
    {
        hud.Tick();
        abacus.UpdateAbacus(hud.DisplayValue, false);

        bool click = Input.GetMouseButtonDown(0);
        bool action = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

        if (tutorial && Input.GetKeyDown(KeyCode.Escape)) { ExitTutorial(); return; }
        if (!tutorial && Input.GetKeyDown(KeyCode.T)) { StartTutorial(); return; }
        if (!tutorial && click && hud.IsOverTutorial(rig.Cam.ScreenToWorldPoint(Input.mousePosition)))
        {
            StartTutorial();
            return;
        }

        if (gameOver)
        {
            if ((action || click) && Time.time >= restartLock) Restart();
            return;
        }

        if (!roundActive)
        {
            if (animating && (action || click)) hud.SkipRequested = true;   // skip the score animation
            return;
        }

        // tutorial intro pages: SPACE/click flips pages, no movement yet
        if (tutorial && tutPage < TUT_INTRO.Length)
        {
            if (action || click) { tutPage++; ShowTutPage(); }
            return;
        }

        // the first SPACE / click only closes the banner
        if (hud.BannerVisible && (action || click))
        {
            hud.HideBanner(animating);
            action = false;
            click = false;
        }

        if (mods.timer)
        {
            if (planning)
            {
                if (!hud.BannerVisible) planLeft -= Time.deltaTime;   // planning clock waits while the banner is up
                int psec = Mathf.Max(0, Mathf.CeilToInt(planLeft));
                if (psec != shownSec) { shownSec = psec; RefreshCounters(); }
                if (planLeft <= 0f) EndPlanning();
            }
            else
            {
                // redemption clock waits for the banner and the gold path flash
                if (!(redemption && (hud.BannerVisible || showingPath))) timeLeft -= Time.deltaTime;
                int sec = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
                if (sec != shownSec) { shownSec = sec; RefreshCounters(); }
                if (timeLeft <= 0f)
                {
                    StartCoroutine(Finish(false, 0f, "TIME UP! "));
                    return;
                }
            }
        }

        // clicking BANK
        if (click)
        {
            Vector3 w = rig.Cam.ScreenToWorldPoint(Input.mousePosition);
            if (hud.IsOverBank(w)) action = true;
        }
        if (action)
        {
            if (redemption)
            {
                hud.Message = "Banking is off here. Run the gold path from memory!";
                sound.PlayTone(150f, 0.08f, 0.3f);
            }
            else if (steps > 0) StartCoroutine(Finish(false));
            return;
        }

        Vector2Int d = Vector2Int.zero;
        if (Pressed(KeyCode.UpArrow, KeyCode.W)) d = Vector2Int.up;
        else if (Pressed(KeyCode.DownArrow, KeyCode.S)) d = Vector2Int.down;
        else if (Pressed(KeyCode.LeftArrow, KeyCode.A)) d = Vector2Int.left;
        else if (Pressed(KeyCode.RightArrow, KeyCode.D)) d = Vector2Int.right;

        if (d != Vector2Int.zero) TryMove(d);
    }

    bool Pressed(KeyCode a, KeyCode b)
    {
        return Input.GetKeyDown(a) || Input.GetKeyDown(b);
    }

    void EndPlanning()
    {
        planning = false;
        shownSec = -1;
        RefreshCounters();
        RefreshAll();   // fog closes in now, if there is any
        hud.ShowGo();
        sound.PlayTone(660f, 0.1f);
        sound.PlayTone(880f, 0.15f);
    }

    // ---------------- movement ----------------

    void TryMove(Vector2Int d)
    {
        Vector2Int np = pos + d;
        if (board.IsBlocked(np))
        {
            abacus.Bonk(d, board.CellToWorld(pos.x, pos.y));
            return;
        }

        if (redemption)
        {
            if (showingPath) return;   // no moving while the path is flashing

            Vector2Int want = redemIdx < redemPath.Count ? redemPath[redemIdx] : new Vector2Int(-1, -1);
            if (np != want)
            {
                // stepped off the path
                abacus.LookAt(d);
                abacus.HopTo(board.CellToWorld(np.x, np.y));
                pos = np;
                StartCoroutine(Finish(true, 0.4f, "OFF THE PATH! "));
                return;
            }
            redemIdx++;
        }

        if (planning) EndPlanning();                      // first move ends planning and starts the clock
        if (hud.BannerVisible) hud.HideBanner(animating);

        abacus.LookAt(d);
        abacus.HopTo(board.CellToWorld(np.x, np.y));

        Land(np);
    }

    void Land(Vector2Int np)
    {
        var t = board.At(np.x, np.y);
        t.visited = true;
        pos = np;
        steps++;
        if (mods.moves) movesLeft--;
        int before = total;

        // roll the random tiles now
        int mv = 0;
        bool gw = false;
        if (t.kind == TileKind.Mystery)
        {
            int chooser = Random.Range(1, 6);                 // 1..5, so 3 is a 20% chance
            if (chooser == 3) mv = target - total;            // lucky: lands exactly on target
            else mv = Random.Range(-6, 13);
        }

        if (t.kind == TileKind.Gamble) gw = Random.value < 0.5f;

        int after;
        string opText;
        switch (t.kind)
        {
            case TileKind.Add: after = total + t.value; opText = "+" + t.value; break;
            case TileKind.Sub: after = total - t.value; opText = "-" + t.value; break;
            case TileKind.Mul: after = total * t.value; opText = "x" + t.value; break;
            case TileKind.Mystery: after = total + mv; opText = mv >= 0 ? "+" + mv : mv.ToString(); break;
            default: after = gw ? total * 2 : total / 2; opText = gw ? "x2" : GameConfig.DIV + "2"; break;
        }
        after = Mathf.Max(0, after);
        Color oc = OpColor(t.kind, mv, gw);
        bool spins = t.kind == TileKind.Mystery || t.kind == TileKind.Gamble;

        // the tile remembers
        board.ShowResult(np.x, np.y, opText);

        // popup above the tile
        Vector3 pp = board.CellToWorld(np.x, np.y) + Vector3.up * 0.95f;
        hud.ShowTileResult(pp, t.kind, opText, oc, spins);

        // real total changes
        total = after;
        hud.SetRealTotal(total, target);
        string tag = t.kind == TileKind.Gamble ? "COIN FLIP " : (t.kind == TileKind.Mystery ? "? " : "");
        string msg = before + "  " + tag + opText + "  =  " + after;
        if (tutorial) msg += "\n" + TutorialHint(t.kind);

        if (spins)
        {
            // reveal the result
            StartCoroutine(RevealAfter(GameConfig.SPIN_TIME, t.kind, before, after, gw, oc, msg, steps));
        }
        else
        {
            hud.Message = msg;
            sound.PlayOpSound(t.kind, before, after, gw, steps);
            hud.SetDisplayGoal(after, oc);
        }

        board.PopTile(np.x, np.y);
        RefreshAll();
        RefreshCounters();

        float delay = spins ? GameConfig.SPIN_TIME : 0f;
        if (total > target) StartCoroutine(Finish(true, delay));
        else if (total == target) StartCoroutine(Finish(false, delay));
        else if (mods.moves && movesLeft <= 0) StartCoroutine(Finish(false, delay, "OUT OF MOVES! "));
        else if (!board.HasMoves(pos)) StartCoroutine(Finish(false, delay, "STUCK! "));
    }

    IEnumerator RevealAfter(float delay, TileKind kind, int before, int after, bool gw, Color oc, string msg, int stepNo)
    {
        yield return new WaitForSeconds(delay);
        sound.PlayOpSound(kind, before, after, gw, stepNo);
        hud.SetDisplayGoal(total, oc);
        if (steps == stepNo && roundActive) hud.Message = msg;
    }

    // popup
    Color OpColor(TileKind k, int mv, bool gw)
    {
        Color green = new Color(0.4f, 1f, 0.55f);
        Color red = new Color(1f, 0.4f, 0.4f);
        Color blue = new Color(0.5f, 0.7f, 1f);
        switch (k)
        {
            case TileKind.Add: return green;
            case TileKind.Sub: return red;
            case TileKind.Mul: return blue;
            case TileKind.Mystery: return mv >= 0 ? green : red;
            default: return gw ? blue : red;
        }
    }

    // ---------------- end of round ----------------

    IEnumerator Finish(bool bust, float delay = 0f, string prefix = "")
    {
        roundActive = false;
        planning = false;
        hud.HideBanner(animating);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        abacus.SnapTo(board.CellToWorld(pos.x, pos.y));
        animating = true;
        hud.SkipRequested = false;

        if (tutorial)
        {
            yield return StartCoroutine(TutorialEnd(bust, prefix));
            yield break;
        }

        // redemption round (bust redemption or bonus round)
        if (redemption)
        {
            bool redeemed = !bust && total == target;
            bool wasBonus = bankRedeem;
            yield return StartCoroutine(RedemptionEnd(redeemed, bust, prefix));

            animating = false;
            hud.SkipRequested = false;
            redemption = false;
            bankRedeem = false;

            bool dead = !redeemed && (!wasBonus || BANK_REDEEM_DEADLY);
            if (dead) EndGame(true);
            else if (round >= GameConfig.ROUNDS) EndGame(false);
            else StartRound();
            yield break;
        }

        if (bust)
        {
            // a bust
            streak = 0;
            bool canRedeem = redemptionsLeft > 0;
            hud.Message = canRedeem
                ? "BUST +0     Redeem yourself to keep playing (redemption earns no points)."
                : "BUST +0     that was your last chance";
            sound.PlayTone(110f, 0.5f, 0.6f);
            rig.Shake();
            abacus.Squash();
            RefreshAll();

            yield return StartCoroutine(hud.ShowBigText("BUST  +0", 100, new Color(1f, 0.4f, 0.4f), 8.5f, 3f, 0.8f, 1.6f));

            animating = false;
            hud.SkipRequested = false;
            if (canRedeem)
            {
                redemptionsLeft--;
                bankRedeem = false;
                StartRound(true);
            }
            else EndGame(true);
            yield break;
        }

        // normal bank
        bool exact = total == target;
        float pct = exact ? 1f : ScoreRules.KeepPct(total, target);
        int factor = exact ? ScoreRules.ExactFactor(PathCount) : 1;

        int oldStreak = streak;
        if (exact) streak *= factor;                 // exact hit directly boosts the streak
        int gained = Mathf.RoundToInt(total * streak * pct);
        score += gained;

        int reached = target > 0 ? total * 100 / target : 0;
        int pctInt = Mathf.RoundToInt(pct * 100f);

        string head = exact ? "EXACT! " : "Banked at " + reached + "%: ";
        hud.Message = prefix + head + total + " x " + streak
            + (exact ? " (streak " + oldStreak + " x " + factor + ")" : " x " + pctInt + "%")
            + " = +" + gained + "     (streak x" + (streak + 1) + " next round)";

        abacus.Celebrate(board.CellToWorld(pos.x, pos.y));

        var rows = new List<string>();
        var cols = new List<Color>();
        rows.Add("TOTAL  " + total); cols.Add(Color.white);
        rows.Add("x " + oldStreak + "   STREAK"); cols.Add(new Color(1f, 0.6f, 0.25f));
        if (exact) { rows.Add("x " + factor + "   EXACT HIT!  streak is now " + streak); cols.Add(GameConfig.GOLD); }
        else { rows.Add("x " + pctInt + "%   KEPT  (reached " + reached + "%)"); cols.Add(new Color(0.6f, 0.85f, 1f)); }
        rows.Add("= " + gained); cols.Add(new Color(0.4f, 1f, 0.55f));

        RefreshAll();
        yield return StartCoroutine(hud.CalcSequence(rows.ToArray(), cols.ToArray(), gained, score));
        yield return StartCoroutine(hud.Wait(0.4f));

        // banked short: offer a bonus round to win back the rest up to 100% (no exact bonus)
        int fullPts = Mathf.RoundToInt(total * streak);
        if (!exact && bankRedeemsLeft > 0 && fullPts > gained)
        {
            bankRedeemsLeft--;
            bonusPoints = fullPts - gained;
            bankRedeem = true;
            yield return StartCoroutine(hud.ShowBigText("BOUNCE BACK ROUND  +" + bonusPoints, 80, GameConfig.GOLD, 10f, 3f, 0.85f, 1.6f));
            animating = false;
            hud.SkipRequested = false;
            StartRound(true);
            yield break;
        }

        animating = false;
        hud.SkipRequested = false;
        if (round >= GameConfig.ROUNDS) EndGame(false);
        else StartRound();
    }

    IEnumerator RedemptionEnd(bool redeemed, bool bust, string prefix)
    {
        string text;
        Color col;
        if (redeemed)
        {
            col = new Color(0.4f, 1f, 0.55f);
            if (bankRedeem)
            {
                text = "BOUNCE BACK POINTS +" + bonusPoints;
                score += bonusPoints;
                hud.SetScoreGoal(score);
                hud.Message = "Perfect run! You get the full points: +" + bonusPoints + " extra.";
            }
            else
            {
                text = "Bounced Back!";
                hud.Message = "BOUNCED BACK! You're back in the game. (Redemption earns no points, your score is still " + score + ".)";
            }
            abacus.Refill(hud.DisplayValue);   // the beads pop back onto the rods
            confetti.Burst(60);
            sound.Arp(523f, 659f, 784f, 1047f);
            abacus.Celebrate(board.CellToWorld(pos.x, pos.y));
        }
        else
        {
            string why = prefix != "" ? prefix : (bust ? "BUST! " : "");
            if (bankRedeem && !BANK_REDEEM_DEADLY)
            {
                text = "MISSED IT";
                col = new Color(1f, 0.7f, 0.3f);
                hud.Message = why + "You keep your banked points.";
            }
            else
            {
                text = "REDEMPTION\nFAILED";
                col = new Color(1f, 0.4f, 0.4f);
                hud.Message = why + "No more chances.";
            }
            sound.PlayTone(110f, 0.5f, 0.6f);
            rig.Shake();
        }
        yield return StartCoroutine(hud.ShowBigText(text, redeemed ? 100 : 76, col, 9f, 2.6f, 0.85f, redeemed ? 1.7f : 1.5f));
    }

    // game over
    void EndGame(bool lost)
    {
        gameOver = true;
        restartLock = Time.time + 2.2f;   // so skip can't restart immediately
        bool newBest = score > best;
        if (newBest) best = score;
        endScreen.Show(score, best, newBest, lost);
    }

    void Restart()
    {
        endScreen.Clear();
        gameOver = false;
        redemption = false;
        bankRedeem = false;
        showingPath = false;
        redemptionsLeft = GameConfig.REDEMPTIONS_PER_RUN;
        bankRedeemsLeft = BANK_REDEEMS_PER_RUN;
        round = 0;
        score = 0;
        streak = 0;
        mods.ResetHistory();
        hud.ResetScore();
        StartRound();
    }

    void RefreshCounters()
    {
        hud.RefreshCounters(mods, planning, planLeft, timeLeft, movesLeft);
    }

    void RefreshAll()
    {
        board.Refresh(pos, planning, mods.fog, onBest);

        if (redemption)
        {
            hud.SetInfo("Round " + round + "/" + GameConfig.ROUNDS + "     " + (bankRedeem ? "BONUS ROUND" : "REDEMPTION ROUND") + "     run the gold path from memory");
        }
        else
        {
            string tier = GameConfig.difficultyTiers ? "  (" + ScoreRules.TierName(PathCount) + ")" : "";
            hud.SetInfo("Round " + round + "/" + GameConfig.ROUNDS + "     STREAK x" + streak + "     exact hit = streak x" + ScoreRules.ExactFactor(PathCount) + tier);
        }
        hud.SetTarget("TARGET " + target);
        RefreshBank();
    }

    // the bank button
    void RefreshBank()
    {
        if (redemption)
        {
            hud.SetBank((bankRedeem ? "Bounce Back Round" : "REDEMPTION") + "\nrun the path", new Color(0.55f, 0.45f, 0.75f));
            return;
        }
        if (total > target)
        {
            hud.SetBank("BUST!", new Color(0.9f, 0.35f, 0.3f));
            return;
        }
        if (steps == 0)
        {
            hud.SetBank("BANK  [Space]\nmake a move first", new Color(0.6f, 0.6f, 0.65f));
            return;
        }

        bool exact = total == target;
        float pct = exact ? 1f : ScoreRules.KeepPct(total, target);
        int pts = Mathf.RoundToInt(total * streak * (exact ? ScoreRules.ExactFactor(PathCount) : 1) * pct);

        Color c;
        if (pct >= 1f) c = new Color(0.3f, 0.95f, 0.45f);
        else if (pct >= 0.9f) c = new Color(0.6f, 0.9f, 0.35f);
        else if (pct >= 0.75f) c = new Color(0.95f, 0.8f, 0.2f);
        else if (pct >= 0.4f) c = new Color(0.95f, 0.6f, 0.25f);
        else c = new Color(0.9f, 0.4f, 0.35f);

        hud.SetBank("BANK  [Space]\nkeep " + Mathf.RoundToInt(pct * 100f) + "%  =  +" + pts, c);
    }
}