using System.Collections;
using Kitayama.Cooking;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(
    typeof(Rigidbody),
    typeof(XRGrabInteractable))]
public sealed class GroundRespawn : MonoBehaviour
{
    public MeatCooking cooking;
    public CookingResult result;

    Rigidbody body;
    XRGrabInteractable grab;

    Transform homeParent;
    Vector3 homePosition;
    Quaternion homeRotation;

    bool homeKinematic;
    bool homeCollisions;

    bool busy;
    bool grabWasEnabled;
    bool throwWasEnabled;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // 再生開始時の位置・向きを記録する。
        homeParent = transform.parent;
        homePosition = transform.localPosition;
        homeRotation = transform.localRotation;

        homeKinematic = body.isKinematic;
        homeCollisions = body.detectCollisions;
    }

    void OnCollisionEnter(Collision hit)
    {
        CheckFloor(hit);
    }

    void OnCollisionStay(Collision hit)
    {
        CheckFloor(hit);
    }

    void CheckFloor(Collision hit)
    {
        if (!isActiveAndEnabled ||
            busy ||
            cooking == null ||
            cooking.Session == null ||
            cooking.Session.Finished ||
            (result != null && result.IsFinished))
        {
            return;
        }

        // RespawnFloor付きの床に触れた場合だけ戻す。
        if (hit.collider.GetComponent<RespawnFloor>() == null)
            return;

        StartCoroutine(ReturnHome());
    }

    IEnumerator ReturnHome()
    {
        busy = true;

        // 移動する前に今回の投げ判定を取り消す。
        cooking.NotifyGroundRespawn();

        grabWasEnabled = grab.enabled;
        throwWasEnabled = grab.throwOnDetach;

        // 強制解除時に投げる力が加わらないようにする。
        grab.throwOnDetach = false;

        if (grab.isSelected &&
            grab.interactionManager != null)
        {
            grab.interactionManager.CancelInteractableSelection(
                (IXRSelectInteractable)grab
            );
        }

        grab.enabled = false;

        StopMotion();

        body.isKinematic = true;
        body.detectCollisions = false;

        MoveHome();

        // 移動直後に再び掴まれないよう、短時間だけ待つ。
        yield return new WaitForSeconds(0.15f);

        MoveHome();

        body.isKinematic = homeKinematic;
        body.detectCollisions = homeCollisions;

        StopMotion();

        grab.enabled = grabWasEnabled;

        // XRIの遅延した手放し処理が終わるまで投擲を無効にする。
        yield return null;

        grab.throwOnDetach = throwWasEnabled;
        busy = false;
    }

    void MoveHome()
    {
        Vector3 position = homeParent != null
            ? homeParent.TransformPoint(homePosition)
            : homePosition;

        Quaternion rotation = homeParent != null
            ? homeParent.rotation * homeRotation
            : homeRotation;

        body.position = position;
        body.rotation = rotation;

        transform.SetPositionAndRotation(
            position,
            rotation
        );
    }

    void StopMotion()
    {
        if (body.isKinematic)
            return;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    void OnDisable()
    {
        StopAllCoroutines();

        // リスポーン処理中に時間切れなどで非表示になった場合。
        if (!busy || body == null || grab == null)
            return;

        body.isKinematic = homeKinematic;
        body.detectCollisions = homeCollisions;

        StopMotion();

        grab.enabled = grabWasEnabled;
        grab.throwOnDetach = throwWasEnabled;

        busy = false;
    }
}