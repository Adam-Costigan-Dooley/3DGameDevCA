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
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = _spawnPosition;
            cc.enabled = true;
        }
        else
        {
            transform.position = _spawnPosition;
        }
        Debug.Log($"Player respawned to {_spawnPosition}");
    }
}