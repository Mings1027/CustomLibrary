using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MGLibrary.CustomTween
{
    public enum TweenState
    {
        Ready,
        Playing,
        Pause,
        Complete
    }

    [System.Serializable]
    public abstract class CustomTween : IDisposable
    {
        internal float elapsedTime;
        internal float duration;
        internal TweenState state;
        internal EaseType easeType;
        internal Func<float, float> easeFunc;
        // internal Action<float> updateCallback;

        internal Action onStart;
        internal Action onPlay;
        internal Action onUpdate;
        internal Action onComplete;
        internal Action onRewind;

        internal bool autoKill = true;
        private bool _playedOnce;

        internal SequenceType sequenceType;
        internal bool isFrom = true;

        internal int loopCount; // 0 = no loop, -1 = infinite, >0 = specific count
        internal int currentLoop;
        internal bool isLooping;

        protected CustomTween()
        {
#if UNITY_EDITOR
            Id = _nextId++;
#endif
        }

        internal void Init(float duration)
        {
            elapsedTime = 0;
            state = TweenState.Ready;
            this.duration = duration;
            easeFunc = TweenEaseManager.GetEaseFunction(EaseType.Linear);
            TweenManager.RegisterTween(this);
        }

        internal virtual CustomTween SetLoop(int count)
        {
            loopCount = count;
            currentLoop = 0;
            isLooping = count != 0;
            return this;
        }

        internal virtual CustomTween SetAutoKill(bool autoKill)
        {
            this.autoKill = autoKill;
            return this;
        }

        internal virtual CustomTween SetEase(EaseType easeType)
        {
            this.easeType = easeType;
            easeFunc = TweenEaseManager.GetEaseFunction(easeType);
            return this;
        }

        internal virtual CustomTween PlayInternal()
        {
            if (state == TweenState.Complete)
            {
                return this;
            }

            if (!_playedOnce)
            {
                _playedOnce = true;
                onStart?.Invoke();
            }

            if (state != TweenState.Playing)
            {
                state = TweenState.Playing;
                onPlay?.Invoke();
            }

            return this;
        }

        internal virtual CustomTween PauseInternal()
        {
            if (state == TweenState.Playing)
            {
                state = TweenState.Pause;
            }

            return this;
        }

        internal virtual CustomTween ReplayInternal()
        {
            Reset();
            state = TweenState.Playing;

            TweenManager.RegisterTween(this);
            return this;
        }

        internal virtual CustomTween RewindInternal()
        {
            Reset();
            TweenManager.RegisterTween(this);
            return this;
        }

        internal virtual CustomTween KillInternal()
        {
            state = TweenState.Complete;
            TweenManager.UnregisterAndKillTween(this);
            return this;
        }

        internal virtual void Update()
        {
            if (state != TweenState.Playing) return;

            elapsedTime += Time.deltaTime;
            var progress = easeFunc(Mathf.Clamp01(elapsedTime / duration));
            UpdateValue(progress);

            onUpdate?.Invoke();

            if (elapsedTime >= duration)
            {
                Complete();
            }
        }

        protected virtual void UpdateValue(float progress) { }

        internal virtual void Complete()
        {
            if (isLooping && ShouldContinueLooping())
            {
                currentLoop++;
                elapsedTime = 0;
                state = TweenState.Playing;

                onComplete?.Invoke();
                return;
            }

            state = TweenState.Complete;
            onComplete?.Invoke();
            if (autoKill && sequenceType == SequenceType.None)
            {
                TweenManager.MarkForAutoKill(this);
            }
        }

        protected bool ShouldContinueLooping()
        {
            if (loopCount == -1) return true; // Infinite loop
            return currentLoop < loopCount - 1; // Check if we have more loops to go
        }

        internal virtual void Reset()
        {
            elapsedTime = 0;
            state = TweenState.Ready;
            currentLoop = 0;
            onRewind?.Invoke();
        }

        internal virtual void Clear()
        {
            // 완전 초기화 (풀로 반환할 때 사용)
            elapsedTime = 0;
            duration = 0;
            easeType = EaseType.Linear;
            easeFunc = null;
            onStart = null;
            onComplete = null;
            onRewind = null;
            state = TweenState.Ready;
            autoKill = true;
            _playedOnce = false;
            loopCount = 0;
            currentLoop = 0;
            isLooping = false;
        }

        public void Dispose()
        {
            TweenManager.UnregisterTween(this);
        }

#if UNITY_EDITOR
        private static int _nextId = 1;
        internal int Id { get; private set; }

        internal UnityEngine.Object TargetObject { get; private set; }
        internal string TweenName { get; private set; }

        [Conditional("UNITY_EDITOR")]
        internal void SetEditorInfo(UnityEngine.Object targetObject, string tweenMethodName)
        {
            TargetObject = targetObject;
            TweenName = tweenMethodName;
        }
#endif
    }
}