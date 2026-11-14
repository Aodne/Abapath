using UnityEngine;

// Which twists HURRY / LIMITED / FOG / ROCKS are active this round.
public class RoundModifiers
{
    public bool timer, moves, fog, rocks;
    int lastPicked = -1;

    public bool Any { get { return timer || moves || fog || rocks; } }

    public void Clear()
    {
        timer = false; moves = false; fog = false; rocks = false;
    }

    public void ResetHistory()
    {
        lastPicked = -1;
    }

    // the redemption round is just a 15 second clock, nothing else
    public void SetRedemption()
    {
        Clear();
        timer = true;
    }

    public void PickFor(int round)
    {
        Clear();
        //if (round == 1) return;   // warmup

        int count = round == GameConfig.ROUNDS ? 2 : 1;   // the final round two twists
        int[] pool = { 0, 1, 2, 3 };
        for (int i = pool.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int swap = pool[i]; pool[i] = pool[j]; pool[j] = swap;
        }
        if (pool[0] == lastPicked)   
        {
            int swap2 = pool[0]; pool[0] = pool[1]; pool[1] = swap2;
        }
        for (int i = 0; i < count; i++) Set(pool[i]);
        lastPicked = pool[0];
    }

    void Set(int id)
    {
        if (id == 0) timer = true;
        else if (id == 1) moves = true;
        else if (id == 2) fog = true;
        else rocks = true;
    }

    // small label shown beside the big total
    public string HudLabel()
    {
        string s = "";
        if (timer) s += "HURRY\n";
        if (moves) s += "MOVE LIMIT\n";
        if (fog) s += "FOG\n";
        if (rocks) s += "ROCKS\n";
        return s.TrimEnd('\n');
    }

    // extra lines for the round banner
    public string BannerLines()
    {
        string s = "";
        if (timer)
        {
            if (GameConfig.PLAN_TIME > 0f) s += "\nHURRY: plan for " + (int)GameConfig.PLAN_TIME + "s, then " + (int)GameConfig.TIME_LIMIT + "s to move";
            else s += "\nHURRY: " + (int)GameConfig.TIME_LIMIT + " seconds, then you auto-bank";
        }
        if (moves) s += "\nLIMITED: " + GameConfig.MOVE_LIMIT + " moves, then you auto-bank";
        if (fog)
        {
            if (timer && GameConfig.PLAN_TIME > 0f) s += "\nFOG: whole board while planning, then " + GameConfig.FOG_RADIUS + " tiles";
            else s += "\nFOG: you only see " + GameConfig.FOG_RADIUS + " tiles ahead";
        }
        if (rocks) s += "\nROCKS: some tiles are blocked";
        return s;
    }
}
