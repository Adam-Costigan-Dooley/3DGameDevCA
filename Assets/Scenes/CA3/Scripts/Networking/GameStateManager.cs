using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameStateManager : NetworkBehaviour
{
    [Networked] public NetworkBool GameOver { get; set; }
    [Networked] public PlayerRef Winner { get; set; }
    [Networked] public NetworkBool IsTie { get; set; }
    [Networked] public int RestartVotes { get; set; }
    [Networked] public NetworkBool Player1Ready { get; set; }
    [Networked] public NetworkBool Player2Ready { get; set; }

    private ScoreManagerCA3 _scoreManager;
    private Collectible[] _collectibles;

    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _textStyle;
    private GUIStyle _buttonStyle;
    private bool _stylesInitialized;

    public override void Spawned()
    {
        _scoreManager = FindObjectOfType<ScoreManagerCA3>();
        _collectibles = FindObjectsOfType<Collectible>();
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _boxStyle = new GUIStyle(GUI.skin.box){padding = new RectOffset(20, 20, 20, 20)};
        _titleStyle = new GUIStyle(GUI.skin.label){fontSize = 28,fontStyle = FontStyle.Bold,alignment = TextAnchor.MiddleCenter};
        _textStyle = new GUIStyle(GUI.skin.label){fontSize = 18,alignment = TextAnchor.MiddleCenter};
        _buttonStyle = new GUIStyle(GUI.skin.button){fontSize = 18,fontStyle = FontStyle.Bold,fixedHeight = 45};
        _stylesInitialized = true;
    }

    public void CheckForGameOver()
    {
        if (!Object.HasStateAuthority) return;
        if (GameOver) return;

        if (_collectibles == null || _collectibles.Length == 0)
            _collectibles = FindObjectsOfType<Collectible>();

        foreach (var collectible in _collectibles)
        {
            if (!collectible.IsCollected)
                return;
        }

        if (_scoreManager == null)
            _scoreManager = FindObjectOfType<ScoreManagerCA3>();

        if (_scoreManager != null && _scoreManager.TryGetWinner(out PlayerRef winner, out bool isTie))
        {
            GameOver = true;
            Winner = winner;
            IsTie = isTie;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcVoteRestart(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (!GameOver) return;

        if (_scoreManager == null) _scoreManager = FindObjectOfType<ScoreManagerCA3>();
        if (_scoreManager == null) return;

        int displayNumber = _scoreManager.GetDisplayPlayerNumber(player);

        if (displayNumber == 1 && !Player1Ready)
        {
            Player1Ready = true;
            RestartVotes++;
        }
        else if (displayNumber == 2 && !Player2Ready)
        {
            Player2Ready = true;
            RestartVotes++;
        }


        if (RestartVotes >= 2)
            RestartGame();
    }

    private void RestartGame()
    {
        if (!Object.HasStateAuthority) return;

        if (_collectibles == null || _collectibles.Length == 0)
            _collectibles = FindObjectsOfType<Collectible>();

        foreach (var collectible in _collectibles)
            collectible.ResetCollectible();

        if (_scoreManager == null)
            _scoreManager = FindObjectOfType<ScoreManagerCA3>();

        if (_scoreManager != null)
            _scoreManager.ResetScores();

        var respawns = FindObjectsOfType<PlayerRespawn>();
        foreach (var respawn in respawns)
            respawn.Respawn();

        GameOver = false;
        Winner = default;
        IsTie = false;
        RestartVotes = 0;
        Player1Ready = false;
        Player2Ready = false;
    }

    private void OnGUI()
    {
        if (!GameOver || Runner == null) return;

        InitStyles();

        float width = 420f;
        float height = 230f;
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        GUI.Box(new Rect(x, y, width, height), "", _boxStyle);

        int winnerNumber = _scoreManager != null ? _scoreManager.GetDisplayPlayerNumber(Winner) : 0;
        string winnerText = IsTie ? "It's a Tie!" : $"Player {winnerNumber} Wins!";
        GUI.Label(new Rect(x + 20, y + 20, width - 40, 40), winnerText, _titleStyle);

        GUI.Label(
            new Rect(x + 20, y + 80, width - 40, 30),
            $"{RestartVotes}/2 players ready",
            _textStyle
        );

        bool localReady =
            (Runner.LocalPlayer.RawEncoded == 1 && Player1Ready) ||
            (Runner.LocalPlayer.RawEncoded == 2 && Player2Ready);

        GUI.enabled = !localReady;

        if (GUI.Button(new Rect(x + 20, y + 120, width - 40, 30), "Press Enter to restart", _textStyle))
        {
            RpcVoteRestart(Runner.LocalPlayer);
        }

        GUI.enabled = true;
    }

    private void Update()
    {
        if (!GameOver || Runner == null) return;

        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            RpcVoteRestart(Runner.LocalPlayer);
        }
    }
}