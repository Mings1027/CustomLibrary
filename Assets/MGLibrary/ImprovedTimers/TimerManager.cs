using System.Collections.Generic;

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
    }
}