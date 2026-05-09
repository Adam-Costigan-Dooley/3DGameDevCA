using Fusion;
using UnityEngine;

public class Collectible : NetworkBehaviour
{
    [Networked] public NetworkBool IsCollected { get; set; }

    private MeshRenderer _renderer;
    private Collider _collider;
    private ScoreManagerCA3 _scoreManager;
    private GameStateManager _gameStateManager;

    public override void Spawned()
    {
        _renderer = GetComponentInChildren<MeshRenderer>();
        _collider = GetComponentInChildren<Collider>();

        _scoreManager = FindObjectOfType<ScoreManagerCA3>();
        _gameStateManager = FindObjectOfType<GameStateManager>();
    }

    private void Update()
    {
        if (_renderer != null)
            _renderer.enabled = !IsCollected;
        if (_collider != null)
            _collider.enabled = !IsCollected;

        if (!IsCollected)
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcCollect(PlayerRef player)
    {
        TryCollect(player);
    }

    public void TryCollect(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (IsCollected) return;

        IsCollected = true;

        if (_scoreManager == null)
            _scoreManager = FindObjectOfType<ScoreManagerCA3>();

        if (_gameStateManager == null)
            _gameStateManager = FindObjectOfType<GameStateManager>();

        if (_scoreManager != null)
            _scoreManager.AddScore(player);

        if (_gameStateManager != null)
            _gameStateManager.CheckForGameOver();
    }

    public void ResetCollectible()
    {
        if (!Object.HasStateAuthority) return;
        IsCollected = false;
    }
}