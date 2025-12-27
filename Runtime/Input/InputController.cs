using System.Collections.Generic;

namespace Basic.Input
{
    public partial class InputRegions
    {
        public static readonly InputRegion Main = new(nameof(Main), GUID.Generate());
        public static readonly InputRegion BLOCKED = new(nameof(BLOCKED), GUID.Generate());
    }

    public class InputRegion
    {
        public string Name { get; private set; }
        public GUID ID { get; private set; }

        private InputRegion() { }

        public InputRegion(string name, GUID id)
        {
            Name = name;
            ID = id;
        }

        public override string ToString() => $"{Name}:{ID}";

        public override bool Equals(object obj) =>
            obj is InputRegion region && EqualityComparer<GUID>.Default.Equals(ID, region.ID);

        public override int GetHashCode() => ID.GetHashCode();

        public static bool operator ==(InputRegion a, InputRegion b) => a.ID == b.ID;

        public static bool operator !=(InputRegion a, InputRegion b) => a.ID != b.ID;
    }

    public struct InputRegionHandler<TInputActions>
        where TInputActions : new()
    {
        public InputRegion Region;
        public System.Action RegionEnteredCallback;
        public System.Action RegionExitedCallback;
        public System.Action<TInputActions> HandleInputCallback;
    }

    public static class InputController<TInputActions>
        where TInputActions : new()
    {
        // Private state
        private static readonly Dictionary<
            GUID,
            InputRegionHandler<TInputActions>
        > _regionHandlers = new();
        private static readonly List<InputRegion> _regionStack = new(8);
        private static readonly TInputActions _inputActions = new();

        // Public API
        public static void RegisterRegionHandler(InputRegionHandler<TInputActions> handler)
        {
            _regionHandlers.TryAdd(handler.Region.ID, handler);
        }

        public static void DeregisterRegionHandler(InputRegionHandler<TInputActions> handler)
        {
            _regionHandlers.Remove(handler.Region.ID);
        }

        public static void DispatchInput()
        {
            if (_regionStack.Count == 0)
            {
                return;
            }

            if (_regionStack[^1] == InputRegions.BLOCKED)
            {
                return;
            }

            if (_regionHandlers.TryGetValue(_regionStack[^1].ID, out var handler))
            {
                handler.HandleInputCallback?.Invoke(_inputActions);
            }
        }

        public static void PushRegion(InputRegion region)
        {
            if (_regionStack.Count > 0 && _regionStack[^1] == region)
            {
                return;
            }

            if (_regionStack.Contains(region))
            {
                _regionStack.Remove(region);
            }

            if (
                _regionStack.Count > 0
                && _regionHandlers.TryGetValue(_regionStack[^1].ID, out var handler)
            )
            {
                handler.RegionExitedCallback?.Invoke();
            }

            _regionStack.Add(region);

            if (_regionHandlers.TryGetValue(_regionStack[^1].ID, out handler))
            {
                handler.RegionEnteredCallback?.Invoke();
            }
        }

        public static void RemoveRegion(InputRegion region)
        {
            if (!_regionStack.Contains(region))
            {
                return;
            }

            if (_regionStack[^1] == region)
            {
                if (_regionHandlers.TryGetValue(_regionStack[^1].ID, out var handler))
                {
                    handler.RegionExitedCallback?.Invoke();
                }

                _regionStack.Remove(region);

                if (
                    _regionStack.Count > 0
                    && _regionHandlers.TryGetValue(_regionStack[^1].ID, out handler)
                )
                {
                    handler.RegionEnteredCallback?.Invoke();
                }
            }
            else
            {
                _regionStack.Remove(region);
            }
        }
    }
}
