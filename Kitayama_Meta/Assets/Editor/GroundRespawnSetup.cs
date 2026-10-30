using System.Collections.Generic;
using Kitayama.Cooking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class GroundRespawnSetup
{
    [MenuItem("Kitayama/Setup Ground Respawn")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();

        Transform kitchen = null;
        CookingResult result = null;

        var floors = new List<Collider>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "KitchenPrototype")
                kitchen = root.transform;

            if (result == null)
            {
                result =
                    root.GetComponentInChildren<CookingResult>(true);
            }

            foreach (Collider collider in
                root.GetComponentsInChildren<Collider>(true))
            {
                // 前回生成した食堂の床と、元からあるPlaneを探す。
                bool namedGround =
                    (
                        root.name == "CafeteriaBackground" &&
                        collider.name == "FloorCollision"
                    )
                    ||
                    (
                        root.name == "Plane" &&
                        collider.gameObject == root
                    );

                bool markedGround =
                    collider.GetComponent<RespawnFloor>() != null;

                if ((namedGround || markedGround) &&
                    !collider.isTrigger &&
                    collider.attachedRigidbody == null)
                {
                    floors.Add(collider);
                }
            }
        }

        Transform meat = kitchen != null
            ? kitchen.Find("Meat")
            : null;

        Transform pan = kitchen != null
            ? kitchen.Find("FryingPan")
            : null;

        MeatCooking cooking = meat != null
            ? meat.GetComponent<MeatCooking>()
            : null;

        if (meat == null ||
            pan == null ||
            cooking == null ||
            result == null ||
            floors.Count == 0 ||
            meat.GetComponent<Rigidbody>() == null ||
            pan.GetComponent<Rigidbody>() == null ||
            meat.GetComponent<XRGrabInteractable>() == null ||
            pan.GetComponent<XRGrabInteractable>() == null)
        {
            Debug.LogError(
                "肉・フライパン・CookingResult・床を確認してください。"
                + "肉とフライパンにはRigidbodyと"
                + "XRGrabInteractableが必要です。"
            );

            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Ground Respawn");

        // 床に目印を付ける。
        foreach (Collider floor in floors)
        {
            if (floor.GetComponent<RespawnFloor>() == null)
            {
                Undo.AddComponent<RespawnFloor>(
                    floor.gameObject
                );
            }
        }

        // 肉とフライパンにリスポーン処理を設定する。
        foreach (Transform target in new[] { meat, pan })
        {
            GroundRespawn respawn =
                target.GetComponent<GroundRespawn>();

            if (respawn == null)
            {
                respawn = Undo.AddComponent<GroundRespawn>(
                    target.gameObject
                );
            }

            Undo.RecordObject(respawn, "Configure respawn");

            respawn.cooking = cooking;
            respawn.result = result;

            EditorUtility.SetDirty(respawn);
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(
            "床への接触によるリスポーンを設定しました。"
            + "Ctrl+Sでシーンを保存してください。"
        );
    }
}