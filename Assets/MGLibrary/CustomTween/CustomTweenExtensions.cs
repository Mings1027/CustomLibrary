using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MGLibrary.CustomTween
{
    public static class CustomTweenExtensions
    {
        public static CustomTweenCore<Vector3, Vector3> TweenMove(this Transform target, Vector3 endValue,
            float duration)
        {
            var tween
                = CustomTweener.Apply<Vector3, Vector3>(() => target.position, x => target.position = x, endValue,
                    new VectorPlugin(), duration);
            tween.OnRewind(() => { target.position = tween.startValue; });

            SetEditorInfo(tween, target);

            return tween;
        }

        public static CustomTweenCore<Vector3, Vector3> TweenScale(this Transform target, Vector3 endValue,
            float duration)
        {
            var tween = CustomTweener.Apply<Vector3, Vector3>(() => target.localScale, x => target.localScale = x,
                endValue, new VectorPlugin(), duration);
            tween.OnRewind(() => { target.localScale = tween.startValue; });
            SetEditorInfo(tween, target);

            return tween;
        }

        public static CustomTweenCore<Quaternion, Vector3> TweenRotate(this Transform target, Vector3 endValue,
            float duration)
        {
            var customTween =
                CustomTweener.Apply<Quaternion, Vector3>(() => target.rotation, x => target.rotation = x, endValue,
                    new QuaternionFromEulerPlugin(), duration);
            customTween.OnRewind(() => { target.rotation = Quaternion.Euler(customTween.startValue); });
            SetEditorInfo(customTween, target);

            return customTween;
        }

        public static CustomTweenCore<Quaternion, Quaternion> TweenRotateQuaternion(this Transform target,
            Quaternion endValue, float duration)
        {
            var tween = CustomTweener.Apply<Quaternion, Quaternion>(() => target.rotation, x => target.rotation = x,
                endValue, new QuaternionPlugin(), duration);
            tween.OnRewind(() => { target.rotation = tween.startValue; });
            SetEditorInfo(tween, target);

            return tween;
        }

        public static CustomTweenCore<Quaternion, Vector3> TweenLocalRotate(this Transform target,
            Vector3 endValue,
            float duration)
        {
            var customTween =
                CustomTweener.Apply<Quaternion, Vector3>(() => target.localRotation, x => target.localRotation = x,
                    endValue, new QuaternionFromEulerPlugin(), duration);
            customTween.OnRewind(() => { target.localRotation = Quaternion.Euler(customTween.startValue); });

            SetEditorInfo(customTween, target);

            return customTween;
        }

        public static CustomTweenCore<Quaternion, Quaternion> TweenLocalRotateQuaternion(
            this Transform target,
            Quaternion endValue, float duration)
        {
            var customTween =
                CustomTweener.Apply<Quaternion, Quaternion>(() => target.localRotation, x => target.localRotation = x,
                    endValue, new QuaternionPlugin(), duration);
            customTween.OnRewind(() => { target.localRotation = customTween.startValue; });
            SetEditorInfo(customTween, target);

            return customTween;
        }

        [Conditional("UNITY_EDITOR")]
        private static void SetEditorInfo(CustomTween tween, Object target, [CallerMemberName] string tweenName = "")
        {
            tween.SetEditorInfo(target, tweenName);
        }
    }
}