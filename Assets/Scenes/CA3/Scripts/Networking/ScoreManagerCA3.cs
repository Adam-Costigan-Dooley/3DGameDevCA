using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class ScoreManagerCA3 : NetworkBehaviour
{
    [Networked, Capacity(8)]
    public NetworkDictionary<PlayerRef, int> Scores => default;

    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _scoreStyle;
    private bool _stylesInitialized;
    private bool _spawned;

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
        _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _scoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        _stylesInitialized = true;
    }

    public void RegisterPlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (!Scores.ContainsKey(player))
            Scores.Add(player, 0);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRegisterPlayer(PlayerRef player)
    {
        RegisterPlayer(player);
    }

    public void AddScore(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (Scores.TryGet(player, out int current))
            Scores.Set(player, current + 1);
        else
            Scores.Add(player, 1);
    }

    public int GetScore(PlayerRef player)
    {
        if (Scores.TryGet(player, out int score))
            return score;

        return 0;
    }

    public void ResetScores()
    {
        if (!Object.HasStateAuthority) return;

        var players = new System.Collections.Generic.List<PlayerRef>();
        foreach (var kvp in Scores)
            players.Add(kvp.Key);

        foreach (var player in players)
            Scores.Set(player, 0);
    }

    public bool TryGetWinner(out PlayerRef winner, out bool isTie)
    {
        winner = default;
        isTie = false;

        bool foundAny = false;
        int highestScore = int.MinValue;
        int winnersAtHighest = 0;

        foreach (var kvp in Scores)
        {
            if (!foundAny || kvp.Value > highestScore)
            {
                foundAny = true;
                highestScore = kvp.Value;
                winner = kvp.Key;
                winnersAtHighest = 1;
            }
            else if (kvp.Value == highestScore)
            {
                winnersAtHighest++;
            }
        }

        if (!foundAny)
            return false;

        isTie = winnersAtHighest > 1;
        return true;
    }

    
    public override void Spawned()
    {
        _spawned = true;
    }

    private void OnGUI()
    {
        if (!_spawned)
        return;

        InitStyles();

        int playerCount = 0;
        foreach (var kvp in Scores) playerCount++;

        float boxWidth = 250;
        float boxHeight = 80 + (playerCount * 30);
        GUI.Box(new Rect(10, 10, boxWidth, boxHeight), "", _boxStyle);
        GUI.Label(new Rect(10, 20, boxWidth, 30), "Scoreboard", _titleStyle);

        float scoreY = 55;
        int index = 0;
        foreach (var kvp in Scores)
        {
            int displayNumber = GetDisplayPlayerNumber(kvp.Key);
            string tag = kvp.Key == Runner.LocalPlayer ? " (You)" : "";
            GUI.Label(
                new Rect(25, scoreY, 200, 30),
                $"Player {displayNumber}{tag}: {kvp.Value}",
                _scoreStyle
            );
            scoreY += 30;
        }
    }

    public int GetDisplayPlayerNumber(PlayerRef player)
    {
        List<PlayerRef> players = new List<PlayerRef>();

        foreach (var kvp in Scores)
            players.Add(kvp.Key);

        players.Sort((a, b) => a.RawEncoded.CompareTo(b.RawEncoded));

        int index = players.IndexOf(player);
        return index >= 0 ? index + 1 : 0;
    }
}