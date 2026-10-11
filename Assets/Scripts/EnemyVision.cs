using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class EnemyVision : MonoBehaviour
{
    public float DetectRange = 7f;
    public float DetectAngle = 45f;
    bool isInAngle, isInRange, isNotHidden;

    public GameObject Player;
    public TMP_Text RangeText, HiddenText, AngleText, DetectedText;

    public bool PlayerDetected; // In range, in cone, not hidden
    public bool PlayerInSight; // In range and not hidden (ignore cone)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 origin = transform.position + new Vector3(0, 0.25f, 0);
        Vector3 target = Player.transform.position + new Vector3(0, 0.25f, 0);
        Vector3 toPlayer = target - origin;

        // Range
        isInRange = toPlayer.magnitude < DetectRange;

        // Angle, measured on the flat ground
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0, toPlayer.z);
        float angle = Vector3.Angle(transform.forward, flatToPlayer);
        isInAngle = angle < DetectAngle;

        // Hidden
        isNotHidden = false;
        RaycastHit hit;
        if (Physics.Raycast(origin, toPlayer.normalized, out hit, DetectRange))
        {
            if (hit.transform.IsChildOf(Player.transform))
            {
                isNotHidden = true;
            }

            PlayerInSight = isInRange && isNotHidden;
            PlayerDetected = isInRange && isInAngle && isNotHidden;

            SetLabel(RangeText, isInRange ? "In Range" : "Not In Range", isInRange);
            SetLabel(HiddenText, isNotHidden ? "Not Hidden" : "Hidden", isNotHidden);
            SetLabel(AngleText, isInAngle ? "In Vision Angle" : "Not In Vision Angle", isInRange);
            SetLabel(DetectedText, PlayerDetected ? "Player Detected" : "Player Not Detected", PlayerDetected);
        }
    }

    void SetLabel(TMP_Text label, string message, bool alert)
    {
        if (label == null) return;

        label.text = message;
        label.color = alert ? Color.red : Color.green;
    }

}
