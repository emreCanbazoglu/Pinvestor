#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
using System.Reflection;
using UnityEngine.UIElements;

namespace EditorReplacement
{
    public class EditorEndChangeCheckEvent : EventBase<EditorEndChangeCheckEvent>
    {
        public static readonly PropertyInfo propagationPI = typeof(EventBase).GetProperty("propagation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        /*
        internal enum EventPropagation
        {
            None = 0,
            Bubbles = 1,
            TricklesDown = 2,
            Cancellable = 4,
        }
        */
        public static readonly int bubblesValue = 1;

        protected override void Init()
        {
            base.Init();
            propagationPI.SetValue(this, bubblesValue);
        }
    }

    public class PropertyEndChangeCheckEvent : EventBase<PropertyEndChangeCheckEvent>
    {
        protected override void Init()
        {
            base.Init();
            EditorEndChangeCheckEvent.propagationPI.SetValue(this, EditorEndChangeCheckEvent.bubblesValue);
        }
    }
}
#endif
