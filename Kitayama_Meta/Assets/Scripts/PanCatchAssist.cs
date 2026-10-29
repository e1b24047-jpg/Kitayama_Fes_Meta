using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kitayama.Cooking
{
    // 加熱・投げ判定の更新後に補助する。
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class PanCatchAssist : MonoBehaviour
    {
        public MeatCooking cooking;

        public bool assistEnabled = true;

        [Header("Throw stabilization")]
        [Range(0f, 1f)]
        public float horizontalRetention = 0.2f;

        [Header("Catch assistance")]
        [Min(0.01f)]
        public float assistRadius = 0.45f;

        [Min(0.01f)]
        public float assistHeight = 0.65f;

        [Min(0.1f)]
        public float centeringSpeed = 10f;

        [Min(0.1f)]
        public float maxHorizontalSpeed = 2f;

        [Min(0.1f)]
        public float maxAcceleration = 10f;

        XRGrabInteractable panGrab;

        bool previouslySupported;
        bool launchCorrected;

        CookingSession previousSession;

        void Awake()
        {
            panGrab = GetComponent<XRGrabInteractable>();

            // 前回の投げ補助と同じ肉を対象にする。
            if (cooking == null)
            {
                PanThrowAssist throwAssist =
                    GetComponent<PanThrowAssist>();

                if (throwAssist != null)
                {
                    cooking = throwAssist.cooking;
                }
            }

            if (cooking == null)
            {
                Debug.LogError(
                    "Assign Meat to the Cooking field " +
                    "on PanCatchAssist.",
                    this
                );
            }
        }

        void OnEnable()
        {
            previouslySupported = false;
            launchCorrected = false;
            previousSession = null;
        }

        void FixedUpdate()
        {
            if (cooking == null ||
                cooking.Session == null ||
                cooking.panBase == null ||
                cooking.meatGrab == null ||
                cooking.MeatBody == null)
            {
                return;
            }

            CookingSession session = cooking.Session;

            // 新しいプレイでは補助の状態もリセットする。
            if (previousSession != session)
            {
                previousSession = session;
                previouslySupported = false;
                launchCorrected = false;
            }

            bool supported = cooking.SupportedByPan;

            bool justLeftPan =
                previouslySupported && !supported;

            previouslySupported = supported;

            bool airborne =
                session.CurrentPhase ==
                CookingSession.Phase.Airborne;

            if (supported && !airborne)
            {
                launchCorrected = false;
            }

            Rigidbody meat = cooking.MeatBody;

            // フライパンを持っている間だけ補助する。
            // 肉を直接掴んでいる場合や、失敗・終了後は補助しない。
            if (!assistEnabled ||
                !cooking.enabled ||
                !session.HasHeated ||
                session.Finished ||
                session.CurrentPhase ==
                    CookingSession.Phase.Failed ||
                !panGrab.isSelected ||
                cooking.meatGrab.isSelected ||
                meat.isKinematic ||
                supported)
            {
                return;
            }

            Vector3 velocity = meat.linearVelocity;

            // 離陸直後の横方向の勢いを、一度だけ減らす。
            // 離陸を取り逃した場合は、
            // 有効な投げと判定された時点で補正する。
            if (!launchCorrected &&
                ((justLeftPan && velocity.y > 0.1f) || airborne))
            {
                velocity.x *= horizontalRetention;
                velocity.z *= horizontalRetention;

                meat.linearVelocity = velocity;

                launchCorrected = true;
            }

            // 引き寄せる処理は、有効な投げの落下中だけ行う。
            if (!airborne || velocity.y > 0f)
            {
                return;
            }

            BoxCollider panBase = cooking.panBase;

            // フライパンが大きく傾いている場合は補助しない。
            if (!panBase.enabled ||
                !panBase.gameObject.activeInHierarchy ||
                Vector3.Dot(
                    panBase.transform.up,
                    Vector3.up) < 0.7f)
            {
                return;
            }

            // フライパンの底の上面、その中心を目標にする。
            Vector3 center =
                panBase.transform.TransformPoint(
                    panBase.center +
                    Vector3.up * panBase.size.y * 0.5f
                );

            Vector3 offset =
                center - meat.worldCenterOfMass;

            float height = -offset.y;

            Vector3 horizontalOffset =
                new Vector3(offset.x, 0f, offset.z);

            // 底より下へ落ちた肉や、
            // 補助範囲から大きく外れた肉は引き戻さない。
            if (height < 0f ||
                height > assistHeight ||
                horizontalOffset.sqrMagnitude >
                    assistRadius * assistRadius)
            {
                return;
            }

            Vector3 horizontalVelocity =
                new Vector3(velocity.x, 0f, velocity.z);

            // 中心へ近づくほど、移動速度を小さくする。
            Vector3 desiredVelocity =
                Vector3.ClampMagnitude(
                    horizontalOffset * centeringSpeed,
                    maxHorizontalSpeed
                );

            // 急にワープさせず、横方向の速度を徐々に補正する。
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                desiredVelocity,
                maxAcceleration * Time.fixedDeltaTime
            );

            // 上下の速度は変更しない。
            meat.linearVelocity = new Vector3(
                horizontalVelocity.x,
                velocity.y,
                horizontalVelocity.z
            );
        }

        void OnValidate()
        {
            horizontalRetention =
                Mathf.Clamp01(horizontalRetention);

            assistRadius =
                Mathf.Max(0.01f, assistRadius);

            assistHeight =
                Mathf.Max(0.01f, assistHeight);

            centeringSpeed =
                Mathf.Max(0.1f, centeringSpeed);

            maxHorizontalSpeed =
                Mathf.Max(0.1f, maxHorizontalSpeed);

            maxAcceleration =
                Mathf.Max(0.1f, maxAcceleration);
        }
    }
}