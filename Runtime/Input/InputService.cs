using System.Collections.Generic;
using UnityEngine;

namespace Basic.Input
{
    public class InputRegions
    {
        public static readonly InputRegion BLOCKED = new("Blocked", GUID.Generate());

        public static readonly InputRegion Main = new("Main", GUID.Generate());
    }

    public class InputRegion
    {
        public GUID ID;
        public string Name;

        public InputRegion(string name, GUID id)
        {
            Name = name;
            ID = id;
        }

        public override string ToString() => $"{Name} :: {ID}";

        public override bool Equals(object obj) =>
            obj is InputRegion region && EqualityComparer<GUID>.Default.Equals(ID, region.ID);

        public override int GetHashCode() => ID.GetHashCode();

        public static bool operator ==(InputRegion a, InputRegion b) => a.Equals(b);

        public static bool operator !=(InputRegion a, InputRegion b) => !a.Equals(b);
    }

    public struct InputRegionHandler<TInputActions>
    {
        public InputRegion Region;
        public System.Action<TInputActions> HandleInputCallback;
        public System.Action RegionEnteredCallback;
        public System.Action RegionExitedCallback;
    }

    public interface IInputService<TInputActions>
    {
        IReadOnlyList<InputRegion> RegionStack { get; }
        void RegisterRegionHandler(InputRegionHandler<TInputActions> handler);
        void DeregisterRegionHandler(InputRegionHandler<TInputActions> handler);
        void DispatchInput();
        void PushRegion(InputRegion region);
        void RemoveRegion(InputRegion region);
        void ClearHandlers();
        void ClearRegions();
    }

    public abstract class InputService<TInputActions> : IInputService<TInputActions>
        where TInputActions : new()
    {
        // Private state
        protected readonly Dictionary<
            GUID,
            List<InputRegionHandler<TInputActions>>
        > _regionHandlers = new();
        protected readonly List<InputRegion> _regionStack = new(8);
        protected readonly TInputActions _inputActions = new();

        // Public getters
        public TInputActions InputActions => _inputActions;
        public IReadOnlyList<InputRegion> RegionStack => _regionStack;

        // Public API
        public void RegisterRegionHandler(InputRegionHandler<TInputActions> handler)
        {
            if (!_regionHandlers.ContainsKey(handler.Region.ID))
            {
                if (!_regionHandlers.TryAdd(handler.Region.ID, new()))
                {
                    Debug.LogError($"Failed to add region {handler.Region.ID} to dictionary.");
                }
            }

            if (!_regionHandlers[handler.Region.ID].Contains(handler))
            {
                _regionHandlers[handler.Region.ID].Add(handler);
            }
            else
            {
                Debug.LogError(
                    $"Failed to register handler for region {handler.Region}. Handler already registered."
                );
            }
        }

        public void DeregisterRegionHandler(InputRegionHandler<TInputActions> handler)
        {
            if (_regionHandlers.TryGetValue(handler.Region.ID, out var handlerList))
            {
                handlerList.Remove(handler);
            }
        }

        public void DispatchInput()
        {
            if (_regionStack.Count == 0)
            {
                return;
            }

            if (_regionStack[^1] == InputRegions.BLOCKED)
            {
                return;
            }

            if (_regionHandlers.TryGetValue(_regionStack[^1].ID, out var handlerList))
            {
                foreach (var handler in handlerList)
                {
                    handler.HandleInputCallback?.Invoke(_inputActions);
                }
            }
        }

        public void PushRegion(InputRegion region)
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
                && _regionHandlers.TryGetValue(_regionStack[^1].ID, out var handlerList)
            )
            {
                foreach (var handler in handlerList)
                {
                    handler.RegionExitedCallback?.Invoke();
                }
            }

            _regionStack.Add(region);

            if (_regionHandlers.TryGetValue(_regionStack[^1].ID, out handlerList))
            {
                foreach (var handler in handlerList)
                {
                    handler.RegionEnteredCallback?.Invoke();
                }
            }
        }

        public void RemoveRegion(InputRegion region)
        {
            if (!_regionStack.Contains(region))
            {
                return;
            }

            if (_regionStack[^1] == region)
            {
                if (_regionHandlers.TryGetValue(_regionStack[^1].ID, out var handlerList))
                {
                    foreach (var handler in handlerList)
                    {
                        handler.RegionExitedCallback?.Invoke();
                    }
                }

                _regionStack.Remove(region);

                if (
                    _regionStack.Count > 0
                    && _regionHandlers.TryGetValue(_regionStack[^1].ID, out handlerList)
                )
                {
                    foreach (var handler in handlerList)
                    {
                        handler.RegionEnteredCallback?.Invoke();
                    }
                }
            }
            else
            {
                _regionStack.Remove(region);
            }
        }

        public void ClearHandlers()
        {
            _regionHandlers.Clear();
        }

        public void ClearRegions()
        {
            _regionStack.Clear();
        }
    }
}
