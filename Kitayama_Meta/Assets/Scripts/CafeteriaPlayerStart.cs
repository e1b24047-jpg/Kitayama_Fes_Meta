using UnityEngine;
using Unity.XR.CoreUtils;

public sealed class CafeteriaPlayerStart : MonoBehaviour
{
    public XROrigin xrOrigin;

    public Vector3 localStandingPosition =
        new Vector3(0f, 0f, -0.85f);

    void OnEnable()
    {
        if (xrOrigin == null || xrOrigin.Camera == null)
            return;

        // 頭の高さはそのまま維持する。
        float height = Vector3.Dot(
            xrOrigin.Camera.transform.position
                - xrOrigin.transform.position,
            Vector3.up
        );

        Vector3 forward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        );

        if (forward.sqrMagnitude < 0.001f)
            return;

        // カウンター正面を向く。
        xrOrigin.MatchOriginUpCameraForward(
            Vector3.up,
            forward.normalized
        );

        // カウンター中央の床位置に合わせる。
        Vector3 floor =
            transform.TransformPoint(localStandingPosition);

        xrOrigin.MoveCameraToWorldLocation(
            floor + Vector3.up * height
        );
    }
}