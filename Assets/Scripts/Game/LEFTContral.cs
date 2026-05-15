using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LEFTContral : MonoBehaviour
{
    [Header("Left Pose")]
    public SteamVR_Behaviour_Pose leftHand;
    [Header("Right Pose")]
    public SteamVR_Behaviour_Pose rightHand;

    private SteamVR_Action_Vector2 leftJoystick;
    private SteamVR_Action_Vector2 rightJoystick;
    private SteamVR_Action_Boolean triggerBtn;
    private SteamVR_Action_Boolean gripBtn;

    [Header("Camera Rig")]
    public Transform cameraRig;
    [Header("VR Headset")]
    public Transform vrHead;

    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float deadzone = 0.15f;
    [Header("Rotation")]
    public float turnSpeed = 80f;

    [Header("UI Settings")]
    public GameObject operationMenu;
    public GameObject itemWheel;

    [Header("3 UI for HP / MP / EXP")]
    public GameObject hpBarObj;
    public GameObject mpBarObj;
    public GameObject expBarObj;

    [Header("Voice Recognition")]
    public RuneThreeModelRecognize_VROnly runeRecognizer;
    [Header("Rune Spawn Point")]
    public Transform runeSpawnPoint;
    [Header("Rune Lifetime (seconds)")]
    public float runeLifeTime = 10f;

    [Header("Burn Effect Prefab")]
    public GameObject burnEffectPrefab;

    [Header("=== Magic Effects ===")]
    public GameObject doubleDamageEffect;
    public GameObject expGainEffect;
    public GameObject fireBallPrefab;
    public GameObject lightningPrefab;
    public GameObject spikePrefab;
    public GameObject recordEffectPrefab;
    public GameObject healEffectPrefab;
    public GameObject manaEffectPrefab;
    public GameObject waterDragonBulletPrefab;
    public GameObject haoLongFireEffect;

    public float haoLongFireTime = 5f;
    private GameObject _curFireEffect;

    [Header("Audio")]
    public AudioClip noManaSound;
    public AudioClip playerDeathSound;

    [Header("Death Screen UI")]
    public GameObject deathScreen;
    public GameObject nextScreen;

    private GameObject _currentRecordEffect;

    [Header("Mana Cost per Rune")]
    public int manaCostPerRune = 1;

    private GameObject lightSaberL;
    private GameObject lightSaberR;
    private GameObject zhang;
    private bool lightSaberUnlocked = false;
    private bool _permanentDoubleDamage = false;

    public List<GameObject> runePrefabs;
    private Dictionary<string, GameObject> runeDict = new Dictionary<string, GameObject>();
    private bool isRecording = false;
    private List<ActiveRune> activeRunes = new List<ActiveRune>();

    private bool isPlayerDead = false;
    private AudioSource _audioSource;
    private Texture2D _grayTexture;
    private bool _showGray = false;

    void Awake()
    {
        leftJoystick = SteamVR_Input.GetVector2Action("default", "Thumbstick");
        rightJoystick = SteamVR_Input.GetVector2Action("default", "Thumbstick");
        triggerBtn = SteamVR_Input.GetBooleanAction("default", "GrabPinch");
        gripBtn = SteamVR_Input.GetBooleanAction("default", "GrabGrip");

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.volume = 1f;

        InitRuneDictionary();
    }

    private void Start()
    {
        lightSaberL = FindInAllChildren("lightSaberL");
        lightSaberR = FindInAllChildren("lightSaberR");
        zhang = FindInAllChildren("zhang");

        if (lightSaberL != null) lightSaberL.SetActive(false);
        if (lightSaberR != null) lightSaberR.SetActive(false);
        if (zhang != null) zhang.SetActive(true);

        if (deathScreen != null) deathScreen.SetActive(false);
        if (nextScreen != null) nextScreen.SetActive(false);

        RefreshAllBarsScale();
        _grayTexture = new Texture2D(1, 1);
        _grayTexture.SetPixel(0, 0, Color.white);
        _grayTexture.Apply();
    }

    GameObject FindInAllChildren(string name)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t.gameObject;
        }
        return null;
    }

    void Update()
    {
        CheckPlayerDeath();

        if (isPlayerDead)
        {
            if (rightHand != null && triggerBtn.GetStateDown(rightHand.inputSource))
            {
                GoToNextScreen();
            }
            return;
        }

        if (cameraRig == null || vrHead == null) return;

        DoLeftMove();
        DoRightTurn();
        DoMenuToggle();
        DoWheelToggle();
        DoVRVoiceRecognition();

        if (rightHand != null && triggerBtn.GetStateDown(rightHand.inputSource))
        {
            CollectAndDestroyAllRunes();
        }

        RefreshAllBarsScale();
    }

    void CheckPlayerDeath()
    {
        if (isPlayerDead) return;
        if (PlayerStatsManager.Instance == null) return;

        var stats = PlayerStatsManager.Instance.playerStats;
        if (stats.Hp[1] <= 0)
        {
            PlayerDie();
        }
    }

    void PlayerDie()
    {
        isPlayerDead = true;

        if (playerDeathSound != null)
            _audioSource.PlayOneShot(playerDeathSound);

        Time.timeScale = 0;

        if (deathScreen != null)
            deathScreen.SetActive(true);

        _showGray = true;
    }

    void GoToNextScreen()
    {
        Time.timeScale = 1;
        isPlayerDead = false;

        if (PlayerStatsManager.Instance != null)
        {
            var stats = PlayerStatsManager.Instance.playerStats;
            stats.Hp[1] = stats.Hp[0];
            RefreshAllBarsScale();
        }

        lightSaberUnlocked = false;
        if (lightSaberL != null) lightSaberL.SetActive(false);
        if (lightSaberR != null) lightSaberR.SetActive(false);
        if (zhang != null) zhang.SetActive(true);

        _permanentDoubleDamage = false;
        PlayerStatsManager.Instance.ResetPermanentDamage();

        activeRunes.Clear();

        isRecording = false;
        if (_currentRecordEffect != null)
        {
            Destroy(_currentRecordEffect);
            _currentRecordEffect = null;
        }

        _showGray = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartRoom");
    }

    void CollectAndDestroyAllRunes()
    {
        activeRunes.RemoveAll(r => r.runeObj == null);
        if (activeRunes.Count == 0) return;

        bool hasFehu = false, hasUruz = false, hasSowilo = false, hasMannaz = false, hasAlgiz = false;
        bool hasJera = false, hasAnsuz = false, hasKenaz = false, hasThurisaz = false;
        bool hasLaguz = false, hasEihwaz = false;

        foreach (var rune in activeRunes)
        {
            if (rune.runeName == "Fehu") hasFehu = true;
            if (rune.runeName == "Uruz") hasUruz = true;
            if (rune.runeName == "Sowilo") hasSowilo = true;
            if (rune.runeName == "Mannaz") hasMannaz = true;
            if (rune.runeName == "Algiz") hasAlgiz = true;

            if (rune.runeName == "Jera") hasJera = true;
            if (rune.runeName == "Ansuz") hasAnsuz = true;
            if (rune.runeName == "Kenaz") hasKenaz = true;
            if (rune.runeName == "Laguz") hasLaguz = true;
            if (rune.runeName == "Eihwaz") hasEihwaz = true;
            if (rune.runeName == "Thurisaz") hasThurisaz = true;
        }

        bool comboTriggered = false;
        if (hasKenaz && hasSowilo && hasThurisaz)
        {
            if (_curFireEffect != null)
            {
                Destroy(_curFireEffect);
            }

            if (haoLongFireEffect != null)
            {
                _curFireEffect = Instantiate(haoLongFireEffect, cameraRig.position, vrHead.rotation);
                _curFireEffect.transform.SetParent(cameraRig);
                Destroy(_curFireEffect, haoLongFireTime);
            }
            comboTriggered = true;
        }

        if (hasLaguz && hasAnsuz && hasEihwaz)
        {
            if (waterDragonBulletPrefab != null)
            {
                GameObject waterDragon = Instantiate(waterDragonBulletPrefab,
                    runeSpawnPoint.position, vrHead.rotation);

                Rigidbody rb = waterDragon.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.velocity = vrHead.forward * 20f;
                }
                Destroy(waterDragon, 5f);
            }
            comboTriggered = true;
        }

        if (!lightSaberUnlocked && hasFehu && hasUruz)
        {
            UnlockLightSaberMode();
            comboTriggered = true;
        }
        else if (lightSaberUnlocked && hasFehu && hasUruz)
        {
            lightSaberUnlocked = false;
            if (lightSaberL != null) lightSaberL.SetActive(false);
            if (lightSaberR != null) lightSaberR.SetActive(false);
            if (zhang != null) zhang.SetActive(true);
            comboTriggered = true;
        }

        if (!_permanentDoubleDamage && hasFehu && hasSowilo)
        {
            _permanentDoubleDamage = true;
            PlayerStatsManager.Instance.ApplyPermanentDoubleDamage();
            if (doubleDamageEffect != null)
            {
                GameObject eff = Instantiate(doubleDamageEffect, cameraRig.position, Quaternion.identity);
                Destroy(eff, 4f);
            }
            comboTriggered = true;
        }

        if (hasMannaz && hasAlgiz)
        {
            PlayerStatsManager.Instance.playerStats.AddExp(100);
            if (expGainEffect != null)
            {
                GameObject eff = Instantiate(expGainEffect, cameraRig.position, Quaternion.identity);
                Destroy(eff, 3f);
            }
            comboTriggered = true;
        }

        if (hasJera && hasMannaz)
        {
            var stats = PlayerStatsManager.Instance.playerStats;
            int healAmount = Mathf.RoundToInt(stats.Hp[0] * 0.2f);
            stats.Hp[1] = Mathf.Min(stats.Hp[1] + healAmount, stats.Hp[0]);

            if (healEffectPrefab != null)
            {
                GameObject healEff = Instantiate(healEffectPrefab, cameraRig.position, Quaternion.identity);
                Destroy(healEff, 2f);
            }

            comboTriggered = true;
        }

        if (hasAnsuz && hasKenaz)
        {
            var stats = PlayerStatsManager.Instance.playerStats;
            int manaAmount = Mathf.RoundToInt(stats.Mp[0] * 0.2f);
            stats.Mp[1] = Mathf.Min(stats.Mp[1] + manaAmount, stats.Mp[0]);

            if (manaEffectPrefab != null)
            {
                GameObject manaEff = Instantiate(manaEffectPrefab, cameraRig.position, Quaternion.identity);
                Destroy(manaEff, 2f);
            }

            comboTriggered = true;
        }

        if (!comboTriggered)
        {
            if (hasFehu && activeRunes.Count == 1)
            {
                if (fireBallPrefab != null)
                {
                    GameObject fire = Instantiate(fireBallPrefab, runeSpawnPoint.position, vrHead.rotation);
                    fire.tag = "magic";
                    Rigidbody rb = fire.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.useGravity = false;
                        rb.isKinematic = false;
                        rb.velocity = vrHead.forward * 15f;
                    }
                    Destroy(fire, 3f);
                }
            }

            if (hasSowilo && activeRunes.Count == 1)
            {
                if (lightningPrefab != null)
                {
                    Vector3 spawnPos = runeSpawnPoint.position + vrHead.forward * 1.5f;
                    GameObject lightning = Instantiate(lightningPrefab, spawnPos, Quaternion.identity);
                    lightning.tag = "magic";
                    Destroy(lightning, 1f);
                }
            }

            if (hasAlgiz && activeRunes.Count == 1)
            {
                if (spikePrefab != null)
                {
                    Vector3 spawnPos = runeSpawnPoint.position + vrHead.forward * 1.5f;
                    spawnPos.y = 0;
                    GameObject spike = Instantiate(spikePrefab, spawnPos, Quaternion.identity);
                    spike.tag = "magic";
                    Destroy(spike, 2f);
                }
            }
        }

        foreach (var rune in activeRunes)
            if (rune.runeObj != null) Destroy(rune.runeObj);
        activeRunes.Clear();

        RefreshAllBarsScale();
    }

    void UnlockLightSaberMode()
    {
        lightSaberUnlocked = true;
        if (lightSaberL != null) lightSaberL.SetActive(true);
        if (lightSaberR != null) lightSaberR.SetActive(true);
        if (zhang != null) zhang.SetActive(false);
    }

    public void RefreshAllBarsScale()
    {
        if (PlayerStatsManager.Instance == null) return;
        var stats = PlayerStatsManager.Instance.playerStats;

        if (hpBarObj != null)
        {
            float ratio = Mathf.Clamp01((float)stats.Hp[1] / stats.Hp[0]);
            hpBarObj.transform.localScale = new Vector3(ratio, 1, 1);
        }

        if (mpBarObj != null)
        {
            float ratio = Mathf.Clamp01((float)stats.Mp[1] / stats.Mp[0]);
            mpBarObj.transform.localScale = new Vector3(ratio, 1, 1);
        }

        if (expBarObj != null)
        {
            float ratio = Mathf.Clamp01((float)stats.Exp / stats.ExpToNextLevel);
            expBarObj.transform.localScale = new Vector3(ratio, 1, 1);
        }
    }

    void InitRuneDictionary()
    {
        runeDict.Clear();
        foreach (var prefab in runePrefabs)
            if (prefab != null && !runeDict.ContainsKey(prefab.name))
                runeDict.Add(prefab.name, prefab);
    }

    void DoVRVoiceRecognition()
    {
        if (rightHand == null || runeRecognizer == null) return;

        if (gripBtn.GetState(rightHand.inputSource))
        {
            if (!isRecording)
            {
                runeRecognizer.StartRecording();
                isRecording = true;

                if (recordEffectPrefab != null && _currentRecordEffect == null)
                {
                    _currentRecordEffect = Instantiate(recordEffectPrefab, runeSpawnPoint.position, runeSpawnPoint.rotation);
                    _currentRecordEffect.transform.SetParent(runeSpawnPoint);
                }
            }
        }
        else
        {
            if (isRecording)
            {
                runeRecognizer.StopRecordingAndPredict();
                isRecording = false;

                if (_currentRecordEffect != null)
                {
                    Destroy(_currentRecordEffect);
                    _currentRecordEffect = null;
                }
            }
        }
    }

    public void OnRuneRecognitionComplete(string runeName, float confidence)
    {
        if (confidence < 0.1f) return;

        if (PlayerStatsManager.Instance.playerStats.Mp[1] < manaCostPerRune)
        {
            PlayNoManaSound();
            return;
        }

        if (runeDict.TryGetValue(runeName, out GameObject prefab))
        {
            PlayerStatsManager.Instance.SpendMana(manaCostPerRune);
            Quaternion finalRot = prefab.transform.rotation * vrHead.rotation;
            GameObject runeObj = Instantiate(prefab, runeSpawnPoint.position, finalRot);
            activeRunes.Add(new ActiveRune(runeObj, prefab.name));
            StartCoroutine(DestroyRuneWithBurnEffect(runeObj));
        }
    }

    void PlayNoManaSound()
    {
        if (noManaSound == null) return;
        _audioSource.PlayOneShot(noManaSound);
    }

    private IEnumerator DestroyRuneWithBurnEffect(GameObject runeObj)
    {
        yield return new WaitForSeconds(runeLifeTime);
        if (runeObj == null) yield break;

        activeRunes.RemoveAll(r => r.runeObj == runeObj);

        if (burnEffectPrefab != null)
        {
            Vector3 effectPos = runeObj.transform.position + new Vector3(0.2f, -0.05f, 0);
            GameObject effect = Instantiate(burnEffectPrefab, effectPos, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 0.25f;
            Destroy(effect, 2f);
        }
        Destroy(runeObj, 0.1f);
    }

    void OnEnable()
    {
        if (runeRecognizer != null)
            runeRecognizer.OnRuneRecognitionComplete += OnRuneRecognitionComplete;
    }

    void OnDisable()
    {
        if (runeRecognizer != null)
            runeRecognizer.OnRuneRecognitionComplete -= OnRuneRecognitionComplete;
    }

    void DoLeftMove()
    {
        if (leftHand == null) return;
        Vector2 stick = leftJoystick.GetAxis(leftHand.inputSource);
        if (stick.magnitude > deadzone)
        {
            stick.Normalize();
            Vector3 fwd = vrHead.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = vrHead.right; right.y = 0; right.Normalize();
            cameraRig.Translate((fwd * stick.y + right * stick.x) * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    void DoRightTurn()
    {
        if (rightHand == null) return;
        Vector2 stick = rightJoystick.GetAxis(rightHand.inputSource);
        if (Mathf.Abs(stick.x) > deadzone)
            cameraRig.Rotate(0, stick.x * turnSpeed * Time.deltaTime, 0);
    }

    void DoMenuToggle()
    {
        if (leftHand == null) return;
        if (triggerBtn.GetStateDown(leftHand.inputSource))
            if (operationMenu != null)
                operationMenu.SetActive(!operationMenu.activeSelf);
    }

    void DoWheelToggle()
    {
        if (leftHand == null) return;
        if (gripBtn.GetStateDown(leftHand.inputSource))
            if (itemWheel != null)
                itemWheel.SetActive(!itemWheel.activeSelf);
    }

    void OnGUI()
    {
        if (_showGray && _grayTexture != null)
        {
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _grayTexture);
        }
    }
}

public class ActiveRune
{
    public GameObject runeObj;
    public string runeName;

    public ActiveRune(GameObject obj, string name)
    {
        runeObj = obj;
        runeName = name;
    }
}