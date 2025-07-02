using UnityEngine;

namespace MGLibrary.CustomTween
{
    public abstract class CustomTweener : CustomTween
    {
        internal abstract CustomTweener SetFrom();
        internal abstract CustomTweener SetTo();

        internal static CustomTweenCore<T1, T2> Apply<T1, T2>(TweenGetter<T1> getter, TweenSetter<T1> setter,
            T2 endValue, ITweenPlugin<T1, T2> plugin, float duration)
        {
            var tweener = TweenManager.GetTweenCore<T1, T2>();
            Setup<T1, T2>(tweener, endValue, getter, setter, plugin, duration);
            return tweener;
        }

        private static bool Setup<T1, T2>(CustomTweenCore<T1, T2> t,
            T2 endValue, TweenGetter<T1> getter, TweenSetter<T1> setter, ITweenPlugin<T1, T2> plugin, float duration)
        {
            t.elapsedTime = 0;
            t.state = TweenState.Ready;
            t.startValue = (T2)(object)getter();
            t.endValue = endValue;
            t.getter = getter;
            t.setter = setter;
            t.plugin = plugin;
            t.duration = duration;
            t.easeFunc = TweenEaseManager.GetEaseFunction(EaseType.Linear);
            TweenManager.RegisterTween(t);
            return true;
        }
    }
}