using UnityEngine;

public class FullBodyTracking_8Trackers : MonoBehaviour
{
    
    public Transform vrHead;
    public Transform vrLeftHand;
    public Transform vrRightHand;

    [Header("8track")]
    public Transform vrChest;
    public Transform vrLeftElbow;
    public Transform vrLeftFoot;
    public Transform vrLeftKnee;
    public Transform vrRightElbow;
    public Transform vrRightFoot;
    public Transform vrRightKnee;
    public Transform vrWaist;

    
    public float bodyHeightOffset = 0.7f;
    public float faceBackOffset = 0.25f;
    [Range(0, 1)] public float ikWeight = 0.8f;
    public float legScale = 1.2f;

    private Animator _ani;
    private Vector3 _rootPos;

    void Start()
    {
        _ani = GetComponent<Animator>();
        _rootPos = transform.position;
    }

    void Update()
    {
        if (vrHead == null) return;

        Vector3 bodyPos = vrHead.position;
        bodyPos.y -= bodyHeightOffset;

        float yaw = vrHead.rotation.eulerAngles.y;
        Vector3 forwardDir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        bodyPos -= forwardDir * faceBackOffset;

        transform.position = bodyPos;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (_ani == null) return;

        // ik for hand
        SetIK(AvatarIKGoal.RightHand, vrRightHand, ikWeight);
        SetIK(AvatarIKGoal.LeftHand, vrLeftHand, ikWeight);

        // ik for foot
        SetIK(AvatarIKGoal.RightFoot, vrRightFoot, ikWeight, legScale);
        SetIK(AvatarIKGoal.LeftFoot, vrLeftFoot, ikWeight, legScale);

        // head look
        if (vrHead != null)
        {
            _ani.SetLookAtPosition(vrHead.position + vrHead.forward * 100f);
            _ani.SetLookAtWeight(0.8f);
        }
    }

    void SetIK(AvatarIKGoal goal, Transform target, float weight, float scale = 1f)
    {
        if (target == null) return;

        //according leg to change
        Vector3 offsetFromRoot = target.position - transform.position;
        Vector3 scaledPos = transform.position + offsetFromRoot * scale;

        _ani.SetIKPosition(goal, scaledPos);
        _ani.SetIKRotation(goal, target.rotation);
        _ani.SetIKPositionWeight(goal, weight);
        _ani.SetIKRotationWeight(goal, weight);
    }
}