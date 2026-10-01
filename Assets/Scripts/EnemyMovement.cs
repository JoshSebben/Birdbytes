using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 360f;
    public float rayLength = 1f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;

    Vector3 destination;
    Quaternion targetRotation;
    int currentPatrolIndex;
    bool isWaiting;

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

        Patrol();
    }

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

    private void Patrol()
    {
        if (isWaiting || patrolPoints.Length == 0) return;

        Vector3 target = patrolPoints[currentPatrolIndex].position;

        if (FlatDistance(transform.position, target) <= stopAtDistance)
        {
            StartCoroutine(WaitAtPatrolPoint());
            return;
        }

        StepToward(target);
    }

    void StepToward(Vector3 target)
    {
        NavMeshHit startHit;
        NavMeshHit targetHit;

        if (!NavMesh.SamplePosition(transform.position, out startHit, 2f, NavMesh.AllAreas)) return;
        if (!NavMesh.SamplePosition(target, out targetHit, 2f, NavMesh.AllAreas)) return;

        NavMeshPath path = new NavMeshPath();
        bool found = NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, path);

        if (!found || path.corners.Length < 2) return;

        Vector3 offset = path.corners[1] - startHit.position;

        // One possible step along each axis (zero if the offset on that axis is negligible)
        Vector3 xStep = Vector3.zero;
        Vector3 zStep = Vector3.zero;

        if (Mathf.Abs(offset.x) > 0.1f)
        {
            xStep = new Vector3(Mathf.Sign(offset.x), 0, 0);
        }

        if (Mathf.Abs(offset.z) > 0.1f)
        {
            zStep = new Vector3(0, 0, Mathf.Sign(offset.z));
        }

        // Prefer the axis with the bigger offset, fall back to the other one
        Vector3 firstChoice;
        Vector3 secondChoice;

        if (Mathf.Abs(offset.x) >= Mathf.Abs(offset.z))
        {
            firstChoice = xStep;
            secondChoice = zStep;
        }
        else
        {
            firstChoice = zStep;
            secondChoice = xStep;
        }

        Vector3 stepDirection = Vector3.zero;

        if (firstChoice != Vector3.zero && Valid(firstChoice))
        {
            stepDirection = firstChoice;
        }
        else if (secondChoice != Vector3.zero && Valid(secondChoice))
        {
            stepDirection = secondChoice;
        }

        // Both directions blocked, so wait and try again next pass
        if (stepDirection == Vector3.zero) return;

        // Turn first if we aren't facing that way, then step on the next pass
        if (GetFacingDirection() != stepDirection)
        {
            targetRotation = Quaternion.LookRotation(stepDirection);
            return;
        }

        TryMove(stepDirection);
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;

        yield return new WaitForSeconds(patrolWaitTime);

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
    }
}

