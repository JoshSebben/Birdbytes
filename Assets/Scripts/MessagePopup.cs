using UnityEngine;
using TMPro;

// Put this on the Canvas (an object that stays active), with the message text as a child
public class MessagePopup : MonoBehaviour
{
    public static MessagePopup instance;

    public TMP_Text messageText;

    void Awake()
    {
        instance = this;
        messageText.gameObject.SetActive(false);
    }

    public void Show(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        messageText.gameObject.SetActive(false);
    }
}