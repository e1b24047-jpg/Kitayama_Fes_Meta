using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Kitayama.Cooking.Editor
{
    public static class TimeLimitSetup
    {
        [MenuItem("Kitayama/Setup Time Limit")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            GameObject startRoot =
                GameObject.Find("CookingStartRoot");

            GameObject resultRoot =
                GameObject.Find("CookingResultRoot");

            CookingStart start = startRoot != null
                ? startRoot.GetComponent<CookingStart>()
                : null;

            CookingResult result = resultRoot != null
                ? resultRoot.GetComponent<CookingResult>()
                : null;

            if (start == null ||
                result == null ||
                start.startButton == null ||
                result.cooking == null ||
                result.cooking.meterLabel == null)
            {
                Debug.LogError(
                    "Set up the start screen, cooking meter " +
                    "and result screen first."
                );

                return;
            }

            Canvas meterCanvas =
                result.cooking.meterLabel
                    .GetComponentInParent<Canvas>(true);

            RectTransform meter = meterCanvas != null
                ? meterCanvas.GetComponent<RectTransform>()
                : null;

            if (meter == null || meter.parent == null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

            if (font == null)
            {
                return;
            }

            Undo.IncrementCurrentGroup();

            int group = Undo.GetCurrentGroup();

            Undo.SetCurrentGroupName("Setup Time Limit");

            // タイマー処理はキッチンの外に配置。
            GameObject root =
                GameObject.Find("CookingTimerRoot");

            if (root == null)
            {
                root = new GameObject("CookingTimerRoot");

                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create Timer Root"
                );
            }

            CookingTimer timer =
                root.GetComponent<CookingTimer>();

            if (timer == null)
            {
                timer = Undo.AddComponent<CookingTimer>(root);
            }

            Transform existing =
                meter.parent.Find("CookingTimerDisplay");

            Text timeLabel;

            if (existing == null)
            {
                float width = meter.rect.width;
                float height = meter.rect.height;

                RectTransform panel = Rect(
                    "CookingTimerDisplay",
                    meter.parent,
                    Vector2.zero,
                    new Vector2(width, height)
                );

                panel.localRotation = meter.localRotation;
                panel.localScale = meter.localScale;

                // 焼きメーター全体と同じサイズで、右隣へ配置。
                panel.localPosition =
                    meter.localPosition +
                    meter.localRotation *
                    new Vector3(
                        (width + 30f) * meter.localScale.x,
                        0f,
                        0f
                    );

                Canvas canvas =
                    panel.gameObject.AddComponent<Canvas>();

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = result.viewCamera;
                canvas.sortingOrder = meterCanvas.sortingOrder;

                Image background = Rect(
                    "Background",
                    panel,
                    Vector2.zero,
                    new Vector2(width, height)
                ).gameObject.AddComponent<Image>();

                background.color =
                    new Color(0.025f, 0.035f, 0.055f, 0.94f);

                background.raycastTarget = false;

                Label(
                    "Title",
                    panel,
                    font,
                    new Vector2(0, height * 0.28f),
                    new Vector2(width * 0.9f, height * 0.25f),
                    24,
                    "TIME"
                );

                timeLabel = Label(
                    "Time",
                    panel,
                    font,
                    new Vector2(0, -height * 0.1f),
                    new Vector2(width * 0.9f, height * 0.65f),
                    64,
                    "02:00"
                );

                Undo.RegisterCreatedObjectUndo(
                    panel.gameObject,
                    "Create Timer Display"
                );
            }
            else
            {
                // 再実行しても画面を重複させない。
                timeLabel = existing.Find("Time")
                    ?.GetComponent<Text>();

                if (timeLabel == null)
                {
                    Undo.RevertAllDownToGroup(group);

                    Debug.LogError(
                        "CookingTimerDisplay exists " +
                        "but its Time text is missing."
                    );

                    return;
                }
            }

            Undo.RecordObject(timer, "Connect Timer");

            timer.startScreen = start;
            timer.result = result;
            timer.timeText = timeLabel;

            EditorUtility.SetDirty(timer);

            EditorSceneManager.MarkSceneDirty(root.scene);

            Undo.CollapseUndoOperations(group);

            Selection.activeGameObject = root;

            Debug.Log(
                "TIME_LIMIT_SETUP_OK: Save the scene. " +
                "The countdown starts when START is pressed."
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
            Vector2 position,
            Vector2 size,
            int fontSize,
            string value)
        {
            Text text = Rect(
                name,
                parent,
                position,
                size
            ).gameObject.AddComponent<Text>();

            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = fontSize;

            return text;
        }
    }
}