using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace MGLibrary.CustomTween
{
    [Serializable]
    public class TweenData
    {
        [SerializeField] internal bool isSelf = true;
        [SerializeField] internal bool isFrom;

        [SerializeField] internal TweenType tweenType;
        [SerializeField] internal float duration = 1f;
        [SerializeField] internal EaseType easeType = EaseType.Linear;
        [SerializeField] internal int loopCount;

        [SerializeField] internal SequenceType sequenceType = SequenceType.Append;

        [SerializeField] internal bool useValue = true;
        [SerializeField] internal Transform endValueTransform;
        [SerializeField] internal Vector3 endValueV3 = Vector3.zero;
        [SerializeField] internal Vector3 endValueV2 = Vector3.zero;
        [SerializeField] internal float endValueFloat;

        [SerializeField] internal Transform targetTransform;

        [SerializeField] internal UnityEvent onStart;
        [SerializeField] internal UnityEvent onPlay;
        [SerializeField] internal UnityEvent onUpdate;
        [SerializeField] internal UnityEvent onComplete;
        [SerializeField] internal UnityEvent onRewind;

        [SerializeField] internal bool isExpanded = true;
        [SerializeField] internal bool hasOnStart;
        [SerializeField] internal bool hasOnPlay;
        [SerializeField] internal bool hasOnUpdate;
        [SerializeField] internal bool hasOnComplete;
        [SerializeField] internal bool hasOnRewind;

        internal void Reset()
        {
            isFrom = false;
            tweenType = TweenType.None;
            duration = 1f;
            easeType = EaseType.Linear;
            loopCount = 0;
            sequenceType = SequenceType.Append;
            useValue = true;
            endValueTransform = null;
            endValueV3 = default;
            endValueV2 = default;
            endValueFloat = 0;
            targetTransform = null;
            onStart = null;
            onPlay = null;
            onUpdate = null;
            onComplete = null;
            onRewind = null;
            isExpanded = true;
            hasOnStart = false;
            hasOnPlay = false;
            hasOnUpdate = false;
            hasOnComplete = false;
            hasOnRewind = false;
        }
    }

    public class TweenBuilder : MonoBehaviour
    {
        [SerializeField] private List<TweenData> tweenDataList = new List<TweenData>();

        [SerializeField] private bool playOnStart;

        [SerializeField] private bool autoKill = true;

        [SerializeField] private CustomTween currentTween;

#if UNITY_EDITOR
        internal List<TweenData> TweenDataList => tweenDataList;
        internal CustomTween CurrentTween => currentTween;
#endif
        private void Start()
        {
            Create();

            if (playOnStart)
            {
                Play();
            }
        }

        private void Create()
        {
            if (tweenDataList.Count == 1)
            {
                currentTween = CreateTween(tweenDataList[0]);
            }
            else
            {
                var currentSequence = Sequence.Create();
                currentTween = currentSequence;

                foreach (var tweenData in tweenDataList)
                {
                    switch (tweenData.sequenceType)
                    {
                        case SequenceType.Append:
                            var appendTween = CreateTween(tweenData);
                            if (appendTween != null)
                                currentSequence.Append(appendTween);
                            break;

                        case SequenceType.Join:
                            var joinTween = CreateTween(tweenData);
                            if (joinTween != null)
                                currentSequence.Join(joinTween);
                            break;

                        case SequenceType.Wait:
                            currentSequence.Wait(tweenData.duration);
                            break;
                    }
                }
            }

            currentTween.SetAutoKill(autoKill);
        }

        public void Play() => currentTween?.PlayInternal();

        public void Pause() => currentTween?.PauseInternal();

        public void Replay() => currentTween?.ReplayInternal();

        public void Rewind() => currentTween?.RewindInternal();

        private CustomTweener CreateTween(TweenData data)
        {
            var target = data.isSelf ? transform : data.targetTransform;

            if (target == null)
            {
                Debug.LogError("No target transform found for tween on " + gameObject.name);
                return null;
            }

            CustomTweener tween = null;

            switch (data.tweenType)
            {
                case TweenType.Move:
                    tween = target.TweenMove(data.useValue ? data.endValueV3 : data.endValueTransform.position,
                        data.duration);
                    break;

                case TweenType.Scale:
                    tween = target.TweenScale(data.endValueV3, data.duration);
                    break;

                case TweenType.Rotation:
                    tween = target.TweenRotate(data.endValueV3, data.duration);
                    break;

                case TweenType.LocalRotation:
                    tween = target.TweenLocalRotate(data.endValueV3, data.duration);
                    break;
            }

            tween?.SetEase(data.easeType);
            
            if (data.onStart.GetPersistentEventCount() > 0)
                tween.OnStart(() => data.onStart.Invoke());

            if (data.onPlay.GetPersistentEventCount() > 0)
                tween.OnPlay(() => data.onPlay.Invoke());

            if (data.onUpdate.GetPersistentEventCount() > 0)
                tween.OnUpdate(() => data.onUpdate.Invoke());

            if (data.onComplete.GetPersistentEventCount() > 0)
                tween.OnComplete(() => data.onComplete.Invoke());

            if (data.isFrom)
            {
                tween.From();
            }
            else
            {
                tween.To();
            }

            tween?.SetLoop(data.loopCount);

            return tween;
        }

        private void OnDestroy()
        {
            currentTween?.Kill();
        }
    }
}