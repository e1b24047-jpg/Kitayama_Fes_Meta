using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.XR.CoreUtils;

public static class CafeteriaStageSetup
{
    const string StageName = "RoundedCafeteriaCounter";
    const string Folder = "Assets/CafeteriaStageGenerated";

    [MenuItem("Kitayama/Setup Rounded Cafeteria")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject kitchen =
            GameObject.Find("KitchenPrototype");

        XROrigin rig =
            Object.FindFirstObjectByType<XROrigin>();

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (kitchen == null ||
            rig == null ||
            rig.Camera == null ||
            shader == null)
        {
            Debug.LogError(
                "KitchenPrototype、Camera付きのXR Origin、"
                + "URP/Litシェーダーが必要です。"
            );

            return;
        }

        if (kitchen.transform.Find(StageName) != null)
        {
            Debug.LogWarning(
                "丸いカウンターは配置済みです。"
            );

            return;
        }

        if (Vector3.Distance(
                kitchen.transform.lossyScale,
                Vector3.one) > 0.001f ||
            Vector3.Dot(
                kitchen.transform.up,
                Vector3.up) < 0.999f)
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
                "CafeteriaStageGenerated"
            );
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Rounded Cafeteria");

        GameObject stage = new GameObject(StageName);
        stage.transform.SetParent(kitchen.transform, false);

        Undo.RegisterCreatedObjectUndo(
            stage,
            "Create rounded counter"
        );

        // カウンター中心線。
        // Vector2のxは横方向、yは奥行き方向として使う。
        var path = new List<Vector2>();

        // 左側の入口から正面へ。
        path.Add(new Vector2(-1.35f, -1.30f));

        AddArc(
            path,
            new Vector2(-0.80f, -0.55f),
            0.55f,
            180f,
            90f
        );

        // 横に長い正面と、右側の曲線。
        AddArc(
            path,
            new Vector2(0.80f, -0.55f),
            0.55f,
            90f,
            0f
        );

        path.Add(new Vector2(1.35f, -1.30f));

        Material wood = MakeMaterial(
            "DarkWood",
            shader,
            new Color(0.19f, 0.115f, 0.09f),
            0f,
            0.25f
        );

        Material rim = MakeMaterial(
            "Ivory",
            shader,
            new Color(0.88f, 0.85f, 0.76f),
            0f,
            0.3f
        );

        Material steel = MakeMaterial(
            "Steel",
            shader,
            new Color(0.55f, 0.58f, 0.60f),
            0.65f,
            0.4f
        );

        // 茶色の本体。
        AddLayer(
            stage.transform,
            "Cabinet",
            path,
            0.31f,
            0.04f,
            0.77f,
            wood,
            true
        );

        // 白い天板の縁。
        AddLayer(
            stage.transform,
            "WhiteRim",
            path,
            0.35f,
            0.77f,
            0.845f,
            rim,
            true
        );

        // 銀色の作業面。上面は元の机と同じ85cm。
        AddLayer(
            stage.transform,
            "SteelWorktop",
            path,
            0.285f,
            0.845f,
            0.85f,
            steel,
            true
        );

        // 元の机だけを非表示にする。
        // 肉、フライパン、コンロなどの参照は維持する。
        Transform oldTable =
            kitchen.transform.Find("KitchenTable");

        if (oldTable != null)
        {
            Undo.RecordObject(
                oldTable.gameObject,
                "Hide old table"
            );

            oldTable.gameObject.SetActive(false);
        }

        CafeteriaPlayerStart start =
            kitchen.GetComponent<CafeteriaPlayerStart>();

        if (start == null)
        {
            start =
                Undo.AddComponent<CafeteriaPlayerStart>(
                    kitchen
                );
        }

        Undo.RecordObject(start, "Set player start");

        start.xrOrigin = rig;
        start.localStandingPosition =
            new Vector3(0f, 0f, -0.85f);

