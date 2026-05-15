using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    public CharacterStats playerStats;

    private float _attackMultiplier = 1f;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (playerStats.Hp == null || playerStats.Hp.Length < 2)
            playerStats.Hp = new int[] { 100, 100 };

        if (playerStats.Mp == null || playerStats.Mp.Length < 2)
            playerStats.Mp = new int[] { 50, 50 };

        if (playerStats.Stamina == null || playerStats.Stamina.Length < 2)
            playerStats.Stamina = new int[] { 100, 100 };
    }

    public int GetPlayerAttack() => playerStats.AttackPower;
    public int GetCurrentHP() => playerStats.Hp[1];
    public int GetMaxHP() => playerStats.Hp[0];

    public void TakeDamage(int damage)
    {
        playerStats.Hp[1] = Mathf.Max(0, playerStats.Hp[1] - damage);
    }

    public void SpendMana(int amount)
    {
        if (playerStats.Mp == null || playerStats.Mp.Length < 2) return;
        playerStats.Mp[1] = Mathf.Max(0, playerStats.Mp[1] - amount);
    }

    public void RestoreMana(int amount)
    {
        if (playerStats.Mp == null || playerStats.Mp.Length < 2) return;
        playerStats.Mp[1] = Mathf.Min(playerStats.Mp[0], playerStats.Mp[1] + amount);
    }
    public void ResetPermanentDamage()
    {
        _attackMultiplier = 1f;
    }


    public void ApplyPermanentDoubleDamage()
    {

        playerStats.attackPower *= 2;
        GetPlayerAttack();
    }

    



    public void Reset()
    {
        playerStats.ResetLevelAndStats();
    }
}