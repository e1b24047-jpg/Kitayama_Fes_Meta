using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StoveSetup
{
    private const string MaterialFolder =
        "Assets/CookingPrototype/Materials";

    [MenuItem("Kitayama/Create Stove")]
    public static void CreateStove()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playを停止してから実行してください。");
            return;
        }

        // 前回作成した、机・肉・フライパンの親を取得。
        GameObject kitchen = GameObject.Find("KitchenPrototype");

        if (kitchen == null)
        {
            Debug.LogWarning(
                "KitchenPrototypeが見つかりません。"
                + "机・肉・フライパンがあるシーンを開いてください。");
            return;
        }

        // 同じコンロの重複配置を防ぐ。
        if (kitchen.transform.Find("Stove") != null)
        {
            Debug.LogWarning("コンロは配置済みです。");
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            Debug.LogError("URP/Litシェーダーが見つかりません。");
            return;
        }

        EnsureMaterialFolder();

        Material bodyMaterial = CreateMaterial(
            "StoveBody",
            new Color(0.30f, 0.33f, 0.36f),
            shader);

        Material plateMaterial = CreateMaterial(
            "StovePlate",
            new Color(0.06f, 0.07f, 0.08f),
            shader);

        Material heatMaterial = CreateMaterial(
            "StoveHeat",
            new Color(1f, 0.30f, 0.03f),
            shader);

        Material knobMaterial = CreateMaterial(
            "StoveKnob",
            new Color(0.025f, 0.025f, 0.025f),
            shader);

        GameObject stove = new GameObject("Stove");
        stove.transform.SetParent(kitchen.transform, false);

        // 前回の机：天板の上面が床から0.85m。
        // フライパンと肉の間、机の中央に配置。
        stove.transform.localPosition =
            new Vector3(0f, 0.85f, 0.06f);

        // 本体。底面を机の上面より1mm上に置く。
        CreateBox(
            stove.transform,
            "Body",
            new Vector3(0f, 0.026f, 0f),
            new Vector3(0.36f, 0.05f, 0.34f),
            bodyMaterial,
            true);

        // フライパンを置く天板。
        // 上面はStoveの原点から0.063m。
        CreateBox(
            stove.transform,
            "CookingPlate",
            new Vector3(0f, 0.057f, 0f),
            new Vector3(0.28f, 0.012f, 0.26f),
            plateMaterial,
            true);

        // オレンジ色の輪：加熱部分を示す装飾。
        // 装飾にはColliderを付けず、接触の引っ掛かりを防ぐ。
        GameObject ring = new GameObject("BurnerRing");
        ring.transform.SetParent(stove.transform, false);

        const int segmentCount = 16;
        const float radius = 0.10f;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i * 360f / segmentCount;
            float radians = angle * Mathf.Deg2Rad;

            GameObject segment = CreateBox(
                ring.transform,
                $"Segment_{i:00}",
                new Vector3(
                    Mathf.Sin(radians) * radius,
                    0.0645f,
                    Mathf.Cos(radians) * radius),
                new Vector3(0.035f, 0.003f, 0.009f),
                heatMaterial,
                false);

            segment.transform.localRotation =
                Quaternion.Euler(0f, angle, 0f);
        }

        // プレイヤー側のつまみ。今回は見た目だけ。
        CreateBox(
            stove.transform,
            "Knob",
            new Vector3(0f, 0.027f, -0.18f),
            new Vector3(0.055f, 0.025f, 0.035f),
            knobMaterial,
            false);

        // 後で加熱範囲や距離を判定するときの基準位置。
        // 現時点では判定処理・Colliderを付けない。
        Transform heatPoint = new GameObject("HeatPoint").transform;
        heatPoint.SetParent(stove.transform, false);
        heatPoint.localPosition = new Vector3(0f, 0.064f, 0f);

        // Rigidbodyは付けず、固定式コンロにする。

        Undo.RegisterCreatedObjectUndo(stove, "Create Stove");
        EditorSceneManager.MarkSceneDirty(stove.scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = stove;

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log(
            "机の中央にコンロを配置しました。"
            + "シーンを保存してください。");
    }

    private static GameObject CreateBox(
        Transform parent,
        string objectName,
        Vector3 position,
        Vector3 scale,
        Material material,
        bool useCollider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;

        box.GetComponent<Renderer>().sharedMaterial = material;

        if (!useCollider)
        {
            Object.DestroyImmediate(box.GetComponent<BoxCollider>());
        }

        return box;
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/CookingPrototype"))
        {
            AssetDatabase.CreateFolder("Assets", "CookingPrototype");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder(
                "Assets/CookingPrototype", "Materials");
        }
    }

    private static Material CreateMaterial(
        string materialName,
        Color color,
        Shader shader)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.3f);

            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }
}