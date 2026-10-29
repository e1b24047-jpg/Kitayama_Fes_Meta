using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kitayama.Cooking
{
    public sealed class RetryToStart : MonoBehaviour
    {
        public Button retryButton;

        bool loading;

        void OnEnable()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(Retry);
            }
        }

        void OnDisable()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(Retry);
            }
        }

        public void Retry()
        {
            // 連続クリックで何度も読み込まないようにする。
            if (loading)
            {
                return;
            }

            string scenePath = gameObject.scene.path;

            if (string.IsNullOrEmpty(scenePath) ||
                !Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError(
                    "Add this scene to the active Build Profile " +
                    "Scene List, then save it.",
                    this
                );

                return;
            }

            loading = true;

            if (retryButton != null)
            {
                retryButton.interactable = false;
            }

            StartCoroutine(Reload(scenePath));
        }

        IEnumerator Reload(string scenePath)
        {
            // ボタンの入力処理が終わってから読み込み直す。
            yield return null;

            AsyncOperation operation = null;

            try
            {
                operation = SceneManager.LoadSceneAsync(
                    scenePath,
                    LoadSceneMode.Single
                );
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }

            // 読み込みを開始できなかった場合は押し直せるようにする。
            if (operation == null)
            {
                loading = false;

                if (retryButton != null)
                {
                    retryButton.interactable = true;
                }

                yield break;
            }

            yield return operation;

            // シーンの再読込でCookingStartも初期化され、
            // START画面から始まる。
        }
    }
}