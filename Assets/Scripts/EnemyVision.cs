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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        isInAngle = false;
        isInRange = false;
        isNotHidden = false;

        if (Vector3.Distance(transform.position, Player.transform.position) < DetectRange)
        {
            isInRange = true;
            RangeText.text = "In Range";
            RangeText.color = Color.red;
        }
        else
        {
            RangeText.text = "Not In Range";
            RangeText.color = Color.green;
        }

        if (Physics.Raycast(transform.position, (Player.transform.position - transform.position), out hit, Mathf.Infinity))
        {
            if (hit.transform == Player.transform)
            {
                isNotHidden = true;
                HiddenText.text = "Not Hidden";
                HiddenText.color = Color.red;
            }
            else
            {
                HiddenText.text = "Hidden";
                HiddenText.color = Color.green;
            }
        }
        else
        {
            HiddenText.text = "Hidden";
            HiddenText.color = Color.green;
        }

        Vector3 side1 = Player.transform.position - transform.position;
        Vector3 side2 = transform.forward;
        float angle = Vector3.SignedAngle(side1, side2, Vector3.up);

        if (angle < DetectAngle && angle < -1 * DetectAngle)
        {
            isInAngle = true;
            AngleText.text = "In Vision Angle";
            AngleText.color = Color.red;
        }
        else
        {
            AngleText.text = "Not In Vision Angle";
            AngleText.color = Color.green;
        }

        if (isInAngle && isInRange && isNotHidden)
        {
            DetectedText.text = "Player Detected";
            DetectedText.color = Color.red;
        }
        else
        {
            DetectedText.text = "Player Not Detected";
            DetectedText.color = Color.green;
        }
    }

}
