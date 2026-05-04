using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private NetworkPrefabRef scoreManagerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointA;
    [SerializeField] private Transform spawnPointB;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    private GUIStyle _buttonStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private bool _stylesInitialized = false;

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            fixedHeight = 50
        };

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperCenter,
            padding = new RectOffset(10, 10, 10, 10)
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        _stylesInitialized = true;
    }

    async void StartGame()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "CrushArena",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    private void OnGUI()
    {
        InitStyles();

        if (_runner == null)
        {
            float boxWidth = 300;
            float boxHeight = 150;
            float x = (Screen.width - boxWidth) / 2;
            float y = (Screen.height - boxHeight) / 2;

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), "", _boxStyle);
            GUI.Label(new Rect(x, y + 15, boxWidth, 40), "Crush Arena", _titleStyle);

            if (GUI.Button(new Rect(x + 40, y + 70, 220, 50), "Join Game", _buttonStyle))
                StartGame();
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
{
            bool isFirstPlayer = player.RawEncoded % 2 == 0;
            Vector3 spawnPos;

            if (spawnPointA != null && spawnPointB != null)
                spawnPos = isFirstPlayer ? spawnPointA.position : spawnPointB.position;
            else
                spawnPos = new Vector3(isFirstPlayer ? -10 : 10, 9, 0);

            NetworkObject networkObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            _spawnedPlayers.Add(player, networkObj);

            PlayerRespawn respawn = networkObj.GetComponent<PlayerRespawn>();
            if (respawn != null)
                respawn.SetSpawnPoint(spawnPos);

            // Only the first player in the session spawns the ScoreManager
            StartCoroutine(SpawnScoreManagerIfNeeded(player));
        }

        StartCoroutine(RegisterPlayerDelayed(player));
    }

    private IEnumerator RegisterPlayerDelayed(PlayerRef player)
    {
        yield return new WaitForSeconds(0.5f);
        var scoreManager = FindObjectOfType<ScoreManagerCA3>();
        if (scoreManager != null)
        {
            scoreManager.RpcRegisterPlayer(player);
        }
    }

    private IEnumerator SpawnScoreManagerIfNeeded(PlayerRef player)
    {
        yield return new WaitForSeconds(1f);

        if (FindObjectOfType<ScoreManagerCA3>() == null)
        {
            _runner.Spawn(scoreManagerPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject networkObj))
        {
            runner.Despawn(networkObj);
            _spawnedPlayers.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}