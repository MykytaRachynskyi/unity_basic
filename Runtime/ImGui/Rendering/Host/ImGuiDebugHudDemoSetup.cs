using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Basic.ImGui.Rendering
{
    [DisallowMultipleComponent]
    public sealed class ImGuiDebugHudDemoSetup : MonoBehaviour
    {
        [SerializeField] Vector2 _referenceResolution = new(1920f, 1080f);
        [SerializeField] Color _backPanelColor = new(0.15f, 0.25f, 0.55f, 0.85f);
        [SerializeField] Color _frontPanelColor = new(0.85f, 0.7f, 0.2f, 0.9f);

        void Awake()
        {
            if (FindFirstObjectByType<ImGuiHost>() != null)
            {
                return;
            }

            EnsureEventSystem();
            var camera = EnsureCamera();
            CreateBackPanel(camera);
            CreateImGuiHud(camera);
            CreateFrontPanel(camera);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemGo);
        }

        static Camera EnsureCamera()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                return camera;
            }

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            cameraGo.tag = "MainCamera";
            return camera;
        }

        void CreateBackPanel(Camera camera)
        {
            var canvas = CreateCanvas("BackUguiCanvas", camera, sortingOrder: 0);
            CreatePanel(canvas.transform, "BackPanel", _backPanelColor, anchorMin: new Vector2(0.02f, 0.02f), anchorMax: new Vector2(0.98f, 0.98f));
        }

        void CreateFrontPanel(Camera camera)
        {
            var canvas = CreateCanvas("FrontUguiCanvas", camera, sortingOrder: 20);
            CreatePanel(canvas.transform, "FrontStrip", _frontPanelColor, anchorMin: new Vector2(0f, 0.88f), anchorMax: new Vector2(1f, 1f));
        }

        void CreateImGuiHud(Camera camera)
        {
            var canvasGo = new GameObject(
                "ImGuiCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            var graphicGo = new GameObject("ImGuiGraphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(ImGuiGraphic));
            graphicGo.transform.SetParent(canvasGo.transform, false);
            var graphicRect = graphicGo.GetComponent<RectTransform>();
            graphicRect.anchorMin = Vector2.zero;
            graphicRect.anchorMax = Vector2.one;
            graphicRect.offsetMin = Vector2.zero;
            graphicRect.offsetMax = Vector2.zero;

            var hostGo = new GameObject("ImGuiHost");
            hostGo.SetActive(false);
            hostGo.transform.SetParent(canvasGo.transform, false);
            var host = hostGo.AddComponent<ImGuiHost>();
            hostGo.AddComponent<ImGuiDebugHud>();
            host.SetReferences(graphicGo.GetComponent<ImGuiGraphic>(), canvas);
            hostGo.SetActive(true);
        }

        static Canvas CreateCanvas(string name, Camera camera, int sortingOrder)
        {
            var canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        static void CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panelGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            var rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panelGo.GetComponent<Image>().color = color;
        }
    }
}