        EditorUtility.SetDirty(start);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(kitchen.scene);

        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = stage;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log(
            "カウンターを作成しました。"
            + "Ctrl+Sでシーンを保存してください。"
        );
    }

    static void AddArc(
        List<Vector2> path,
        Vector2 center,
        float radius,
        float from,
        float to)
    {
        for (int i = 0; i <= 24; i++)
        {
            float angle =
                Mathf.Lerp(from, to, i / 24f)
                * Mathf.Deg2Rad;

            path.Add(
                center
                + new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * radius
            );
        }
    }

    static Material MakeMaterial(
        string name,
        Shader shader,
        Color color,
        float metallic,
        float smoothness)
    {
        string path = Folder + "/" + name + ".mat";

        Material mat =
            AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
            return mat;

        mat = new Material(shader);

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);

        AssetDatabase.CreateAsset(mat, path);

        return mat;
    }

    static void AddLayer(
        Transform parent,
        string name,
        List<Vector2> path,
        float radius,
        float bottom,
        float top,
        Material mat,
        bool collision)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var left = new List<Vector2>();
        var right = new List<Vector2>();

        // 中心線から左右の輪郭を作る。
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 tangent;

            if (i == 0)
            {
                tangent = path[1] - path[0];
            }
            else if (i == path.Count - 1)
            {
                tangent = path[i] - path[i - 1];
            }
            else
            {
                tangent =
                    (path[i] - path[i - 1]).normalized
                    + (path[i + 1] - path[i]).normalized;
            }

            tangent.Normalize();

            Vector2 normal =
                new Vector2(-tangent.y, tangent.x);

            left.Add(path[i] + normal * radius);
            right.Add(path[i] - normal * radius);
        }

        // 上面、下面、側面を作る。
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 a = left[i];
            Vector2 b = left[i + 1];
            Vector2 c = right[i + 1];
            Vector2 d = right[i];

            Quad(
                vertices, triangles,
                V(a, top), V(b, top),
                V(c, top), V(d, top)
            );

            Quad(
                vertices, triangles,
                V(d, bottom), V(c, bottom),
                V(b, bottom), V(a, bottom)
            );

            Wall(
                vertices, triangles,
                a, b, bottom, top
            );

            Wall(
                vertices, triangles,
                c, d, bottom, top
            );
        }

        // 入口側の端も半円形にする。
        Cap(
            vertices, triangles,
            path[0], left[0], right[0],
            bottom, top
        );

        int last = path.Count - 1;

        Cap(
            vertices, triangles,
            path[last], right[last], left[last],
            bottom, top
        );

        Mesh mesh = new Mesh { name = name };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 保存後やQuestへのビルド後にも形状を保持する。
        AssetDatabase.CreateAsset(
            mesh,
            AssetDatabase.GenerateUniqueAssetPath(
                Folder + "/" + name + ".asset"
            )
        );

        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);

        part.AddComponent<MeshFilter>().sharedMesh = mesh;

        part.AddComponent<MeshRenderer>()
            .sharedMaterial = mat;

        if (collision)
        {
            part.AddComponent<MeshCollider>()
                .sharedMesh = mesh;
        }
    }

    static Vector3 V(Vector2 point, float height)
    {
        return new Vector3(point.x, height, point.y);
    }

    static void Quad(
        List<Vector3> v,
        List<int> t,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int n = v.Count;

        v.Add(a);
        v.Add(b);
        v.Add(c);
        v.Add(d);

        t.Add(n);
        t.Add(n + 1);
        t.Add(n + 2);

        t.Add(n);
        t.Add(n + 2);
        t.Add(n + 3);
    }

    static void Wall(
        List<Vector3> v,
        List<int> t,
        Vector2 a,
        Vector2 b,
        float bottom,
        float top)
    {
        Quad(
            v, t,
            V(a, bottom),
            V(b, bottom),
            V(b, top),
            V(a, top)
        );
    }

    static void Cap(
        List<Vector3> v,
        List<int> t,
        Vector2 center,
        Vector2 start,
        Vector2 end,
        float bottom,
        float top)
    {
        Vector2 delta = start - center;

        float angle =
            Mathf.Atan2(delta.y, delta.x);

        Vector2 previous = start;

        for (int i = 1; i <= 24; i++)
        {
            float a = angle + Mathf.PI * i / 24f;

            Vector2 next = i == 24
                ? end
                : center + new Vector2(
                    Mathf.Cos(a),
                    Mathf.Sin(a)
                ) * delta.magnitude;

            Triangle(
                v, t,
                V(center, top),
                V(next, top),
                V(previous, top)
            );

            Triangle(
                v, t,
                V(center, bottom),
                V(previous, bottom),
                V(next, bottom)
            );

            Wall(
                v, t,
                next, previous,
                bottom, top
            );

            previous = next;
        }
    }

    static void Triangle(
        List<Vector3> v,
        List<int> t,
        Vector3 a,
        Vector3 b,
        Vector3 c)
    {
        int n = v.Count;

        v.Add(a);
        v.Add(b);
        v.Add(c);

        t.Add(n);
        t.Add(n + 1);
        t.Add(n + 2);
    }
}
