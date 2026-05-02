using UnityEngine;
using UnityEngine.AI;

public class CrusherNPC : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float detectionRange = 30f;
    public float hoverHeight = 6f;

    [Header("Crush Attack")]
    public float pauseBeforeCrush = 0.8f;
    public float crushSpeed = 20f;
    public float riseSpeed = 3f;
    public float crushGroundY = 0.5f;

    [Header("Patrol")]
    public float patrolRadius = 15f;
    public float patrolWaitTime = 2f;

    private NavMeshAgent _agent;
    private Transform _visual;
    private float _patrolTimer;

    private enum State { Patrol, Chasing, Hovering, Crushing, Rising }
    private State _state = State.Patrol;
    private float _stateTimer;
    private Vector3 _crushTarget;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;

        // The NavMeshAgent moves on the ground plane
        // The visual (child cube) floats above
        _visual = transform.GetChild(0);
        _visual.localPosition = new Vector3(0, hoverHeight, 0);

        PickNewPatrolPoint();
    }

    void Update()
    {
        switch (_state)
        {
            case State.Patrol:
                Patrol();
                CheckForPlayers();
                UpdateVisualHover();
                break;

            case State.Chasing:
                CheckForPlayers();
                UpdateVisualHover();
                if (!_agent.pathPending && _agent.remainingDistance < 1.5f)
                {
                    // Arrived above player, start hover pause
                    _agent.isStopped = true;
                    _state = State.Hovering;
                    _stateTimer = 0f;
                }
                break;

            case State.Hovering:
                _stateTimer += Time.deltaTime;
                // Shake slightly to telegraph the attack
                float shake = Mathf.Sin(_stateTimer * 30f) * 0.1f;
                _visual.localPosition = new Vector3(shake, hoverHeight, 0);

                if (_stateTimer >= pauseBeforeCrush)
                {
                    _state = State.Crushing;
                    _crushTarget = new Vector3(0, crushGroundY, 0);
                }
                break;

            case State.Crushing:
                // Slam down fast
                Vector3 crushPos = _visual.localPosition;
                crushPos.y = Mathf.MoveTowards(crushPos.y, crushGroundY, crushSpeed * Time.deltaTime);
                _visual.localPosition = crushPos;

                if (Mathf.Abs(crushPos.y - crushGroundY) < 0.05f)
                {
                    _visual.localPosition = new Vector3(0, crushGroundY, 0);
                    _state = State.Rising;
                    _stateTimer = 0f;
                }
                break;

            case State.Rising:
                // Rise back up slowly
                Vector3 risePos = _visual.localPosition;
                risePos.y = Mathf.MoveTowards(risePos.y, hoverHeight, riseSpeed * Time.deltaTime);
                _visual.localPosition = risePos;

                if (Mathf.Abs(risePos.y - hoverHeight) < 0.05f)
                {
                    _visual.localPosition = new Vector3(0, hoverHeight, 0);
                    _agent.isStopped = false;
                    _state = State.Patrol;
                    PickNewPatrolPoint();
                }
                break;
        }
    }

    private void UpdateVisualHover()
    {
        // Gentle bob while moving
        float bob = Mathf.Sin(Time.time * 2f) * 0.15f;
        _visual.localPosition = new Vector3(0, hoverHeight + bob, 0);
    }

    private void CheckForPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = detectionRange;
        Transform closest = null;

        foreach (var player in players)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = player.transform;
            }
        }

        if (closest != null)
        {
            _agent.SetDestination(closest.position);
            if (_state == State.Patrol)
                _state = State.Chasing;
        }
        else if (_state == State.Chasing)
        {
            _state = State.Patrol;
            PickNewPatrolPoint();
        }
    }

    private void Patrol()
    {
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            _patrolTimer += Time.deltaTime;
            if (_patrolTimer >= patrolWaitTime)
            {
                PickNewPatrolPoint();
                _patrolTimer = 0f;
            }
        }
    }

    private void PickNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
        _patrolTimer = 0f;
    }
}