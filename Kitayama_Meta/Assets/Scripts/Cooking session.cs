using System;

namespace Kitayama.Cooking
{
    // Unityに依存しない、加熱時間と投げる→キャッチの進行管理。
    public sealed class CookingSession
    {
        public enum Doneness { Raw, Cooked, Burnt }
        public enum Phase { Waiting, Heating, Paused, Airborne, Finished }

        public float Seconds { get; private set; }
        public int FilledSegments => Math.Min(10, (int)Math.Floor(Seconds / fullTime * 10f + 0.00001f));
        public Doneness Grade => FilledSegments >= burntAt ? Doneness.Burnt
            : FilledSegments >= cookedAt ? Doneness.Cooked : Doneness.Raw;
        public Phase CurrentPhase { get; private set; } = Phase.Waiting;
        public bool Finished => CurrentPhase == Phase.Finished;

        readonly float fullTime, minAirTime, minRise, catchTime;
        readonly int cookedAt, burntAt;
        bool hasHeated, previousOnPan, departing, airborne;
        float airSeconds, catchSeconds, lastSupportedHeight;

        public CookingSession(float fullTime = 20f, int cookedAt = 4, int burntAt = 8,
            float minAirTime = 0.08f, float minRise = 0.05f, float catchTime = 0.12f)
        {
            if (fullTime <= 0 || cookedAt < 1 || burntAt <= cookedAt || burntAt > 10
                || minAirTime <= 0 || minRise <= 0 || catchTime <= 0)
                throw new ArgumentException("Invalid cooking settings.");
            this.fullTime = fullTime;
            this.cookedAt = cookedAt;
            this.burntAt = burntAt;
            this.minAirTime = minAirTime;
            this.minRise = minRise;
            this.catchTime = catchTime;
        }

        // onPan: フライパンの底で受け止められているか。
        // held: 肉そのものを手でつかんでいるか。
        public void Step(float dt, bool onPan, bool nearHeat, bool held, float meatHeight)
        {
            if (Finished || dt <= 0) return;

            if (held)
            {
                // 手で持ち上げる動作を「投げる」と誤判定しない。
                previousOnPan = departing = airborne = false;
                airSeconds = catchSeconds = 0;
                CurrentPhase = hasHeated ? Phase.Paused : Phase.Waiting;
                return;
            }

            if (onPan)
            {
                if (airborne)
                {
                    // 側壁に一瞬当たっただけでは終了させない。
                    catchSeconds += dt;
                    CurrentPhase = catchSeconds >= catchTime ? Phase.Finished : Phase.Airborne;
                    previousOnPan = true;
                    return;
                }

                departing = false;
                airSeconds = catchSeconds = 0;
                lastSupportedHeight = meatHeight;
                if (nearHeat)
                {
                    Seconds = Math.Min(fullTime, Seconds + dt);
                    hasHeated = true;
                    CurrentPhase = Phase.Heating;
                }
                else CurrentPhase = hasHeated ? Phase.Paused : Phase.Waiting;
                previousOnPan = true;
                return;
            }

            catchSeconds = 0;
            if (previousOnPan && hasHeated)
            {
                departing = true;
                airSeconds = 0;
            }
            if (departing)
            {
                airSeconds += dt;
                if (airSeconds >= minAirTime && meatHeight - lastSupportedHeight >= minRise)
                    airborne = true;
            }
            CurrentPhase = airborne ? Phase.Airborne : hasHeated ? Phase.Paused : Phase.Waiting;
            previousOnPan = false;
        }
    }
}
