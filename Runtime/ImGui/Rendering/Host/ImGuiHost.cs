using System;
using System.Diagnostics;
using Basic.ImGui.Layout;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Basic.ImGui.Rendering
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ImGui Host")]
    public class ImGuiHost : MonoBehaviour
    {
        [SerializeField] ImGuiGraphic _graphic;
        [SerializeField] Canvas _canvas;
        [SerializeField] bool _flipY;

        ImGuiContext _context;
        ITextMeasurer _textMeasurer;
        FontRegistry _fontRegistry;
        CanvasRenderBackend _backend;
        ProfilerRecorder _gcAllocRecorder;
        bool _ownsTextMeasurer;

        bool _useTestPointer;
        Vector2 _testPointerPosition;
        bool _testPointerDown;
        Vector2 _testScrollDelta;
        int _frameIndex;
        bool _stressRanWithoutOverflow = true;
        ImGuiFrameStats _lastFrameStats;
        ImGuiPerfGateResults _perfGates;

        public ImGuiContext Context => _context;
        public ImGuiGraphic Graphic => _graphic;
        public ImGuiFrameStats LastFrameStats => _lastFrameStats;
        public ImGuiPerfGateResults PerfGates => _perfGates;
        public bool GatesReady => _frameIndex >= ImGuiPerfGates.WarmupFrames;
        public int FrameIndex => _frameIndex;

        public event Action<ImGuiContext, float> DeclareLayout;

        protected virtual void OnDeclareLayout(ImGuiContext context, float deltaTime)
        {
        }

        public void ConfigureForTests(
            ImGuiGraphic graphic,
            Canvas canvas,
            ImGuiContext context,
            ITextMeasurer textMeasurer,
            FontRegistry fontRegistry)
        {
            _context?.Dispose();
            if (_ownsTextMeasurer && _textMeasurer is IDisposable disposable)
            {
                disposable.Dispose();
            }

            SetReferences(graphic, canvas);
            _context = context;
            _textMeasurer = textMeasurer;
            _fontRegistry = fontRegistry;
            _ownsTextMeasurer = false;
            _backend = new CanvasRenderBackend(_graphic);
            _graphic.Configure(_context, _fontRegistry);
            enabled = true;
        }

        public void SetReferences(ImGuiGraphic graphic, Canvas canvas)
        {
            _graphic = graphic;
            _canvas = canvas;
            EnsureBackend();
        }

        public void SetTestPointer(Vector2 layoutPosition, bool isDown, Vector2 scrollDelta = default)
        {
            _useTestPointer = true;
            _testPointerPosition = layoutPosition;
            _testPointerDown = isDown;
            _testScrollDelta = scrollDelta;
        }

        public void ClearTestPointer() => _useTestPointer = false;

        void Awake()
        {
            ResolveReferences();
            EnsureContext();
            EnsureFontRegistry();
            _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            EnsureBackend();
        }

        void Start()
        {
            ResolveReferences();
            EnsureFontRegistry();
            EnsureBackend();
        }

        void ResolveReferences()
        {
            if (_graphic == null)
            {
                _graphic = GetComponent<ImGuiGraphic>();
                if (_graphic == null)
                {
                    _graphic = GetComponentInChildren<ImGuiGraphic>(true);
                }

                if (_graphic == null)
                {
                    var canvasTransform = transform.parent;
                    if (canvasTransform != null)
                    {
                        _graphic = canvasTransform.GetComponentInChildren<ImGuiGraphic>(true);
                    }
                }
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
        }

        void EnsureContext()
        {
            if (_context != null)
            {
                return;
            }

            EnsureFontRegistry();
            _textMeasurer = new FontRegistryTextMeasurer(_fontRegistry);
            _ownsTextMeasurer = true;
            _context = new ImGuiContext(_textMeasurer);
        }

        void EnsureBackend()
        {
            if (_graphic == null)
            {
                return;
            }

            EnsureContext();
            EnsureFontRegistry();

            if (_backend == null)
            {
                _backend = new CanvasRenderBackend(_graphic);
            }

            _graphic.Configure(_context, _fontRegistry);
        }

        void EnsureFontRegistry()
        {
            if (_fontRegistry == null)
            {
                _fontRegistry = FontRegistry.CreateWithDefaultFont();
            }
            else if (!_fontRegistry.TryGetFont(FontId.Default, out _))
            {
                var defaultFont = FontRegistry.TryResolveDefaultFontAsset();
                if (defaultFont != null)
                {
                    _fontRegistry.Register(FontId.Default, defaultFont);
                }
            }

            if (_graphic != null && _context != null)
            {
                _graphic.Configure(_context, _fontRegistry);
            }
        }

        void OnDestroy()
        {
            if (_gcAllocRecorder.Valid)
            {
                _gcAllocRecorder.Dispose();
            }

            _context?.Dispose();
            _context = null;

            if (_ownsTextMeasurer && _textMeasurer is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _textMeasurer = null;
        }

        void LateUpdate()
        {
            ResolveReferences();
            EnsureBackend();

            if (_context == null || _graphic == null || _backend == null)
            {
                return;
            }

            if (_fontRegistry == null || !_fontRegistry.TryGetFont(FontId.Default, out _))
            {
                EnsureFontRegistry();
            }

            RunFrame(Time.unscaledDeltaTime);
        }

        void RunFrame(float deltaTime)
        {
            _frameIndex++;
            var layoutSize = GetLayoutDimensions();
            var pointer = GetPointerState();

            _context.SetLayoutDimensions(layoutSize);
            _context.SetPointerState(pointer.Position, pointer.IsDown);
            _context.UpdateScrollContainers(pointer.IsDown, pointer.ScrollDelta, deltaTime);

            var layoutStopwatch = Stopwatch.StartNew();
            _context.BeginLayout();

            try
            {
                OnDeclareLayout(_context, deltaTime);
                DeclareLayout?.Invoke(_context, deltaTime);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("maximum layout node count"))
            {
                _stressRanWithoutOverflow = false;
                throw;
            }

            var frame = _context.EndLayout(deltaTime);
            layoutStopwatch.Stop();

            var renderStopwatch = Stopwatch.StartNew();
            _backend.Draw(frame, new RenderBackendContext(Matrix4x4.identity, _flipY));
            renderStopwatch.Stop();

            var gcAlloc = _gcAllocRecorder.Valid ? _gcAllocRecorder.LastValue : 0L;
            _lastFrameStats = new ImGuiFrameStats(
                _context.LastElementCount,
                frame.Commands.Length,
                _graphic.LastBatchCount,
                _graphic.LastVertexCount,
                layoutStopwatch.Elapsed.TotalMilliseconds,
                renderStopwatch.Elapsed.TotalMilliseconds,
                gcAlloc,
                _frameIndex);

            if (GatesReady)
            {
                _perfGates = ImGuiPerfGates.Evaluate(_lastFrameStats, _stressRanWithoutOverflow);
            }
        }

        Vector2 GetLayoutDimensions()
        {
            if (_graphic != null)
            {
                var rect = _graphic.rectTransform.rect;
                if (rect.width > 0f && rect.height > 0f)
                {
                    return new Vector2(rect.width, rect.height);
                }
            }

            if (_canvas != null)
            {
                var size = _canvas.pixelRect.size;
                if (size.x > 0f && size.y > 0f)
                {
                    return size;
                }
            }

            return new Vector2(Screen.width, Screen.height);
        }

        PointerFrameState GetPointerState()
        {
            if (_useTestPointer)
            {
                return new PointerFrameState(_testPointerPosition, _testPointerDown, _testScrollDelta);
            }

            var screenPosition = ImGuiPointerInput.MousePosition;
            var isDown = ImGuiPointerInput.IsPrimaryButtonDown;
            var scrollDelta = ImGuiPointerInput.ScrollDelta;

            if (_canvas != null && _graphic != null)
            {
                var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _graphic.rectTransform,
                        screenPosition,
                        camera,
                        out var localPoint))
                {
                    var rect = _graphic.rectTransform.rect;
                    var layoutPosition = new Vector2(localPoint.x - rect.xMin, rect.yMax - localPoint.y);
                    return new PointerFrameState(layoutPosition, isDown, scrollDelta);
                }
            }

            return new PointerFrameState(
                new Vector2(screenPosition.x, Screen.height - screenPosition.y),
                isDown,
                scrollDelta);
        }

        readonly struct PointerFrameState
        {
            public Vector2 Position { get; }
            public bool IsDown { get; }
            public Vector2 ScrollDelta { get; }

            public PointerFrameState(Vector2 position, bool isDown, Vector2 scrollDelta)
            {
                Position = position;
                IsDown = isDown;
                ScrollDelta = scrollDelta;
            }
        }
    }
}
