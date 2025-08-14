using System.Collections.Generic;
using UnityEngine;

namespace Basic
{
    public abstract class BaseEvent
    {
        protected readonly List<System.Action> voidListeners = new(4);
        protected readonly List<System.Action> oneTimeVoidListeners = new(4);
        protected readonly bool allowDuplicateListeners;

        private readonly List<System.Action> listenersThatWantToBeRemoved = new(4);

        private bool _canRemoveTypedListeners = true;

        public BaseEvent(bool allowDuplicateListeners = false)
        {
            this.allowDuplicateListeners = allowDuplicateListeners;
        }

        public void AddVoidListener(System.Action voidListener)
        {
            if (!allowDuplicateListeners && voidListeners.Contains(voidListener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                voidListeners.Add(voidListener);
            }
        }

        public void AddOneTimeVoidListener(System.Action voidListener)
        {
            if (!allowDuplicateListeners && oneTimeVoidListeners.Contains(voidListener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                oneTimeVoidListeners.Add(voidListener);
            }
        }

        public void RemoveVoidListener(System.Action voidListener)
        {
            if (_canRemoveTypedListeners)
            {
                voidListeners.Remove(voidListener);
            }
            else
            {
                listenersThatWantToBeRemoved.Add(voidListener);
            }
        }

        public void RemoveOneTimeVoidListener(System.Action voidListener)
        {
            oneTimeVoidListeners.Remove(voidListener);
        }

        public virtual void Clear()
        {
            voidListeners.Clear();
            oneTimeVoidListeners.Clear();
        }

        protected void InvokeVoidListeners()
        {
            _canRemoveTypedListeners = false;

            for (int i = voidListeners.Count - 1; i >= 0; --i)
            {
                voidListeners[i]?.Invoke();
            }

            for (int i = oneTimeVoidListeners.Count - 1; i >= 0; --i)
            {
                oneTimeVoidListeners[i]?.Invoke();
            }

            oneTimeVoidListeners.Clear();

            _canRemoveTypedListeners = true;

            foreach (var listener in listenersThatWantToBeRemoved)
            {
                voidListeners.Remove(listener);
            }

            listenersThatWantToBeRemoved.Clear();
        }
    }

    public sealed class Event : BaseEvent, IEvent
    {
        public void Invoke()
        {
            InvokeVoidListeners();
        }

        public Event(bool allowDuplicateListeners = false)
            : base(allowDuplicateListeners) { }
    }

    public interface IEvent
    {
        void AddVoidListener(System.Action listener);
        void AddOneTimeVoidListener(System.Action listener);
        void RemoveVoidListener(System.Action listener);
        void RemoveOneTimeVoidListener(System.Action listener);
    }

    public interface IEvent<T> : IEvent
    {
        void AddListener(System.Action<T> listener);
        void AddOneTimeListener(System.Action<T> listener);
        void RemoveListener(System.Action<T> listener);
        void RemoveOneTimeListener(System.Action<T> listener);
    }

    public abstract class EventWrapper<T> : BaseEvent, IEvent<T>
    {
        public EventWrapper(bool allowDuplicateListeners = false)
            : base(allowDuplicateListeners) { }

        public abstract void AddListener(System.Action<T> listener);
        public abstract void AddOneTimeListener(System.Action<T> listener);
        public abstract void RemoveListener(System.Action<T> listener);
        public abstract void RemoveOneTimeListener(System.Action<T> listener);
    }

    public sealed class Event<T> : BaseEvent, IEvent<T>
    {
        private readonly List<System.Action<T>> typedListeners = new(4);
        private readonly List<System.Action<T>> typedOneTimeListeners = new(4);

        private readonly List<System.Action<T>> listenersThatWantToBeRemoved = new(4);

        private bool _canRemoveTypedListeners = true;

        public Event(bool allowDuplicateListeners = false)
            : base(allowDuplicateListeners) { }

        public void AddListener(System.Action<T> listener)
        {
            if (!allowDuplicateListeners && typedListeners.Contains(listener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                typedListeners.Add(listener);
            }
        }

        public void AddOneTimeListener(System.Action<T> listener)
        {
            if (!allowDuplicateListeners && typedOneTimeListeners.Contains(listener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                typedOneTimeListeners.Add(listener);
            }
        }

        public void RemoveListener(System.Action<T> listener)
        {
            if (_canRemoveTypedListeners)
            {
                typedListeners.Remove(listener);
            }
            else
            {
                listenersThatWantToBeRemoved.Add(listener);
            }
        }

        public void RemoveOneTimeListener(System.Action<T> listener)
        {
            typedOneTimeListeners.Remove(listener);
        }

        public void Invoke(T arg)
        {
            _canRemoveTypedListeners = false;

            InvokeVoidListeners();

            for (int i = typedListeners.Count - 1; i >= 0; --i)
            {
                typedListeners[i]?.Invoke(arg);
            }

            for (int i = typedOneTimeListeners.Count - 1; i >= 0; --i)
            {
                typedOneTimeListeners[i]?.Invoke(arg);
            }

            typedOneTimeListeners.Clear();

            _canRemoveTypedListeners = true;

            foreach (var listener in listenersThatWantToBeRemoved)
            {
                typedListeners.Remove(listener);
            }

            listenersThatWantToBeRemoved.Clear();
        }

        public override void Clear()
        {
            base.Clear();
            typedListeners.Clear();
            typedOneTimeListeners.Clear();
        }
    }

    public sealed class Event<T1, T2> : EventWrapper<T1>, IEvent<T2>
    {
        private readonly List<System.Action<T1, T2>> typedListeners = new(4);
        private readonly List<System.Action<T1, T2>> typedOneTimeListeners = new(4);

        private readonly List<System.Action<T1, T2>> listenersThatWantToBeRemoved = new(4);

        private bool _canRemoveTypedListeners = true;

        private Event<T1> _childEvent1 = new();
        private Event<T2> _childEvent2 = new();

        public Event(bool allowDuplicateListeners = false)
            : base(allowDuplicateListeners) { }

        public void AddListener(System.Action<T1, T2> listener)
        {
            if (!allowDuplicateListeners && typedListeners.Contains(listener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                typedListeners.Add(listener);
            }
        }

        public override void AddListener(System.Action<T1> listener) =>
            _childEvent1.AddListener(listener);

        public override void AddOneTimeListener(System.Action<T1> listener) =>
            _childEvent1.AddOneTimeListener(listener);

        public override void RemoveListener(System.Action<T1> listener) =>
            _childEvent1.RemoveListener(listener);

        public override void RemoveOneTimeListener(System.Action<T1> listener) =>
            _childEvent1.RemoveOneTimeListener(listener);

        public void AddListener(System.Action<T2> listener) => _childEvent2.AddListener(listener);

        public void AddOneTimeListener(System.Action<T2> listener) =>
            _childEvent2.AddOneTimeListener(listener);

        public void RemoveListener(System.Action<T2> listener) =>
            _childEvent2.RemoveListener(listener);

        public void RemoveOneTimeListener(System.Action<T2> listener) =>
            _childEvent2.RemoveOneTimeListener(listener);

        public void AddOneTimeListener(System.Action<T1, T2> listener)
        {
            if (!allowDuplicateListeners && typedOneTimeListeners.Contains(listener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                typedOneTimeListeners.Add(listener);
            }
        }

        public void RemoveListener(System.Action<T1, T2> listener)
        {
            if (_canRemoveTypedListeners)
            {
                typedListeners.Remove(listener);
            }
            else
            {
                listenersThatWantToBeRemoved.Add(listener);
            }
        }

        public void RemoveOneTimeListener(System.Action<T1, T2> listener)
        {
            typedOneTimeListeners.Remove(listener);
        }

        public void Invoke(T1 arg1, T2 arg2)
        {
            _canRemoveTypedListeners = false;

            InvokeVoidListeners();

            for (int i = typedListeners.Count - 1; i >= 0; --i)
            {
                typedListeners[i]?.Invoke(arg1, arg2);
            }

            for (int i = typedOneTimeListeners.Count - 1; i >= 0; --i)
            {
                typedOneTimeListeners[i]?.Invoke(arg1, arg2);
            }

            typedOneTimeListeners.Clear();

            _canRemoveTypedListeners = true;

            foreach (var listener in listenersThatWantToBeRemoved)
            {
                typedListeners.Remove(listener);
            }

            listenersThatWantToBeRemoved.Clear();

            _childEvent1.Invoke(arg1);
            _childEvent2.Invoke(arg2);
        }

        public override void Clear()
        {
            base.Clear();
            typedListeners.Clear();
            typedOneTimeListeners.Clear();
            _childEvent1.Clear();
            _childEvent2.Clear();
        }
    }

    public sealed class Event<T1, T2, T3> : BaseEvent
    {
        private readonly List<System.Action<T1, T2, T3>> typedListeners = new(4);
        private readonly List<System.Action<T1, T2, T3>> typedOneTimeListeners = new(4);

        private readonly List<System.Action<T1, T2, T3>> listenersThatWantToBeRemoved = new(4);

        private bool _canRemoveTypedListeners = true;

        public Event(bool allowDuplicateListeners = false)
            : base(allowDuplicateListeners) { }

        public void AddListener(System.Action<T1, T2, T3> listener)
        {
            if (!allowDuplicateListeners && typedListeners.Contains(listener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                typedListeners.Add(listener);
            }
        }

        public void AddOneTimeListener(System.Action<T1, T2, T3> listener)
        {
            if (!allowDuplicateListeners && typedOneTimeListeners.Contains(listener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                typedOneTimeListeners.Add(listener);
            }
        }

        public void RemoveListener(System.Action<T1, T2, T3> listener)
        {
            if (_canRemoveTypedListeners)
            {
                typedListeners.Remove(listener);
            }
            else
            {
                listenersThatWantToBeRemoved.Add(listener);
            }
        }

        public void RemoveOneTimeListener(System.Action<T1, T2, T3> listener)
        {
            typedOneTimeListeners.Remove(listener);
        }

        public void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            _canRemoveTypedListeners = false;

            InvokeVoidListeners();

            for (int i = typedListeners.Count - 1; i >= 0; --i)
            {
                typedListeners[i]?.Invoke(arg1, arg2, arg3);
            }

            for (int i = typedOneTimeListeners.Count - 1; i >= 0; --i)
            {
                typedOneTimeListeners[i]?.Invoke(arg1, arg2, arg3);
            }

            typedOneTimeListeners.Clear();

            _canRemoveTypedListeners = true;

            foreach (var listener in listenersThatWantToBeRemoved)
            {
                typedListeners.Remove(listener);
            }

            listenersThatWantToBeRemoved.Clear();
        }

        public override void Clear()
        {
            base.Clear();
            typedListeners.Clear();
            typedOneTimeListeners.Clear();
        }
    }

    public sealed class Event<T1, T2, T3, T4> : BaseEvent
    {
        private readonly List<System.Action<T1, T2, T3, T4>> typedListeners = new(4);
        private readonly List<System.Action<T1, T2, T3, T4>> typedOneTimeListeners = new(4);

        private readonly List<System.Action<T1, T2, T3, T4>> listenersThatWantToBeRemoved = new(4);

        private bool _canRemoveTypedListeners = true;

        public Event(bool allowDuplicateListeners = false)
            : base(allowDuplicateListeners) { }

        public void AddListener(System.Action<T1, T2, T3, T4> listener)
        {
            if (!allowDuplicateListeners && typedListeners.Contains(listener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                typedListeners.Add(listener);
            }
        }

        public void AddOneTimeListener(System.Action<T1, T2, T3, T4> listener)
        {
            if (!allowDuplicateListeners && typedOneTimeListeners.Contains(listener))
            {
                Debug.LogError($"Trying to add listener that is already on the list!");
            }
            else
            {
                typedOneTimeListeners.Add(listener);
            }
        }

        public void RemoveListener(System.Action<T1, T2, T3, T4> listener)
        {
            if (_canRemoveTypedListeners)
            {
                typedListeners.Remove(listener);
            }
            else
            {
                listenersThatWantToBeRemoved.Add(listener);
            }
        }

        public void RemoveOneTimeListener(System.Action<T1, T2, T3, T4> listener)
        {
            typedOneTimeListeners.Remove(listener);
        }

        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            _canRemoveTypedListeners = false;

            InvokeVoidListeners();

            for (int i = typedListeners.Count - 1; i >= 0; --i)
            {
                typedListeners[i]?.Invoke(arg1, arg2, arg3, arg4);
            }

            for (int i = typedOneTimeListeners.Count - 1; i >= 0; --i)
            {
                typedOneTimeListeners[i]?.Invoke(arg1, arg2, arg3, arg4);
            }

            typedOneTimeListeners.Clear();

            _canRemoveTypedListeners = true;

            foreach (var listener in listenersThatWantToBeRemoved)
            {
                typedListeners.Remove(listener);
            }

            listenersThatWantToBeRemoved.Clear();
        }

        public override void Clear()
        {
            base.Clear();
            typedListeners.Clear();
            typedOneTimeListeners.Clear();
        }
    }
}
