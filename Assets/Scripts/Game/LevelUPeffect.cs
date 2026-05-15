using UnityEngine;

public class LevelUpEffectManager : MonoBehaviour
{
    public static LevelUpEffectManager Instance;

    [Header("Level Up Effect Prefab")]
    public GameObject levelUpEffectPrefab;
    [Header("Level Up Sound")]
    public AudioClip levelUpSound;
    [Header("Sound Volume")]
    public float soundVolume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayLevelUpEffect(Vector3 pos)
    {
        if (levelUpEffectPrefab != null)
        {
            GameObject eff = Instantiate(levelUpEffectPrefab, pos, Quaternion.identity);
            Destroy(eff, 3f);
        }

        if (levelUpSound != null)
        {
            audioSource.PlayOneShot(levelUpSound, soundVolume);
        }
    }
}