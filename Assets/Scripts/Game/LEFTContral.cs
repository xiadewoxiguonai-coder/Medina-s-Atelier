using UnityEngine;
using Valve.VR;
using System.Collections.Generic;

public class LEFTContral : MonoBehaviour
{
    [Header("left Pose")]
    public SteamVR_Behaviour_Pose leftHand;
    [Header("right Pose")]
    public SteamVR_Behaviour_Pose rightHand;

    private SteamVR_Action_Vector2 leftJoystick;
    private SteamVR_Action_Vector2 rightJoystick;
    private SteamVR_Action_Boolean triggerBtn;
    private SteamVR_Action_Boolean gripBtn;

    [Header("playarea (CameraRig)")]
    public Transform cameraRig;

    [Header("head set")]
    public Transform vrHead;

    [Header("move setting")]
    public float moveSpeed = 3.5f;
    public float deadzone = 0.15f;

    [Header("rotation")]
    public float turnSpeed = 80f;
    public bool smoothTurn = true;

    [Header("UI setting")]
    public GameObject operationMenu;
    public GameObject itemWheel;

    [Header("🎤 语音识别脚本")]
    public RuneThreeModelRecognize_VROnly runeRecognizer;

    [Header("✨ 固定生成点")]
    public Transform runeSpawnPoint;

    [Header("⏱ 符文存活时间(秒)")]
    public float runeLifeTime = 10f;

    public List<GameObject> runePrefabs;

    private Dictionary<string, GameObject> runeDict = new Dictionary<string, GameObject>();
    private bool isRecording = false;

    void Awake()
    {
        leftJoystick = SteamVR_Input.GetVector2Action("default", "Thumbstick");
        rightJoystick = SteamVR_Input.GetVector2Action("default", "Thumbstick");
        triggerBtn = SteamVR_Input.GetBooleanAction("default", "GrabPinch");
        gripBtn = SteamVR_Input.GetBooleanAction("default", "GrabGrip");

        InitRuneDictionary();
    }

    void Update()
    {
        if (cameraRig == null || vrHead == null) return;

        DoLeftMove();
        DoRightTurn();
        DoMenuToggle();
        DoWheelToggle();
        DoVRVoiceRecognition();
    }

    void InitRuneDictionary()
    {
        runeDict.Clear();
        foreach (var prefab in runePrefabs)
        {
            if (prefab != null && !runeDict.ContainsKey(prefab.name))
                runeDict.Add(prefab.name, prefab);
        }
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
            }
        }
        else
        {
            if (isRecording)
            {
                runeRecognizer.StopRecordingAndPredict();
                isRecording = false;
            }
        }
    }

    public void OnRuneRecognitionComplete(string runeName, float confidence)
    {
        if (confidence < 0.1f) return;

        if (runeDict.TryGetValue(runeName, out GameObject prefab))
        {
            Quaternion finalRot = prefab.transform.rotation * vrHead.rotation;
            GameObject runeObj = Instantiate(prefab, runeSpawnPoint.position, finalRot);

            
            Destroy(runeObj, runeLifeTime);
        }
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
            Vector3 fwd = vrHead.forward;
            Vector3 right = vrHead.right;
            fwd.y = 0; right.y = 0;
            fwd.Normalize(); right.Normalize();

            Vector3 dir = fwd * stick.y + right * stick.x;
            cameraRig.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    void DoRightTurn()
    {
        if (rightHand == null) return;

        Vector2 stick = rightJoystick.GetAxis(rightHand.inputSource);

        if (Mathf.Abs(stick.x) > deadzone)
        {
            float turnAmount = stick.x * turnSpeed * Time.deltaTime;
            cameraRig.Rotate(0, turnAmount, 0);
        }
    }

    void DoMenuToggle()
    {
        if (leftHand == null) return;

        if (triggerBtn.GetStateDown(leftHand.inputSource))
        {
            if (operationMenu != null)
                operationMenu.SetActive(!operationMenu.activeSelf);
        }
    }

    void DoWheelToggle()
    {
        if (leftHand == null) return;

        if (gripBtn.GetStateDown(leftHand.inputSource))
        {
            if (itemWheel != null)
                itemWheel.SetActive(!itemWheel.activeSelf);
        }
    }
}