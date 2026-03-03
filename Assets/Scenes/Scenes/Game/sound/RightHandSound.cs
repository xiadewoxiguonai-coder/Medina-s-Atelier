using UnityEngine;
using Valve.VR;

public class VRRightHand : MonoBehaviour
{
    public SteamVR_Behaviour_Pose pose;
    public SteamVR_Action_Boolean trigger = SteamVR_Input.GetBooleanAction("GrabPinch");

    private soundUIsystem uiSystem;

    void Start()
    {
        uiSystem = GameObject.Find("/Ω≈±æº”‘ÿ").GetComponent<soundUIsystem>();
    }

    void Update()
    {
        if (trigger.GetStateDown(pose.inputSource))
        {
            uiSystem.runFunction();
        }
    }
}