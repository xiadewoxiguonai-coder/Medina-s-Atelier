using UnityEngine;
using Valve.VR;
using UnityEngine.Events;


public class VRLeftHandRecorder : MonoBehaviour
{
    public SteamVR_Behaviour_Pose leftHandPose; //
    public SteamVR_Action_Boolean triggerAction = SteamVR_Input.GetBooleanAction("GrabPinch"); 

    public RuneVoiceGame_3Model runeVoiceGame; 

    private bool isRecording = false;

    void Update()
    {

        if (triggerAction.GetStateDown(leftHandPose.inputSource))
        {
            if (runeVoiceGame != null)
            {
                runeVoiceGame.OnRecordButtonClick(); 
                isRecording = !isRecording;

            }
        }
    }
}