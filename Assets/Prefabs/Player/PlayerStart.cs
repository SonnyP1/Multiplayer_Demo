using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStart : MonoBehaviour
{
    [SerializeField] BoxCollider SpawnArea;



    public Vector3 GetRandomSpawnPos()
    {
        Vector3 spawnExtend = SpawnArea.bounds.size;
        Vector3 spawnAreaCorner = SpawnArea.bounds.center - SpawnArea.bounds.extents;
        Vector3 SpawnPos = spawnAreaCorner + spawnExtend.normalized * Random.Range(0, spawnExtend.magnitude);
        return SpawnPos;
    }
}
