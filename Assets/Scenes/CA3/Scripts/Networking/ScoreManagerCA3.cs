using Fusion;
using UnityEngine;

public class ScoreManagerCA3 : NetworkBehaviour
{
    [Networked, Capacity(8)]
    public NetworkDictionary<PlayerRef, int> Scores => default;

    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _scoreStyle;
    private GUIStyle _buttonStyle;
    private bool _stylesInitialized;

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
        _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _scoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold, fixedHeight = 40 };
        _stylesInitialized = true;
    }

    public void RegisterPlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (!Scores.ContainsKey(player))
            Scores.Add(player, 0);
    }

    public void AddScore(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (Scores.TryGet(player, out int current))
            Scores.Set(player, current + 1);
        else
            Scores.Add(player, 1);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestScore(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (Scores.TryGet(player, out int current))
            Scores.Set(player, current + 1);
        else
            Scores.Add(player, 1);
    }

    private void OnGUI()
    {
        if (Runner == null) return;
        InitStyles();

        int playerCount = 0;
        foreach (var kvp in Scores) playerCount++;

        float boxWidth = 250;
        float boxHeight = 80 + (playerCount * 30) + 60;
        GUI.Box(new Rect(10, 10, boxWidth, boxHeight), "", _boxStyle);
        GUI.Label(new Rect(10, 20, boxWidth, 30), "Scoreboard", _titleStyle);

        float scoreY = 55;
        int index = 0;
        foreach (var kvp in Scores)
        {
            index++;
            string tag = kvp.Key == Runner.LocalPlayer ? " (You)" : "";
            GUI.Label(new Rect(25, scoreY, 200, 30),
                $"Player {index}{tag}: {kvp.Value}", _scoreStyle);
            scoreY += 30;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRegisterPlayer(PlayerRef player)
        {
            if (!Object.HasStateAuthority) return;
            if (!Scores.ContainsKey(player))
                Scores.Add(player, 0);
        }
}