using Kitayama.Cooking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameAudioSetup
{
    [MenuItem("Kitayama/Setup Game Audio")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();

        CookingStart start = null;
        MeatCooking meat = null;
        CookingResult result = null;
        CookingGameAudio audio = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (start == null)
            {
                start =
                    root.GetComponentInChildren<CookingStart>(
                        true
                    );
            }

            if (meat == null)
            {
                meat =
                    root.GetComponentInChildren<MeatCooking>(
                        true
                    );
            }

            if (result == null)
            {
                result =
                    root.GetComponentInChildren<CookingResult>(
                        true
                    );
            }

            if (audio == null)
            {
                audio =
                    root.GetComponentInChildren<CookingGameAudio>(
                        true
                    );
            }
        }

        if (start == null ||
            meat == null ||
            result == null ||
            start.startButton == null ||
            start.startPanel == null ||
            start.kitchen == null ||
            result.resultPanel == null)
        {
            Debug.LogError(
                "スタート画面・調理処理・リザルト画面を"
                + "先に設定してください。"
            );

            return;
        }

        // ProjectでMP3を選択してから実行した場合、
        // その音声をBGMとして設定する。
        AudioClip selectedBgm =
            Selection.activeObject as AudioClip;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Game Audio");

        if (audio == null)
        {
            // キッチンが非表示になっても動作するように、
            // 独立したオブジェクトとして作る。
            GameObject root =
                new GameObject("CookingAudioRoot");

            Undo.RegisterCreatedObjectUndo(
                root,
                "Create audio controller"
            );

            audio =
                Undo.AddComponent<CookingGameAudio>(root);
        }

        Undo.RecordObject(audio, "Configure audio");

        audio.startScreen = start;
        audio.cooking = meat;
        audio.result = result;

        if (selectedBgm != null)
            audio.startBgm = selectedBgm;

        EditorUtility.SetDirty(audio);
        EditorSceneManager.MarkSceneDirty(scene);

        Undo.CollapseUndoOperations(group);

        Selection.activeGameObject = audio.gameObject;

        Debug.Log(
            "ゲーム音を設定しました。"
            + "Start Bgmを確認し、"
            + "Ctrl+Sでシーンを保存してください。"
        );
    }
}