using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Kitayama.Cooking.Editor
{
    public static class StartScreenSetup
    {
        [MenuItem("Kitayama/Setup Start Screen")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // 重複作成を防ぐ。
            if (GameObject.Find("CookingStartRoot") != null)
            {
                Debug.Log(
                    "Start screen is already set up."
                );

                return;
            }

            GameObject kitchen =
                GameObject.Find("KitchenPrototype");

            Camera camera = Camera.main;

            if (kitchen == null ||
                camera == null ||
                camera.transform.IsChildOf(kitchen.transform))
            {
                Debug.LogError(
                    "KitchenPrototype and a MainCamera " +
                    "outside the kitchen are required."
                );

                return;
            }

            // Unity標準フォントを使用する。
            Font font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

            if (font == null)
            {
                Debug.LogError(
                    "Font could not be loaded."
                );

                return;
            }

            EventSystem events = null;

            foreach (var candidate in
                Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != kitchen.scene)
                {
                    continue;
                }

                if (events != null ||
                    candidate.transform.IsChildOf(kitchen.transform) ||
                    !candidate.gameObject.activeInHierarchy)
                {
                    Debug.LogError(
                        "Use one active EventSystem " +
                        "outside KitchenPrototype."
                    );

                    return;
                }

                events = candidate;
            }

            Undo.IncrementCurrentGroup();

            int group = Undo.GetCurrentGroup();

            Undo.SetCurrentGroupName("Setup Start Screen");

            // EventSystemがなければ作成。
            if (events == null)
            {
                GameObject obj = new GameObject(
                    "EventSystem",
                    typeof(EventSystem)
                );

                Undo.RegisterCreatedObjectUndo(
                    obj,
                    "Create EventSystem"
                );

                events = obj.GetComponent<EventSystem>();
            }

            Undo.RecordObject(
                events,
                "Enable EventSystem"
            );

            events.enabled = true;

            // 入力モジュールの競合を防ぎ、XR用を使用する。
            foreach (var module in
                events.GetComponents<BaseInputModule>())
            {
                Undo.RecordObject(
                    module,
                    "Configure UI input"
                );

                module.enabled = module is XRUIInputModule;

                EditorUtility.SetDirty(module);
            }

            XRUIInputModule input =
                events.GetComponent<XRUIInputModule>();

            if (input == null)
            {
                input = Undo.AddComponent<XRUIInputModule>(
                    events.gameObject
                );
            }

            Undo.RecordObject(
                input,
                "Configure XR input"
            );

            input.enabled = true;
            input.enableXRInput = true;
            input.enableMouseInput = true;
            input.enableBuiltinActionsAsFallback = true;

            EditorUtility.SetDirty(input);

            // 既存のXR Ray InteractorでUIを操作できるようにする。
            foreach (var ray in
                camera.transform.root
                    .GetComponentsInChildren<XRRayInteractor>(true))
            {
                Undo.RecordObject(
                    ray,
                    "Enable UI interaction"
                );

                ray.enableUIInteraction = true;

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(ray);
            }

            // Near-Far Interactorにも対応。
            foreach (var ray in
                camera.transform.root
                    .GetComponentsInChildren<NearFarInteractor>(true))
            {
                Undo.RecordObject(
                    ray,
                    "Enable UI interaction"
                );

                ray.enableUIInteraction = true;

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(ray);
            }

            // キッチンとは別のルートに配置する。
            GameObject root =
                new GameObject("CookingStartRoot");

            Undo.RegisterCreatedObjectUndo(
                root,
                "Create Start Screen"
            );

            CookingStart start =
                root.AddComponent<CookingStart>();

            start.kitchen = kitchen;
            start.viewCamera = camera;

            // パネル本体。
            RectTransform panel = Rect(
                "StartPanel",
                root.transform,
                Vector2.zero,
                new Vector2(760, 460)
            );

            panel.localScale =
                Vector3.one * 0.0015f;

            panel.position =
                camera.transform.position +
                camera.transform.forward * 1.4f;

            panel.rotation =
                camera.transform.rotation;

            Canvas canvas =
                panel.gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 40;

            // マウス操作用。
            panel.gameObject
                .AddComponent<GraphicRaycaster>();

            // Questのコントローラー操作用。
            panel.gameObject
                .AddComponent<TrackedDeviceGraphicRaycaster>();

            // 背景。
            Image background = Rect(
                "Background",
                panel,
                Vector2.zero,
                new Vector2(760, 460)
            ).gameObject.AddComponent<Image>();

            background.color =
                new Color(0.025f, 0.04f, 0.07f, 0.98f);

            background.raycastTarget = false;

            // 上部のゲームタイトル。
            Label(
                "Title",
                panel,
                font,
                new Vector2(0, 115),
                new Vector2(700, 110),
                64,
                "meat can fly"
            );

            // タイトルの下にSTARTボタンを配置。
            RectTransform buttonRect = Rect(
                "StartButton",
                panel,
                new Vector2(0, -70),
                new Vector2(350, 105)
            );

            Image buttonImage =
                buttonRect.gameObject.AddComponent<Image>();

            buttonImage.color =
                new Color(1f, 0.72f, 0.2f);

            Button button =
                buttonRect.gameObject.AddComponent<Button>();

            button.targetGraphic = buttonImage;

            // 指したとき・押したときに色を変える。
            ColorBlock colors = button.colors;

            colors.highlightedColor =
                new Color(1f, 1f, 0.75f);

            colors.pressedColor =
                new Color(0.7f, 0.7f, 0.7f);

            button.colors = colors;

            Text label = Label(
                "Label",
                buttonRect,
                font,
                Vector2.zero,
                new Vector2(330, 95),
                42,
                "START"
            );

            label.color =
                new Color(0.08f, 0.06f, 0.02f);

            start.startButton = button;
            start.startPanel = panel.gameObject;

            EditorUtility.SetDirty(start);

            EditorSceneManager.MarkSceneDirty(
                root.scene
            );

            Undo.CollapseUndoOperations(group);

            Selection.activeGameObject = root;

            Debug.Log(
                "START_SETUP_OK: Save the scene, " +
                "then enter Play Mode."
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
            text.text = value;
            text.color = Color.white;

            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            return text;
        }
    }
}