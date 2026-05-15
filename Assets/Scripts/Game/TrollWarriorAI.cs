using UnityEngine;
using UnityEngine.AI;

public class TrollWarriorAI : MonsterAIBase
{
    [Header("Troll Warrior")]
    public float chaseSpeed = 3.5f;
    public float normalAttackCd = 1.8f;
    public float attackAnimDuration = 1.2f;

    [Header("=== Sound ===")]
    public AudioClip[] attackSounds;
    public AudioClip deathSound;
    [Range(0, 1)] public float soundVolume = 0.6f;

    private float _attackCdTimer;
    private float _attackAnimTimer;
    private bool _isAttacking;
    private AudioSource _audioSource;

    protected override void Awake()
    {
        base.Awake();
        // Automatically add AudioSource if missing
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.volume = soundVolume;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        if (_attackCdTimer > 0)
            _attackCdTimer -= Time.deltaTime;

        if (_attackAnimTimer > 0)
        {
            _attackAnimTimer -= Time.deltaTime;
        }
        else
        {
            _isAttacking = false;
        }
    }

    protected override void ChaseAndAttack(float distance)
    {
        if (agent == null || !agent.isOnNavMesh || player == null) return;

        // Lock movement and rotation during attack
        if (_isAttacking)
        {
            agent.isStopped = true;
            return;
        }

        // Rotate smoothly toward player
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.magnitude > 0.1f)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime);

        agent.isStopped = false;

        if (distance > attackRange)
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
            if (_attackCdTimer <= 0)
            {
                PlayAttack();
            }
        }
    }

    void PlayAttack()
    {
        _isAttacking = true;
        _attackCdTimer = normalAttackCd;
        _attackAnimTimer = attackAnimDuration;

        anim.SetTrigger("Attack" + Random.Range(1, 8));

        // Play random attack sound
        if (attackSounds != null && attackSounds.Length > 0)
        {
            int rand = Random.Range(0, attackSounds.Length);
            _audioSource.PlayOneShot(attackSounds[rand]);
        }
    }

    // Called by animation event at the end of attack
    public void OnAttackAnimEnd()
    {
        _isAttacking = false;
        _attackAnimTimer = 0;
    }

    protected override void Die()
    {
        base.Die();
        anim.SetTrigger("Death1");

        // Play death sound
        if (deathSound != null)
        {
            _audioSource.PlayOneShot(deathSound);
        }

        Destroy(gameObject, 2f);
    }
}