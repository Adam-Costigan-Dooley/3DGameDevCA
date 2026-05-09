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
    [SerializeField] private NetworkPrefabRef gameStateManagerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointA;
    [SerializeField] private Transform spawnPointB;

    [SerializeField] private NetworkPrefabRef collectiblePrefab;
    [SerializeField] private int collectibleCount = 10;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    private GUIStyle _buttonStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private bool _stylesInitialized = false;
    private bool _collectiblesSpawned = false;

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
        _runner.AddCallbacks(this);

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
            Vector3 spawnPos = GetSpawnPositionForPlayer(player);

            NetworkObject networkObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            _spawnedPlayers[player] = networkObj;

            PlayerRespawn respawn = networkObj.GetComponent<PlayerRespawn>();
            if (respawn != null)
                respawn.SetSpawnPoint(spawnPos);

            // Only the first player in the session spawns the ScoreManager
            StartCoroutine(SpawnManagersIfNeeded(player));
            StartCoroutine(SpawnCollectibles());
        }

        StartCoroutine(RegisterPlayerDelayed(player));
    }

    private Vector3 GetSpawnPositionForPlayer(PlayerRef player)
    {
        List<PlayerRef> players = new List<PlayerRef>(_runner.ActivePlayers);
        players.Sort((a, b) => a.RawEncoded.CompareTo(b.RawEncoded));

        int index = players.IndexOf(player);

        bool useA = index <= 0;

        if (spawnPointA != null && spawnPointB != null)
            return useA ? spawnPointA.position : spawnPointB.position;

        return new Vector3(useA ? -10f : 10f, 9f, 0f);
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

    private IEnumerator SpawnManagersIfNeeded(PlayerRef player)
    {
        yield return new WaitForSeconds(1f);

        if (FindObjectOfType<ScoreManagerCA3>() == null)
            _runner.Spawn(scoreManagerPrefab, Vector3.zero, Quaternion.identity, player);

        yield return new WaitForSeconds(0.2f);

        if (FindObjectOfType<GameStateManager>() == null)
            _runner.Spawn(gameStateManagerPrefab, Vector3.zero, Quaternion.identity, player);
    }


    private IEnumerator SpawnCollectibles()
    {
        yield return new WaitForSeconds(1.5f);
        
        if (_collectiblesSpawned) yield break;
        
        // Check if collectibles already exist (spawned by another player)
        if (FindObjectOfType<Collectible>() != null)
        {
            _collectiblesSpawned = true;
            yield break;
        }

        for (int i = 0; i < collectibleCount; i++)
        {
            float x = UnityEngine.Random.Range(-15f, 15f);
            float z = UnityEngine.Random.Range(-15f, 15f);
            Vector3 pos = new Vector3(x, 1f, z);
            _runner.Spawn(collectiblePrefab, pos, Quaternion.identity);
        }
        _collectiblesSpawned = true;
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