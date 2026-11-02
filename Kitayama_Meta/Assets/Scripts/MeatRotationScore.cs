using System;
using UnityEngine;

namespace Kitayama.Cooking
{
    // MeatCookingがキャッチを判定する前に回転を記録する。
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

        // キャッチ成功後だけ、確定した回転数を返す。
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
            armed = false;
            tracking = false;
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
                armed = false;
                tracking = false;
                previous = current;
            }

            if (session.Finished)
                return;

            bool held =
                cooking.meatGrab != null &&
                cooking.meatGrab.isSelected;

            // 肉を直接掴んだ場合や、投げる工程に失敗した場合。
            if (held ||
                session.CurrentPhase ==
                    CookingSession.Phase.Failed)
            {
                degrees = 0;
                armed = false;
                tracking = false;
                previous = current;
                return;
            }

            if (cooking.SupportedByPan)
            {
                // フライパン上での回転や、
                // キャッチ後に安定するまでの揺れは数えない。
                if (session.CurrentPhase !=
                    CookingSession.Phase.Airborne)
                {
                    degrees = 0;
                    tracking = false;
                    armed = true;
                }

                previous = current;
                return;
            }

            // 加熱済みの肉がフライパンから離れたら計測開始。
            if (!tracking && armed && session.HasHeated)
            {
                tracking = true;
                armed = false;
                degrees = 0;
            }

            if (tracking)
            {
                Quaternion delta =
                    Quaternion.Inverse(previous) * current;

                degrees += RotationScoreMath.DeltaDegrees(
                    delta.x,
                    delta.y,
                    delta.z,
                    delta.w
                );
            }

            previous = current;
        }
    }

    // このクラスも同じファイルに入れてください。
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

            // ごく小さな計算誤差だけ補正する。
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