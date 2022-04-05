using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float MoveSpeed;

    void Update()
    {
        transform.position += transform.right * MoveSpeed *Time.deltaTime;
    }
}
