using UnityEngine;

public static class GameConfig
{
    // switches
    public static bool devSecret = false;        // paints the shortest winning route gold
    public static bool difficultyTiers = true; 

    // board
    public const int N = 5;
    public const float CELL = 1.5f;
    public const float GRID_Y = -0.2f;

    // run
    public const int ROUNDS = 5;
    public const int START_TOTAL = 5;

    // board generation
    public const int MAX_ATTEMPTS = 150;
    public const int SOLVER_BUDGET = 60000;

    // redemption
    public const int REDEMPTIONS_PER_RUN = 1;
    public const int REDEMPTION_ROUTES = 4;

    // round modifiers
    public const float PLAN_TIME = 7f;
    public const float TIME_LIMIT = 15f;
    public const int MOVE_LIMIT = 11;
    public const int ROCK_COUNT = 4;
    public const int FOG_RADIUS = 2;
    public const float BANNER_TIME = 4f;

    // display
    public const string DIV = "\u00F7";
    public const int FONT_SCALE = 4;            // text is rendered at 4x and scaled down so it stays sharp
    public const float HUD_HALF_HEIGHT = 7.6f;
    public const float HUD_HALF_WIDTH = 7.5f;
    public const float SPIN_TIME = 0.45f;

    // movement directions (right, left, up, down)
    public static readonly int[] DX = { 1, -1, 0, 0 };
    public static readonly int[] DY = { 0, 0, 1, -1 };

    // shared colors
    public static readonly Color GOLD = new Color(1f, 0.8f, 0.1f);
    public static readonly Color SCORE_COLOR = new Color(1f, 0.85f, 0.3f);
}
