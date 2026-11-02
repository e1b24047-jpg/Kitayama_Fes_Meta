using Kitayama.Cooking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RotationScoreSetup
{
    [MenuItem("Kitayama/Setup Rotation Score")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        CookingResult result = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (result == null)
            {
                result =
                    root.GetComponentInChildren<CookingResult>(
                        true
                    );
            }
        }

        if (result == null ||
            result.cooking == null ||
            result.resultPanel == null ||
            result.cookingText == null ||
            result.heightText == null ||
            result.totalText == null)
        {
            Debug.LogError(
                "先にリザルト画面を設定してください。"
            );

            return;
        }

        RectTransform panel =
            result.resultPanel.GetComponent<RectTransform>();

        RectTransform background = panel != null
            ? panel.Find("Background") as RectTransform
            : null;

        RectTransform title = panel != null
            ? panel.Find("Title") as RectTransform
            : null;

        RectTransform retry = panel != null
            ? panel.Find("RetryButton") as RectTransform
            : null;

        if (panel == null ||
            background == null ||
            title == null)
        {
            Debug.LogError(
                "ResultPanel・Background・Titleを確認してください。"
            );

            return;
        }

        Transform existing = panel.Find("RotationText");

        Text rotationText = existing != null
            ? existing.GetComponent<Text>()
            : null;

        if (existing != null && rotationText == null)
        {
            Debug.LogError(
                "RotationTextにTextコンポーネントがありません。"
            );

            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Rotation Score");

        MeatRotationScore tracker =
            result.cooking.GetComponent<MeatRotationScore>();

        if (tracker == null)
        {
            tracker =
                Undo.AddComponent<MeatRotationScore>(
                    result.cooking.gameObject
                );
        }

        if (rotationText == null)
        {
            GameObject obj = new GameObject(
                "RotationText",
                typeof(RectTransform),
                typeof(Text)
            );

            obj.transform.SetParent(panel, false);

            rotationText = obj.GetComponent<Text>();

            rotationText.font = result.heightText.font;
            rotationText.fontSize = result.heightText.fontSize;
            rotationText.color = result.heightText.color;
            rotationText.alignment = TextAnchor.MiddleCenter;
            rotationText.raycastTarget = false;

            rotationText.text =
                "ROTATION  (0 x 180deg)     0 pts";

            Undo.RegisterCreatedObjectUndo(
                obj,
                "Create rotation score label"
            );
        }

        Undo.RecordObject(
            result,
            "Connect rotation score"
        );

        result.rotationScore = tracker;
        result.rotationText = rotationText;

        EditorUtility.SetDirty(result);

        // 1行増えるため、パネルを少し縦に広げる。
        Undo.RecordObject(
            panel,
            "Resize result panel"
        );

        panel.sizeDelta = new Vector2(680f, 620f);

        Place(background, 0, 680, 620);
        Place(title, 225, 620, 70);

        // 上から、焼き加減・高さ・回転・合計。
        Place(
            result.cookingText.rectTransform,
            130, 620, 55
        );

        Place(
            result.heightText.rectTransform,
            65, 620, 55
        );

        Place(
            rotationText.rectTransform,
            0, 620, 55
        );

        Place(
            result.totalText.rectTransform,
            -85, 620, 75
        );

        // 既存のRETRYボタンも下へ移動する。
        if (retry != null)
        {
            Place(
                retry,
                -210, 300, 85
            );
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(group);

        Selection.activeGameObject = result.gameObject;

        Debug.Log(
            "回転得点を設定しました。"
            + "Ctrl+Sでシーンを保存してください。"
        );
    }

    static void Place(
        RectTransform rect,
        float y,
        float width,
        float height)
    {
        Undo.RecordObject(
            rect,
            "Arrange result screen"
        );

        rect.anchorMin =
            rect.anchorMax =
            rect.pivot = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(width, height);

        EditorUtility.SetDirty(rect);
    }
}