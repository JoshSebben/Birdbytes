using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door")]
    public EnemyRank requiredRank = EnemyRank.Low;
    public float interactRange = 2f;
    public KeyCode openKey = KeyCode.F;

    [Header("Opening")]
    public float openHeight = 3f;
    public float openTime = 1f;

    Transform player;
    bool isOpen;
    bool isShowingMessage;

    void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>().transform;
    }

    void Update()
    {
        if (isOpen) return;

        Vector3 offset = player.position - transform.position;
        offset.y = 0;

        // Too far away: hide our message if we were showing one
        if (offset.magnitude > interactRange)
        {
            HideMessage();
            return;
        }

        if (ScanDatabase.HasRankAccess(requiredRank))
        {
            ShowMessage("Press " + openKey + " to open");

            if (Input.GetKeyDown(openKey))
            {
                StartCoroutine(OpenDoor());
            }
        }
        else
        {
            ShowMessage(RankNames.GetName(requiredRank) + " Access Only");
        }
    }

    IEnumerator OpenDoor()
    {
        isOpen = true;
        HideMessage();

        // Slide the door up, then switch it off
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * openHeight;
        float timer = 0f;

        while (timer < openTime)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, endPosition, timer / openTime);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    void ShowMessage(string message)
    {
        isShowingMessage = true;
        MessagePopup.instance.Show(message);
    }

    void HideMessage()
    {
        if (!isShowingMessage) return;

        isShowingMessage = false;
        MessagePopup.instance.Hide();
    }
}