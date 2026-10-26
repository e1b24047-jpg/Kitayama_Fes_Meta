using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kitayama.Cooking
{
    [RequireComponent(
        typeof(Rigidbody),
        typeof(XRGrabInteractable))]
    public sealed class PanThrowAssist : MonoBehaviour
    {
        public MeatCooking cooking;

        public bool assistEnabled = true;

        [Min(0.1f)]
        public float gestureSpeed = 0.7f;

        [Min(0.05f)]
        public float targetHeight = 0.25f;

        [Min(0.1f)]
        public float cooldown = 0.7f;

        XRGrabInteractable grab;

        Vector3 previousPosition;

        bool wasHeld;
        bool armed;

        float nextLaunchTime;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            previousPosition = transform.position;

            wasHeld = false;
            armed = false;
        }

        void FixedUpdate()
        {
            Vector3 position = transform.position;

            float speed =
                (position.y - previousPosition.y) /
                Time.fixedDeltaTime;

            previousPosition = position;

            bool held = grab != null && grab.isSelected;

            if (!held || !wasHeld)
            {
                wasHeld = held;
                armed = false;

                // 掴んだ瞬間の位置移動では発動させない。
                return;
            }

            // 一度静止、またはゆっくり動かすと次の上振りを受け付ける。
            if (Mathf.Abs(speed) < gestureSpeed * 0.35f)
            {
                armed = true;
            }

            if (!armed || speed < gestureSpeed)
            {
                return;
            }

            armed = false;

            if (!assistEnabled ||
                Time.time < nextLaunchTime ||
                cooking == null ||
                !cooking.enabled ||
                cooking.Session == null ||
                !cooking.Session.HasHeated ||
                cooking.Session.Finished ||
                cooking.Session.CurrentPhase ==
                    CookingSession.Phase.Airborne ||
                cooking.Session.CurrentPhase ==
                    CookingSession.Phase.Failed ||
                cooking.meatGrab == null ||
                cooking.meatGrab.isSelected ||
                !cooking.SupportedByPan ||
                Vector3.Dot(transform.up, Vector3.up) < 0.7f)
            {
                return;
            }

            Rigidbody meat = cooking.MeatBody;

            if (meat == null ||
                meat.isKinematic ||
                !meat.useGravity ||
                Physics.gravity.y >= -0.01f)
            {
                return;
            }

            Vector3 velocity = meat.linearVelocity;

            // 静止位置から約25cm上がる速度を目安にする。
            // すでに速く上昇している場合は減速させない。
            velocity.y = Mathf.Max(
                velocity.y,
                Mathf.Sqrt(
                    2f * -Physics.gravity.y * targetHeight
                )
            );

            meat.linearVelocity = velocity;
            meat.WakeUp();

            nextLaunchTime = Time.time + cooldown;
        }

        void OnValidate()
        {
            gestureSpeed = Mathf.Max(0.1f, gestureSpeed);
            targetHeight = Mathf.Max(0.05f, targetHeight);
            cooldown = Mathf.Max(0.1f, cooldown);
        }
    }
}