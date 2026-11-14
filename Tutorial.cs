/*

using UnityEngine;

public class TutorialScreen : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Vector2 scroll;
    GUIStyle body, title, btn;
    Texture2D dim;

    const float REF_H = 720f;
    float S { get { return Screen.height / REF_H; } }
    Rect ButtonRect { get { return new Rect(Screen.width / S - 150f, 10f, 140f, 34f); } }

    void Awake()
    {
        dim = new Texture2D(1, 1);
        dim.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.88f));
        dim.Apply();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.F1)) IsOpen = !IsOpen;
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) IsOpen = false;
    }

    public bool IsOverButton(Vector3 mouse)
    {
        Vector2 p = new Vector2(mouse.x / S, (Screen.height - mouse.y) / S);
        return ButtonRect.Contains(p);
    }

    void OnGUI()
    {
        if (body == null)
        {
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = true };
            body.normal.textColor = Color.white;
            title = new GUIStyle(body) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btn = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
        }

        float s = S;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = Screen.height / s;

        if (!IsOpen)
        {
            if (GUI.Button(ButtonRect, "? HOW TO PLAY [H]", btn)) IsOpen = true;
            return;
        }

        GUI.DrawTexture(new Rect(0, 0, W, H), dim);
        float pw = Mathf.Min(760f, W - 40f);
        Rect panel = new Rect((W - pw) / 2f, 20f, pw, H - 40f);
        GUILayout.BeginArea(panel);
        GUILayout.Label("HOW TO PLAY", title);
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(panel.height - 100f));
        GUILayout.Label(Text(), body);
        GUILayout.EndScrollView();
        GUILayout.Space(8f);
        if (GUILayout.Button("CLOSE  [H / Esc]", btn, GUILayout.Height(40f))) IsOpen = false;
        GUILayout.EndArea();
    }

    string Text()
    {
        return
"<b>GOAL</b>\n" +
"Move across the board and land your total exactly on the TARGET. You play " + GameConfig.ROUNDS + " rounds. " +
"You start every round with a total of " + GameConfig.START_TOTAL + ". The target is 25 in round 1 and goes up by 10 each round.\n\n" +

"<b>CONTROLS</b>\n" +
"  Arrow keys / WASD  -  move one tile\n" +
"  SPACE / Enter / click BANK  -  bank your total and end the round\n" +
"  SPACE / click  -  close the round banner, or skip a score animation\n" +
"  H / F1  -  open or close this screen (the game pauses while it is open)\n" +
"  You cannot walk into rocks or off the board. You cannot bank before your first move.\n\n" +

"<b>TILES</b>\n" +
"  <color=#66ff8c>+N</color>  adds N to your total\n" +
"  <color=#ff6666>-N</color>  subtracts N (the total never goes below 0)\n" +
"  <color=#80b3ff>xN</color>  multiplies your total by N\n" +
"  <color=#ffd24d>?</color>  Mystery: a random amount is added or subtracted\n" +
"  <color=#ffd24d>COIN FLIP</color>  Gamble: 50% chance to double your total, 50% to halve it\n" +
"Every tile you step on applies its effect, even ones you have already visited.\n\n" +

"<b>BANKING AND BUSTING</b>\n" +
"  Land exactly on the target and the round banks automatically.\n" +
"  Go over the target and it's a <color=#ff6666>BUST</color>: 0 points and your streak resets.\n" +
"  You can also bank early with SPACE. The closer you are to the target, the bigger the share of your total you keep. " +
"The BANK button shows the exact percentage and points live.\n" +
"  Getting stuck with no moves left, running out of time, or running out of moves also ends the round.\n\n" +

"<b>SCORING</b>\n" +
"  Banked round:  total x streak x keep%\n" +
"  Streak goes up by 1 every round you survive.\n" +
"  <color=#ffd24d>EXACT HIT</color>: you keep 100% and your streak is <b>multiplied</b> by the exact bonus. " +
"That bigger streak carries into later rounds, so exact hits snowball. " +
"The bonus is bigger on boards with fewer possible routes (harder boards).\n\n" +

"<b>REDEMPTION</b>\n" +
"  If you bust, you get a redemption round (" + GameConfig.REDEMPTIONS_PER_RUN + " per run). " +
"Banking is off: you must land exactly on the target within " + (int)GameConfig.TIME_LIMIT + " seconds. " +
"It earns no points, but if you pull it off you keep playing. Fail it and the run ends.\n\n" +

"<b>ROUND TWISTS</b>  (a banner tells you which are active)\n" +
"  TIMER  -  you have " + (int)GameConfig.TIME_LIMIT + "s per round. You get " + (int)GameConfig.PLAN_TIME + "s to study the board first; your first move or the end of planning starts the clock.\n" +
"  MOVE LIMIT  -  you only get " + GameConfig.MOVE_LIMIT + " moves.\n" +
"  ROCKS  -  blocked tiles you must walk around.\n" +
"  FOG  -  tiles away from you are hidden once planning ends.\n\n" +

"<b>TIPS</b>\n" +
"  Plan the route before you move. Multipliers early, small adds at the end. " +
"Banking a little short is safer than risking a bust, but an exact hit is worth far more.";
    }
}
*/