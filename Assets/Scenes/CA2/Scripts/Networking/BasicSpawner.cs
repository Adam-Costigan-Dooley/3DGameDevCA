using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private NetworkPrefabRef scoreManagerPrefab;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private bool _scoreManagerSpawned = false;

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

    async void StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "CA2TestRoom",
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
            float boxHeight = 200;
            float x = (Screen.width - boxWidth) / 2;
            float y = (Screen.height - boxHeight) / 2;

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), "", _boxStyle);
            GUI.Label(new Rect(x, y + 15, boxWidth, 40), "CA2 Network Test", _titleStyle);

            if (GUI.Button(new Rect(x + 40, y + 70, 220, 50), "Host Game", _buttonStyle))
                StartGame(GameMode.Host);
            if (GUI.Button(new Rect(x + 40, y + 130, 220, 50), "Join Game", _buttonStyle))
                StartGame(GameMode.Client);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (!_scoreManagerSpawned)
            {
                runner.Spawn(scoreManagerPrefab, Vector3.zero, Quaternion.identity);
                _scoreManagerSpawned = true;
            }

            Vector3 spawnPos = new Vector3(player.RawEncoded % 2 * 2 - 1, 1, 0);
            NetworkObject networkObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            _spawnedPlayers.Add(player, networkObj);
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