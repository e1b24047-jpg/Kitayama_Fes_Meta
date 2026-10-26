using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kitayama.Cooking.Editor
{
    public static class ThrowSetup
    {
        [MenuItem("Kitayama/Setup Throw Step")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "Stop Play Mode before setup."
                );

                return;
            }

            GameObject kitchen =
                GameObject.Find("KitchenPrototype");

            Transform meat = kitchen != null
                ? kitchen.transform.Find("Meat")
                : null;

            Transform pan = kitchen != null
                ? kitchen.transform.Find("FryingPan")
                : null;

            MeatCooking cooking = meat != null
                ? meat.GetComponent<MeatCooking>()
                : null;

            if (cooking == null ||
                pan == null ||
                cooking.panBase == null ||
                cooking.heatPoint == null ||
                cooking.meatGrab == null ||
                cooking.statusLabel == null ||
                cooking.meterLabel == null ||
                pan.GetComponent<Rigidbody>() == null ||
                pan.GetComponent<XRGrabInteractable>() == null)
            {
                Debug.LogError(
                    "Set up and test the existing cooking step first."
                );

                return;
            }

            Undo.IncrementCurrentGroup();

            int group = Undo.GetCurrentGroup();

            Undo.SetCurrentGroupName("Setup Throw Step");

            PanThrowAssist assist =
                pan.GetComponent<PanThrowAssist>();

            if (assist == null)
            {
                assist = Undo.AddComponent<PanThrowAssist>(
                    pan.gameObject
                );
            }

            Undo.RecordObject(assist, "Connect meat");

            assist.cooking = cooking;

            Undo.RecordObject(
                cooking,
                "Connect feedback audio"
            );

            if (cooking.feedbackSource == null)
            {
                // í ímâπêÍópÇÃAudioSourceÇçÏê¨Ç∑ÇÈÅB
                GameObject feedback =
                    new GameObject("ThrowFeedback");

                feedback.transform.SetParent(pan, false);

                AudioSource audio =
                    feedback.AddComponent<AudioSource>();

                audio.playOnAwake = false;
                audio.spatialBlend = 1f;
                audio.minDistance = 1f;
                audio.maxDistance = 8f;

                cooking.feedbackSource = audio;

                Undo.RegisterCreatedObjectUndo(
                    feedback,
                    "Create feedback audio"
                );
            }

            Undo.RecordObject(
                cooking.statusLabel,
                "Fit throw status"
            );

            cooking.statusLabel.resizeTextForBestFit = true;
            cooking.statusLabel.resizeTextMinSize = 12;
            cooking.statusLabel.resizeTextMaxSize = 18;

            EditorUtility.SetDirty(cooking.statusLabel);
            EditorUtility.SetDirty(assist);
            EditorUtility.SetDirty(cooking);

            EditorSceneManager.MarkSceneDirty(
                meat.gameObject.scene
            );

            Undo.CollapseUndoOperations(group);

            Selection.activeGameObject = pan.gameObject;

            Debug.Log(
                "THROW_SETUP_OK: Save the scene. " +
                "Heat, lift the pan, toss upward, then catch."
            );
        }
    }
}