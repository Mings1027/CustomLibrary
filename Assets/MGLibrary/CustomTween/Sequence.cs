using System.Collections.Generic;
using UnityEngine;

namespace MGLibrary.CustomTween
{
    public class Sequence : CustomTween
    {
        private readonly List<CustomTween> _tweens = new();
        private int _currentIndex;

#if UNITY_EDITOR
        internal List<CustomTween> Tweens => _tweens;
        internal int CurrentIndex => _currentIndex;
#endif

        internal static Sequence Insert(Sequence inSequence, CustomTween t)
        {
            TweenManager.UnregisterTween(t);
            t.sequenceType = SequenceType.Append;
            inSequence.duration += t.duration;
            inSequence._tweens.Add(t);

            return inSequence;
        }

        internal static Sequence JoinInsert(Sequence inSequence, CustomTween t)
        {
            if (inSequence._tweens.Count == 0)
            {
                return Insert(inSequence, t);
            }

            TweenManager.UnregisterTween(t);
            t.sequenceType = SequenceType.Join;
            inSequence._tweens.Add(t);

            return inSequence;
        }

        internal static Sequence WaitInsert(Sequence inSequence, float duration)
        {
            var delayTween = TweenManager.GetTweenCore<float, float>();
            delayTween.Init(duration);
            TweenManager.UnregisterTween(delayTween);
            delayTween.sequenceType = SequenceType.Wait;
            inSequence.duration += duration;
            inSequence._tweens.Add(delayTween);

            return inSequence;
        }

        public static Sequence Create()
        {
            var sequence = TweenManager.GetSequence();
            TweenManager.RegisterTween(sequence);
            return sequence;
        }

        internal override void Update()
        {
            if (state != TweenState.Playing || _tweens.Count == 0) return;
            UpdateInternalTweens();

            if (_currentIndex >= _tweens.Count)
            {
                Complete();
                return;
            }

            StartCurrentTweens();
            CheckAndMoveToNext();
        }

        private void UpdateInternalTweens()
        {
            for (int i = 0; i < _tweens.Count; i++)
            {
                var tween = _tweens[i];
                if (tween.state == TweenState.Playing)
                {
                    tween.Update();
                }
            }
        }

        internal override void Complete()
        {
            if (isLooping && ShouldContinueLooping())
            {
                currentLoop++;
                _currentIndex = 0;

                for (int i = 0; i < _tweens.Count; i++)
                {
                    _tweens[i].Reset();
                }

                elapsedTime = 0f;
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

        private void StartCurrentTweens()
        {
            if (_currentIndex >= _tweens.Count) return;

            var currentTween = _tweens[_currentIndex];

            if (currentTween.state != TweenState.Playing && currentTween.state != TweenState.Complete)
            {
                currentTween.PlayInternal();
            }

            int nextIndex = _currentIndex + 1;
            while (nextIndex < _tweens.Count && _tweens[nextIndex].sequenceType == SequenceType.Join)
            {
                var joinedTween = _tweens[nextIndex];
                if (joinedTween.state != TweenState.Playing && joinedTween.state != TweenState.Complete)
                {
                    joinedTween.PlayInternal();
                }

                nextIndex++;
            }
        }

        private void CheckAndMoveToNext()
        {
            if (_currentIndex >= _tweens.Count) return;

            bool allCompleted = _tweens[_currentIndex].state == TweenState.Complete;

            int nextIndex = _currentIndex + 1;
            while (nextIndex < _tweens.Count && _tweens[nextIndex].sequenceType == SequenceType.Join)
            {
                allCompleted &= _tweens[nextIndex].state == TweenState.Complete;
                nextIndex++;
            }

            if (allCompleted)
            {
                _currentIndex = nextIndex;
            }
        }

        internal override CustomTween SetLoop(int count)
        {
            base.SetLoop(count);

            for (int i = 0; i < _tweens.Count; i++)
            {
                _tweens[i].SetLoop(count);
            }

            return this;
        }

        internal override CustomTween PlayInternal()
        {
            base.PlayInternal();

            for (var i = 0; i < _tweens.Count; i++)
            {
                var tween = _tweens[i];
                if (tween.state == TweenState.Pause)
                {
                    tween.PlayInternal();
                }
            }

            return this;
        }

        internal override CustomTween PauseInternal()
        {
            base.PauseInternal();

            for (var i = 0; i < _tweens.Count; i++)
            {
                var tween = _tweens[i];
                if (tween.state == TweenState.Playing)
                {
                    tween.PauseInternal();
                }
            }

            return this;
        }

        internal override CustomTween ReplayInternal()
        {
            _currentIndex = 0;
            for (var i = _tweens.Count - 1; i >= 0; i--)
            {
                var tween = _tweens[i];
                tween.Reset();
            }

            base.ReplayInternal();
            return this;
        }

        internal override CustomTween RewindInternal()
        {
            _currentIndex = 0;
            for (var i = _tweens.Count - 1; i >= 0; i--)
            {
                var tween = _tweens[i];
                tween.Reset();
            }

            base.RewindInternal();
            return this;
        }


        internal override CustomTween KillInternal()
        {
            for (int i = 0; i < _tweens.Count; i++)
            {
                _tweens[i].KillInternal();
            }

            _tweens.Clear();
            base.KillInternal();
            return this;
        }

        internal override CustomTween SetAutoKill(bool autoKill)
        {
            base.SetAutoKill(autoKill);

            for (int i = 0; i < _tweens.Count; i++)
            {
                _tweens[i].SetAutoKill(autoKill);
            }

            return this;
        }

        internal override CustomTween SetEase(EaseType easeType)
        {
            base.SetEase(easeType);

            for (int i = 0; i < _tweens.Count; i++)
            {
                _tweens[i].SetEase(easeType);
            }

            return this;
        }

        internal override void Clear()
        {
            for (int i = 0; i < _tweens.Count; i++)
            {
                _tweens[i]?.Clear();
            }

            _tweens.Clear();
            _currentIndex = 0;

            base.Clear();
        }
    }
}