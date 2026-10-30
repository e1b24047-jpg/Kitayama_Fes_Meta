using System;

namespace Kitayama.Cooking
{
    // 高さは、離陸前の肉の重心を基準にする。
    public sealed class CookingSession
    {
        public enum Doneness
        {
            Raw,
            Cooked,
            Burnt
        }

        public enum Phase
        {
            Waiting,
            Heating,
            Paused,
            Airborne,
            Finished,
            Failed,
            RetryReady
        }

        public float Seconds { get; private set; }

        public int FilledSegments =>
            Math.Min(
                10,
                (int)Math.Floor(
                    Seconds / fullTime * 10f + 0.00001f
                )
            );

        public Doneness Grade =>
            FilledSegments >= burntAt
                ? Doneness.Burnt
                : FilledSegments >= cookedAt
                    ? Doneness.Cooked
                    : Doneness.Raw;

        public Phase CurrentPhase { get; private set; } = Phase.Waiting;

        public bool Finished => CurrentPhase == Phase.Finished;

        public bool CookingLocked { get; private set; }

        public bool HasHeated => Seconds > 0;

        public float PeakHeight { get; private set; }

        public int Attempts { get; private set; }

        readonly float fullTime;
        readonly float minAirTime;
        readonly float minRise;
        readonly float catchTime;
        readonly float timeout;
        readonly float dropDistance;

        readonly int cookedAt;
        readonly int burntAt;

        bool previousOnPan;
        bool departing;
        bool airborne;
        bool failed;

        float airSeconds;
        float catchSeconds;
        float lastSupportedHeight;
        float launchHeight;
        float candidatePeak;

        public CookingSession(
            float fullTime = 20f,
            int cookedAt = 4,
            int burntAt = 8,
            float minAirTime = 0.08f,
            float minRise = 0.05f,
            float catchTime = 0.12f,
            float timeout = 5f,
            float dropDistance = 0.6f)
        {
            if (fullTime <= 0 ||
                cookedAt < 1 ||
                burntAt <= cookedAt ||
                burntAt > 10 ||
                minAirTime <= 0 ||
                minRise <= 0 ||
                catchTime <= 0 ||
                timeout <= 0 ||
                dropDistance <= 0)
            {
                throw new ArgumentException(
                    "Invalid cooking settings."
                );
            }

            this.fullTime = fullTime;
            this.cookedAt = cookedAt;
            this.burntAt = burntAt;
            this.minAirTime = minAirTime;
            this.minRise = minRise;
            this.catchTime = catchTime;
            this.timeout = timeout;
            this.dropDistance = dropDistance;
        }

        // failedContact:
        // 床・机などへの接触。フライパン自身は除外する。
        public void Step(
            float dt,
            bool onPan,
            bool nearHeat,
            bool held,
            float meatHeight,
            bool failedContact = false,
            float verticalVelocity = 0f)
        {
            if (Finished || dt <= 0)
            {
                return;
            }

            // 肉を直接手で掴んでいる場合。
            if (held)
            {
                if (airborne)
                {
                    Fail();
                }

                previousOnPan = false;
                departing = false;
                airSeconds = 0;
                catchSeconds = 0;

                CurrentPhase = failed
                    ? Phase.Failed
                    : HasHeated
                        ? Phase.Paused
                        : Phase.Waiting;

                return;
            }

            // 失敗後は、肉をフライパンへ戻して安定させる。
            if (failed)
            {
                catchSeconds = onPan
                    ? catchSeconds + dt
                    : 0;

                if (catchSeconds >= catchTime)
                {
                    failed = false;
                    previousOnPan = true;
                    lastSupportedHeight = meatHeight;
                    catchSeconds = 0;

                    CurrentPhase = Phase.RetryReady;
                }

                return;
            }

            // 投げた後の処理。
            if (airborne)
            {
                airSeconds += dt;

                PeakHeight = Math.Max(
                    PeakHeight,
                    meatHeight - launchHeight
                );

                if (failedContact ||
                    airSeconds >= timeout ||
                    meatHeight < launchHeight - dropDistance)
                {
                    Fail();
                    return;
                }

                // 上昇中や一瞬だけの接触では成功にしない。
                catchSeconds =
                    onPan && verticalVelocity <= 0.2f
                        ? catchSeconds + dt
                        : 0;

                if (catchSeconds >= catchTime)
                {
                    CurrentPhase = Phase.Finished;
                }

                return;
            }

            // 肉がフライパンに載っている場合。
            if (onPan)
            {
                departing = false;
                airSeconds = 0;
                catchSeconds = 0;
                lastSupportedHeight = meatHeight;

                if (nearHeat && !CookingLocked)
                {
                    Seconds = Math.Min(
                        fullTime,
                        Seconds + dt
                    );

                    CurrentPhase = Phase.Heating;
                }
                else
                {
                    CurrentPhase = CookingLocked
                        ? Phase.RetryReady
                        : HasHeated
                            ? Phase.Paused
                            : Phase.Waiting;
                }

                previousOnPan = true;
                return;
            }

            // フライパンから離れた瞬間を記録する。
            if (previousOnPan && HasHeated)
            {
                departing = true;
                launchHeight = lastSupportedHeight;
                airSeconds = 0;
                candidatePeak = 0;
            }

            previousOnPan = false;

            if (departing)
            {
                airSeconds += dt;

                candidatePeak = Math.Max(
                    candidatePeak,
                    meatHeight - launchHeight
                );

                if (failedContact ||
                    airSeconds >= timeout ||
                    meatHeight < launchHeight - dropDistance)
                {
                    departing = false;

                    CurrentPhase = HasHeated
                        ? Phase.Paused
                        : Phase.Waiting;

                    return;
                }

                // 一定時間離れ、一定の高さに達したら有効な投げ。
                if (airSeconds >= minAirTime &&
                    candidatePeak >= minRise)
                {
                    airborne = true;
                    departing = false;

                    // 最初の有効な投げで焼き加減を固定する。
                    CookingLocked = true;

                    PeakHeight = candidatePeak;
                    Attempts++;

                    CurrentPhase = Phase.Airborne;
                    return;
                }
            }

            CurrentPhase = HasHeated
                ? Phase.Paused
                : Phase.Waiting;
        }

        public void CancelThrowForRespawn()
        {
            if (Finished)
                return;

            previousOnPan = false;
            departing = false;
            airborne = false;

            airSeconds = 0f;
            catchSeconds = 0f;
            candidatePeak = 0f;
            PeakHeight = 0f;

            // 焼き時間・焼き加減の固定状態・試行回数は維持する。
            failed = CookingLocked;

            CurrentPhase = failed
                ? Phase.Failed
                : HasHeated
                    ? Phase.Paused
                    : Phase.Waiting;
        }

        void Fail()
        {
            failed = true;
            airborne = false;
            departing = false;
            previousOnPan = false;
            catchSeconds = 0;

            CurrentPhase = Phase.Failed;
        }
    }
}