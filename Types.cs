
public enum TileKind { Add, Sub, Mul, Mystery, Gamble, Wall }

// A board as pure numbers checked by solver
public class BoardData
{
    public TileKind[,] kind = new TileKind[GameConfig.N, GameConfig.N];
    public int[,] val = new int[GameConfig.N, GameConfig.N];
}
