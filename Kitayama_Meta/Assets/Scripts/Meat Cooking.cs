using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kitayama.Cooking
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MeatCooking : MonoBehaviour
    {
        [Header("Scene references (set by CookingSetup)")]
        public BoxCollider panBase;
        public Transform heatPoint;
        public XRGrabInteractable meatGrab;
        public Renderer[] meatRenderers;
        public Image[] meterSegments;
        public Text meterLabel;
        public Text statusLabel;

        [Header("Heating")]
        [Min(1f)] public float fullCookingSeconds = 20f;
        [Range(1, 9)] public int cookedAtSegment = 4;
        [Range(2, 10)] public int burntAtSegment = 8;
        [Min(0.01f)] public float heatingRadius = 0.15f;
        [Min(0.01f)] public float heatingHeight = 0.08f;

        [Header("Throw and catch")]
        [Min(0.01f)] public float minimumAirSeconds = 0.08f;
        [Min(0.01f)] public float minimumRise = 0.05f;
        [Min(0.01f)] public float stableCatchSeconds = 0.12f;

        [Header("Meat colors")]
        public Color rawColor = new Color(0.75f, 0.15f, 0.20f);
        public Color cookedColor = new Color(0.40f, 0.19f, 0.07f);
        public Color burntColor = new Color(0.035f, 0.025f, 0.02f);

        // 後でスコア・リザルト処理から購読できる終了イベント。
        public event Action<CookingSession.Doneness, int> CookingCompleted;
        public CookingSession Session { get; private set; }
        Rigidbody body;
        MaterialPropertyBlock properties;
        bool touchingBase;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            properties = new MaterialPropertyBlock();
            if (panBase == null || heatPoint == null || meatGrab == null)
            {
                Debug.LogError("Cooking references are missing. Run Kitayama > Setup Cooking.", this);
                enabled = false;
                return;
            }
            ResetCooking();
        }

        [ContextMenu("Reset Cooking (Play Mode)")]
        public void ResetCooking()
        {
            if (!Application.isPlaying) return;
            Session = new CookingSession(fullCookingSeconds, cookedAtSegment, burntAtSegment,
                minimumAirSeconds, minimumRise, stableCatchSeconds);
            RefreshAppearance();
        }

        void Update()
        {
            if (Session == null || Session.Finished) return;
            Session.Step(Time.deltaTime, IsSupportedByPan(), IsOverHeat(),
                meatGrab.isSelected, body.worldCenterOfMass.y);
            RefreshAppearance();
            if (Session.Finished)
            {
                Debug.Log($"COOKING_FINISHED: {Session.Grade}, {Session.FilledSegments}/10", this);
                CookingCompleted?.Invoke(Session.Grade, Session.FilledSegments);
            }
        }

        bool IsSupportedByPan()
        {
            if (!touchingBase || !panBase.enabled || !panBase.gameObject.activeInHierarchy) return false;
            if (Vector3.Dot(panBase.transform.up, Vector3.up) < 0.35f) return false;
            Vector3 p = panBase.transform.InverseTransformPoint(body.worldCenterOfMass) - panBase.center;
            Vector3 half = panBase.size * 0.5f;
            if (Mathf.Abs(p.x) > half.x || Mathf.Abs(p.z) > half.z) return false;
            Vector3 top = panBase.transform.TransformPoint(panBase.center + Vector3.up * half.y);
            float gap = Vector3.Dot(body.worldCenterOfMass - top, panBase.transform.up);
            return gap >= -0.01f && gap <= 0.10f;
        }

        bool IsOverHeat()
        {
            if (!heatPoint.gameObject.activeInHierarchy) return false;
            if (Vector3.Dot(panBase.transform.up, heatPoint.up) < 0.7f) return false;
            Vector3 bottom = panBase.transform.TransformPoint(
                panBase.center - Vector3.up * panBase.size.y * 0.5f);
            Vector3 offset = bottom - heatPoint.position;
            float height = Vector3.Dot(offset, heatPoint.up);
            Vector3 horizontal = Vector3.ProjectOnPlane(offset, heatPoint.up);
            return height >= -0.02f && height <= heatingHeight
                && horizontal.sqrMagnitude <= heatingRadius * heatingRadius;
        }

        void OnCollisionEnter(Collision collision) { TrackContact(collision); }
        void OnCollisionStay(Collision collision) { TrackContact(collision); }
        void TrackContact(Collision collision)
        {
            if (collision.collider == panBase) touchingBase = true;
        }
        void OnCollisionExit(Collision collision)
        {
            if (collision.collider == panBase) touchingBase = false;
        }
        void OnDisable() { touchingBase = false; }

        void RefreshAppearance()
        {
            Color meatColor = Session.Grade == CookingSession.Doneness.Raw ? rawColor
                : Session.Grade == CookingSession.Doneness.Cooked ? cookedColor : burntColor;
            if (meatRenderers != null)
                foreach (Renderer renderer in meatRenderers)
                {
                    if (renderer == null) continue;
                    renderer.GetPropertyBlock(properties);
                    properties.SetColor("_BaseColor", meatColor);
                    properties.SetColor("_Color", meatColor);
                    renderer.SetPropertyBlock(properties);
                }

            Color barColor = Session.Grade == CookingSession.Doneness.Raw ? new Color(0.95f, 0.35f, 0.35f)
                : Session.Grade == CookingSession.Doneness.Cooked ? new Color(0.25f, 0.85f, 0.35f)
                : new Color(1f, 0.5f, 0.1f);
            if (meterSegments != null)
                for (int i = 0; i < meterSegments.Length; i++)
                    if (meterSegments[i] != null)
                        meterSegments[i].color = i < Session.FilledSegments ? barColor : new Color(0.20f, 0.22f, 0.25f);

            string grade = Session.Grade.ToString().ToUpperInvariant();
            if (meterLabel != null) meterLabel.text = $"{grade}   {Session.FilledSegments} / 10";
            if (statusLabel != null)
            {
                switch (Session.CurrentPhase)
                {
                    case CookingSession.Phase.Waiting: statusLabel.text = "Put meat in the pan on the stove"; break;
                    case CookingSession.Phase.Heating: statusLabel.text = "Heating - toss and catch to finish"; break;
                    case CookingSession.Phase.Paused: statusLabel.text = "Heating paused"; break;
                    case CookingSession.Phase.Airborne: statusLabel.text = "Catch the meat with the pan!"; break;
                    case CookingSession.Phase.Finished: statusLabel.text = "FINISHED - " + grade; break;
                }
            }
        }

        void OnValidate()
        {
            fullCookingSeconds = Mathf.Max(1f, fullCookingSeconds);
            cookedAtSegment = Mathf.Clamp(cookedAtSegment, 1, 9);
            burntAtSegment = Mathf.Clamp(burntAtSegment, cookedAtSegment + 1, 10);
            minimumAirSeconds = Mathf.Max(0.01f, minimumAirSeconds);
            minimumRise = Mathf.Max(0.01f, minimumRise);
            stableCatchSeconds = Mathf.Max(0.01f, stableCatchSeconds);
        }
    }
}

