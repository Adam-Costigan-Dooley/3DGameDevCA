using Fusion;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    [Networked] public int ScorePlayer1 { get; set; }
    [Networked] public int ScorePlayer2 { get; set; }

    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _scoreStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _localPlayerStyle;
    private bool _stylesInitialized = false;

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        _scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft
        };

        _localPlayerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            fixedHeight = 40
        };

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (Runner == null) return;

        InitStyles();

        float boxWidth = 250;
        float boxHeight = 200;
        float x = 10;
        float y = 10;

        GUI.Box(new Rect(x, y, boxWidth, boxHeight), "", _boxStyle);
        GUI.Label(new Rect(x, y + 10, boxWidth, 30), "Scoreboard", _titleStyle);

        bool isPlayer1 = Runner.LocalPlayer.RawEncoded == 2;
        string p1Tag = isPlayer1 ? " (You)" : "";
        string p2Tag = !isPlayer1 ? " (You)" : "";

        GUI.Label(new Rect(x + 15, y + 50, 200, 30), $"Player 1{p1Tag}: {ScorePlayer1}", _scoreStyle);
        GUI.Label(new Rect(x + 15, y + 80, 200, 30), $"Player 2{p2Tag}: {ScorePlayer2}", _scoreStyle);

        if (GUI.Button(new Rect(x + 25, y + 130, 200, 40), "Score Point", _buttonStyle))
        {
            RpcRequestScore(Runner.LocalPlayer);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestScore(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (player.RawEncoded == 2)
            ScorePlayer1++;
        else
            ScorePlayer2++;
    }
}