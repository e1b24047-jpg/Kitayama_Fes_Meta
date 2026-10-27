using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Kitayama.Cooking.Editor
{
    public static class ResultSetup
    {
        [MenuItem("Kitayama/Setup Result Step")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            GameObject kitchen =
                GameObject.Find("KitchenPrototype");

            Transform meat = kitchen != null
                ? kitchen.transform.Find("Meat")
                : null;

            MeatCooking cooking = meat != null
                ? meat.GetComponent<MeatCooking>()
                : null;

            Camera camera = Camera.main;

            if (cooking == null || camera == null)
            {
                Debug.LogError(
                    "MeatCooking and a camera tagged " +
                    "MainCamera are required."
                );

                return;
            }

            // キッチンを隠す際にXRカメラも消えないよう確認する。
            if (camera.transform.IsChildOf(kitchen.transform))
            {
                Debug.LogError(
                    "Move the XR Origin outside " +
                    "KitchenPrototype first."
                );

                return;
            }

            // 再実行時の重複作成を防ぐ。
            if (GameObject.Find("CookingResultRoot") != null)
            {
                Debug.Log(
                    "Result is already set up. " +
                    "Select CookingResultRoot to adjust it."
                );

                return;
            }

            Font font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf"
                );

            if (font == null)
            {
                return;
            }

            // キッチンとは別のルートに作成する。
            GameObject root =
                new GameObject("CookingResultRoot");

            Undo.RegisterCreatedObjectUndo(
                root,
                "Setup Result Step"
            );

            CookingResult result =
                root.AddComponent<CookingResult>();

            result.cooking = cooking;
            result.kitchen = kitchen;
            result.viewCamera = camera;

            GameObject panel = new GameObject(
                "ResultPanel",
                typeof(RectTransform),
                typeof(Canvas)
            );

            panel.transform.SetParent(
                root.transform,
                false
            );

            RectTransform panelRect =
                panel.GetComponent<RectTransform>();

            panelRect.sizeDelta =
                new Vector2(680, 400);

            panelRect.localScale =
                Vector3.one * 0.0015f;

            Canvas canvas = panel.GetComponent<Canvas>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 50;

            // 背景パネル。
            Image background = Rect(
                "Background",
                panel.transform,
                Vector2.zero,
                new Vector2(680, 400)
            ).gameObject.AddComponent<Image>();

            background.color =
                new Color(0.025f, 0.04f, 0.07f, 0.98f);

            background.raycastTarget = false;

            // タイトル。
            Label(
                "Title",
                panel.transform,
                font,
                46,
                135,
                "RESULT",
                Color.white
            );

            // 焼き加減点。
            result.cookingText = Label(
                "CookingPoints",
                panel.transform,
                font,
                30,
                55,
                "COOKING     0 pts",
                Color.white
            );

            // 高さ点。
            result.heightText = Label(
                "HeightPoints",
                panel.transform,
                font,
                30,
                -5,
                "HEIGHT     0 pts",
                Color.white
            );

            // 合計点。
            result.totalText = Label(
                "Total",
                panel.transform,
                font,
                48,
                -105,
                "TOTAL   0 pts",
                new Color(1f, 0.8f, 0.2f)
            );

            result.resultPanel = panel;

            // 成功するまでは非表示にする。
            panel.SetActive(false);

            EditorUtility.SetDirty(result);

            EditorSceneManager.MarkSceneDirty(
                root.scene
            );

            Selection.activeGameObject = root;

            Debug.Log(
                "RESULT_SETUP_OK: Save the scene, " +
                "then test a successful catch."
            );
        }

        static RectTransform Rect(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            GameObject obj = new GameObject(
                name,
                typeof(RectTransform)
            );

            RectTransform rect =
                obj.GetComponent<RectTransform>();

            rect.SetParent(parent, false);

            rect.anchorMin =
                rect.anchorMax =
                rect.pivot = new Vector2(0.5f, 0.5f);

            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return rect;
        }

        static Text Label(
            string name,
            Transform parent,
            Font font,
            int size,
            float y,
            string value,
            Color color)
        {
            Text text = Rect(
                name,
                parent,
                new Vector2(0, y),
                new Vector2(630, 70)
            ).gameObject.AddComponent<Text>();

            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = size;

            return text;
        }
    }
}