using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Kitayama.Cooking.Editor
{
    public static class RetrySetup
    {
        [MenuItem("Kitayama/Setup Retry Button")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            GameObject resultRoot =
                GameObject.Find("CookingResultRoot");

            GameObject startRoot =
                GameObject.Find("CookingStartRoot");

            CookingResult result = resultRoot != null
                ? resultRoot.GetComponent<CookingResult>()
                : null;

            CookingStart start = startRoot != null
                ? startRoot.GetComponent<CookingStart>()
                : null;

            if (result == null ||
                start == null ||
                result.resultPanel == null ||
                result.cookingText == null ||
                result.heightText == null ||
                result.totalText == null ||
                result.viewCamera == null ||
                start.startPanel == null ||
                start.startButton == null ||
                resultRoot.scene != startRoot.scene)
            {
                Debug.LogError(
                    "Set up both the start screen " +
                    "and result screen first."
                );

                return;
            }

            var scene = resultRoot.scene;

            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError(
                    "Save the scene before running this setup."
                );

                return;
            }

            RectTransform panel =
                result.resultPanel.GetComponent<RectTransform>();

            if (panel == null)
            {
                return;
            }

            RectTransform background =
                panel.Find("Background") as RectTransform;

            RectTransform title =
                panel.Find("Title") as RectTransform;

            if (background == null || title == null)
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

            Undo.SetCurrentGroupName("Setup Retry Button");

            // ボタンを置けるよう、パネルを縦に広げる。
            Resize(panel, 680, 560);
            Resize(background, 680, 560);

            Position(title, 190);
            Position(result.cookingText.rectTransform, 100);
            Position(result.heightText.rectTransform, 40);
            Position(result.totalText.rectTransform, -45);

            // マウスとQuestコントローラーの両方に対応。
            if (panel.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(
                    panel.gameObject
                );
            }

            if (panel.GetComponent<TrackedDeviceGraphicRaycaster>()
                == null)
            {
                Undo.AddComponent<TrackedDeviceGraphicRaycaster>(
                    panel.gameObject
                );
            }

            Canvas canvas = panel.GetComponent<Canvas>();

            Undo.RecordObject(canvas, "Set UI camera");

            canvas.worldCamera = result.viewCamera;

            EditorUtility.SetDirty(canvas);

            // 再実行してもボタンを重複させない。
            Transform existing = panel.Find("RetryButton");

            Button button = existing != null
                ? existing.GetComponent<Button>()
                : null;

            if (existing != null && button == null)
            {
                Undo.RevertAllDownToGroup(group);

                Debug.LogError(
                    "RetryButton already exists " +
                    "but has no Button component."
                );

                return;
            }

            if (button == null)
            {
                GameObject obj = new GameObject(
                    "RetryButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button)
                );

                obj.transform.SetParent(panel, false);

                RectTransform rect =
                    obj.GetComponent<RectTransform>();

                rect.anchorMin =
                    rect.anchorMax =
                    rect.pivot = new Vector2(0.5f, 0.5f);

                rect.anchoredPosition =
                    new Vector2(0, -175);

                rect.sizeDelta =
                    new Vector2(300, 85);

                Image image = obj.GetComponent<Image>();

                image.color =
                    new Color(1f, 0.72f, 0.2f);

                button = obj.GetComponent<Button>();

                button.targetGraphic = image;

                ColorBlock colors = button.colors;

                colors.highlightedColor =
                    new Color(1f, 1f, 0.75f);

                colors.pressedColor =
                    new Color(0.7f, 0.7f, 0.7f);

                button.colors = colors;

                // ボタンの文字。
                GameObject labelObj = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(Text)
                );

                labelObj.transform.SetParent(
                    obj.transform,
                    false
                );

                RectTransform labelRect =
                    labelObj.GetComponent<RectTransform>();

                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;

                labelRect.offsetMin =
                    labelRect.offsetMax = Vector2.zero;

                Text label = labelObj.GetComponent<Text>();

                label.font = font;
                label.fontSize = 36;
                label.text = "RETRY";

                label.alignment =
                    TextAnchor.MiddleCenter;

                label.color =
                    new Color(0.08f, 0.06f, 0.02f);

                label.raycastTarget = false;

                Undo.RegisterCreatedObjectUndo(
                    obj,
                    "Create Retry Button"
                );
            }

            // リトライ処理とボタンを接続する。
            RetryToStart retry =
                resultRoot.GetComponent<RetryToStart>();

            if (retry == null)
            {
                retry = Undo.AddComponent<RetryToStart>(
                    resultRoot
                );
            }

            Undo.RecordObject(
                retry,
                "Connect Retry Button"
            );

            retry.retryButton = button;

            EditorUtility.SetDirty(retry);

            // 再読込できるよう、共通のScene Listへ追加。
            // 他の登録済みシーンは残す。
            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes
            );

            int index = scenes.FindIndex(
                entry => entry.path == scene.path
            );

            if (index < 0)
            {
                scenes.Add(
                    new EditorBuildSettingsScene(
                        scene.path,
                        true
                    )
                );
            }
            else
            {
                scenes[index] =
                    new EditorBuildSettingsScene(
                        scene.path,
                        true
                    );
            }

            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.MarkSceneDirty(scene);

            Undo.CollapseUndoOperations(group);

            Selection.activeGameObject = resultRoot;

            Debug.Log(
                "RETRY_SETUP_OK: Save the scene. " +
                "If the active Build Profile overrides the " +
                "Scene List, add this scene there too."
            );
        }

        static void Resize(
            RectTransform rect,
            float width,
            float height)
        {
            Undo.RecordObject(
                rect,
                "Resize Result Panel"
            );

            rect.sizeDelta =
                new Vector2(width, height);

            EditorUtility.SetDirty(rect);
        }

        static void Position(
            RectTransform rect,
            float y)
        {
            Undo.RecordObject(
                rect,
                "Move Result Label"
            );

            rect.anchoredPosition =
                new Vector2(0, y);

            EditorUtility.SetDirty(rect);
        }
    }
}