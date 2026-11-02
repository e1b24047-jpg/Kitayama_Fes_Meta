using System.Collections.Generic;
using Kitayama.Cooking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class GameResetSetup
{
    [MenuItem("Kitayama/Setup Reset Button")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();

        CookingStart start = null;
        CookingResult result = null;
        CookingTimer timer = null;
        GameObject resetRoot = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (start == null)
                start = root.GetComponentInChildren<CookingStart>(true);

            if (result == null)
                result = root.GetComponentInChildren<CookingResult>(true);

            if (timer == null)
                timer = root.GetComponentInChildren<CookingTimer>(true);

            if (root.name == "CookingResetRoot")
                resetRoot = root;
        }

        if (start == null ||
            start.kitchen == null ||
            result == null ||
            result.cooking == null ||
            result.cooking.meterLabel == null ||
            result.viewCamera == null ||
            timer == null ||
            timer.timeText == null ||
            string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError(
                "スタート画面・メーター・タイマー・リザルトを"
                + "設定し、シーンを保存してから実行してください。"
            );

            return;
        }

        Canvas meterCanvas =
            result.cooking.meterLabel
                .GetComponentInParent<Canvas>(true);

        Canvas timeCanvas =
            timer.timeText.GetComponentInParent<Canvas>(true);

        RectTransform meter = meterCanvas != null
            ? meterCanvas.GetComponent<RectTransform>()
            : null;

        RectTransform time = timeCanvas != null
            ? timeCanvas.GetComponent<RectTransform>()
            : null;

        if (meter == null ||
            time == null ||
            meter.parent == null ||
            !meter.IsChildOf(start.kitchen.transform))
        {
            Debug.LogError(
                "メーターがKitchenPrototypeの下にあるか確認してください。"
            );

            return;
        }

        if (meter.parent.Find("GameResetPanel") != null)
        {
            Debug.LogWarning(
                "RESETボタンはすでに配置されています。"
            );

            return;
        }

        Font font = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );

        if (font == null)
            return;

        // メーターとタイマーの範囲を調べる。
        // その中央・上側にRESETボタンを置く。
        Vector3 minimum =
            Vector3.one * float.PositiveInfinity;

        Vector3 maximum =
            Vector3.one * float.NegativeInfinity;

        var corners = new Vector3[4];

        foreach (RectTransform display in new[] { meter, time })
        {
            display.GetWorldCorners(corners);

            foreach (Vector3 corner in corners)
            {
                Vector3 local =
                    meter.InverseTransformPoint(corner);

                minimum = Vector3.Min(minimum, local);
                maximum = Vector3.Max(maximum, local);
            }
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Reset Button");

        // プレイ中に表示するワールド空間のCanvas。
        GameObject panel = new GameObject(
            "GameResetPanel",
            typeof(RectTransform),
            typeof(Canvas)
        );

        RectTransform panelRect =
            panel.GetComponent<RectTransform>();

        panelRect.SetParent(meter.parent, false);
        panelRect.localRotation = meter.localRotation;
        panelRect.localScale = meter.localScale;

        panelRect.sizeDelta = new Vector2(260f, 74f);

        panelRect.position = meter.TransformPoint(
            new Vector3(
                (minimum.x + maximum.x) * 0.5f,
                maximum.y + 25f + 37f,
                -5f
            )
        );

        Canvas canvas = panel.GetComponent<Canvas>();

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = result.viewCamera;
        canvas.sortingOrder = meterCanvas.sortingOrder + 1;

        // マウスとQuestのUI操作に対応。
        panel.AddComponent<GraphicRaycaster>();
        panel.AddComponent<TrackedDeviceGraphicRaycaster>();

        // 赤いRESETボタン。
        GameObject obj = new GameObject(
            "ResetButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        obj.transform.SetParent(panelRect, false);
        Stretch(obj.GetComponent<RectTransform>());

        Image image = obj.GetComponent<Image>();

        image.color =
            new Color(0.65f, 0.13f, 0.1f, 1f);

        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;

        colors.highlightedColor =
            new Color(1f, 0.8f, 0.65f);

        colors.pressedColor =
            new Color(0.65f, 0.65f, 0.65f);

        button.colors = colors;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        // ボタンの文字。
        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(Text)
        );

        labelObject.transform.SetParent(
            obj.transform,
            false
        );

        Stretch(labelObject.GetComponent<RectTransform>());

        Text label = labelObject.GetComponent<Text>();

        label.font = font;
        label.fontSize = 32;
        label.text = "RESET";
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(
            panel,
            "Create reset display"
        );

        // 読み直し処理はキッチンの外に置く。
        // キッチンが非表示になっても処理を継続できる。
        if (resetRoot == null)
        {
            resetRoot = new GameObject("CookingResetRoot");

            Undo.RegisterCreatedObjectUndo(
                resetRoot,
                "Create reset controller"
            );
        }

        RetryToStart reset =
            resetRoot.GetComponent<RetryToStart>();

        if (reset == null)
        {
            reset = Undo.AddComponent<RetryToStart>(
                resetRoot
            );
        }

        Undo.RecordObject(
            reset,
            "Connect reset button"
        );

        // リザルトのRETRYとは別のインスタンスを使用する。
        reset.retryButton = button;

        EditorUtility.SetDirty(reset);

        // シーンを再読込できるよう登録する。
        // すでに登録されている他のシーンは残す。
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
            scenes[index] = new EditorBuildSettingsScene(
                scene.path,
                true
            );
        }

        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(group);

        Selection.activeGameObject = panel;

        Debug.Log(
            "RESETボタンを作成しました。"
            + "Ctrl+Sでシーンを保存してください。"
            + "Build ProfileでScene Listを上書きしている場合は、"
            + "そちらにも現在のシーンを追加してください。"
        );
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}