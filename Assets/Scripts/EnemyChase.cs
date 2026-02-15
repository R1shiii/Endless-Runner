using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    private Player_Movement playerMovement;
    private Rigidbody playerRb;

    [Tooltip("How often (seconds) the agent updates its destination.")]
    public float updateInterval = 0.15f;

    [Tooltip("Multiplier to make the enemy slightly faster than the player so it can catch up.")]
    public float speedMultiplier = 1.25f;

    [Tooltip("How far ahead (seconds) to predict the player's position.")]
    public float predictionFactor = 0.5f;

    [Tooltip("Minimum distance change required to call SetDestination again (meters).")]
    public float destinationThreshold = 0.5f;

    [Tooltip("If true, the enemy will attempt to warp to the nearest NavMesh if not on it.")]
    public bool tryRecoverOffMesh = true;

    // Jump-related
    [Header("Jumping")]
    [Tooltip("Forward raycast distance to detect small obstacles.")]
    public float obstacleDetectDistance = 1.2f;
    [Tooltip("Maximum obstacle collider height that the enemy will attempt to jump over.")]
    public float maxJumpableHeight = 1.2f;
    [Tooltip("Upward velocity applied when jumping.")]
    public float jumpUpVelocity = 6f;
    [Tooltip("Forward impulse multiplier applied during jump.")]
    public float forwardJumpMultiplier = 0.6f;

    float timer;
    Vector3 lastDestination = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

    Rigidbody rb;
    bool isJumping;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (agent == null)
        {
            Debug.LogError("EnemyChase requires a NavMeshAgent component.");
            enabled = false;
            return;
        }

        if (rb == null)
        {
            Debug.LogWarning("EnemyChase: Rigidbody missing. Add a Rigidbody to enable jumping. Jumping will be disabled until Rigidbody is present.");
        }

        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            playerMovement = player.GetComponent<Player_Movement>();
            playerRb = player.GetComponent<Rigidbody>();
        }

        // Make agent more responsive so it can keep up
        agent.acceleration = Mathf.Max(agent.acceleration, 20f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 120f);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // sensible agent configuration
        agent.autoBraking = false;
        agent.autoRepath = true;

        // If not on navmesh try to recover (useful for dynamic levels)
        if (!agent.isOnNavMesh && tryRecoverOffMesh)
        {
            RecoverToNavMesh();
        }
    }

    void Update()
    {
        if (player == null || agent == null)
            return;

        // ensure agent remains on navmesh
        if (!agent.isOnNavMesh)
        {
            if (tryRecoverOffMesh)
            {
                RecoverToNavMesh();
            }
            return;
        }

        // If currently jumping let physics handle movement; nav agent will be re-enabled by coroutine
        if (isJumping)
            return;

        // Simple obstacle detection in front
        DetectAndMaybeJump();

        timer += Time.deltaTime;
        if (timer < updateInterval)
            return;
        timer = 0f;

        // Sync speed to player (with multiplier so enemy can catch up)
        if (playerMovement != null)
        {
            agent.speed = Mathf.Max(1f, playerMovement.Speed * speedMultiplier);
        }
        else if (playerRb != null)
        {
            agent.speed = Mathf.Max(agent.speed, playerRb.linearVelocity.magnitude * speedMultiplier);
        }

        // Predict player's future position using velocity if available
        Vector3 predicted = player.position;
        if (playerRb != null)
            predicted += playerRb.linearVelocity * predictionFactor;
        else if (playerMovement != null)
            predicted += player.forward * (playerMovement.Speed * predictionFactor);

        // Only update destination if it moved enough (reduces path thrashing)
        if ((lastDestination - predicted).sqrMagnitude > destinationThreshold * destinationThreshold)
        {
            if (!agent.pathPending)
            {
                agent.SetDestination(predicted);
                lastDestination = predicted;
            }
        }

        // If the path is stale or the agent path is invalid, reset and try again
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.isPathStale)
        {
            agent.ResetPath();
            agent.SetDestination(predicted);
        }
    }

    void DetectAndMaybeJump()
    {
        if (rb == null || isJumping)
            return;

        // cast a short ray from slightly above ground forward
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, obstacleDetectDistance))
        {
            // ignore triggers and the player
            if (hit.collider != null && !hit.collider.isTrigger && hit.transform != player)
            {
                float obstacleHeight = hit.collider.bounds.size.y;

                // If obstacle is small enough, attempt jump
                if (obstacleHeight <= maxJumpableHeight)
                {
                    StartCoroutine(JumpOverObstacle());
                }
            }
        }
    }

    IEnumerator JumpOverObstacle()
    {
        if (rb == null || agent == null)
            yield break;

        isJumping = true;

        // disable agent so physics moves the transform
        agent.enabled = false;

        // preserve horizontal velocity from agent to rb
        Vector3 horizontalVelocity = Vector3.zero;
        if (agent.velocity.sqrMagnitude > 0.01f)
            horizontalVelocity = new Vector3(agent.velocity.x, 0f, agent.velocity.z);

        rb.linearVelocity = horizontalVelocity;

        // apply upward and forward impulse
        rb.AddForce(Vector3.up * jumpUpVelocity, ForceMode.VelocityChange);
        rb.AddForce(transform.forward * agent.speed * forwardJumpMultiplier, ForceMode.VelocityChange);

        // wait until we detect grounded
        yield return new WaitForFixedUpdate();
        int safety = 0;
        while (!IsGrounded() && safety < 300) // safety to avoid infinite loop
        {
            safety++;
            yield return null;
        }

        // small delay to stabilize, then place agent back on NavMesh and resume
        yield return new WaitForFixedUpdate();

        RecoverToNavMesh();

        // re-enable agent
        agent.enabled = true;
        isJumping = false;
    }

    bool IsGrounded()
    {
        Collider col = GetComponent<Collider>();
        float halfHeight = col ? (col.bounds.size.y * 0.5f) : 0.5f;
        return Physics.Raycast(transform.position, Vector3.down, halfHeight + 0.1f);
    }

    void RecoverToNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            // fallback: try a slightly larger radius
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
    }
}
