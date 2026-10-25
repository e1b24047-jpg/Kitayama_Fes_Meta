using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class KitchenSetup
{
    private const string RootName = "KitchenPrototype";
    private const string MaterialFolder =
        "Assets/CookingPrototype/Materials";

    // Unity上部のメニューに項目を追加する。
    [MenuItem("Kitayama/Create Kitchen Objects")]
    public static void CreateKitchen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playを停止してから実行してください。");
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            Debug.LogWarning(
                "KitchenPrototypeは配置済みです。"
                + "作り直す場合は、Hierarchyから削除してください。");
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            Debug.LogError("URP/Litシェーダーが見つかりません。");
            return;
        }

        EnsureMaterialFolder();

        Material wood = CreateMaterial(
            "Worktop", new Color(0.57f, 0.35f, 0.18f), shader);

        Material metal = CreateMaterial(
            "Metal", new Color(0.25f, 0.29f, 0.32f), shader);

        Material panMaterial = CreateMaterial(
            "Pan", new Color(0.07f, 0.08f, 0.10f), shader);

        Material handleMaterial = CreateMaterial(
            "Handle", new Color(0.035f, 0.04f, 0.045f), shader);

        Material meatMaterial = CreateMaterial(
            "RawMeat", new Color(0.75f, 0.15f, 0.20f), shader);

        GameObject root = new GameObject(RootName);

        // XR Originが原点にあり、前方が+Zの配置を想定。
        root.transform.position = new Vector3(0f, 0f, 0.85f);

        CreateTable(root.transform, wood, metal);
        CreatePan(root.transform, panMaterial, handleMaterial);
        CreateMeat(root.transform, meatMaterial);

        // 生成したオブジェクトをまとめてUndoできるようにする。
        Undo.RegisterCreatedObjectUndo(root, "Create Kitchen Objects");

        EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = root;

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("机・フライパン・肉を配置しました。シーンを保存してください。");
    }

    private static void CreateTable(
        Transform parent, Material wood, Material metal)
    {
        GameObject table = new GameObject("KitchenTable");
        table.transform.SetParent(parent, false);

        // 幅1.6m、奥行0.75m。天板の上面は床から0.85m。
        CreateBox(
            table.transform, "Worktop",
            new Vector3(0f, 0.81f, 0f),
            new Vector3(1.6f, 0.08f, 0.75f),
            wood);

        // 机の脚を4本作る。
        foreach (float x in new[] { -0.68f, 0.68f })
        {
            foreach (float z in new[] { -0.25f, 0.25f })
            {
                CreateBox(
                    table.transform, "Leg",
                    new Vector3(x, 0.385f, z),
                    new Vector3(0.065f, 0.77f, 0.065f),
                    metal);
            }
        }

        // Rigidbodyを付けないため、机は固定される。
    }

    private static void CreatePan(
        Transform parent, Material panMaterial, Material handleMaterial)
    {
        GameObject pan = new GameObject("FryingPan");
        pan.transform.SetParent(parent, false);

        // プレイヤーから見て机の左側。
        pan.transform.localPosition =
            new Vector3(-0.30f, 0.861f, 0.07f);

        // 底と側壁を別々のColliderにすることで、
        // 内側に肉を置ける空間を作る。
        CreateBox(
            pan.transform, "Base",
            Vector3.zero,
            new Vector3(0.30f, 0.018f, 0.30f),
            panMaterial);

        CreateBox(
            pan.transform, "LeftWall",
            new Vector3(-0.147f, 0.035f, 0f),
            new Vector3(0.018f, 0.07f, 0.30f),
            panMaterial);

        CreateBox(
            pan.transform, "RightWall",
            new Vector3(0.147f, 0.035f, 0f),
            new Vector3(0.018f, 0.07f, 0.30f),
            panMaterial);

        CreateBox(
            pan.transform, "BackWall",
            new Vector3(0f, 0.035f, 0.147f),
            new Vector3(0.276f, 0.07f, 0.018f),
            panMaterial);

        CreateBox(
            pan.transform, "FrontWall",
            new Vector3(0f, 0.035f, -0.147f),
            new Vector3(0.276f, 0.07f, 0.018f),
            panMaterial);

        // 持ち手はプレイヤー側に伸ばす。
        CreateBox(
            pan.transform, "Handle",
            new Vector3(0f, 0.025f, -0.285f),
            new Vector3(0.045f, 0.035f, 0.26f),
            handleMaterial);

        Transform attach = new GameObject("HandleAttach").transform;
        attach.SetParent(pan.transform, false);
        attach.localPosition = new Vector3(0f, 0.025f, -0.30f);

        XRGrabInteractable grab = AddGrabSettings(pan, 0.7f);

        // つかむと持ち手が手元に来るようにする。
        grab.attachTransform = attach;
        grab.useDynamicAttach = false;
    }

    private static void CreateMeat(
        Transform parent, Material meatMaterial)
    {
        GameObject meat = new GameObject("Meat");
        meat.transform.SetParent(parent, false);

        // プレイヤーから見て机の右側。
        meat.transform.localPosition =
            new Vector3(0.30f, 0.87f, 0f);

        // 見た目を分離し、後からモデルを差し替えやすくする。
        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(meat.transform, false);

        GameObject mesh = CreateBox(
            visual, "PlaceholderMesh",
            Vector3.zero,
            new Vector3(0.12f, 0.035f, 0.09f),
            meatMaterial);

        // 肉の当たり判定はルート側の1個だけにする。
        Object.DestroyImmediate(mesh.GetComponent<BoxCollider>());

        BoxCollider collider = meat.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.12f, 0.035f, 0.09f);

        XRGrabInteractable grab = AddGrabSettings(meat, 0.18f);

        // 触れた位置を基準につかむ。
        grab.useDynamicAttach = true;
    }

    private static XRGrabInteractable AddGrabSettings(
        GameObject target, float mass)
    {
        Rigidbody body = target.AddComponent<Rigidbody>();
        body.mass = mass;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        XRGrabInteractable grab =
            target.AddComponent<XRGrabInteractable>();

        // Rigidbodyの速度で追従させ、物理的な接触を扱う。
        grab.movementType =
            XRBaseInteractable.MovementType.VelocityTracking;

        grab.throwOnDetach = true;
        grab.throwVelocityScale = 1f;
        grab.throwAngularVelocityScale = 1f;

        return grab;
    }

    private static GameObject CreateBox(
        Transform parent,
        string objectName,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;

        // CreatePrimitiveが作成したBoxColliderをそのまま使う。
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
        string materialName, Color color, Shader shader)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

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
