using Fusion;
using UnityEngine;

public class PlayerCollector : NetworkBehaviour
{
    public float pickupRange = 1.5f;

    void Update()
    {
        if (!Object.HasStateAuthority) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hit in hits)
        {
            Collectible collectible = hit.GetComponent<Collectible>();
            if (collectible != null && !collectible.IsCollected)
            {
                collectible.RpcCollect(Runner.LocalPlayer);
            }
        }
    }
}