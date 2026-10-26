using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kitayama.Cooking.Editor
{
    // 既存のKitchenSetup / StoveSetupで作ったオブジェクトへ配線する。
    public static class CookingSetup
    {
        [MenuItem("Kitayama/Setup Cooking")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before setup.");
                return;
            }
            GameObject kitchen = GameObject.Find("KitchenPrototype");
            if (kitchen == null) { Debug.LogError("KitchenPrototype was not found."); return; }
            Transform meat = kitchen.transform.Find("Meat");
            Transform pan = kitchen.transform.Find("FryingPan");
            Transform stove = kitchen.transform.Find("Stove");
            if (meat == null || pan == null || stove == null)
            {
                Debug.LogError("Create the kitchen and stove first (KitchenSetup and StoveSetup).");
                return;
            }
            BoxCollider panBase = pan.Find("Base")?.GetComponent<BoxCollider>();
            Transform heatPoint = stove.Find("HeatPoint");
            XRGrabInteractable grab = meat.GetComponent<XRGrabInteractable>();
            Transform visual = meat.Find("Visual");
            if (panBase == null || heatPoint == null || grab == null || visual == null
                || meat.GetComponent<Rigidbody>() == null)
            {
                Debug.LogError("Expected FryingPan/Base, Stove/HeatPoint, Meat/Visual, Rigidbody and XRGrabInteractable.");
                return;
            }
            if (meat.GetComponent<MeatCooking>() != null || stove.Find("CookingMeter") != null)
            {
                Debug.LogWarning("Cooking is already set up. Existing settings were not changed.");
                return;
            }
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) { Debug.LogError("LegacyRuntime.ttf could not be loaded."); return; }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Cooking");
            GameObject panel = new GameObject("CookingMeter", typeof(RectTransform), typeof(Canvas));
            panel.transform.SetParent(stove, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(380f, 140f);
            panelRect.localPosition = new Vector3(0f, 0.38f, 0.22f);
            panelRect.localRotation = Quaternion.identity;
            panelRect.localScale = Vector3.one * 0.0013f;
            Canvas canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            Image background = ImageObject("Background", panel.transform,
                Vector2.zero, new Vector2(380f, 140f));
            background.color = new Color(0.025f, 0.035f, 0.055f, 0.94f);
            Text title = TextObject("Value", panel.transform, new Vector2(0, 42),
                new Vector2(360, 34), font, 25, "RAW   0 / 10");
            Image[] bars = new Image[10];
            for (int i = 0; i < bars.Length; i++)
            {
                bars[i] = ImageObject("Segment_" + i.ToString("00"), panel.transform,
                    new Vector2(-153f + i * 34f, 0), new Vector2(28, 30));
                bars[i].color = new Color(0.20f, 0.22f, 0.25f);
            }
            Text status = TextObject("Status", panel.transform, new Vector2(0, -44),
                new Vector2(365, 32), font, 18, "Put meat in the pan on the stove");
            // 表示専用なのでGraphicRaycasterやColliderは付けない。
            Undo.RegisterCreatedObjectUndo(panel, "Create Cooking Meter");

            MeatCooking cooking = Undo.AddComponent<MeatCooking>(meat.gameObject);
            cooking.panBase = panBase;
            cooking.heatPoint = heatPoint;
            cooking.meatGrab = grab;
            cooking.meatRenderers = visual.GetComponentsInChildren<Renderer>(true);
            cooking.meterSegments = bars;
            cooking.meterLabel = title;
            cooking.statusLabel = status;
            EditorUtility.SetDirty(cooking);
            EditorSceneManager.MarkSceneDirty(meat.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = meat.gameObject;
            Debug.Log("COOKING_SETUP_OK: Save the scene, then enter Play Mode.");
        }

        static RectTransform RectObject(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
        static Image ImageObject(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Image image = RectObject(name, parent, position, size).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
        static Text TextObject(string name, Transform parent, Vector2 position, Vector2 size,
            Font font, int fontSize, string value)
        {
            Text text = RectObject(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }
    }
}
