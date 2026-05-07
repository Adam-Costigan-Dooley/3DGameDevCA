using Fusion;
using UnityEngine;

public class Collectible : NetworkBehaviour
{
    [Networked] public NetworkBool IsCollected { get; set; }

    private MeshRenderer _renderer;
    private Collider _collider;

    public override void Spawned()
    {
        _renderer = GetComponentInChildren<MeshRenderer>();
        _collider = GetComponentInChildren<Collider>();
    }

    void Update()
    {
        if (_renderer != null)
            _renderer.enabled = !IsCollected;
        if (_collider != null)
            _collider.enabled = !IsCollected;

        if (!IsCollected)
            transform.Rotate(0, 90 * Time.deltaTime, 0);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcCollect(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (IsCollected) return;

        IsCollected = true;

        var scoreManager = FindObjectOfType<ScoreManagerCA3>();
        if (scoreManager != null)
            scoreManager.RpcRequestScore(player);
    }
}