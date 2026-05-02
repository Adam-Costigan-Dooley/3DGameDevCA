using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 _spawnPosition;

    public void SetSpawnPoint(Vector3 position)
    {
        _spawnPosition = position;
    }

    public void Respawn()
    {
        transform.position = _spawnPosition;
    }
}