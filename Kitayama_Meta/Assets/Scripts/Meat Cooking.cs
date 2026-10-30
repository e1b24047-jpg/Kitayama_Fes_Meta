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
        [Min(1f)]
        public float fullCookingSeconds = 20f;

        [Range(1, 9)]
        public int cookedAtSegment = 4;

        [Range(2, 10)]
        public int burntAtSegment = 8;

        [Min(0.01f)]
        public float heatingRadius = 0.15f;

        [Min(0.01f)]
        public float heatingHeight = 0.08f;

        [Header("Throw and catch")]
        [Min(0.01f)]
        public float minimumAirSeconds = 0.08f;

        [Min(0.01f)]
        public float minimumRise = 0.05f;

        [Min(0.01f)]
        public float stableCatchSeconds = 0.12f;

        [Min(0.5f)]
        public float throwTimeout = 5f;

        [Min(0.1f)]
        public float failureDropDistance = 0.6f;

        public AudioSource feedbackSource;
        public AudioClip catchClip;
        public AudioClip failureClip;

        [Header("Meat colors")]
        public Color rawColor =
            new Color(0.75f, 0.15f, 0.20f);

        public Color cookedColor =
            new Color(0.40f, 0.19f, 0.07f);

        public Color burntColor =
            new Color(0.035f, 0.025f, 0.02f);

        // 後からスコア・リザルト処理で利用できる終了イベント。
        public event Action<CookingSession.Doneness, int>
            CookingCompleted;

        public CookingSession Session { get; private set; }

        public bool SupportedByPan =>
            body != null &&
            panBase != null &&
            IsSupportedByPan();

        public Rigidbody MeatBody => body;

        Rigidbody body;
        MaterialPropertyBlock properties;

        bool touchingBase;
        bool failedContact;

        AudioClip generatedCatch;
        AudioClip generatedFailure;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            properties = new MaterialPropertyBlock();

            if (panBase == null ||
                heatPoint == null ||
                meatGrab == null)
            {
                Debug.LogError(
                    "Cooking references are missing. " +
                    "Run Kitayama > Setup Cooking.",
                    this
                );

                enabled = false;
                return;
            }

            ResetCooking();
        }

        [ContextMenu("Reset Cooking (Play Mode)")]
        public void ResetCooking()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Session = new CookingSession(
                fullCookingSeconds,
                cookedAtSegment,
                burntAtSegment,
                minimumAirSeconds,
                minimumRise,
                stableCatchSeconds,
                throwTimeout,
                failureDropDistance
            );

            failedContact = false;
            RefreshAppearance();
        }


        public void NotifyGroundRespawn()
        {
            if (Session == null || Session.Finished)
                return;

            Session.CancelThrowForRespawn();

            // 移動前の接触状態を残さない。
            touchingBase = false;
            failedContact = false;

            RefreshAppearance();
        }


        void FixedUpdate()
        {
            if (Session == null || Session.Finished)
            {
                return;
            }

            CookingSession.Phase previousPhase =
                Session.CurrentPhase;

            Session.Step(
                Time.fixedDeltaTime,
                IsSupportedByPan(),
                IsOverHeat(),
                meatGrab.isSelected,
                body.worldCenterOfMass.y,
                failedContact,
                body.linearVelocity.y
            );

            failedContact = false;

            if (Session.CurrentPhase ==
                    CookingSession.Phase.Failed &&
                previousPhase != Session.CurrentPhase)
            {
                PlayFeedback(false);
            }

            RefreshAppearance();

            if (Session.Finished)
            {
                PlayFeedback(true);

                Debug.Log(
                    $"COOKING_FINISHED: " +
                    $"{Session.Grade}, " +
                    $"{Session.FilledSegments}/10",
                    this
                );

                CookingCompleted?.Invoke(
                    Session.Grade,
                    Session.FilledSegments
                );
            }
        }

        bool IsSupportedByPan()
        {
            if (!touchingBase ||
                !panBase.enabled ||
                !panBase.gameObject.activeInHierarchy)
            {
                return false;
            }

            // 大きく裏返ったフライパンは底で支えている扱いにしない。
            if (Vector3.Dot(
                    panBase.transform.up,
                    Vector3.up) < 0.35f)
            {
                return false;
            }

            Vector3 localPosition =
                panBase.transform.InverseTransformPoint(
                    body.worldCenterOfMass
                ) - panBase.center;

            Vector3 halfSize = panBase.size * 0.5f;

            if (Mathf.Abs(localPosition.x) > halfSize.x ||
                Mathf.Abs(localPosition.z) > halfSize.z)
            {
                return false;
            }

            Vector3 top =
                panBase.transform.TransformPoint(
                    panBase.center +
                    Vector3.up * halfSize.y
                );

            float gap = Vector3.Dot(
                body.worldCenterOfMass - top,
                panBase.transform.up
            );

            return gap >= -0.01f && gap <= 0.10f;
        }

        bool IsOverHeat()
        {
            if (!heatPoint.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (Vector3.Dot(
                    panBase.transform.up,
                    heatPoint.up) < 0.7f)
            {
                return false;
            }

            Vector3 bottom =
                panBase.transform.TransformPoint(
                    panBase.center -
                    Vector3.up * panBase.size.y * 0.5f
                );

            Vector3 offset = bottom - heatPoint.position;

            float height = Vector3.Dot(
                offset,
                heatPoint.up
            );

            Vector3 horizontal = Vector3.ProjectOnPlane(
                offset,
                heatPoint.up
            );

            return height >= -0.02f &&
                   height <= heatingHeight &&
                   horizontal.sqrMagnitude <=
                       heatingRadius * heatingRadius;
        }

        void OnCollisionEnter(Collision collision)
        {
            TrackContact(collision);
        }

        void OnCollisionStay(Collision collision)
        {
            TrackContact(collision);
        }

        void TrackContact(Collision collision)
        {
            if (collision.collider == panBase)
            {
                touchingBase = true;
            }
            // フライパンの底・側壁・取っ手は同じRigidbody。
            // 机など、それ以外の物体への接触を失敗候補にする。
            else if (panBase != null &&
                     collision.rigidbody !=
                         panBase.attachedRigidbody)
            {
                failedContact = true;
            }
        }

        void OnCollisionExit(Collision collision)
        {
            if (collision.collider == panBase)
            {
                touchingBase = false;
            }
        }

        void OnDisable()
        {
            touchingBase = false;
        }

        void RefreshAppearance()
        {
            Color meatColor =
                Session.Grade == CookingSession.Doneness.Raw
                    ? rawColor
                    : Session.Grade ==
                        CookingSession.Doneness.Cooked
                        ? cookedColor
                        : burntColor;

            if (meatRenderers != null)
            {
                foreach (Renderer renderer in meatRenderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.GetPropertyBlock(properties);

                    properties.SetColor(
                        "_BaseColor",
                        meatColor
                    );

                    properties.SetColor(
                        "_Color",
                        meatColor
                    );

                    renderer.SetPropertyBlock(properties);
                }
            }

            Color barColor =
                Session.Grade == CookingSession.Doneness.Raw
                    ? new Color(0.95f, 0.35f, 0.35f)
                    : Session.Grade ==
                        CookingSession.Doneness.Cooked
                        ? new Color(0.25f, 0.85f, 0.35f)
                        : new Color(1f, 0.5f, 0.1f);

            if (meterSegments != null)
            {
                for (int i = 0; i < meterSegments.Length; i++)
                {
                    if (meterSegments[i] == null)
                    {
                        continue;
                    }

                    meterSegments[i].color =
                        i < Session.FilledSegments
                            ? barColor
                            : new Color(
                                0.20f,
                                0.22f,
                                0.25f
                            );
                }
            }

            string grade =
                Session.Grade.ToString().ToUpperInvariant();

            if (meterLabel != null)
            {
                meterLabel.text =
                    $"{grade}   {Session.FilledSegments} / 10";
            }

            if (statusLabel == null)
            {
                return;
            }

            switch (Session.CurrentPhase)
            {
                case CookingSession.Phase.Waiting:
                    statusLabel.text =
                        "Put meat in the pan on the stove";
                    break;

                case CookingSession.Phase.Heating:
                    statusLabel.text =
                        "Heating - toss and catch to finish";
                    break;

                case CookingSession.Phase.Paused:
                    statusLabel.text =
                        "Heating paused";
                    break;

                case CookingSession.Phase.Airborne:
                    statusLabel.text =
                        $"Catch!  Height: " +
                        $"{Session.PeakHeight * 100f:0} cm";
                    break;

                case CookingSession.Phase.Failed:
                    statusLabel.text =
                        "MISS - put meat back in the pan";
                    break;

                case CookingSession.Phase.RetryReady:
                    statusLabel.text =
                        "Ready - toss again!";
                    break;

                case CookingSession.Phase.Finished:
                    statusLabel.text =
                        $"CATCH!  Height: " +
                        $"{Session.PeakHeight * 100f:0} cm";
                    break;
            }
        }

        void PlayFeedback(bool success)
        {
            if (feedbackSource == null)
            {
                return;
            }

            AudioClip clip = success
                ? catchClip
                : failureClip;

            if (clip == null)
            {
                if (success && generatedCatch == null)
                {
                    generatedCatch = MakeTone(880f);
                }

                if (!success && generatedFailure == null)
                {
                    generatedFailure = MakeTone(220f);
                }

                clip = success
                    ? generatedCatch
                    : generatedFailure;
            }

            feedbackSource.PlayOneShot(clip, 0.35f);
        }

        // 音素材がなくても試せる短い通知音。
        static AudioClip MakeTone(float frequency)
        {
            const int sampleRate = 24000;
            const int count = 4800;

            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float envelope =
                    Mathf.Min(i / 240f, 1f) *
                    (1f - i / (float)count);

                samples[i] =
                    Mathf.Sin(
                        2f * Mathf.PI *
                        frequency * i / sampleRate
                    ) * envelope * 0.3f;
            }

            AudioClip clip = AudioClip.Create(
                "Cooking feedback",
                count,
                1,
                sampleRate,
                false
            );

            clip.SetData(samples, 0);

            return clip;
        }

        void OnDestroy()
        {
            if (generatedCatch != null)
            {
                Destroy(generatedCatch);
            }

            if (generatedFailure != null)
            {
                Destroy(generatedFailure);
            }
        }

        void OnValidate()
        {
            fullCookingSeconds =
                Mathf.Max(1f, fullCookingSeconds);

            cookedAtSegment =
                Mathf.Clamp(cookedAtSegment, 1, 9);

            burntAtSegment =
                Mathf.Clamp(
                    burntAtSegment,
                    cookedAtSegment + 1,
                    10
                );

            minimumAirSeconds =
                Mathf.Max(0.01f, minimumAirSeconds);

            minimumRise =
                Mathf.Max(0.01f, minimumRise);

            stableCatchSeconds =
                Mathf.Max(0.01f, stableCatchSeconds);

            throwTimeout =
                Mathf.Max(0.5f, throwTimeout);

            failureDropDistance =
                Mathf.Max(0.1f, failureDropDistance);
        }
    }
}