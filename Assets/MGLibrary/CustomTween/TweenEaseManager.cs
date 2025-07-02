using System;
using UnityEngine;

namespace MGLibrary.CustomTween
{
    public static class TweenEaseManager
    {
        // Constants matching DOTween implementation
        private const float PiOver2 = 1.5707964f;
        private const float TwoPi = 6.2831855f;

        // Default values for overshoot and period
        private const float DefaultOvershoot = 1.70158f;
        private const float DefaultPeriod = 0.3f;

        public static Func<float, float> GetEaseFunction(EaseType type)
        {
            return GetEaseFunction(type, DefaultOvershoot, period: DefaultPeriod);
        }

        public static Func<float, float> GetEaseFunction(EaseType type, float overshootOrAmplitude, float period = DefaultPeriod)
        {
            return t => Evaluate(type, t, 1f, overshootOrAmplitude, period);
        }

        /// <summary>
        /// Returns a value between 0 and 1 (inclusive) based on the elapsed time and ease selected
        /// </summary>
        public static float Evaluate(EaseType easeType, float time, float duration, float overshootOrAmplitude, float period)
        {
            switch (easeType)
            {
                case EaseType.Linear:
                    return time / duration;

                // Sine
                case EaseType.InSine:
                    return -Mathf.Cos(time / duration * PiOver2) + 1f;
                case EaseType.OutSine:
                    return Mathf.Sin(time / duration * PiOver2);
                case EaseType.InOutSine:
                    return -0.5f * (Mathf.Cos(Mathf.PI * time / duration) - 1f);

                // Quad
                case EaseType.InQuad:
                    return (time /= duration) * time;
                case EaseType.OutQuad:
                    return -(time /= duration) * (time - 2f);
                case EaseType.InOutQuad:
                    return (time /= duration * 0.5f) < 1f ? 0.5f * time * time : -0.5f * (--time * (time - 2f) - 1f);

                // Cubic
                case EaseType.InCubic:
                    return (time /= duration) * time * time;
                case EaseType.OutCubic:
                    return (time = time / duration - 1f) * time * time + 1f;
                case EaseType.InOutCubic:
                    return (time /= duration * 0.5f) < 1f ? 0.5f * time * time * time : 0.5f * ((time -= 2f) * time * time + 2f);

                // Quart
                case EaseType.InQuart:
                    return (time /= duration) * time * time * time;
                case EaseType.OutQuart:
                    return -((time = time / duration - 1f) * time * time * time - 1f);
                case EaseType.InOutQuart:
                    return (time /= duration * 0.5f) < 1f ? 0.5f * time * time * time * time : -0.5f * ((time -= 2f) * time * time * time - 2f);

                // Quint
                case EaseType.InQuint:
                    return (time /= duration) * time * time * time * time;
                case EaseType.OutQuint:
                    return (time = time / duration - 1f) * time * time * time * time + 1f;
                case EaseType.InOutQuint:
                    return (time /= duration * 0.5f) < 1f ? 0.5f * time * time * time * time * time : 0.5f * ((time -= 2f) * time * time * time * time + 2f);

                // Expo
                case EaseType.InExpo:
                    return Mathf.Approximately(time, 0f) ? 0f : Mathf.Pow(2f, 10f * (time / duration - 1f));
                case EaseType.OutExpo:
                    return Mathf.Approximately(time, duration) ? 1f : -Mathf.Pow(2f, -10f * time / duration) + 1f;
                case EaseType.InOutExpo:
                    if (Mathf.Approximately(time, 0f)) return 0f;
                    if (Mathf.Approximately(time, duration)) return 1f;
                    return (time /= duration * 0.5f) < 1f ? 0.5f * Mathf.Pow(2f, 10f * (time - 1f)) : 0.5f * (-Mathf.Pow(2f, -10f * --time) + 2f);

                // Circ
                case EaseType.InCirc:
                    return -(Mathf.Sqrt(1f - (time /= duration) * time) - 1f);
                case EaseType.OutCirc:
                    return Mathf.Sqrt(1f - (time = time / duration - 1f) * time);
                case EaseType.InOutCirc:
                    return (time /= duration * 0.5f) < 1f ? -0.5f * (Mathf.Sqrt(1f - time * time) - 1f) : 0.5f * (Mathf.Sqrt(1f - (time -= 2f) * time) + 1f);

                // Back
                case EaseType.InBack:
                    return (time /= duration) * time * ((overshootOrAmplitude + 1f) * time - overshootOrAmplitude);
                case EaseType.OutBack:
                    return (time = time / duration - 1f) * time * ((overshootOrAmplitude + 1f) * time + overshootOrAmplitude) + 1f;
                case EaseType.InOutBack:
                    return (time /= duration * 0.5f) < 1f ? 0.5f * (time * time * (((overshootOrAmplitude *= 1.525f) + 1f) * time - overshootOrAmplitude)) : 0.5f * ((time -= 2f) * time * (((overshootOrAmplitude *= 1.525f) + 1f) * time + overshootOrAmplitude) + 2f);

                // Elastic
                case EaseType.InElastic:
                    if (Mathf.Approximately(time, 0f)) return 0f;
                    if (Mathf.Approximately(time /= duration, 1f)) return 1f;
                    if (Mathf.Approximately(period, 0f)) period = duration * 0.3f;
                    float s1;
                    if (overshootOrAmplitude < 1f)
                    {
                        overshootOrAmplitude = 1f;
                        s1 = period / 4f;
                    }
                    else
                        s1 = period / TwoPi * Mathf.Asin(1f / overshootOrAmplitude);
                    return -(overshootOrAmplitude * Mathf.Pow(2f, 10f * --time) * Mathf.Sin((time * duration - s1) * TwoPi / period));

                case EaseType.OutElastic:
                    if (Mathf.Approximately(time, 0f)) return 0f;
                    if (Mathf.Approximately(time /= duration, 1f)) return 1f;
                    if (Mathf.Approximately(period, 0f)) period = duration * 0.3f;
                    float s2;
                    if (overshootOrAmplitude < 1f)
                    {
                        overshootOrAmplitude = 1f;
                        s2 = period / 4f;
                    }
                    else
                        s2 = period / TwoPi * Mathf.Asin(1f / overshootOrAmplitude);
                    return overshootOrAmplitude * Mathf.Pow(2f, -10f * time) * Mathf.Sin((time * duration - s2) * TwoPi / period) + 1f;

                case EaseType.InOutElastic:
                    if (Mathf.Approximately(time, 0f)) return 0f;
                    if (Mathf.Approximately(time /= duration * 0.5f, 2f)) return 1f;
                    if (Mathf.Approximately(period, 0f)) period = duration * 0.45000002f;
                    float s3;
                    if (overshootOrAmplitude < 1f)
                    {
                        overshootOrAmplitude = 1f;
                        s3 = period / 4f;
                    }
                    else
                        s3 = period / TwoPi * Mathf.Asin(1f / overshootOrAmplitude);
                    return time < 1f ? -0.5f * (overshootOrAmplitude * Mathf.Pow(2f, 10f * --time) * Mathf.Sin((time * duration - s3) * TwoPi / period)) : overshootOrAmplitude * Mathf.Pow(2f, -10f * --time) * Mathf.Sin((time * duration - s3) * TwoPi / period) * 0.5f + 1f;

                // Bounce
                case EaseType.InBounce:
                    return EaseInBounce(time, duration);
                case EaseType.OutBounce:
                    return EaseOutBounce(time, duration);
                case EaseType.InOutBounce:
                    return time < duration * 0.5f ? EaseInBounce(time * 2f, duration) * 0.5f : EaseOutBounce(time * 2f - duration, duration) * 0.5f + 0.5f;

                default:
                    return time / duration;
            }
        }

        // Bounce helper functions matching DOTween implementation
        private static float EaseInBounce(float time, float duration)
        {
            return 1f - EaseOutBounce(duration - time, duration);
        }

        private static float EaseOutBounce(float time, float duration)
        {
            if ((time /= duration) < 1f / 2.75f)
            {
                return 7.5625f * time * time;
            }

            if (time < 2f / 2.75f)
            {
                return 7.5625f * (time -= 1.5f / 2.75f) * time + 0.75f;
            }
            if (time < 2.5f / 2.75f)
            {
                return 7.5625f * (time -= 2.25f / 2.75f) * time + 0.9375f;
            }

            return 7.5625f * (time -= 2.625f / 2.75f) * time + 0.984375f;
        }
    }
}