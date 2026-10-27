using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kitayama.Cooking.Editor
{
    public static class CatchEffectSetup
    {
        [MenuItem("Kitayama/Setup Catch Effect")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            GameObject kitchen =
                GameObject.Find("KitchenPrototype");

            Transform pan = kitchen != null
                ? kitchen.transform.Find("FryingPan")
                : null;

            Transform meat = kitchen != null
                ? kitchen.transform.Find("Meat")
                : null;

            MeatCooking cooking = meat != null
                ? meat.GetComponent<MeatCooking>()
                : null;

            if (pan == null ||
                cooking == null ||
                cooking.panBase == null)
            {
                Debug.LogError(
                    "Set up the cooking and throw steps first."
                );

                return;
            }

            Shader shader = Shader.Find(
                "Universal Render Pipeline/Particles/Unlit"
            );

            if (shader == null)
            {
                Debug.LogError(
                    "URP particle shader was not found."
                );

                return;
            }

            const string folder = "Assets/KitayamaCatchEffect";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(
                    "Assets",
                    "KitayamaCatchEffect"
                );
            }

            // 丸い粒の画像を自動生成する。
            string texturePath = folder + "/CatchDot.asset";

            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    texturePath
                );

            if (texture == null)
            {
                texture = new Texture2D(
                    32,
                    32,
                    TextureFormat.RGBA32,
                    false
                );

                texture.name = "CatchDot";
                texture.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float radius = new Vector2(
                            (x - 15.5f) / 15.5f,
                            (y - 15.5f) / 15.5f
                        ).magnitude;

                        float alpha = Mathf.Clamp01(
                            (1f - radius) * 3f
                        );

                        texture.SetPixel(
                            x,
                            y,
                            new Color(1f, 1f, 1f, alpha)
                        );
                    }
                }

                texture.Apply();

                AssetDatabase.CreateAsset(
                    texture,
                    texturePath
                );
            }

            // エフェクト用のマテリアルを作成する。
            string materialPath = folder + "/CatchSpark.mat";

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

            // フライパンにエフェクトのスクリプトを追加する。
            PanCatchEffect effect =
                pan.GetComponent<PanCatchEffect>();

            if (effect == null)
            {
                effect = Undo.AddComponent<PanCatchEffect>(
                    pan.gameObject
                );
            }

            Undo.RecordObject(
                effect,
                "Setup Catch Effect"
            );

            effect.cooking = cooking;
            effect.effectMaterial = material;

            EditorUtility.SetDirty(effect);
            AssetDatabase.SaveAssets();

            EditorSceneManager.MarkSceneDirty(
                pan.gameObject.scene
            );

            Selection.activeGameObject = pan.gameObject;

            Debug.Log(
                "CATCH_EFFECT_SETUP_OK: Save the scene."
            );
        }
    }
}