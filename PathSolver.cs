using System.Collections.Generic;
using UnityEngine;
public class PathSolver
{
    const int N = GameConfig.N;

    BoardData board;
    bool[,] seen;
    int target, cap, maxDepth, nearMin, found, budget;
    bool near, needNear;

    List<Vector2Int> bestPath = new List<Vector2Int>();
    List<Vector2Int> curPath = new List<Vector2Int>();
    int bestLen;

    // false if the last CountSolutions ran out of budget
    public bool Complete { get; private set; }

    // true if some route reached the 95-99% band
    public bool ReachesNearBand { get { return near; } }

    static int Apply(TileKind kind, int tot, int val)
    {
        if (kind == TileKind.Add) return tot + val;
        if (kind == TileKind.Sub) return Mathf.Max(0, tot - val);
        return tot * val;
    }

    static bool IsUnusable(TileKind k)
    {
        return k == TileKind.Mystery || k == TileKind.Gamble || k == TileKind.Wall;
    }

    // Counts self-avoiding routes from the start 
    // Stops after max routes reached
    public int CountSolutions(BoardData b, int tgt, int routeCap, int depthLimit, bool needNearBand)
    {
        board = b;
        target = tgt;
        cap = routeCap;
        maxDepth = depthLimit;
        nearMin = (tgt * 95 + 99) / 100;   
        near = false;
        needNear = needNearBand;
        seen = new bool[N, N];
        seen[0, 0] = true;
        found = 0;
        budget = GameConfig.SOLVER_BUDGET;
        Dfs(0, 0, GameConfig.START_TOTAL, 0);
        Complete = budget >= 0;
        return found;
    }

    void Dfs(int x, int y, int tot, int depth)
    {
        if (found >= cap && (near || !needNear)) return;
        if (budget-- <= 0) return;
        if (depth >= maxDepth) return;

        for (int i = 0; i < 4; i++)
        {
            int nx = x + GameConfig.DX[i], ny = y + GameConfig.DY[i];
            if (nx < 0 || nx >= N || ny < 0 || ny >= N || seen[nx, ny]) continue;

            TileKind kd = board.kind[nx, ny];
            if (IsUnusable(kd)) continue;

            int nt = Apply(kd, tot, board.val[nx, ny]);

            if (nt > target) continue;                        // would bust
            if (nt < target && nt >= nearMin) near = true;
            if (nt == target) { found++; continue; }

            seen[nx, ny] = true;
            Dfs(nx, ny, nt, depth + 1);
            seen[nx, ny] = false;
        }
    }

    // finds the shortest route
    public List<Vector2Int> ShortestPath(BoardData b, int tgt, int depthLimit)
    {
        board = b;
        target = tgt;
        maxDepth = depthLimit;
        bestPath = new List<Vector2Int>();
        curPath.Clear();
        bestLen = int.MaxValue;
        seen = new bool[N, N];
        seen[0, 0] = true;
        budget = GameConfig.SOLVER_BUDGET * 3;
        BestDfs(0, 0, GameConfig.START_TOTAL, 0);
        return bestPath;
    }

    void BestDfs(int x, int y, int tot, int depth)
    {
        if (budget-- <= 0) return;
        if (depth >= maxDepth) return;

        for (int i = 0; i < 4; i++)
        {
            if (depth + 1 >= bestLen) return;  

            int nx = x + GameConfig.DX[i], ny = y + GameConfig.DY[i];
            if (nx < 0 || nx >= N || ny < 0 || ny >= N || seen[nx, ny]) continue;

            TileKind kd = board.kind[nx, ny];
            if (IsUnusable(kd)) continue;

            int nt = Apply(kd, tot, board.val[nx, ny]);
            if (nt > target) continue;

            curPath.Add(new Vector2Int(nx, ny));
            if (nt == target)
            {
                bestLen = depth + 1;
                bestPath = new List<Vector2Int>(curPath);
            }
            else
            {
                seen[nx, ny] = true;
                BestDfs(nx, ny, nt, depth + 1);
                seen[nx, ny] = false;
            }
            curPath.RemoveAt(curPath.Count - 1);
        }
    }
}
