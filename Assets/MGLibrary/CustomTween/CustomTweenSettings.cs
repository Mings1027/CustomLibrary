using System;

namespace MGLibrary.CustomTween
{
    public static class CustomTweenSettings
    {
        public static Sequence Append(this Sequence s, CustomTween t)
        {
            Sequence.Insert(s, t);
            return s;
        }

        public static Sequence Join(this Sequence s, CustomTween t)
        {
            Sequence.JoinInsert(s, t);
            return s;
        }

        public static Sequence Wait(this Sequence s, float duration)
        {
            Sequence.WaitInsert(s, duration);
            return s;
        }

        public static T SetLoop<T>(this T tween, int count) where T : CustomTween
        {
            tween.SetLoop(count);
            return tween;
        }

        public static T Play<T>(this T tween) where T : CustomTween
        {
            tween.PlayInternal();
            return tween;
        }

        public static T Pause<T>(this T tween) where T : CustomTweener
        {
            tween.PauseInternal();
            return tween;
        }

        public static T Replay<T>(this T tween) where T : CustomTween
        {
            tween.ReplayInternal();
            return tween;
        }

        public static T Rewind<T>(this T tween) where T : CustomTween
        {
            tween.RewindInternal();
            return tween;
        }

        public static T SetAutoKill<T>(this T tween, bool autoKill) where T : CustomTween
        {
            tween.SetAutoKill(autoKill);
            return tween;
        }

        public static T SetEase<T>(this T tween, EaseType easeType) where T : CustomTween
        {
            tween.SetEase(easeType);
            return tween;
        }

        public static T Kill<T>(this T tween) where T : CustomTween
        {
            tween.KillInternal();
            return tween;
        }

        public static T From<T>(this T t) where T : CustomTweener
        {
            t.SetFrom();
            return t;
        }

        public static T To<T>(this T tween) where T : CustomTweener
        {
            tween.SetTo();
            return tween;
        }

        public static T OnStart<T>(this T tween, Action onStart) where T : CustomTween
        {
            tween.onStart = onStart;
            return tween;
        }

        public static T OnPlay<T>(this T tween, Action onPlay) where T : CustomTween
        {
            tween.onPlay = onPlay;
            return tween;
        }

        public static T OnUpdate<T>(this T tween, Action onUpdate) where T : CustomTween
        {
            tween.onUpdate = onUpdate;
            return tween;
        }

        public static T OnComplete<T>(this T tween, Action onComplete) where T : CustomTween
        {
            tween.onComplete = onComplete;
            return tween;
        }

        public static T OnRewind<T>(this T tween, Action onRewind) where T : CustomTween
        {
            tween.onRewind = onRewind;
            return tween;
        }
    }
}