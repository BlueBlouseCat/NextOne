using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string _spawnPointId;

    public string SpawnPointId => _spawnPointId;
}
