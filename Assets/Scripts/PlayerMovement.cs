using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 360f;
    public float rayLength = 1f;

    Vector3 destination;
    Quaternion targetRotation;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        destination = transform.position;
        targetRotation = transform.rotation;

    }

    // Update is called once per frame
    void Update()
    {
        // Glide toward the current target position and rotation
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        bool isMoving = Vector3.Distance(transform.position, destination) > 0.001f;
        bool isTurning = Quaternion.Angle(transform.rotation, targetRotation) > 0.1f;

        // Ignore input until the current move or turn has finished
        if (isMoving || isTurning)
        {
            return;
        }

        // Snap to the exact grid position and rotation to prevent drift
        transform.position = destination;
        transform.rotation = targetRotation;

        HandleInput();
    }

    void HandleInput()
    {
        // Turning
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            targetRotation = transform.rotation * Quaternion.Euler(0, -90, 0);
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            targetRotation = transform.rotation * Quaternion.Euler(0, 90, 0);
        }
        // Moving relative to the direction player is facing
        else if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            TryMove(GetFacingDirection());
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            TryMove(-GetFacingDirection());
        }
    }

    // transform.forward rounded to a clean grid direction like (0,0,1) or (1,0,0)
    Vector3 GetFacingDirection()
    {
        Vector3 forward = transform.forward;
        return new Vector3(Mathf.Round(forward.x), 0, Mathf.Round(forward.z));
    }

    void TryMove(Vector3 direction)
    {
        if (Valid(direction))
        {
            destination = transform.position + direction;
        }
    }


    bool Valid(Vector3 direction)
    {
        Vector3 origin = transform.position + new Vector3(0, 0.25f, 0);
        Ray myRay = new Ray(origin, direction);
        RaycastHit hit;

        Debug.DrawRay(origin, direction * rayLength, Color.red);

        if (Physics.Raycast(myRay, out hit, rayLength))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                return false;
            }
        }

        return true;
    }
}
