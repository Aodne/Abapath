using UnityEngine;

// One tile on screen.
public class BoardTile
{
    public TileKind kind;
    public int value;
    public string text;
    public Color baseColor;
    public SpriteRenderer bg;
    public TextMesh label;
    public bool visited;   // rocks count as visited so nobody can walk into them
}

// Builds the grid of tiles and keeps their colours and labels up to date.
// It only draws: the rules live in GameManager.
public class BoardView : MonoBehaviour
{
    const int N = GameConfig.N;

    BoardTile[,] grid;
    GameObject gridRoot;

    public BoardTile At(int x, int y) { return grid[x, y]; }

    public Vector3 CellToWorld(int x, int y)
    {
        return new Vector3((x - (N - 1) / 2f) * GameConfig.CELL, (y - (N - 1) / 2f) * GameConfig.CELL + GameConfig.GRID_Y, 0);
    }

    // throws away the old grid and builds a new one from board data
    public void Build(BoardData data)
    {
        if (gridRoot != null) Destroy(gridRoot);
        gridRoot = new GameObject("Grid");
        grid = new BoardTile[N, N];
        for (int x = 0; x < N; x++)
            for (int y = 0; y < N; y++)
                grid[x, y] = MakeTile(x, y, data.kind[x, y], data.val[x, y]);

        grid[0, 0].visited = true;  
    }

    BoardTile MakeTile(int x, int y, TileKind kind, int value)
    {
        var t = new BoardTile();
        t.kind = kind;
        t.value = value;
        bool isStart = x == 0 && y == 0;
        t.baseColor = isStart ? new Color(0.5f, 0.5f, 0.55f) : ColorFor(kind);

        if (isStart) t.text = "START";
        else
        {
            switch (kind)
            {
                case TileKind.Add: t.text = "+" + value; break;
                case TileKind.Sub: t.text = "-" + value; break;
                case TileKind.Mul: t.text = "x" + value; break;
                case TileKind.Mystery: t.text = "?"; break;
                case TileKind.Gamble: t.text = "x2\nOR " + GameConfig.DIV + "2"; break;
                default: t.text = "X"; t.visited = true; break;   // rocks can't be entered
            }
        }

        Vector3 wp = CellToWorld(x, y);
        float size = GameConfig.CELL * 0.92f;
        t.bg = SpriteFactory.MakeBox("Tile", wp, new Vector2(size, size), t.baseColor, gridRoot.transform);
        t.label = SpriteFactory.MakeText("Label", wp, kind == TileKind.Gamble && !isStart ? 30 : 45, Color.white, gridRoot.transform);
        t.label.text = t.text;
        return t;
    }

    static Color ColorFor(TileKind k)
    {
        switch (k)
        {
            case TileKind.Add: return new Color(0.25f, 0.7f, 0.4f);
            case TileKind.Sub: return new Color(0.8f, 0.3f, 0.3f);
            case TileKind.Mul: return new Color(0.3f, 0.45f, 0.9f);
            case TileKind.Mystery: return new Color(0.6f, 0.35f, 0.8f);
            case TileKind.Wall: return new Color(0.35f, 0.27f, 0.2f);
            default: return new Color(0.95f, 0.55f, 0.15f);
        }
    }

    // ---------- queries ----------
    // off the board, already visited, or a rock
    public bool IsBlocked(Vector2Int p)
    {
        return p.x < 0 || p.x >= N || p.y < 0 || p.y >= N || grid[p.x, p.y].visited;
    }

    public bool HasMoves(Vector2Int pos)
    {
        for (int i = 0; i < 4; i++)
        {
            int nx = pos.x + GameConfig.DX[i], ny = pos.y + GameConfig.DY[i];
            if (nx >= 0 && nx < N && ny >= 0 && ny < N && !grid[nx, ny].visited) return true;
        }
        return false;
    }

    // ---------- updating visuals ----------
    // a random tile remembers what it did, so show the result on it
    public void ShowResult(int x, int y, string text)
    {
        var t = grid[x, y];
        t.text = text;
        t.label.fontSize = 48 * GameConfig.FONT_SCALE;
    }

    public void PopTile(int x, int y)
    {
        StartCoroutine(Juice.Pop(grid[x, y].bg.transform));
    }

    // recolours every tile. onBest marks the dev gold route.
    public void Refresh(Vector2Int pos, bool planning, bool fog, bool[,] onBest)
    {
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < N; y++)
            {
                var t = grid[x, y];
                bool current = pos.x == x && pos.y == y;
                int dist = Mathf.Abs(x - pos.x) + Mathf.Abs(y - pos.y);
                bool adjacent = dist == 1 && !t.visited;
                bool onPath = onBest[x, y];
                // fog hides far tiles, except while planning
                bool hidden = fog && !planning && !t.visited && !current && dist > GameConfig.FOG_RADIUS && !onPath;

                Color c = t.baseColor;
                if (t.kind == TileKind.Wall) c = t.baseColor;
                else if (current) c = new Color(1f, 1f, 1f);
                else if (onPath && t.visited) c = new Color(0.55f, 0.45f, 0.1f);
                else if (onPath) c = GameConfig.GOLD;
                else if (t.visited) c = new Color(0.2f, 0.2f, 0.25f);
                else if (hidden) c = new Color(0.16f, 0.17f, 0.22f);
                else if (!adjacent) c = t.baseColor * 0.55f;
                c.a = 1f;

                t.bg.color = c;
                t.label.text = hidden ? "" : t.text;

                bool darkText = current || (onPath && !t.visited);
                t.label.color = t.kind == TileKind.Wall ? new Color(0.15f, 0.1f, 0.08f) : (darkText ? Color.black : Color.white);
            }
        }
    }
}
