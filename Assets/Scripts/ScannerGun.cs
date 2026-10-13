using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScannerGun : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject scanPanel; // ui pop up
    public TMP_Text promptText;
    public Image progressBar;

    [Header("Settings")]
    public KeyCode scanKey = KeyCode.E;
    public float scanTime = 3f;
    public float scanRange = 1.5f; // how close the player must be to the enemy
    public float behindAngle = 135f; // 180 = exactly behind, lower = wider behind zone

    ScannableEnemy currentTarget;
    float scanProgress;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scanPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        ScannableEnemy target = FindEnemyToScan();

        // Target changed or went away: cancel any scan in progress
        if (target != currentTarget)
        {
            currentTarget = target;
            scanProgress = 0f;
        }

        if (currentTarget == null)
        {
            scanPanel.SetActive(false);
            return;
        }

        scanPanel.SetActive(true);

        // Already scanned this type of enemy, so don't scan again
        if (ScanDatabase.HasScanned(currentTarget.enemyID))
        {
            promptText.text = currentTarget.enemyID + " scanned";
            progressBar.fillAmount = 1f;
            return;
        }

        if (Input.GetKey(scanKey))
        {
            promptText.text = "Scanning...";
            scanProgress += Time.deltaTime / scanTime;

            if (scanProgress >= 1f)
            {
                ScanDatabase.AddScan(currentTarget.enemyID);
                Debug.Log("Scanned " + currentTarget.enemyID);
            }
        }
        else
        {
            // Let go of the key: start over
            promptText.text = "Hold " + scanKey + " to scan";
            scanProgress = 0f;
        }

        progressBar.fillAmount = Mathf.Clamp01(scanProgress);
    }

    ScannableEnemy FindEnemyToScan()
    {
        foreach (ScannableEnemy enemy in ScannableEnemy.allEnemies)
        {
            if (enemy.IsAlerted()) continue;

            Vector3 enemyToPlayer = player.position - enemy.transform.position;
            enemyToPlayer.y = 0;

            if (enemyToPlayer.magnitude > scanRange) continue;

            // Angle between where the enemy faces and where the player is
            float angle = Vector3.Angle(enemy.transform.forward, enemyToPlayer);

            if (angle < behindAngle) continue;

            return enemy;
        }

        return null;
    }
}
