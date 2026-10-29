using UnityEngine;
using UnityEngine.UI;

namespace Kitayama.Cooking
{
    // 料理の処理より先に時間切れを確認する。
    [DefaultExecutionOrder(-500)]
    public sealed class CookingTimer : MonoBehaviour
    {
        public CookingStart startScreen;
        public CookingResult result;
        public Text timeText;

        [Min(1f)]
        public float timeLimitSeconds = 120f;

        Button subscribedButton;

        double deadline;

        bool started;
        bool running;

        int lastDisplayed = -1;

        void OnEnable()
        {
            if (startScreen == null ||
                startScreen.startButton == null ||
                result == null ||
                timeText == null)
            {
                Debug.LogError(
                    "Run Kitayama > Setup Time Limit first.",
                    this
                );

                enabled = false;
                return;
            }

            // 既存のSTARTボタンを押したときに計測を開始する。
            subscribedButton = startScreen.startButton;
            subscribedButton.onClick.AddListener(BeginTimer);

            if (!started)
            {
                Draw(timeLimitSeconds);
            }
        }

        void OnDisable()
        {
            if (subscribedButton != null)
            {
                subscribedButton.onClick.RemoveListener(BeginTimer);
            }

            subscribedButton = null;
        }

        void BeginTimer()
        {
            // 連続クリックなどで残り時間を戻さない。
            if (started || result.IsFinished)
            {
                return;
            }

            started = true;
            running = true;

            deadline =
                Time.realtimeSinceStartupAsDouble +
                Mathf.Max(1f, timeLimitSeconds);

            Draw(timeLimitSeconds);
        }

        void Update()
        {
            Tick();
        }

        void FixedUpdate()
        {
            Tick();
        }

        void Tick()
        {
            if (!running)
            {
                return;
            }

            // キャッチ成功後は、エフェクト表示中も計測を止める。
            if (result.IsFinished)
            {
                running = false;
                return;
            }

            double remaining =
                deadline - Time.realtimeSinceStartupAsDouble;

            Draw(remaining);

            if (remaining <= 0d)
            {
                running = false;

                result.ShowTimeoutResult();
            }
        }

        void Draw(double remaining)
        {
            int seconds = (int)System.Math.Ceiling(
                System.Math.Max(0d, remaining)
            );

            // 表示する秒数が変わったときだけ更新する。
            if (seconds == lastDisplayed)
            {
                return;
            }

            lastDisplayed = seconds;

            timeText.text =
                $"{seconds / 60:00}:{seconds % 60:00}";

            if (seconds <= 10)
            {
                timeText.color =
                    new Color(1f, 0.3f, 0.25f);
            }
            else if (seconds <= 30)
            {
                timeText.color =
                    new Color(1f, 0.8f, 0.2f);
            }
            else
            {
                timeText.color = Color.white;
            }
        }
    }
}