using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CafeteriaBackgroundSetup
{
    const string RootName = "CafeteriaBackground";
    const string Folder = "Assets/CafeteriaBackgroundGenerated";

    [MenuItem("Kitayama/Setup Cafeteria Background")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        Transform kitchen = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == RootName)
            {
                Debug.LogWarning("背景はすでに配置されています。");
                return;
            }

            if (root.name == "KitchenPrototype")
                kitchen = root.transform;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (kitchen == null || shader == null)
        {
            Debug.LogError(
                "KitchenPrototypeがあるシーンを開いてください。"
                + "URP/Litシェーダーも必要です。"
            );
            return;
        }

        if (Vector3.Distance(
                kitchen.lossyScale, Vector3.one) > 0.001f ||
            Vector3.Dot(
                kitchen.up, Vector3.up) < 0.999f)
        {
            Debug.LogError(
                "KitchenPrototypeのScaleを(1,1,1)にし、"
                + "X・Z方向に傾けないでください。"
            );
            return;
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "CafeteriaBackgroundGenerated"
            );
        }

        GameObject background = new GameObject(RootName);

        // 前回作成したカウンターのプレイヤー開始位置を基準にする。
        background.transform.SetPositionAndRotation(
            kitchen.TransformPoint(new Vector3(0, 0, -0.85f)),
            kitchen.rotation
        );

        Undo.RegisterCreatedObjectUndo(
            background,
            "Create cafeteria background"
        );

        // Unity標準のCubeの形状を取得する。
        GameObject sample =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        Mesh cube =
            sample.GetComponent<MeshFilter>().sharedMesh;

        Object.DestroyImmediate(sample);

        var groups =
            new Dictionary<Material, List<CombineInstance>>();

        Material floor = Mat(
            "Floor", shader,
            new Color(.72f, .72f, .69f)
        );

        Material teal = Mat(
            "TealTile", shader,
            new Color(.39f, .56f, .56f)
        );

        Material grout = Mat(
            "Grout", shader,
            new Color(.49f, .50f, .48f)
        );

        Material white = Mat(
            "Wall", shader,
            new Color(.87f, .86f, .81f)
        );

        Material frame = Mat(
            "WindowFrame", shader,
            new Color(.69f, .73f, .73f)
        );

        Material wood = Mat(
            "TableTop", shader,
            new Color(.68f, .53f, .35f)
        );

        Material dark = Mat(
            "Chair", shader,
            new Color(.12f, .16f, .17f)
        );

        Material sky = Mat(
            "Sky", shader,
            new Color(.55f, .76f, .89f),
            true
        );

        Material green = Mat(
            "Trees", shader,
            new Color(.34f, .54f, .29f)
        );

        Material light = Mat(
            "CeilingLight", shader,
            new Color(.96f, .97f, .91f),
            true
        );

        // 各パーツを材質ごとに記録し、最後にまとめて生成する。
        void Box(
            Material material,
            Vector3 position,
            Vector3 size,
            float yaw = 0)
        {
            if (!groups.TryGetValue(material, out var list))
            {
                list = new List<CombineInstance>();
                groups.Add(material, list);
            }

            list.Add(new CombineInstance
            {
                mesh = cube,
                transform = Matrix4x4.TRS(
                    position,
                    Quaternion.Euler(0, yaw, 0),
                    size
                )
            });
        }

        // 床：横20m、奥行き18m。
        // 既存のPlaneと重なってちらつかないよう、少し上に配置。
        Box(
            grout,
            new Vector3(0, -.025f, 0),
            new Vector3(20, .05f, 18)
        );

        for (int x = 0; x < 20; x++)
        {
            for (int z = 0; z < 18; z++)
            {
                Material tile =
                    x % 4 == 1 && z % 4 == 2
                    ? teal
                    : floor;

                Box(
                    tile,
                    new Vector3(x - 9.5f, .003f, z - 8.5f),
                    new Vector3(.99f, .006f, .99f)
                );
            }
        }

        // 正面・背面・右側の壁。
        Box(
            white,
            new Vector3(0, 2.5f, 9),
            new Vector3(20, 5, .18f)
        );

        Box(
            white,
            new Vector3(0, 2.5f, -9),
            new Vector3(20, 5, .18f)
        );

        Box(
            white,
            new Vector3(10, 2.5f, 0),
            new Vector3(.18f, 5, 18)
        );

        // 天井は床から5m。
        Box(
            white,
            new Vector3(0, 5.08f, 0),
            new Vector3(20.2f, .16f, 18.2f)
        );

        // 左側の壁は、中央を窓として開けておく。
        Box(
            white,
            new Vector3(-10, .5f, 0),
            new Vector3(.18f, 1, 18)
        );

        Box(
            white,
            new Vector3(-10, 4.65f, 0),
            new Vector3(.18f, .7f, 18)
        );

        // 窓の外の空と地面。
        // 透明なガラスは使わず、軽い背景として表現する。
        Box(
            sky,
            new Vector3(-12, 2.65f, 0),
            new Vector3(.1f, 3.3f, 18)
        );

        Box(
            green,
            new Vector3(-11, .6f, 0),
            new Vector3(2, .1f, 18)
        );

        // 窓の外の簡単な木。
        for (int i = 0; i < 7; i++)
        {
            float z = -7.5f + i * 2.5f;

            Box(
                wood,
                new Vector3(-11.3f, 1.1f, z),
                new Vector3(.18f, 1.2f, .18f)
            );

            Box(
                green,
                new Vector3(
                    -11.3f,
                    2.1f + (i % 2) * .25f,
                    z
                ),
                new Vector3(.8f, 1.4f, 1.65f),
                15f
            );
        }

        // 窓の縦枠。
        for (int i = 0; i <= 6; i++)
        {
            Box(
                frame,
                new Vector3(-9.9f, 2.65f, -9 + i * 3),
                new Vector3(.16f, 3.3f, .09f)
            );
        }

        // 窓の横枠。
        foreach (float y in new[] { 1f, 3.5f, 4.3f })
        {
            Box(
                frame,
                new Vector3(-9.9f, y, 0),
                new Vector3(.16f, .08f, 18)
            );
        }

        // 柱はカウンターから離して配置する。
        foreach (float x in new[] { -7f, 7f })
        {
            foreach (float z in new[] { -5.5f, 5.5f })
            {
                Box(
                    white,
                    new Vector3(x, 2.5f, z),
                    new Vector3(.55f, 5, .55f)
                );
            }
        }

        // 天井の照明パネル。
        // 発光する見た目だけで、リアルタイムライトは追加しない。
        foreach (float x in new[] { -6f, 0f, 6f })
        {
            foreach (float z in new[] { -5f, 1f, 6f })
            {
                Box(
                    light,
                    new Vector3(x, 4.98f, z),
                    new Vector3(1.5f, .025f, .7f)
                );
            }
        }

        // 椅子。
        void Chair(Vector3 p, float yaw)
        {
            Quaternion r = Quaternion.Euler(0, yaw, 0);

            Box(
                dark,
                p + Vector3.up * .45f,
                new Vector3(.43f, .065f, .43f),
                yaw
            );

            Box(
                dark,
                p + r * new Vector3(0, .73f, -.19f),
                new Vector3(.43f, .5f, .055f),
                yaw
            );

            foreach (float x in new[] { -.17f, .17f })
            {
                foreach (float z in new[] { -.17f, .17f })
                {
                    Box(
                        dark,
                        p + r * new Vector3(x, .22f, z),
                        new Vector3(.035f, .44f, .035f),
                        yaw
                    );
                }
            }
        }

        // 机1台と椅子4脚。
        void DiningSet(Vector3 p)
        {
            Box(
                wood,
                p + Vector3.up * .76f,
                new Vector3(1.6f, .07f, .85f)
            );

            foreach (float x in new[] { -.65f, .65f })
            {
                foreach (float z in new[] { -.30f, .30f })
                {
                    Box(
                        dark,
                        p + new Vector3(x, .36f, z),
                        new Vector3(.055f, .72f, .055f)
                    );
                }
            }

            foreach (float x in new[] { -.45f, .45f })
            {
                Chair(
                    p + new Vector3(x, 0, -.72f),
                    0
                );

                Chair(
                    p + new Vector3(x, 0, .72f),
                    180
                );
            }
        }

        DiningSet(new Vector3(-5.2f, .006f, 4));
        DiningSet(new Vector3(0, .006f, 6.6f));
        DiningSet(new Vector3(5.2f, .006f, 4.7f));

        // 奥の白い仕切り。
        for (int i = 0; i < 3; i++)
        {
            Vector3 p =
                new Vector3(6 + i * 1.05f, 0, 7.5f);

            Box(
                frame,
                p + Vector3.up * 1.05f,
                new Vector3(1.04f, 2.1f, .08f)
            );

            Box(
                white,
                p + new Vector3(0, 1.08f, -.05f),
                new Vector3(.95f, 1.95f, .04f)
            );
        }

        // 同じ材質の形状をまとめ、Rendererの数を減らす。
        foreach (var pair in groups)
        {
            Mesh mesh = new Mesh
            {
                name = pair.Key.name,
                indexFormat = IndexFormat.UInt32
            };

            mesh.CombineMeshes(
                pair.Value.ToArray(),
                true,
                true
            );

            mesh.RecalculateBounds();

            // シーンの再読込・Questへのビルド後も形状を保持する。
            AssetDatabase.CreateAsset(
                mesh,
                AssetDatabase.GenerateUniqueAssetPath(
                    Folder + "/" + pair.Key.name + ".asset"
                )
            );

            GameObject part = new GameObject(pair.Key.name);
            part.transform.SetParent(background.transform, false);

            part.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer =
                part.AddComponent<MeshRenderer>();

            renderer.sharedMaterial = pair.Key;

            // 天井や壁で既存のDirectional Lightを遮らない。
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        // 床の当たり判定。
        GameObject collision = new GameObject("FloorCollision");
        collision.transform.SetParent(background.transform, false);

        BoxCollider floorCollider =
            collision.AddComponent<BoxCollider>();

        floorCollider.center =
            new Vector3(0, -.044f, 0);

        floorCollider.size =
            new Vector3(20, .1f, 18);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);

        Selection.activeGameObject = background;

        Debug.Log(
            "食堂の背景を作成しました。"
            + "Ctrl+Sでシーンを保存してください。"
        );
    }

    static Material Mat(
        string name,
        Shader shader,
        Color color,
        bool emission = false)
    {
        string path = Folder + "/" + name + ".mat";

        Material mat =
            AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
            return mat;

        mat = new Material(shader) { name = name };

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", .12f);

        if (emission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * .7f);
        }

        AssetDatabase.CreateAsset(mat, path);

        return mat;
    }
}