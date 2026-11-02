using Kitayama.Cooking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CookingSmokeSetup
{
    const string Folder = "Assets/CookingSmokeGenerated";

    [MenuItem("Kitayama/Setup Cooking Smoke")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();

        MeatCooking meat = null;
        CookingResult result = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
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
        }

        Shader shader = Shader.Find(
            "Universal Render Pipeline/Particles/Unlit"
        );

        if (meat == null || result == null || shader == null)
        {
            Debug.LogError(
                "調理処理・リザルト処理・"
                + "URPのパーティクル用シェーダーが必要です。"
            );

            return;
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "CookingSmokeGenerated"
            );
        }

        // 周囲がぼやけた、柔らかい煙の画像を作る。
        string texturePath = Folder + "/SoftSmoke.asset";

        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                texturePath
            );

        if (texture == null)
        {
            const int size = 64;

            texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

            texture.name = "SoftSmoke";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx =
                        x / (size - 1f) * 2f - 1f;

                    float dy =
                        y / (size - 1f) * 2f - 1f;

                    float alpha = Mathf.Pow(
                        Mathf.Clamp01(
                            1f - dx * dx - dy * dy
                        ),
                        3f
                    );

                    pixels[y * size + x] =
                        new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            AssetDatabase.CreateAsset(
                texture,
                texturePath
            );
        }

        // 透明な煙用マテリアルを作る。
        string materialPath = Folder + "/SmallSmoke.mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                materialPath
            );

        if (material == null)
        {
            material = new Material(shader);

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);

            material.SetInt(
                "_SrcBlend",
                (int)BlendMode.SrcAlpha
            );

            material.SetInt(
                "_DstBlend",
                (int)BlendMode.OneMinusSrcAlpha
            );

            material.SetInt(
                "_SrcBlendAlpha",
                (int)BlendMode.One
            );

            material.SetInt(
                "_DstBlendAlpha",
                (int)BlendMode.OneMinusSrcAlpha
            );

            material.SetInt("_ZWrite", 0);
            material.SetInt("_Cull", (int)CullMode.Off);

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT"
            );

            material.SetOverrideTag(
                "RenderType",
                "Transparent"
            );

            material.renderQueue =
                (int)RenderQueue.Transparent;

            AssetDatabase.CreateAsset(
                material,
                materialPath
            );
        }

        // 肉へスクリプトを付け、参照を設定する。
        MeatSmoke smoke = meat.GetComponent<MeatSmoke>();

        if (smoke == null)
        {
            smoke = Undo.AddComponent<MeatSmoke>(
                meat.gameObject
            );
        }

        Undo.RecordObject(
            smoke,
            "Configure cooking smoke"
        );

        smoke.cooking = meat;
        smoke.result = result;
        smoke.smokeMaterial = material;

        EditorUtility.SetDirty(smoke);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);

        Selection.activeGameObject = meat.gameObject;

        Debug.Log(
            "小さな煙を設定しました。"
            + "Ctrl+Sでシーンを保存してください。"
        );
    }
}