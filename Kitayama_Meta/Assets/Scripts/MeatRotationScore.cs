using System;
using UnityEngine;

namespace Kitayama.Cooking
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(
        typeof(MeatCooking),
        typeof(Rigidbody))]
    public sealed class MeatRotationScore : MonoBehaviour
    {
        MeatCooking cooking;
        Rigidbody body;

        CookingSession trackedSession;
        Quaternion previous;

        double degrees;

        bool armed;
        bool tracking;
        bool previousSupported;

        // Inspectorで確認するための計測値。
        // 手動で変更する必要はありません。
        [SerializeField]
        float measuredDegrees;

        public int SuccessfulTurns =>
            cooking != null &&
            cooking.Session == trackedSession &&
            trackedSession != null &&
            trackedSession.Finished &&
            tracking
                ? RotationScoreMath.WholeTurns(degrees)
                : 0;

        void Awake()
        {
            cooking = GetComponent<MeatCooking>();
            body = GetComponent<Rigidbody>();
        }

        void OnEnable()
        {
            trackedSession = null;
            degrees = 0;
            measuredDegrees = 0f;

            armed = false;
            tracking = false;
            previousSupported = false;
        }

        void FixedUpdate()
        {
            if (cooking == null ||
                body == null ||
                !cooking.enabled ||
                cooking.Session == null)
            {
                return;
            }

            CookingSession session = cooking.Session;
            Quaternion current = body.rotation;

            // 新しいゲームになった場合。
            if (trackedSession != session)
            {
                trackedSession = session;

                degrees = 0;
                measuredDegrees = 0f;

                armed = false;
                tracking = false;

                previous = current;
                previousSupported = cooking.SupportedByPan;
            }

            if (session.Finished)
                return;

            bool held =
                cooking.meatGrab != null &&
                cooking.meatGrab.isSelected;

            // 直接掴んだ場合や失敗した場合は、
            // その試行の回転を取り消す。
            if (held ||
                session.CurrentPhase ==
                    CookingSession.Phase.Failed)
            {
                degrees = 0;
                measuredDegrees = 0f;

                armed = false;
                tracking = false;

                previous = current;
                previousSupported = cooking.SupportedByPan;

                return;
            }

            if (cooking.SupportedByPan)
            {
                // 修正点：
                // 前回は空中、今回はフライパン上なら、
                // 着地までに回った最後の角度も加算する。
                if (tracking &&
                    !previousSupported &&
                    session.CurrentPhase ==
                        CookingSession.Phase.Airborne)
                {
                    AddRotation(current);
                }

                // 投げる前のフライパン上の回転は数えない。
                // キャッチ判定待ちの場合は、計測値を維持する。
                if (session.CurrentPhase !=
                    CookingSession.Phase.Airborne)
                {
                    degrees = 0;
                    measuredDegrees = 0f;

                    tracking = false;
                    armed = true;
                }

                previous = current;
                previousSupported = true;

                return;
            }

            // 加熱済みの肉がフライパンから離れたら計測開始。
            if (!tracking && armed && session.HasHeated)
            {
                tracking = true;
                armed = false;

                degrees = 0;
                measuredDegrees = 0f;
            }

            if (tracking)
            {
                AddRotation(current);
            }

            previous = current;
            previousSupported = false;
        }

        void AddRotation(Quaternion current)
        {
            Quaternion delta =
                Quaternion.Inverse(previous) * current;

            degrees += RotationScoreMath.DeltaDegrees(
                delta.x,
                delta.y,
                delta.z,
                delta.w
            );

            measuredDegrees = (float)degrees;
        }
    }

    // 以下も同じファイルに入れてください。
    public static class RotationScoreMath
    {
        public static double DeltaDegrees(
            double x,
            double y,
            double z,
            double w)
        {
            return
                2d * Math.Atan2(
                    Math.Sqrt(x * x + y * y + z * z),
                    Math.Abs(w)
                ) * 180d / Math.PI;
        }

        public static int WholeTurns(double degrees)
        {
            if (double.IsNaN(degrees) ||
                double.IsInfinity(degrees))
            {
                return 0;
            }

            // ごく小さな数値誤差だけを補正する。
            double turns = Math.Floor(
                (Math.Max(0d, degrees) + 0.001d) / 360d
            );

            return (int)Math.Min(int.MaxValue, turns);
        }

        public static long Points(int turns)
        {
            return Math.Max(0, turns) * 3L;
        }
    }
}