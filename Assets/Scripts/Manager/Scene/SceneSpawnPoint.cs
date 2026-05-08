using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    public enum SpawnFacing
    {
        UsePlayerDefault = 0,
        FaceLeft = 1,
        FaceRight = 2
    }

    [SerializeField] private string _spawnPointId;
    [SerializeField] private SpawnFacing _spawnFacing = SpawnFacing.UsePlayerDefault;

    public string SpawnPointId => _spawnPointId;
    public SpawnFacing Facing => _spawnFacing;
}
