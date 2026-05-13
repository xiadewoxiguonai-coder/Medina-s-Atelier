using UnityEngine;

/// <summary>
/// VR Player Ground Alignment System
/// Automatically adjust player height to fit stairs, slopes and uneven ground
/// </summary>
public class VRGroundAlign : MonoBehaviour
{
    [Header("Raycast Spacing (Left/Right Width)")]
    public float checkWidth = 0.4f;

    [Header("Ray Start Height / Downward Detection Range")]
    public float rayHeight = 0.6f;
    public float rayDistance = 1.5f;

    [Header("Player Height Offset From Ground")]
    public float floorOffset = 0.25f;

    [Header("Smooth Follow Speed")]
    public float alignSmooth = 12f;

    [Header("Ground Layer Mask")]
    public LayerMask groundLayer;

    private float targetY;

    void Update()
    {
        CheckGroundThreePoint();
        AlignToGround();
    }

    /// <summary>
    /// Three-point ground detection: Left, Center, Right
    /// Supports stairs, slopes and height differences
    /// </summary>
    void CheckGroundThreePoint()
    {
        Vector3 center = transform.position;
        Vector3 leftPos = center + Vector3.left * checkWidth;
        Vector3 midPos = center;
        Vector3 rightPos = center + Vector3.right * checkWidth;

        float highestGround = -999f;

        CheckSinglePoint(leftPos, ref highestGround);
        CheckSinglePoint(midPos, ref highestGround);
        CheckSinglePoint(rightPos, ref highestGround);

        if (highestGround > -999f)
            targetY = highestGround + floorOffset;
        else
            targetY = transform.position.y;
    }

    /// <summary>
    /// Single raycast to detect ground height
    /// </summary>
    void CheckSinglePoint(Vector3 pos, ref float highestY)
    {
        Vector3 origin = pos + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
        {
            if (hit.point.y > highestY)
                highestY = hit.point.y;
        }
    }

    /// <summary>
    /// Smoothly move player to target height
    /// </summary>
    void AlignToGround()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, alignSmooth * Time.deltaTime);
        transform.position = pos;
    }

    /// <summary>
    /// Draw Gizmos for three raycasts in Editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;

        Vector3 leftPos = center + Vector3.left * checkWidth;
        Vector3 midPos = center;
        Vector3 rightPos = center + Vector3.right * checkWidth;

        DrawRayGizmo(leftPos);
        DrawRayGizmo(midPos);
        DrawRayGizmo(rightPos);
    }

    /// <summary>
    /// Draw single ray gizmo
    /// </summary>
    void DrawRayGizmo(Vector3 pos)
    {
        Vector3 origin = pos + Vector3.up * rayHeight;
        Gizmos.DrawLine(origin, origin + Vector3.down * rayDistance);
    }
}