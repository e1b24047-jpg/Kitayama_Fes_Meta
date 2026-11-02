using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class PanStabilitySetup
{
    const string Folder = "Assets/PanStabilityGenerated";

    [MenuItem("Kitayama/Fix Pan Pickup")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        Transform kitchen = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "KitchenPrototype")
                kitchen = root.transform;
        }

        Transform pan = kitchen != null
            ? kitchen.Find("FryingPan")
            : null;

        Transform handleAttach = pan != null
            ? pan.Find("HandleAttach")
            : null;

        Transform counter = kitchen != null
            ? kitchen.Find("RoundedCafeteriaCounter")
            : null;

        Transform rim = counter != null
            ? counter.Find("WhiteRim")
            : null;

        Transform steel = counter != null
            ? counter.Find("SteelWorktop")
            : null;

        Transform cabinet = counter != null
            ? counter.Find("Cabinet")
            : null;

        XRGrabInteractable grab = pan != null
            ? pan.GetComponent<XRGrabInteractable>()
            : null;

        Rigidbody body = pan != null
            ? pan.GetComponent<Rigidbody>()
            : null;

        MeshFilter rimMesh = rim != null
            ? rim.GetComponent<MeshFilter>()
            : null;

        MeshFilter steelMesh = steel != null
            ? steel.GetComponent<MeshFilter>()
            : null;

        MeshFilter cabinetMesh = cabinet != null
            ? cabinet.GetComponent<MeshFilter>()
            : null;

        if (grab == null ||
            body == null ||
            handleAttach == null ||
            rimMesh == null ||
            steelMesh == null ||
            cabinetMesh == null ||
            rimMesh.sharedMesh == null ||
            steelMesh.sharedMesh == null ||
            cabinetMesh.sharedMesh == null)
        {
            Debug.LogError(
                "FryingPanと、その子のHandleAttach、"
                + "カウンターのWhiteRim・SteelWorktop・"
                + "Cabinetを確認してください。"
            );

            return;
        }

        if (!rimMesh.sharedMesh.isReadable ||
            counter.GetComponentInParent<Rigidbody>() != null ||
            Vector3.Dot(counter.up, Vector3.up) < 0.999f ||
            Vector3.Dot(rim.up, counter.up) < 0.999f ||
            Vector3.Dot(steel.up, counter.up) < 0.999f ||
            Vector3.Dot(cabinet.up, counter.up) < 0.999f)
        {
            Debug.LogError(
                "カウンターは傾けず、Rigidbodyのない"
                + "固定オブジェクトにしてください。"
                + "WhiteRimのメッシュは読み取り可能である必要があります。"
            );

            return;
        }

        // 現在の形状からカウンターの高さを取得する。
        float bottom = CounterY(
            counter,
            cabinet,
            cabinetMesh.sharedMesh.bounds.min.y
        );

        float top = CounterY(
            counter,
            steel,
            steelMesh.sharedMesh.bounds.max.y
        );

        float rimBottom = CounterY(
            counter,
            rim,
            rimMesh.sharedMesh.bounds.min.y
        );

        float rimTop = CounterY(
            counter,
            rim,
            rimMesh.sharedMesh.bounds.max.y
        );

        if (top <= bottom ||
            rimTop - rimBottom < 0.0001f)
        {
            Debug.LogError(
                "カウンターの高さを確認してください。"
            );

            return;
        }

        // 白い縁のU字型を利用し、
        // 本体の下端から天板まで厚みのある判定を作る。
        // 見た目のメッシュは変更しない。
        Mesh collisionMesh =
            Object.Instantiate(rimMesh.sharedMesh);

        collisionMesh.name = "StableCounterCollision";

        Vector3[] vertices = collisionMesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = counter.InverseTransformPoint(
                rim.TransformPoint(vertices[i])
            );

            p.y = Mathf.Lerp(
                bottom,
                top,
                Mathf.InverseLerp(
                    rimBottom,
                    rimTop,
                    p.y
                )
            );

            vertices[i] = p;
        }

        collisionMesh.vertices = vertices;
        collisionMesh.RecalculateNormals();
        collisionMesh.RecalculateBounds();

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "PanStabilityGenerated"
            );
        }

        // 保存後・Questへのビルド後も判定を保持する。
        AssetDatabase.CreateAsset(
            collisionMesh,
            AssetDatabase.GenerateUniqueAssetPath(
                Folder + "/StableCounterCollision.asset"
            )
        );

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fix pan pickup");

        Transform collisionRoot =
            counter.Find("StableCounterCollision");

        if (collisionRoot == null)
        {
            GameObject obj =
                new GameObject("StableCounterCollision");

            obj.transform.SetParent(counter, false);

            Undo.RegisterCreatedObjectUndo(
                obj,
                "Create stable counter collision"
            );

            collisionRoot = obj.transform;
        }

        Undo.RecordObject(
            collisionRoot,
            "Reset collider transform"
        );

        collisionRoot.localPosition = Vector3.zero;
        collisionRoot.localRotation = Quaternion.identity;
        collisionRoot.localScale = Vector3.one;

        MeshCollider collider =
            collisionRoot.GetComponent<MeshCollider>();

        if (collider == null)
        {
            collider = Undo.AddComponent<MeshCollider>(
                collisionRoot.gameObject
            );
        }

        Undo.RecordObject(
            collider,
            "Set solid counter collider"
        );

        // U字の中央の空間を埋めないため、Convexは無効。
        collider.convex = false;
        collider.isTrigger = false;
        collider.sharedMesh = collisionMesh;
        collider.enabled = true;

        // 元の3層の当たり判定だけを無効にする。
        // 見た目を表示するRendererは残す。
        foreach (Transform layer in new[] { cabinet, rim, steel })
        {
            foreach (Collider oldCollider in
                layer.GetComponents<Collider>())
            {
                Undo.RecordObject(
                    oldCollider,
                    "Disable layered collision"
                );

                oldCollider.enabled = false;
            }
        }

        // 元の四角い机も非表示にして、
        // カウンターと判定が重ならないようにする。
        Transform oldTable =
            kitchen.Find("KitchenTable");

        if (oldTable != null)
        {
            Undo.RecordObject(
                oldTable.gameObject,
                "Keep old table inactive"
            );

            oldTable.gameObject.SetActive(false);
        }

        // 持ち手の位置・向きへ合わせる操作方式を維持する。
        Undo.RecordObject(
            grab,
            "Stabilize grab pose"
        );

        grab.attachTransform = handleAttach;
        grab.useDynamicAttach = false;

        // 急な移動・回転を和らげる。
        grab.attachEaseInTime = 0.25f;

        // 肉との物理的な接触を維持する。
        grab.movementType =
            XRBaseInteractable.MovementType.VelocityTracking;

        grab.trackScale = false;

        Undo.RecordObject(
            body,
            "Keep continuous collision detection"
        );

        body.interpolation =
            RigidbodyInterpolation.Interpolate;

        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        EditorUtility.SetDirty(grab);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(collider);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);

        Undo.CollapseUndoOperations(group);

        Selection.activeGameObject = pan.gameObject;

        Debug.Log(
            "固定の持ち方を維持して、"
            + "フライパンとカウンターの設定を修正しました。"
            + "Ctrl+Sでシーンを保存してください。"
        );
    }

    static float CounterY(
        Transform counter,
        Transform part,
        float localY)
    {
        return counter.InverseTransformPoint(
            part.TransformPoint(
                new Vector3(0f, localY, 0f)
            )
        ).y;
    }
}