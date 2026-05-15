using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class MonsterAIBase : MonoBehaviour
{
    public Monster monsterData;

    protected GameObject _hpBarInstance;

    [Header("Vision Settings")]
    public float visionRange = 8f;
    public float visionAngle = 60f;

    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float moveSpeed = 2f;

    [Header("Aggro Settings")]
    public float lostAggroRange = 30f;
    public float aggroKeepTime = 15f;

    [Header("Patrol Settings")]
    public bool enablePatrol = true;
    public float patrolRadius = 5f;
    public float patrolWaitTime = 2f;

    [Header("Ground Align")]
    public float groundRayLength = 1.5f;
    public float groundOffset = 0.1f;
    public float alignSpeed = 8f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    protected NavMeshAgent agent;
    protected Animator anim;
    protected Transform player;
    public bool isDead = false;
    public bool isAggro = false;
    protected float aggroTimer = 0f;
    protected Vector3 startPos;
    protected Vector3 patrolTarget;
    protected float patrolWaitTimer = 0f;

    private float _standbyTimer = 0f;
    private bool _justResetPatrol = false;
    private const int MaxPatrolRetry = 20;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        startPos = transform.position;
        patrolTarget = GetValidGroundPatrolPoint();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.isStopped = false;
        }

        // Recursively find child with exact name: 4-Red
        _hpBarInstance = FindExactChild(transform, "4-Red");
        if (_hpBarInstance != null)
            RefreshHpBar();
    }

    // Recursively find child by exact name
    private GameObject FindExactChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child.gameObject;

            GameObject res = FindExactChild(child, targetName);
            if (res != null) return res;
        }
        return null;
    }

    void RefreshHpBar()
    {
        if (_hpBarInstance == null) return;

        float percent = (float)monsterData.Hp / monsterData.MaxHp;
        // Fixed YZ scale 0.06, only change X
        _hpBarInstance.transform.localScale = new Vector3(percent * 0.06f, 0.06f, 0.06f);
    }

    protected virtual void Update()
    {
        if (isDead || player == null || agent == null || !agent.isOnNavMesh)
        {
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsRunning", false);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = CheckVision(distanceToPlayer);

        if (canSeePlayer || (isAggro && distanceToPlayer < lostAggroRange))
        {
            isAggro = true;
            aggroTimer = aggroKeepTime;
        }

        if (isAggro)
        {
            aggroTimer -= Time.deltaTime;
            if (aggroTimer <= 0) isAggro = false;
        }

        if (!isAggro && agent.velocity.magnitude < 0.05f)
        {
            _standbyTimer += Time.deltaTime;
            if (_standbyTimer >= 2f && !_justResetPatrol)
            {
                _justResetPatrol = true;
                patrolTarget = GetValidGroundPatrolPoint();
                agent.isStopped = false;
                agent.ResetPath();
                Invoke(nameof(AllowResetAgain), 1f);
            }
        }
        else
        {
            _standbyTimer = 0f;
        }

        if (isAggro)
        {
            ChaseAndAttack(distanceToPlayer);
        }
        else if (enablePatrol)
        {
            Patrol();
        }
        else
        {
            agent.isStopped = true;
        }

        float vel = agent.velocity.magnitude;
        anim.SetBool("IsWalking", vel > 0.1f && !isAggro);
        anim.SetBool("IsRunning", vel > 0.1f && isAggro);

        AlignToGround();
    }

    void AllowResetAgain() => _justResetPatrol = false;

    protected virtual bool CheckVision(float distance)
    {
        if (distance > visionRange) return false;
        Vector3 dir = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dir) > visionAngle / 2) return false;
        if (Physics.Linecast(transform.position + Vector3.up * 1.2f, player.position + Vector3.up * 1.2f, obstacleLayer)) return false;
        return true;
    }

    protected virtual void ChaseAndAttack(float distance) { }

    protected virtual void Patrol()
    {
        float dis = Vector3.Distance(transform.position, patrolTarget);
        agent.isStopped = false;

        if (dis < 0.6f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0;
                patrolTarget = GetValidGroundPatrolPoint();
            }
        }
        agent.SetDestination(patrolTarget);
    }

    protected virtual Vector3 GetValidGroundPatrolPoint()
    {
        for (int i = 0; i < MaxPatrolRetry; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * Random.Range(1f, patrolRadius);
            Vector3 point = startPos + new Vector3(rnd.x, 0, rnd.y);
            if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, 5f, groundLayer))
                return point;
        }
        return startPos;
    }

    protected virtual void AlignToGround()
    {
        if (groundLayer == 0) return;
        if (Physics.Raycast(transform.position + Vector3.up * groundRayLength, Vector3.down, out RaycastHit hit, groundRayLength, groundLayer))
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, hit.point.y + groundOffset, alignSpeed * Time.deltaTime);
            transform.position = pos;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        int realDamage = Mathf.Max(1, damage - monsterData.Toughness);
        monsterData.Hp -= realDamage;
        RefreshHpBar();

        if (monsterData.Hp <= 0)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        agent.isStopped = true;
        anim.SetBool("IsWalking", false);
        anim.SetBool("IsRunning", false);

        if (_hpBarInstance != null)
            _hpBarInstance.SetActive(false);

        GameObject mapObj = GameObject.Find("map");
        if (mapObj != null)
        {
            MonsterCountManager manager = mapObj.GetComponent<MonsterCountManager>();
            if (manager != null)
            {
                manager.MonsterDie();
            }
        }

        if (PlayerStatsManager.Instance != null)
        {
            int getExp = monsterData.level * 15;
            PlayerStatsManager.Instance.playerStats.AddExp(getExp);
            Debug.Log($"Monster Lv.{monsterData.level} killed. Exp gained: {getExp}");
        }

        Destroy(gameObject, 3f);
    }
}