using UnityEngine.Profiling;

namespace MGLibrary.CustomTween
{
    public class CustomTweenCore<T1, T2> : CustomTweener
    {
        public T2 startValue;
        public T2 endValue;
        public TweenGetter<T1> getter;
        public TweenSetter<T1> setter;
        public ITweenPlugin<T1, T2> plugin;
        private bool _playOnce;
        private T2 tempValue;

        internal override CustomTweener SetFrom()
        {
            isFrom = true;
            startValue = endValue;
            endValue = (T2)(object)getter();
            return this;
        }

        internal override CustomTweener SetTo()
        {
            isFrom = false;
            return this;
        }

        protected override void UpdateValue(float progress)
        {
            if (plugin == null || setter == null) return;
            if (!_playOnce)
            {
                _playOnce = true;
                tempValue = (T2)(object)getter();
            }

            plugin.EvaluateAndApply(tempValue, endValue, setter, progress);
        }

        internal override void Reset()
        {
            base.Reset();
            _playOnce = false;
        }

        internal override void Clear()
        {
            base.Clear();
            startValue = default(T2);
            endValue = default(T2);
            getter = null;
            setter = null;
            plugin = null;
        }
    }
}