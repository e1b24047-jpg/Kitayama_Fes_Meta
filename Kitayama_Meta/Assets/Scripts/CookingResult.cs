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

        void OnCompleted(CookingSession.Doneness grade, int meter)
        {
            // 同じ成功で何度も画面を切り替えない。
            if (completed || cooking.Session == null)
            {
                return;
            }

            completed = true;

            // キャッチ成功時の値を確定する。
            int cook = CookingScore.CookingPoints(
                meter,
                cookingPoints
            );

            int height = CookingScore.HeightPoints(
                cooking.Session.PeakHeight
            );

            long total = (long)cook + height;

            cookingText.text =
                $"COOKING  ({meter}/10)     {cook} pts";

            heightText.text =
                $"HEIGHT  ({height} cm)     {height} pts";

            totalText.text =
                $"TOTAL   {total} pts";

            transition = StartCoroutine(ShowResult());
        }

        IEnumerator ShowResult()
        {
            // キャッチエフェクトを見せてから切り替える。
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, effectWaitSeconds)
            );

            // XRカメラは残し、料理用オブジェクトを非表示にする。
            kitchen.SetActive(false);

            // 視界の正面にリザルトを表示する。
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

            transition = null;
        }

        void OnDestroy()
        {
            // 再生中にカメラへ移したパネルも片付ける。
            if (resultPanel != null)
            {
                Destroy(resultPanel);
            }
        }
    }
}