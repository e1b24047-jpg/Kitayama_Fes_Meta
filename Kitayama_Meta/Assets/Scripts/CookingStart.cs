using UnityEngine;
using UnityEngine.UI;

namespace Kitayama.Cooking
{
    // 料理の処理より先に、スタート待機へ切り替える。
    [DefaultExecutionOrder(-1000)]
    public sealed class CookingStart : MonoBehaviour
    {
        public GameObject kitchen;
        public Camera viewCamera;
        public GameObject startPanel;
        public Button startButton;

        bool started;

        void Awake()
        {
            if (kitchen == null ||
                viewCamera == null ||
                startPanel == null ||
                startButton == null)
            {
                Debug.LogError(
                    "Run Kitayama > Setup Start Screen first.",
                    this
                );

                enabled = false;
                return;
            }

            // 料理オブジェクトを止めてスタートを待つ。
            // XRの操作やUI入力は動かしたままにする。
            kitchen.SetActive(false);

            // スタート画面を視界の正面へ配置。
            startPanel.transform.SetParent(
                viewCamera.transform,
                false
            );

            startPanel.transform.localPosition =
                new Vector3(0f, 0f, 1.4f);

            startPanel.transform.localRotation =
                Quaternion.identity;

            startPanel.transform.localScale =
                Vector3.one * 0.0015f;

            startPanel.SetActive(true);
        }

        void OnEnable()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
            }
        }

        void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }
        }

        public void StartGame()
        {
            // 二重クリックなどで繰り返し開始しない。
            if (started ||
                kitchen == null ||
                startPanel == null)
            {
                return;
            }

            started = true;

            startButton.interactable = false;
            startPanel.SetActive(false);

            // キッチンを表示してゲーム開始。
            kitchen.SetActive(true);

            MeatCooking cooking =
                kitchen.GetComponentInChildren<MeatCooking>();

            if (cooking != null)
            {
                cooking.ResetCooking();
            }
        }

        void OnDestroy()
        {
            // 再生中にカメラへ移したパネルも片付ける。
            if (startPanel != null)
            {
                Destroy(startPanel);
            }
        }
    }
}