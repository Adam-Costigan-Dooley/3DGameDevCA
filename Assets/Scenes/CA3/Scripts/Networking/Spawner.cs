using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Fusion.Photon.Realtime;

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
    private GUIStyle _statusStyle;
    private bool _stylesInitialized = false;
    private bool _collectiblesSpawned = false;

    private bool _isConnecting = false;
    private string _statusMessage = "Not signed in";
    private string _unityAccessToken = "";
    private string _playerId = "";


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

        _statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };


        _stylesInitialized = true;
    }

    private async void BeginAuthenticatedJoin()
    {
        if (_isConnecting) return;
        _isConnecting = true;

        try
        {
            _statusMessage = "Initializing Unity Services...";
            await EnsureUnityServicesAndSignIn();

            _statusMessage = "Starting Fusion session...";
            await StartGame(_unityAccessToken);
        }
        catch (Exception ex)
        {
            _statusMessage = $"Auth/Join failed: {ex.Message}";
            Debug.LogError($"[AUTH] BeginAuthenticatedJoin failed: {ex}");
            _isConnecting = false;
        }
    }

    private async Task EnsureUnityServicesAndSignIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            _statusMessage = "Signing in anonymously...";
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        _unityAccessToken = AuthenticationService.Instance.AccessToken;
        _playerId = AuthenticationService.Instance.PlayerId;

        if (string.IsNullOrEmpty(_unityAccessToken))
        {
            throw new Exception("Unity Authentication returned an empty access token.");
        }

        _statusMessage = $"Signed in. Player ID: {_playerId}";
        Debug.Log($"[AUTH] Unity sign-in success. PlayerId={_playerId}");
    }


    private async Task StartGame(string accessToken)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        var authValues = new AuthenticationValues();
        authValues.AuthType = CustomAuthenticationType.Custom;
        authValues.AddAuthParameter("token", accessToken);
        authValues.AddAuthParameter("playerId", _playerId);
        
        var result = await _runner.StartGame(new StartGameArgs()
        {
            AuthValues = authValues,
            GameMode = GameMode.Shared,
            SessionName = "CrushArena",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok == false)
        {
            _statusMessage = $"Fusion StartGame failed: {result.ShutdownReason}";
            Debug.LogError($"[AUTH] Fusion StartGame failed: {result.ShutdownReason}");
            _isConnecting = false;
            return;
        }

        _statusMessage = "Connected";
        _isConnecting = false;
    }


    private void OnGUI()
    {
        InitStyles();

        if (_runner == null)
        {
            float boxWidth = 340;
            float boxHeight = 210;
            float x = (Screen.width - boxWidth) / 2;
            float y = (Screen.height - boxHeight) / 2;

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), "", _boxStyle);
            GUI.Label(new Rect(x, y + 15, boxWidth, 40), "Crush Arena", _titleStyle);

            GUI.Label(new Rect(x + 20, y + 60, boxWidth - 40, 45), _statusMessage, _statusStyle);

            GUI.enabled = !_isConnecting;
            if (GUI.Button(new Rect(x + 40, y + 120, 260, 50), _isConnecting ? "Connecting..." : "Join Game", _buttonStyle))
            {
                BeginAuthenticatedJoin();
            }
            GUI.enabled = true;
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

    public void OnConnectedToServer(NetworkRunner runner)
    {
        _statusMessage = "Connected to Fusion";
        Debug.Log("[AUTH] Connected to Fusion");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _statusMessage = $"Disconnected: {reason}";
        _isConnecting = false;
        Debug.LogWarning($"[AUTH] Disconnected from Fusion: {reason}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        _statusMessage = $"Connect failed: {reason}";
        _isConnecting = false;
        Debug.LogError($"[AUTH] Connect failed: {reason}");
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        Debug.Log("[AUTH] Custom auth response received from Fusion.");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _statusMessage = $"Shutdown: {shutdownReason}";
        _isConnecting = false;
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}