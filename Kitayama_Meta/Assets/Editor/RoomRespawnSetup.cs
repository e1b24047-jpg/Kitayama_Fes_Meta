using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoomRespawnSetup
{
    const string BoundsName = "RespawnRoomBounds";

    [MenuItem("Kitayama/Setup Wall And Ceiling Respawn")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        Transform background = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "CafeteriaBackground")
            {
                background = root.transform;
                break;
            }
        }

        if (background == null)
        {
            Debug.LogError(
                "CafeteriaBackgroundがあるシーンを開いてください。"
            );
            return;
        }

        if (background.Find(BoundsName) != null)
        {
            Debug.LogWarning(
                "壁・天井のリスポーン判定は配置済みです。"
            );
            return;
        }

        GameObject bounds = new GameObject(BoundsName);
        bounds.transform.SetParent(background, false);

        // 前回生成した背景の寸法：
        // 横20m、奥行き18m、天井高5m。

        AddSurface(
            bounds.transform,
            "FrontWall",
            new Vector3(0f, 2.5f, 9f),
            new Vector3(20f, 5f, 0.18f)
        );

        AddSurface(
            bounds.transform,
            "BackWall",
            new Vector3(0f, 2.5f, -9f),
            new Vector3(20f, 5f, 0.18f)
        );

        AddSurface(
            bounds.transform,
            "RightWall",
            new Vector3(10f, 2.5f, 0f),
            new Vector3(0.18f, 5f, 18f)
        );

        // 左側は窓の部分にも判定を付ける。
        AddSurface(
            bounds.transform,
            "LeftWallAndWindows",
            new Vector3(-10f, 2.5f, 0f),
            new Vector3(0.18f, 5f, 18f)
        );

        AddSurface(
            bounds.transform,
            "Ceiling",
            new Vector3(0f, 5.08f, 0f),
            new Vector3(20.2f, 0.16f, 18.2f)
        );

        Undo.RegisterCreatedObjectUndo(
            bounds,
            "Add wall and ceiling respawn"
        );

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = bounds;

        Debug.Log(
            "壁・天井の判定を追加しました。"
            + "Ctrl+Sでシーンを保存してください。"
        );
    }

    static void AddSurface(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 size)
    {
        GameObject surface = new GameObject(name);

        surface.transform.SetParent(parent, false);
        surface.transform.localPosition = position;

        // 見た目は追加せず、当たり判定だけを作る。
        BoxCollider collider =
            surface.AddComponent<BoxCollider>();

        collider.size = size;
        collider.isTrigger = false;

        // 前回のリスポーン処理が認識する目印を付ける。
        surface.AddComponent<RespawnFloor>();
    }
}