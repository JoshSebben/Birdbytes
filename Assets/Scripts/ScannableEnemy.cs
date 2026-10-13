using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ScannableEnemy : MonoBehaviour
{
    public string enemyID = "Alien1";

    public static List<ScannableEnemy> allEnemies = new List<ScannableEnemy>();

    EnemyMovement movement;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
    }

    void OnEnable()
    {
        allEnemies.Add(this);
    }

    void OnDisable()
    {
        allEnemies.Remove(this);
    }

    public bool IsAlerted()
    {
        return movement.IsChasing();
    }
}
