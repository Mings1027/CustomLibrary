using UnityEngine;

namespace MGLibrary.CustomTween
{
    public interface ITweenPlugin<out T1, in T2>
    {
        void EvaluateAndApply(T2 startValue, T2 endValue, TweenSetter<T1> setter,
            float progress);
    }

    public struct VectorPlugin : ITweenPlugin<Vector3, Vector3>
    {
        public void EvaluateAndApply(Vector3 startValue, Vector3 endValue, TweenSetter<Vector3> setter, float progress)
        {
            setter(Vector3.Lerp(startValue, endValue, progress));
        }
    }

    public struct QuaternionPlugin : ITweenPlugin<Quaternion, Quaternion>
    {
        public void EvaluateAndApply(Quaternion startValue, Quaternion endValue, TweenSetter<Quaternion> setter, float progress)
        {
            setter(Quaternion.Slerp(startValue, endValue, progress));
        }
    }
    
    public struct QuaternionFromEulerPlugin : ITweenPlugin<Quaternion, Vector3>
    {
        public void EvaluateAndApply(Vector3 startValue, Vector3 endValue, TweenSetter<Quaternion> setter, float progress)
        {
            Quaternion startQuat = Quaternion.Euler(startValue);
            Quaternion endQuat = Quaternion.Euler(endValue);
            Quaternion result = Quaternion.Slerp(startQuat, endQuat, progress);
            setter(result);
        }
    }
}