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
        public Text totalText;

        [Min(0f)]
        public float effectWaitSeconds = 0.8f;

        public int[] cookingPoints =
            CookingScore.DefaultCookingPoints();

        // タイマー側からゲーム終了を確認するための情報。
        public bool IsFinished => completed;

        MeatCooking subscribedCooking;

        bool completed;
        Coroutine transition;

        void Awake()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
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
                    "Run Kitayama > Setup Result Step first.",
                    this
                );

                return;
            }

            subscribedCooking = cooking;
            subscribedCooking.CookingCompleted += OnCompleted;
        }

        void OnDisable()
        {
            if (subscribedCooking != null)
            {
                subscribedCooking.CookingCompleted -= OnCompleted;
            }

            subscribedCooking = null;

            if (transition != null)
            {
                StopCoroutine(transition);
            }

            transition = null;
        }

        // 通常のキャッチ成功。
        void OnCompleted(CookingSession.Doneness grade, int meter)
        {
            if (completed || cooking.Session == null)
            {
                return;
            }

            completed = true;

            SetScores(
                meter,
                cooking.Session.PeakHeight,
                false
            );

            transition = StartCoroutine(ShowAfterEffect());
        }

        // 時間切れ。その時点の点数で即座に終了する。
        public void ShowTimeoutResult()
        {
            if (completed || cooking == null)
            {
                return;
            }

            completed = true;

            CookingSession session = cooking.Session;

            int meter = session != null
                ? session.FilledSegments
                : 0;

            float height = session != null
                ? session.PeakHeight
                : 0f;

            SetScores(meter, height, true);
            ShowPanel();
        }

        void SetScores(
            int meter,
            float peakHeight,
            bool timeUp)
        {
            int cook = CookingScore.CookingPoints(
                meter,
                cookingPoints
            );

            int height = CookingScore.HeightPoints(
                peakHeight
            );

            long total = (long)cook + height;

            cookingText.text =
                $"COOKING  ({meter}/10)     {cook} pts";

            heightText.text =
                $"HEIGHT  ({height} cm)     {height} pts";

            totalText.text =
                $"TOTAL   {total} pts";

            Transform title =
                resultPanel.transform.Find("Title");

            if (title != null &&
                title.TryGetComponent<Text>(out var label))
            {
                label.text = timeUp ? "TIME UP" : "RESULT";
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
            // キッチンを止めることで、
            // 時間切れ後に加熱や投げが進まないようにする。
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
            {
                Destroy(resultPanel);
            }
        }
    }
}