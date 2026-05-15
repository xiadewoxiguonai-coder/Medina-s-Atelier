using UnityEngine;

public class PlayerHited : MonoBehaviour
{
    public float hitCooldown = 1f;
    public float knockbackForce = 0.1f;

    [Header("Recenter Delay")]
    public float recenterDelay = 0.5f;

    private bool canBeHit = true;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MonsterAtack") && canBeHit)
        {
            TakeDamage(30);
            ApplyKnockback(other.transform);

            Invoke(nameof(RecenterRotation), recenterDelay);
        }
    }

    public void TakeDamage(int damage)
    {
        if (PlayerStatsManager.Instance == null) return;

        PlayerStatsManager.Instance.TakeDamage(damage);
        Debug.Log("Player took damage: " + damage + "  Current HP: " + PlayerStatsManager.Instance.GetCurrentHP());

        canBeHit = false;
        Invoke(nameof(ResetHit), hitCooldown);
    }

    void ApplyKnockback(Transform attacker)
    {
        if (rb == null) return;

        Vector3 dir = (transform.position - attacker.position).normalized;
        dir.y = 0.2f;
        dir.Normalize();

        bool wasKinematic = rb.isKinematic;
        rb.isKinematic = false;
        rb.AddForce(dir * knockbackForce, ForceMode.Impulse);

        Invoke(nameof(StopKnockback), 0.2f);
    }

    void StopKnockback()
    {
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
    }

    void RecenterRotation()
    {
        float currentY = transform.rotation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, currentY, 0);
    }

    void ResetHit() => canBeHit = true;
}