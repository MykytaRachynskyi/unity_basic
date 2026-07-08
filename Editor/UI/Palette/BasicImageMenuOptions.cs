#if UNITY_EDITOR
using Basic.UI;
using UnityEditor;
using UnityEditor.EventSystems;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Basic.UnityEditorTools
{
    static class BasicImageMenuOptions
    {
        private const int MenuPriority = 2000;
        private const string UILayerName = "UI";

        [MenuItem("GameObject/UI (Canvas)/Basic Image", false, MenuPriority)]
        private static void CreateBasicImage(MenuCommand menuCommand)
        {
            GameObject go;
            using (new FactorySwapToEditor())
                go = DefaultControls.CreateImage(new DefaultControls.Resources());

            var image = go.GetComponent<Image>();
            Undo.DestroyObjectImmediate(image);
            Undo.AddComponent<BasicImage>(go);

            go.name = "Basic Image";
            PlaceUIElementRoot(go, menuCommand);
        }

        private static void PlaceUIElementRoot(GameObject element, MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var explicitParentChoice = true;

            if (parent == null)
            {
                parent = CreateNewUI();
                explicitParentChoice = false;

                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null && !prefabStage.IsPartOfPrefabContents(parent))
                    parent = prefabStage.prefabContentsRoot;
            }

            if (parent.GetComponentsInParent<Canvas>(true).Length == 0)
            {
                var canvas = CreateNewUI();
                Undo.SetTransformParent(canvas.transform, parent.transform, "");
                parent = canvas;
            }

            GameObjectUtility.EnsureUniqueNameForSibling(element);
            GameObjectUtility.SetParentAndAlign(element, parent);

            if (!explicitParentChoice)
            {
                var canvasRect = parent.GetComponent<RectTransform>();
                var itemRect = element.GetComponent<RectTransform>();
                if (canvasRect != null && itemRect != null)
                    CenterInSceneView(canvasRect, itemRect);
            }

            Undo.RegisterFullObjectHierarchyUndo(parent == null ? element : parent, "");
            Undo.SetCurrentGroupName("Create " + element.name);
            Selection.activeGameObject = element;
        }

        private static GameObject CreateNewUI()
        {
            var root = ObjectFactory.CreateGameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            root.layer = LayerMask.NameToLayer(UILayerName);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            StageUtility.PlaceGameObjectInCurrentStage(root);

            var customScene = false;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                Undo.SetTransformParent(root.transform, prefabStage.prefabContentsRoot.transform, "");
                customScene = true;
            }

            Undo.SetCurrentGroupName("Create " + root.name);

            if (!customScene)
                EnsureEventSystem();

            return root;
        }

        private static void EnsureEventSystem(GameObject parent = null)
        {
            var stage = parent == null ? StageUtility.GetCurrentStageHandle() : StageUtility.GetStageHandle(parent);
            if (stage.FindComponentOfType<EventSystem>() != null)
                return;

            var eventSystem = ObjectFactory.CreateGameObject("EventSystem");
            if (parent == null)
                StageUtility.PlaceGameObjectInCurrentStage(eventSystem);
            else
                GameObjectUtility.SetParentAndAlign(eventSystem, parent);

            ObjectFactory.AddComponent<EventSystem>(eventSystem);
            InputModuleComponentFactory.AddInputModule(eventSystem);
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create " + eventSystem.name);
        }

        private static void CenterInSceneView(RectTransform canvasRect, RectTransform itemRect)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                return;

            var camera = sceneView.camera;
            if (
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    new Vector2(camera.pixelWidth / 2f, camera.pixelHeight / 2f),
                    camera,
                    out var localPlanePosition
                )
            )
                return;

            localPlanePosition.x += canvasRect.sizeDelta.x * canvasRect.pivot.x;
            localPlanePosition.y += canvasRect.sizeDelta.y * canvasRect.pivot.y;

            localPlanePosition.x = Mathf.Clamp(localPlanePosition.x, 0f, canvasRect.sizeDelta.x);
            localPlanePosition.y = Mathf.Clamp(localPlanePosition.y, 0f, canvasRect.sizeDelta.y);

            var position = new Vector3(
                localPlanePosition.x - canvasRect.sizeDelta.x * itemRect.anchorMin.x,
                localPlanePosition.y - canvasRect.sizeDelta.y * itemRect.anchorMin.y,
                0f
            );

            itemRect.anchoredPosition = position;
        }

        private sealed class FactorySwapToEditor : System.IDisposable
        {
            private DefaultControls.IFactoryControls _previousFactory;

            public FactorySwapToEditor()
            {
                _previousFactory = DefaultControls.factory;
                DefaultControls.factory = DefaultEditorFactory.Default;
            }

            public void Dispose() => DefaultControls.factory = _previousFactory;

            private sealed class DefaultEditorFactory : DefaultControls.IFactoryControls
            {
                public static readonly DefaultEditorFactory Default = new();

                public GameObject CreateGameObject(string name, params System.Type[] components) =>
                    ObjectFactory.CreateGameObject(name, components);
            }
        }
    }
}
#endif
