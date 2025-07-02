using System;
using MGLibrary.CustomTween;
using UnityEngine;

namespace MGLibrary.ImprovedTimers
{
    public class ConditionalTimer : Timer
    {
        public Func<bool> Condition { get; private set; }
        public Action OnConditionMet = delegate { };
        public Action OnConditionLost = delegate { };

        private bool _wasConditionMet;

        /// <summary>
        /// 조건부 타이머 생성
        /// </summary>
        /// <param name="value">타이머 시간</param>
        /// <param name="condition">진행 조건을 확인하는 함수</param>
        public ConditionalTimer(float value, Func<bool> condition = null) : base(value)
        {
            Condition = condition ?? (() => true);
        }

        public override void Tick()
        {
            if (!IsRunning) return;

            bool conditionMet = Condition.Invoke();

            // 조건 상태 변화 감지
            if (conditionMet != _wasConditionMet)
            {
                if (conditionMet)
                    OnConditionMet.Invoke();
                else
                    OnConditionLost.Invoke();

                _wasConditionMet = conditionMet;
            }

            // 조건이 만족될 때만 시간 감소
            if (conditionMet && CurrentTime > 0)
            {
                CurrentTime -= Time.deltaTime;
            }

            if (IsRunning && CurrentTime <= 0)
            {
                Stop();
            }
        }

        public override bool IsFinished => CurrentTime <= 0;

        /// <summary>
        /// 현재 조건이 만족되고 있는지 확인
        /// </summary>
        public bool IsConditionMet => Condition.Invoke();

        /// <summary>
        /// 조건을 런타임에 변경
        /// </summary>
        public void SetCondition(Func<bool> newCondition)
        {
            Condition = newCondition ?? (() => true);
        }

        public override void Reset()
        {
            base.Reset();
            _wasConditionMet = false;
        }
    }

    public class EaseTimer : Timer
    {
        public EaseType EasingType { get; set; }
        private Func<float, float> _easeFunc;

        public EaseTimer(float value, EaseType easeType = EaseType.Linear) : base(value)
        {
            EasingType = easeType;
            _easeFunc = TimerManager.GetEaseFunction(easeType);
        }

        public override void Tick()
        {
            if (IsRunning && CurrentTime > 0)
            {
                CurrentTime -= Time.deltaTime;
            }

            if (IsRunning && CurrentTime <= 0)
            {
                Stop();
            }
        }

        public override bool IsFinished => CurrentTime <= 0;
    }

    public class RepeatingTimer : Timer
    {
        public Action OnRepeat = delegate { };
        public int RepeatCount { get; private set; }
        public int MaxRepeats { get; private set; }

        /// <summary>
        /// Creates a repeating timer
        /// </summary>
        /// <param name="value"> </param>
        /// <param name="maxRepeats"> -1 = Infinite repeats</param>
        public RepeatingTimer(float value, int maxRepeats = -1) : base(value)
        {
            MaxRepeats = maxRepeats;
        }

        public override void Tick()
        {
            if (!IsRunning) return;

            CurrentTime -= Time.deltaTime;

            if (CurrentTime <= 0)
            {
                RepeatCount++;
                OnRepeat.Invoke();

                if (MaxRepeats > 0 && RepeatCount >= MaxRepeats)
                {
                    Stop();
                }
                else
                {
                    CurrentTime = initTime;
                }
            }
        }

        public override bool IsFinished => MaxRepeats > 0 && RepeatCount >= MaxRepeats;

        public override void Reset()
        {
            base.Reset();
            RepeatCount = 0;
        }
    }

    public class CountdownTimer : Timer
    {
        public CountdownTimer(float value) : base(value) { }

        public override void Tick()
        {
            if (IsRunning && CurrentTime > 0)
            {
                CurrentTime -= Time.deltaTime;
            }

            if (IsRunning && CurrentTime <= 0)
            {
                Stop();
            }
        }

        public override bool IsFinished => CurrentTime <= 0;
    }

    public abstract class Timer : IDisposable
    {
        public float CurrentTime { get; protected set; }
        public bool IsRunning { get; private set; }
        protected float initTime;

        public float Progress => Mathf.Clamp(CurrentTime / initTime, 0, 1);

        public Action OnTimerStart = delegate { };
        public Action OnTimerStop = delegate { };

        protected Timer(float value)
        {
            initTime = value;
        }

        public void Start()
        {
            CurrentTime = initTime;
            if (!IsRunning)
            {
                IsRunning = true;
                TimerManager.RegisterTimer(this);
                OnTimerStart.Invoke();
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                TimerManager.UnregisterTimer(this);
                OnTimerStop.Invoke();
            }
        }

        public abstract void Tick();
        public abstract bool IsFinished { get; }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public virtual void Reset() => CurrentTime = initTime;

        public virtual void Reset(float newTime)
        {
            initTime = newTime;
            Reset();
        }

        bool disposed;
        ~Timer() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                TimerManager.UnregisterTimer(this);
            }

            disposed = true;
        }
    }
}