using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kitayama.Cooking
{
    public sealed class CookingResult : MonoBehaviour
    {
        public MeatCooking cooking;
        public GameObject kitchen;
        public Camera viewCamera;

        public GameObject resultPanel;

        public Text cookingText;
        public Text heightText;
        public Text rotationText;
        public Text totalText;

        public MeatRotationScore rotationScore;

        [Min(0f)]
        public float effectWaitSeconds = 0.8f;

        public int[] cookingPoints =
            CookingScore.DefaultCookingPoints();

        public bool IsFinished => completed;

        MeatCooking subscribedCooking;

        bool completed;
        Coroutine transition;

        void Awake()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);
        }

        void OnEnable()
        {
            if (cooking == null ||
                kitchen == null ||
                viewCamera == null ||
                resultPanel == null ||
                cookingText == null ||
                heightText == null ||
                totalText == null)
            {
                Debug.LogError(
                    "先にリザルト画面を設定してください。",
                    this
                );

                return;
            }

            if (rotationScore == null || rotationText == null)
            {
                Debug.LogWarning(
                    "Kitayama → Setup Rotation Scoreを"
                    + "実行してください。",
                    this
                );
            }

            subscribedCooking = cooking;
            subscribedCooking.CookingCompleted += OnCompleted;
        }

        void OnDisable()
        {
            if (subscribedCooking != null)
            {
                subscribedCooking.CookingCompleted -=
                    OnCompleted;
            }

            subscribedCooking = null;

            if (transition != null)
                StopCoroutine(transition);

            transition = null;
        }

        void OnCompleted(
            CookingSession.Doneness grade,
            int meter)
        {
            if (completed || cooking.Session == null)
                return;

            completed = true;

            int turns = rotationScore != null
                ? rotationScore.SuccessfulTurns
                : 0;

            SetScores(
                meter,
                cooking.Session.PeakHeight,
                turns,
                false
            );

            transition = StartCoroutine(ShowAfterEffect());
        }

        public void ShowTimeoutResult()
        {
            if (completed || cooking == null)
                return;

            completed = true;

            int meter = cooking.Session != null
                ? cooking.Session.FilledSegments
                : 0;

            // キャッチできず時間切れになった場合、
            // 高さと回転の点数は0にする。
            SetScores(
                meter,
                0f,
                0,
                true
            );

            ShowPanel();
        }

        void SetScores(
            int meter,
            float peakHeight,
            int turns,
            bool timeUp)
        {
            int cook = CookingScore.CookingPoints(
                meter,
                cookingPoints
            );

            int height = CookingScore.HeightPoints(
                peakHeight
            );

            long rotation = RotationScoreMath.Points(
                turns
            );

            long total =
                (long)cook + height + rotation;

            cookingText.text =
                $"COOKING  ({meter}/10)     {cook} pts";

            heightText.text =
                $"HEIGHT  ({height} cm)     {height} pts";

            if (rotationText != null)
            {
                rotationText.text =
                    $"ROTATION  ({turns} x 180deg)     {rotation} pts";
            }

            totalText.text =
                $"TOTAL   {total} pts";

            Transform title =
                resultPanel.transform.Find("Title");

            if (title != null &&
                title.TryGetComponent<Text>(out var label))
            {
                label.text = timeUp
                    ? "TIME UP"
                    : "RESULT";
            }
        }

        IEnumerator ShowAfterEffect()
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, effectWaitSeconds)
            );

            ShowPanel();
            transition = null;
        }

        void ShowPanel()
        {
            kitchen.SetActive(false);

            resultPanel.transform.SetParent(
                viewCamera.transform,
                false
            );

            resultPanel.transform.localPosition =
                new Vector3(0f, 0f, 1.4f);

            resultPanel.transform.localRotation =
                Quaternion.identity;

            resultPanel.transform.localScale =
                Vector3.one * 0.0015f;

            resultPanel.SetActive(true);
        }

        void OnDestroy()
        {
            if (resultPanel != null)
                Destroy(resultPanel);
        }
    }
}