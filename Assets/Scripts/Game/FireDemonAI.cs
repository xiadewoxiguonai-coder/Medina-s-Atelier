using UnityEngine;
using UnityEngine.AI;

public class FireDemonAI : MonsterAIBase
{
    [Header("Fire Demon Basic Settings")]
    public float chaseSpeed = 4.5f;
    public float normalAttackCd = 2.0f;
    public float attackAnimDuration = 1.4f;

    [Header("Rage Attack Settings")]
    public bool enableRageAttack = true;
    [Range(0.1f, 0.5f)] public float rageHpPercent = 0.3f;

    [Header("Turn Settings")]
    public float turnAngleThreshold = 45f;
    public float turnLerpSpeed = 12f;

    [Header("Audio Settings")]
    public AudioClip[] normalAttackSounds;
    public AudioClip rageAttackSound;
    public AudioClip takeDamageSound;
    public AudioClip deathSound;
    [Range(0, 1)] public float soundVolume = 0.6f;

    private float _attackCdTimer;
    private float _attackAnimTimer;
    private bool _isAttacking;
    private bool _isTakingHit;
    private AudioSource _audioSource;

    protected override void Awake()
    {
        base.Awake();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.volume = soundVolume;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        // Cooldown timer for attack
        if (_attackCdTimer > 0)
            _attackCdTimer -= Time.deltaTime;

        // Lock movement during attack animation
        if (_attackAnimTimer > 0)
        {
            _attackAnimTimer -= Time.deltaTime;
        }
        else
        {
            _isAttacking = false;
        }

        // Sync movement animations with parent AI logic
        float vel = agent.velocity.magnitude;
        anim.SetBool("IsWalking", vel > 0.1f && !isAggro);
        anim.SetBool("IsRunning", vel > 0.1f && isAggro);
    }

    protected override void ChaseAndAttack(float distance)
    {
        if (agent == null || !agent.isOnNavMesh || player == null) return;

        // Stop movement and rotation during attack or hit stun
        if (_isAttacking || _isTakingHit)
        {
            agent.isStopped = true;
            return;
        }

        Vector3 dirToPlayer = player.position - transform.position;
        dirToPlayer.y = 0;

        // Play turn animations if angle is too large
        float angle = Vector3.SignedAngle(transform.forward, dirToPlayer, Vector3.up);
        if (Mathf.Abs(angle) > turnAngleThreshold)
        {
            if (angle > 0)
                anim.SetTrigger("TurnRight");
            else
                anim.SetTrigger("TurnLeft");
        }

        // Smooth rotation toward player
        if (dirToPlayer.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(dirToPlayer), turnLerpSpeed * Time.deltaTime);
        }

        agent.isStopped = false;

        // Chase player if out of attack range
        if (distance > attackRange)
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
        // Stop and attack if in range
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

        // Check if health is low enough for rage attack
        bool lowHp = (float)monsterData.Hp / monsterData.MaxHp <= rageHpPercent;

        if (enableRageAttack && lowHp)
        {
            anim.SetTrigger("AttackRage");
            if (rageAttackSound != null)
                _audioSource.PlayOneShot(rageAttackSound);
        }
        else
        {
            // Randomize between two normal attacks
            int rand = Random.Range(0, 2);
            if (rand == 0)
                anim.SetTrigger("AttackPunch");
            else
                anim.SetTrigger("AttackHand");

            // Play random attack sound
            if (normalAttackSounds != null && normalAttackSounds.Length > 0)
            {
                int sRand = Random.Range(0, normalAttackSounds.Length);
                _audioSource.PlayOneShot(normalAttackSounds[sRand]);
            }
        }
    }

    // Called by animation event at the end of attack
    public void OnAttackAnimEnd()
    {
        _isAttacking = false;
        _attackAnimTimer = 0;
    }

    // External damage call (from player spells, bullets, etc.)
    public new void TakeDamage(int damage)
    {
        if (isDead || _isTakingHit) return;

        base.TakeDamage(damage);
        _isTakingHit = true;

        // Play hit react animation and sound
        anim.SetTrigger("TakeDamage");
        if (takeDamageSound != null)
            _audioSource.PlayOneShot(takeDamageSound);

        // Play pushing stagger after a short delay
        Invoke(nameof(PlayPushing), 0.25f);
        // Recover from hit stun
        Invoke(nameof(RecoverFromHit), 0.7f);
    }

    void PlayPushing()
    {
        anim.SetTrigger("Pushing");
    }

    void RecoverFromHit()
    {
        _isTakingHit = false;
        // Play stand-up animation after stagger
        anim.SetTrigger("StandUp");
    }

    protected override void Die()
    {
        base.Die();
        anim.SetTrigger("Death");

        // Play death sound
        if (deathSound != null)
            _audioSource.PlayOneShot(deathSound);

        Destroy(gameObject, 2f);
    }
}