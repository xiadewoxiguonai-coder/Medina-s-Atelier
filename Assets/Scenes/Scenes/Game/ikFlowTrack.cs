using UnityEngine;
using Valve.VR;

public class PicoTrackerReader : MonoBehaviour
{
    public int deviceIndex;

    private CVRSystem vrSystem;
    private TrackedDevicePose_t[] devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    void Start()
    {
        vrSystem = OpenVR.System;
    }

    void Update()
    {
        if (vrSystem == null) return;

     
        vrSystem.GetDeviceToAbsoluteTrackingPose(
            ETrackingUniverseOrigin.TrackingUniverseStanding,
            0f,
            devicePoses
        );

        if (deviceIndex < 0 || deviceIndex >= devicePoses.Length) return;

        TrackedDevicePose_t pose = devicePoses[deviceIndex];

       
    }
}