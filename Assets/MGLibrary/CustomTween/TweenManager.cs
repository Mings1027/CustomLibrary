using System;
using System.Collections.Generic;
using UnityEngine;

namespace MGLibrary.CustomTween
{
    public static class TweenManager
    {
        private static CustomTween[] _activeTweens = new CustomTween[32]; // 초기 크기
        private static int _activeTweenCount = 0;
        private static readonly Stack<CustomTween> _tweenPool = new();
        private static readonly Stack<Sequence> _sequencePool = new();

        public static void RegisterTween(CustomTween tween)
        {
            // 이미 등록된 트윈인지 확인
            for (int i = 0; i < _activeTweenCount; i++)
            {
                if (_activeTweens[i] == tween)
                    return;
            }

            // 배열 크기가 부족하면 확장
            if (_activeTweenCount >= _activeTweens.Length)
            {
                Array.Resize(ref _activeTweens, _activeTweens.Length * 2);
                Debug.Log($"TweenManager: Array resized to {_activeTweens.Length}");
            }

            _activeTweens[_activeTweenCount] = tween;
            _activeTweenCount++;
        }

        public static void UnregisterTween(CustomTween tween)
        {
            for (int i = 0; i < _activeTweenCount; i++)
            {
                if (_activeTweens[i] == tween)
                {
                    // 마지막 요소를 현재 위치로 이동 (순서는 중요하지 않음)
                    _activeTweens[i] = _activeTweens[_activeTweenCount - 1];
                    _activeTweens[_activeTweenCount - 1] = null;
                    _activeTweenCount--;
                    return;
                }
            }
        }

        public static void UnregisterAndKillTween(CustomTween tween)
        {
            UnregisterTween(tween);
            ReturnTween(tween);
        }

        public static void MarkForAutoKill(CustomTween tween)
        {
            UnregisterTween(tween);
            if (tween is Sequence sequence)
            {
                ReturnSequence(sequence);
            }
            else
            {
                ReturnTween(tween);
            }
        }

        public static CustomTweenCore<T1, T2> GetTweenCore<T1, T2>()
        {
            if (_tweenPool.Count > 0)
            {
                var pooledTween = _tweenPool.Pop();
                pooledTween.Reset(); // 풀에서 가져올 때 초기화
                return (CustomTweenCore<T1, T2>)pooledTween;
            }

            return new CustomTweenCore<T1, T2>();
        }

        public static void ReturnTween(CustomTween tween)
        {
            if (tween == null) return;

            UnregisterTween(tween);
            tween.Clear(); // 완전 초기화
            _tweenPool.Push(tween);
        }

        public static void UpdateTweens()
        {
            // 역순으로 순회하여 안전하게 제거
            for (int i = _activeTweenCount - 1; i >= 0; i--)
            {
                var tween = _activeTweens[i];
                if (tween == null) continue;

                tween.Update();

                // 트윈이 완료된 경우 처리
                if (tween.state == TweenState.Complete)
                {
                    if (tween.autoKill)
                    {
                        MarkForAutoKill(tween);
                    }
                }
            }
        }

        public static Sequence GetSequence()
        {
            if (_sequencePool.Count > 0)
            {
                var seq = _sequencePool.Pop();
                seq.Reset();
                return seq;
            }

            var newSeq = new Sequence();
            return newSeq;
        }

        public static void ReturnSequence(Sequence seq)
        {
            if (seq == null) return;
            UnregisterTween(seq);
            seq.Clear();
            _sequencePool.Push(seq);
        }

        public static void Clear()
        {
            // 모든 활성 트윈들을 정리
            for (int i = 0; i < _activeTweenCount; i++)
            {
                if (_activeTweens[i] != null)
                {
                    _activeTweens[i].Dispose();
                    _activeTweens[i] = null;
                }
            }

            _activeTweenCount = 0;
            _tweenPool.Clear();
            _sequencePool.Clear();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 활성 트윈 목록을 반환합니다
        /// </summary>
        public static List<CustomTween> GetAllTweens()
        {
            var result = new List<CustomTween>(_activeTweenCount);
            for (int i = 0; i < _activeTweenCount; i++)
            {
                if (_activeTweens[i] != null)
                {
                    result.Add(_activeTweens[i]);
                }
            }

            return result;
        }

        public static void PlayAllTweens()
        {
            for (int i = 0; i < _activeTweenCount; i++)
            {
                if (_activeTweens[i] != null)
                {
                    _activeTweens[i].PlayInternal();
                }
            }
        }

        public static void PauseAllTweens()
        {
            for (int i = 0; i < _activeTweenCount; i++)
            {
                if (_activeTweens[i] != null)
                {
                    _activeTweens[i].PauseInternal();
                }
            }
        }

        public static void ReplayAllTweens()
        {
            // 현재 활성 트윈들만 리플레이
            for (int i = 0; i < _activeTweenCount; i++)
            {
                if (_activeTweens[i] != null)
                {
                    _activeTweens[i].ReplayInternal();
                }
            }
        }

        public static void RewindAllTweens()
        {
            for (int i = 0; i < _activeTweenCount; i++)
            {
                if (_activeTweens[i] != null)
                {
                    _activeTweens[i].RewindInternal();
                }
            }
        }

        public static void KillAllTweens()
        {
            for (int i = _activeTweenCount - 1; i >= 0; i--)
            {
                if (_activeTweens[i] != null)
                {
                    _activeTweens[i].KillInternal();
                }
            }
        }

        // 디버깅용 메서드들
        public static int GetActiveTweenCount() => _activeTweenCount;
        public static int GetArrayCapacity() => _activeTweens.Length;
        public static float GetArrayUtilization() => (float)_activeTweenCount / _activeTweens.Length;
#endif
    }
}