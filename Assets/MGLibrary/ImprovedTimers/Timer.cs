using System;
using UnityEngine;

namespace MGLibrary.ImprovedTimers
{
    public class CountdownTimer : Timer
    {
        public CountdownTimer(float value) : base(value) { }

        internal override void Tick()
        {
            if (State == TimerState.Running)
            {
                if (CurrentTime > 0)
                {
                    _currentTime -= Time.deltaTime;
                    OnTick.Invoke();
                    OnProgress.Invoke(Progress);
                }
                else
                {
                    State = TimerState.Complete;
                    TimerManager.UnregisterTimer(this);
                    OnTimerComplete.Invoke();
                }
            }
        }
    }

    public abstract class Timer : IDisposable
    {
        public enum TimerState
        {
            Ready,
            Running,
            Paused,
            Complete,
        }

        public float CurrentTime => _currentTime;
        public float Duration => _duration;

        public TimerState State { get; protected set; }

        public bool IsReady => State == TimerState.Ready;
        public bool IsRunning => State == TimerState.Running;
        public bool IsPaused => State == TimerState.Paused;
        public bool IsFinished => State == TimerState.Complete;

        public float Progress => Mathf.Clamp(_currentTime / _duration, 0, 1);

        public Action OnTimerPlay = delegate { };
        public Action OnTimerResume = delegate { };
        public Action OnTimerPause = delegate { };
        public Action OnTick = delegate { };
        public Action<float> OnProgress = delegate { };
        public Action OnTimerComplete = delegate { };

        protected float _currentTime;
        private float _duration;

        protected Timer(float value)
        {
            _duration = value;
            _currentTime = value;
            State = TimerState.Ready;
        }

        public void Play()
        {
            if (State == TimerState.Running) return;

            if (State == TimerState.Ready)
            {
                OnTimerPlay.Invoke();
            }
            else if (State == TimerState.Paused)
            {
                OnTimerResume.Invoke();
            }

            State = TimerState.Running;
            TimerManager.RegisterTimer(this);
        }

        public void Pause()
        {
            if (State == TimerState.Running)
            {
                State = TimerState.Paused;
                TimerManager.UnregisterTimer(this);
                OnTimerPause.Invoke();
            }
        }

        public void Rewind()
        {
            if (State == TimerState.Running)
            {
                TimerManager.UnregisterTimer(this);
            }

            State = TimerState.Ready;
            _currentTime = _duration;
        }

        public void Restart()
        {
            Rewind();
            Play();
        }

        public void ResetTime(float newTime)
        {
            Rewind();
            _duration = newTime;
            _currentTime = newTime;
        }

        internal abstract void Tick();

        private bool _disposed;
        ~Timer() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                TimerManager.UnregisterTimer(this);
            }

            _disposed = true;
        }
    }
}