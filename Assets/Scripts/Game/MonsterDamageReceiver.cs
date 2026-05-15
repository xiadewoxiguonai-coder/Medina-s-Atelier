using UnityEngine;

public class MonsterDamageReceiver : MonoBehaviour
{
    [Header("Extra Damage Multipliers")]
    public float playerAttackMultiplier = 1f;    // Player normal attack
    public float arrowDamageMultiplier = 1f;    // Arrow
    public float lightSaberMultiplier = 1.5f;   // Light saber
    public float magicMultiplier = 2f;          // Magic

    [Header("Cooldown")]
    public float hitCooldown = 0.4f;

    [Header("=== Hit Sounds ===")]
    public AudioClip[] hitSounds; // Assign 2+ sounds
    [Range(0, 1)] public float hitVolume = 0.5f;

    private MonsterAIBase _monsterAI;
    private float _lastHit;
    private AudioSource _audioSource;

    void Awake()
    {
        _monsterAI = GetComponent<MonsterAIBase>();

        // Auto add AudioSource if missing
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.volume = hitVolume;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_monsterAI == null || _monsterAI.isDead) return;
        if (Time.time < _lastHit + hitCooldown) return;

        int playerAtk = PlayerStatsManager.Instance.GetPlayerAttack();
        int finalDamage = playerAtk;
        bool isHit = false;

        // Player normal attack
        if (other.CompareTag("playerAttack"))
        {
            finalDamage = Mathf.RoundToInt(playerAtk * playerAttackMultiplier);
            isHit = true;
            PlayRandomHitSound();
        }
        // Arrow attack
        else if (other.CompareTag("arrow"))
        {
            finalDamage = Mathf.RoundToInt(playerAtk * arrowDamageMultiplier);
            Destroy(other.gameObject);
            isHit = true;
        }
        // Light saber attack
        else if (other.CompareTag("lightSaber"))
        {
            finalDamage = Mathf.RoundToInt(playerAtk * lightSaberMultiplier);
            isHit = true;
        }
        // Magic attack
        else if (other.CompareTag("magic"))
        {
            finalDamage = Mathf.RoundToInt(playerAtk * magicMultiplier);
            isHit = true;
        }

        // Apply damage only if hit
        if (isHit)
        {
            _lastHit = Time.time;
            _monsterAI.TakeDamage(finalDamage);
        }
    }

    // Play random hit sound
    void PlayRandomHitSound()
    {
        if (hitSounds == null || hitSounds.Length == 0) return;

        int rand = Random.Range(0, hitSounds.Length);
        _audioSource.PlayOneShot(hitSounds[rand]);
    }
}