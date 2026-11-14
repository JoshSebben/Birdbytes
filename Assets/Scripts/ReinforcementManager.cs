using System.Collections;
using UnityEngine;

public class ReinforcementManager : MonoBehaviour
{
    // Lets Low-Rank enemies call ReinforcementManager.instance.CallReinforcement(...) without a reference
    public static ReinforcementManager instance;

    [Header("References")]
    public GameObject midRankPrefab;
    public GameObject highRankPrefab;
    public Transform[] spawnPoints;
    public GameObject player;

    [Header("Arrival time (seconds)")]
    public float minDelay = 15f;
    public float maxDelay = 25f;

    void Awake()
    {
        instance = this;
    }

    public void CallReinforcement(EnemyRank rank, Vector3 targetPosition)
    {
        StartCoroutine(ReinforcementRoutine(rank, targetPosition));
    }

    IEnumerator ReinforcementRoutine(EnemyRank rank, Vector3 targetPosition)
    {
        // The called enemy takes a while to arrive
        yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("ReinforcementManager has no spawn points");
            yield break;
        }

        GameObject prefab;

        if (rank == EnemyRank.Mid)
        {
            prefab = midRankPrefab;
        }
        else
        {
            prefab = highRankPrefab;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject newEnemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        // A prefab can't point at scene objects, so give the new enemy the player here
        newEnemy.GetComponent<EnemyVision>().Player = player;

        // Wait a frame so the new enemy's Start() has run, then send it to where the player was seen
        yield return null;
        newEnemy.GetComponent<EnemyMovement>().ReceiveAlert(targetPosition);
    }
}