using System;
using System.Collections.Generic;
using MGLibrary.CustomTween;
using UnityEngine;

namespace MGLibrary.ImprovedTimers
{
    public static class TimerManager
    {
        static readonly List<Timer> timers = new();

        public static void RegisterTimer(Timer timer) => timers.Add(timer);
        public static void UnregisterTimer(Timer timer) => timers.Remove(timer);

        public static void UpdateTimers()
        {
            // 원본에서 원소가 제거되면 인덱스가 앞쪽으로 한칸씩 이동하기때문에 원본을 그대로 사용하지 않고 아래와 같이 사용함
            // foreach (var timer in new List<Timer>(timers))
            // {
            //     timer.Tick();
            // }

            // 순방향으로 할 시 위 foreach를 원본으로 하는것과 같은 문제가 생기기 때문에 역순으로 함
            // 해당 방법이 메모리 할당을 하지않아 효율적임
            for (var i = timers.Count - 1; i >= 0; i--)
            {
                timers[i].Tick();
            }
        }

        public static void Clear() => timers.Clear();

        public static Func<float, float> GetEaseFunction(EaseType type)
        {
            return type switch
            {
                EaseType.Linear => t => t,
                EaseType.InQuad => t => t * t,
                EaseType.OutQuad => t => t * (2f - t),
                EaseType.InOutQuad => t => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
                EaseType.InCubic => t => t * t * t,
                EaseType.OutCubic => t => (t - 1f) * (t - 1f) * (t - 1f) + 1f,
                EaseType.InOutCubic => t => t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f,
                EaseType.InQuart => t => t * t * t * t,
                EaseType.OutQuart => t => 1f - Mathf.Pow(t - 1f, 4f),
                EaseType.InOutQuart => t => t < 0.5f ? 8f * Mathf.Pow(t, 4f) : 1f - 8f * Mathf.Pow(t - 1f, 4f),
                EaseType.InSine => t => 1f - Mathf.Cos(t * Mathf.PI / 2f),
                EaseType.OutSine => t => Mathf.Sin(t * Mathf.PI / 2f),
                EaseType.InOutSine => t => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f,
                EaseType.InExpo => t => Mathf.Approximately(t, 0f) ? 0f : Mathf.Pow(2f, 10f * (t - 1f)),
                EaseType.OutExpo => t => Mathf.Approximately(t, 1f) ? 1f : 1f - Mathf.Pow(2f, -10f * t),
                EaseType.InOutExpo => t =>
                {
                    if (Mathf.Approximately(t, 0f)) return 0f;
                    if (Mathf.Approximately(t, 1f)) return 1f;
                    return t < 0.5f
                        ? Mathf.Pow(2f, 20f * t - 10f) / 2f
                        : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
                },
                _ => t => t
            };
        }
    }
}