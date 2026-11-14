using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Rank")]
    public EnemyRank rank = EnemyRank.Low;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;

    [Header("General")]
    public float turnSpeed = 360f;
    public float rayLength = 1f;
    [SerializeField] private float attackRange = 1.1f;   // 1.1 = the cell right next to the enemy
    [SerializeField] private float alertRadius = 8f;     // how far an alert reaches other enemies

    [Header("Backup (Low-Rank)")]
    [SerializeField] private float backupMinTime = 15f;  // backup should arrive between these many seconds
    [SerializeField] private float backupMaxTime = 25f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private int maxDashTiles = 8;             // Mid-Rank: longest straight-line dash
    [SerializeField] private float restTimeMin = 1f;           // Mid-Rank: rest after a dash
    [SerializeField] private float restTimeMax = 2f;
    [SerializeField] private float captainDashCooldown = 5f;   // Captain: two-tile dash

    [Header("Grab (High-Rank and Captain)")]
    [SerializeField] private Transform limb;                   // thin cube child of the enemy, collider removed
    [SerializeField] private float limbThickness = 0.1f;
    [SerializeField] private int grabRangeTiles = 4;
    [SerializeField] private float grabExtendTime = 0.4f;
    [SerializeField] private float grabHoldTime = 1f;
    [SerializeField] private float grabCooldown = 4f;

    // How far off a row or column the player can be and still count as "in line"
    float lineTolerance = 0.4f;

    float detectRange;
    float alertTime;
    float patrolSpeed;
    float chaseSpeed;
    bool callsBackup;
    bool canRookDash;
    bool canShortDash;
    bool canGrab;

    enum State { Patrol, Chase }
    State state = State.Patrol;

    EnemyVision vision;
    Transform player;

    float moveSpeed;
    Vector3 destination;
    Quaternion targetRotation;

    Vector3 lastSeenPosition;
    bool playerVisible;
    float alertProgress;
    bool hasCalledBackup;

    int currentPatrolIndex;
    bool isWaiting;
    Coroutine waitRoutine;

    bool isDashing;
    float restTimer;
    float dashCooldownTimer;

    bool isGrabbing;
    float grabCooldownTimer;
    float limbHeight;

    // Every enemy adds itself here so enemies can alert each other
    public static List<EnemyMovement> allEnemies = new List<EnemyMovement>();

    void OnEnable()
    {
        allEnemies.Add(this);
    }

    void OnDisable()
    {
        allEnemies.Remove(this);
    }

    void Start()
    {
        vision = GetComponent<EnemyVision>();
        player = vision.Player.transform;

        ApplyRankSettings();

        Vector3 start = transform.position;
        transform.position = new Vector3(Mathf.Round(start.x), start.y, Mathf.Round(start.z));

        destination = transform.position;
        targetRotation = transform.rotation;
        moveSpeed = patrolSpeed;

        if (limb != null)
        {
            limbHeight = limb.localPosition.y;
            SetLimbLength(0f);
        }
    }

    // All the per-rank numbers live here, so this is the place to tune them
    void ApplyRankSettings()
    {
        switch (rank)
        {
            case EnemyRank.Low:
                detectRange = 4f;
                alertTime = 0.5f;
                patrolSpeed = 2.5f;
                chaseSpeed = 4f;
                callsBackup = true;
                break;

            case EnemyRank.Mid:
                detectRange = 9f;
                alertTime = 2f;
                patrolSpeed = 2.5f;
                chaseSpeed = 4f;
                canRookDash = true;
                break;

            case EnemyRank.High:
                detectRange = 6f;
                alertTime = 1f;
                patrolSpeed = 4f;
                chaseSpeed = 6.5f;
                canGrab = true;
                break;

            case EnemyRank.Captain:
                detectRange = 7f;
                alertTime = 0.8f;
                patrolSpeed = 3.5f;
                chaseSpeed = 5f;
                canShortDash = true;
                canGrab = true;
                break;
        }

        vision.DetectRange = detectRange;
    }

    void Update()
    {
        UpdateAlert();
        UpdateTimers();

        // Glide toward the current target position and rotation
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        bool isMoving = Vector3.Distance(transform.position, destination) > 0.001f;
        bool isTurning = Quaternion.Angle(transform.rotation, targetRotation) > 0.1f;

        if (isMoving || isTurning)
        {
            return;
        }

        transform.position = destination;
        transform.rotation = targetRotation;

        if (isDashing)
        {
            isDashing = false;
            moveSpeed = chaseSpeed;

            if (canRookDash)
            {
                restTimer = Random.Range(restTimeMin, restTimeMax);
            }
        }

        if (restTimer > 0f || isGrabbing)
        {
            return;
        }

        UpdateState();

        if (state == State.Patrol)
        {
            Patrol();
        }
        else
        {
            Chase();
        }
    }

    void UpdateTimers()
    {
        if (restTimer > 0f)
        {
            restTimer -= Time.deltaTime;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (grabCooldownTimer > 0f)
        {
            grabCooldownTimer -= Time.deltaTime;
        }
    }

    // Runs every frame: the player has to stay detected for alertTime seconds before we notice
    void UpdateAlert()
    {
        if (state != State.Patrol) return;

        if (vision.PlayerDetected)
        {
            alertProgress += Time.deltaTime;
        }
        else
        {
            alertProgress = Mathf.Max(0f, alertProgress - Time.deltaTime);
        }

        if (vision.PlayerDetected && alertProgress >= alertTime)
        {
            StartChase(player.position, true);
        }
    }

    // shout = true when this enemy noticed the player itself (it alerts others and Low-Rank calls backup)
    void StartChase(Vector3 position, bool shout)
    {
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
        }
        isWaiting = false;

        state = State.Chase;
        moveSpeed = chaseSpeed;
        lastSeenPosition = position;
        alertProgress = 0f;

        if (shout)
        {
            AlertNearbyEnemies(position);

            if (callsBackup && !hasCalledBackup)
            {
                hasCalledBackup = true;
                CallBackup(position);
            }
        }
    }

    // Other enemies (and reinforcements) call this to tell us where the player is
    public void ReceiveAlert(Vector3 position)
    {
        if (state == State.Chase)
        {
            if (!playerVisible)
            {
                lastSeenPosition = position;
            }

            return;
        }

        StartChase(position, false);
    }

    void AlertNearbyEnemies(Vector3 position)
    {
        foreach (EnemyMovement other in allEnemies)
        {
            if (other == this) continue;

            if (FlatDistance(transform.position, other.transform.position) <= alertRadius)
            {
                other.ReceiveAlert(position);
            }
        }
    }

    // Low-Rank: 50/50 between calling a Mid-Rank and a High-Rank that is already on the map
    void CallBackup(Vector3 position)
    {
        EnemyRank calledRank;
        EnemyRank otherRank;

        if (Random.value < 0.5f)
        {
            calledRank = EnemyRank.Mid;
            otherRank = EnemyRank.High;
        }
        else
        {
            calledRank = EnemyRank.High;
            otherRank = EnemyRank.Mid;
        }

        EnemyMovement responder = FindNearestEnemy(calledRank, position);

        if (responder == null)
        {
            responder = FindNearestEnemy(otherRank, position);
        }

        if (responder == null)
        {
            Debug.Log("No Mid-Rank or High-Rank available to answer the call");
            return;
        }

        StartCoroutine(SendBackup(responder, position));
    }

    // The responder starts heading over after a delay
    IEnumerator SendBackup(EnemyMovement responder, Vector3 position)
    {
        float arriveIn = Random.Range(backupMinTime, backupMaxTime);
        float travelTime = responder.EstimateTravelTime(position);

        yield return new WaitForSeconds(Mathf.Max(0f, arriveIn - travelTime));

        if (responder != null)
        {
            responder.ReceiveAlert(position);
        }
    }

    // The closest enemy of that rank that isn't already chasing
    EnemyMovement FindNearestEnemy(EnemyRank wantedRank, Vector3 position)
    {
        EnemyMovement nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (EnemyMovement other in allEnemies)
        {
            if (other.rank != wantedRank) continue;
            if (other.IsChasing()) continue;

            float distance = FlatDistance(other.transform.position, position);

            if (distance < nearestDistance)
            {
                nearest = other;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    public float EstimateTravelTime(Vector3 target)
    {
        return FlatDistance(transform.position, target) / chaseSpeed;
    }

    public bool IsChasing()
    {
        return state == State.Chase;
    }

    void UpdateState()
    {
        playerVisible = vision.PlayerInSight;

        if (state != State.Chase) return;

        if (playerVisible)
        {
            lastSeenPosition = player.position;
        }
        else if (FlatDistance(transform.position, lastSeenPosition) <= stopAtDistance)
        {
            state = State.Patrol;
            moveSpeed = patrolSpeed;
            alertProgress = 0f;
            hasCalledBackup = false;
        }
    }

    void Patrol()
    {
        if (isWaiting || patrolPoints.Length == 0) return;

        // Rounds the patrol point to the nearest grid cell
        Vector3 point = patrolPoints[currentPatrolIndex].position;
        Vector3 target = new Vector3(Mathf.Round(point.x), point.y, Mathf.Round(point.z));

        if (FlatDistance(transform.position, target) <= stopAtDistance)
        {
            waitRoutine = StartCoroutine(WaitAtPatrolPoint());
            return;
        }

        StepToward(target);
    }

    void Chase()
    {
        float distance = FlatDistance(transform.position, player.position);

        // Touching the player
        if (playerVisible && distance <= attackRange)
        {
            DeathScreen.instance.Show();
            return;
        }

        if (canGrab && TryGrab()) return;
        if (canRookDash && TryRookDash()) return;
        if (canShortDash && TryShortDash()) return;

        StepToward(lastSeenPosition);
    }

    IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;

        yield return new WaitForSeconds(patrolWaitTime);

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
        waitRoutine = null;
    }

    bool TryRookDash()
    {
        if (!playerVisible) return false;

        Vector3 direction;
        int tiles;

        if (!GetLineToPlayer(out direction, out tiles)) return false;

        int dashTiles = Mathf.Min(tiles - 1, maxDashTiles);

        if (dashTiles < 2) return false;

        if (!FaceDirection(direction)) return true;

        StartDash(direction * dashTiles);
        return true;
    }

    bool TryShortDash()
    {
        if (dashCooldownTimer > 0f || !playerVisible) return false;

        if (FlatDistance(transform.position, lastSeenPosition) < 3f) return false;

        Vector3 direction = GetStepDirection(lastSeenPosition);

        if (direction == Vector3.zero) return false;

        if (!PathIsClear(direction, 2f)) return false;

        if (!FaceDirection(direction)) return true;

        dashCooldownTimer = captainDashCooldown;
        StartDash(direction * 2f);
        return true;
    }

    void StartDash(Vector3 offset)
    {
        isDashing = true;
        moveSpeed = dashSpeed;
        destination = transform.position + offset;
    }

    bool TryGrab()
    {
        if (grabCooldownTimer > 0f || !playerVisible) return false;

        Vector3 direction;
        int tiles;

        if (!GetLineToPlayer(out direction, out tiles)) return false;
        if (tiles < 2 || tiles > grabRangeTiles) return false;

        if (!FaceDirection(direction)) return true;

        StartCoroutine(GrabRoutine(direction, tiles));
        return true;
    }

    IEnumerator GrabRoutine(Vector3 direction, int tiles)
    {
        isGrabbing = true;

        yield return StartCoroutine(MoveLimb(0f, tiles, grabExtendTime));

        Vector3 newDirection;
        int newTiles;
        bool hit = GetLineToPlayer(out newDirection, out newTiles) && newDirection == direction && newTiles <= tiles;

        if (hit)
        {
            GrabPlayer();
            yield return new WaitForSeconds(grabHoldTime);
        }

        // Pull the limb back
        yield return StartCoroutine(MoveLimb(tiles, 0f, grabExtendTime));

        grabCooldownTimer = grabCooldown;
        isGrabbing = false;
    }

    void GrabPlayer()
    {
        if (MessagePopup.instance != null)
        {
            MessagePopup.instance.Show("You have been grabbed!");
        }

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }

    IEnumerator MoveLimb(float startLength, float endLength, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            SetLimbLength(Mathf.Lerp(startLength, endLength, timer / duration));
            yield return null;
        }

        SetLimbLength(endLength);
    }

    // Stretches the limb
    void SetLimbLength(float length)
    {
        if (limb == null) return;

        limb.gameObject.SetActive(length > 0.01f);
        limb.localScale = new Vector3(limbThickness, limbThickness, length);
        limb.localPosition = new Vector3(0f, limbHeight, length / 2f);
    }

    // True if the player is in the same row or column. Gives the direction and how many tiles away.
    bool GetLineToPlayer(out Vector3 direction, out int tiles)
    {
        direction = Vector3.zero;
        tiles = 0;

        Vector3 delta = player.position - transform.position;
        delta.y = 0;

        if (Mathf.Abs(delta.z) < lineTolerance)
        {
            // Same row
            direction = new Vector3(Mathf.Sign(delta.x), 0, 0);
            tiles = Mathf.RoundToInt(Mathf.Abs(delta.x));
        }
        else if (Mathf.Abs(delta.x) < lineTolerance)
        {
            // Same column
            direction = new Vector3(0, 0, Mathf.Sign(delta.z));
            tiles = Mathf.RoundToInt(Mathf.Abs(delta.z));
        }
        else
        {
            return false;
        }

        return PathIsClear(direction, tiles);
    }

    // True if no wall is within this distance in that direction
    bool PathIsClear(Vector3 direction, float distance)
    {
        Vector3 origin = transform.position + new Vector3(0, 0.25f, 0);
        RaycastHit hit;

        Debug.DrawRay(origin, direction * distance, Color.red);

        if (Physics.Raycast(origin, direction, out hit, distance))
        {
            if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Door")) 
            {
                return false;
            }
        }

        return true;
    }

    // Takes one grid step (or one turn) toward the target using the NavMesh path
    void StepToward(Vector3 target)
    {
        Vector3 stepDirection = GetStepDirection(target);

        if (stepDirection == Vector3.zero) return;

        if (!FaceDirection(stepDirection)) return;

        TryMove(stepDirection);
    }

    // Works out which grid direction the next step should go in (zero if there's no step to take)
    Vector3 GetStepDirection(Vector3 target)
    {
        NavMeshHit startHit;
        NavMeshHit targetHit;

        if (!NavMesh.SamplePosition(transform.position, out startHit, 2f, NavMesh.AllAreas)) return Vector3.zero;
        if (!NavMesh.SamplePosition(target, out targetHit, 2f, NavMesh.AllAreas)) return Vector3.zero;

        NavMeshPath path = new NavMeshPath();
        bool found = NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, path);

        if (!found || path.corners.Length < 2) return Vector3.zero;

        Vector3 offset = path.corners[1] - startHit.position;

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

        if (firstChoice != Vector3.zero && Valid(firstChoice))
        {
            return firstChoice;
        }

        if (secondChoice != Vector3.zero && Valid(secondChoice))
        {
            return secondChoice;
        }

        // Both directions blocked
        return Vector3.zero;
    }

    bool FaceDirection(Vector3 direction)
    {
        if (GetFacingDirection() == direction)
        {
            return true;
        }

        targetRotation = Quaternion.LookRotation(direction);
        return false;
    }

    Vector3 GetFacingDirection()
    {
        Vector3 forward = transform.forward;
        return new Vector3(Mathf.Round(forward.x), 0, Mathf.Round(forward.z));
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
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
        return PathIsClear(direction, rayLength);
    }
}