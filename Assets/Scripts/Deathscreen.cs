using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    // Lets the enemy call DeathScreen.instance.Show() without needing a reference
    public static DeathScreen instance;

    [Header("References")]
    public GameObject deathPanel;
    public GameObject firstImage;
    public GameObject secondImage;
    public Button respawnButton;

    [Header("Timing (seconds)")]
    public float firstImageTime = 1.5f;
    public float secondImageTime = 1.5f;

    bool isDead;

    void Awake()
    {
        instance = this;

        // Start with everything hidden
        deathPanel.SetActive(false);
        firstImage.SetActive(false);
        secondImage.SetActive(false);
        respawnButton.gameObject.SetActive(false);

        respawnButton.onClick.AddListener(Respawn);
    }

    public void Show()
    {
        //Debug.Log("Show called");

        // The enemy calls this every frame while it stands next to the player (run once)
        if (isDead) return;
        isDead = true;

        // Stop the game and free the mouse so the button can be clicked
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        deathPanel.SetActive(true);

        firstImage.SetActive(true);
        yield return new WaitForSecondsRealtime(firstImageTime);

        secondImage.SetActive(true);
        yield return new WaitForSecondsRealtime(secondImageTime);

        respawnButton.gameObject.SetActive(true);
    }

    void Respawn()
    {
        // Time scale carries over between scenes, so turn it back on before reloading
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}