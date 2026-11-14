using UnityEngine;


public class BoardGenerator
{
    const int N = GameConfig.N;

    readonly PathSolver solver = new PathSolver();

    // how many reliable routes the last board has 
    public int PathCount { get; private set; }

   
    public bool Solvable { get; private set; }

    public BoardGenerator()
    {
        PathCount = 3;
    }

    public BoardData Generate(int round, int target, bool redemption, RoundModifiers mods)
    {
        // 3+ = easy , 2 = medium, 1 = hard
        int wantTier = 3;
        if (!redemption && GameConfig.difficultyTiers && round > 1)
        {
            float r = Random.value;
            wantTier = r < 0.45f ? 3 : (r < 0.8f ? 2 : 1);
        }

        int cap = redemption ? GameConfig.REDEMPTION_ROUTES : (GameConfig.difficultyTiers ? 3 : 2);
        int maxDepth = mods.moves ? GameConfig.MOVE_LIMIT : 999;
        string label = redemption ? "Redemption" : "Round " + round;

        BoardData fallback = null;
        BoardData last = null;
        int fallbackCount = -1;

        for (int attempt = 1; attempt <= GameConfig.MAX_ATTEMPTS; attempt++)
        {
            BoardData b = Roll(redemption, mods);
            last = b;

            int c = solver.CountSolutions(b, target, cap, maxDepth, !redemption);
            if (c == 0) continue;

            bool ok;
            if (redemption) ok = c >= GameConfig.REDEMPTION_ROUTES;
            else if (!GameConfig.difficultyTiers) ok = c >= 2;
            else if (c >= 3) ok = wantTier == 3;
            else ok = solver.Complete && c == wantTier;   // exact counts of 1 or 2 need a finished search

            // normal boards must  let you reach the 95-99% band
            if (ok && !redemption && !solver.ReachesNearBand) ok = false;

            if (ok)
            {
                PathCount = Mathf.Min(c, 3);
                Solvable = true;
                Debug.Log(label + ": board ready after " + attempt + " roll(s), routes = " + c + (c >= cap ? "+" : ""));
                return b;
            }

            
            if (fallback == null || !redemption || c > fallbackCount)
            {
                fallback = b;
                fallbackCount = c;
            }
        }

        if (fallback != null)
        {
            PathCount = Mathf.Min(fallbackCount, 3);
            Solvable = true;
            Debug.LogWarning(label + ": no board matched exactly, using a fallback with " + fallbackCount + " route(s).");
            return fallback;
        }

        Solvable = false;
        return last;
    }


    BoardData Roll(bool friendly, RoundModifiers mods)
    {
        var b = new BoardData();
        float addEnd = friendly ? 0.52f : 0.38f;
        float subEnd = friendly ? 0.56f : 0.58f;
        float mulEnd = friendly ? 0.80f : 0.73f;
        float mysEnd = friendly ? 0.92f : 0.88f;

        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < N; y++)
            {
                float r = Random.value;
                TileKind k;
                int v = 0;
                if (r < addEnd) { k = TileKind.Add; v = Random.Range(2, 10); }
                else if (r < subEnd) { k = TileKind.Sub; v = Random.Range(1, 5); }
                else if (r < mulEnd) { k = TileKind.Mul; v = Random.value < 0.7f ? 2 : 3; }
                else if (r < mysEnd) { k = TileKind.Mystery; }
                else { k = TileKind.Gamble; }
                b.kind[x, y] = k;
                b.val[x, y] = v;
            }
        }
        b.kind[0, 0] = TileKind.Add;   // start tile
        b.val[0, 0] = 0;

        if (mods.rocks)
        {
            int placed = 0, guard = 0;
            while (placed < GameConfig.ROCK_COUNT && guard++ < 200)
            {
                int x = Random.Range(0, N), y = Random.Range(0, N);
                if ((x == 0 && y == 0) || b.kind[x, y] == TileKind.Wall) continue;
                b.kind[x, y] = TileKind.Wall;
                b.val[x, y] = 0;
                placed++;
            }
        }
        return b;
    }
}
