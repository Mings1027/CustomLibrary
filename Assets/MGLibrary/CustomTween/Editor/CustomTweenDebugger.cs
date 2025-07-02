using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MGLibrary.CustomTween
{
#if UNITY_EDITOR
    public class TweenDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private const float RefreshInterval = 0.1f;
        private double _lastRefreshTime;
        private readonly Dictionary<int, bool> _showDetailStates = new(); // ID를 키로 사용
        private readonly Dictionary<int, int> _tweenIndexMap = new(); // ID -> 표시 순서 매핑
        private readonly Dictionary<int, bool> _sequenceTweenDetailStates = new(); // Sequence 내부 트윈들의 detail 상태

        [MenuItem("Tools/Custom Tween/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<TweenDebugWindow>("Tween Debug");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Tween Debug Window is only available in Play Mode", MessageType.Info);
                return;
            }

            DrawHeader();
            EditorGUILayout.Space();
            DrawAllTweenControlButtons();
            DrawTweenList();
        }

        private void DrawHeader()
        {
            // Statistics
            var allTweens = TweenManager.GetAllTweens();

            EditorGUILayout.LabelField($"Active Tweens: {allTweens.Count}", EditorStyles.boldLabel,
                GUILayout.Width(120));
        }

        private void DrawAllTweenControlButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play All", GUILayout.Width(70)))
            {
                TweenManager.PlayAllTweens();
            }

            if (GUILayout.Button("Pause All", GUILayout.Width(70)))
            {
                TweenManager.PauseAllTweens();
            }

            if (GUILayout.Button("Replay All", GUILayout.Width(70)))
            {
                TweenManager.ReplayAllTweens();
            }

            if (GUILayout.Button("Rewind All", GUILayout.Width(70)))
            {
                TweenManager.RewindAllTweens();
            }

            // Global control buttons
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Kill All", GUILayout.Width(70)))
            {
                if (EditorUtility.DisplayDialog("Kill All Tweens", "Kill All Tweens?", "Yes", "No"))
                {
                    TweenManager.KillAllTweens();
                    _showDetailStates.Clear();
                    _tweenIndexMap.Clear();
                    _sequenceTweenDetailStates.Clear();
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTweenList()
        {
            var allTweens = TweenManager.GetAllTweens();

            if (allTweens.Count == 0)
            {
                EditorGUILayout.HelpBox("No tweens found", MessageType.Info);
                return;
            }

            // ID 순으로 정렬하여 일관된 순서 유지
            var sortedTweens = new List<CustomTween>();
            foreach (var tween in allTweens.OrderBy(t => t.Id)) sortedTweens.Add(tween);

            // 새로운 트윈의 인덱스 할당
            UpdateTweenIndexMapping(sortedTweens);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < sortedTweens.Count; i++)
            {
                var tween = sortedTweens[i];
                if (tween == null || tween.sequenceType != SequenceType.None) continue;

                // 트윈의 고정된 표시 순서 사용
                DrawTweenItem(tween);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();

            // 존재하지 않는 트윈의 상태 정리
            CleanupOldTweenStates(sortedTweens);
        }

        private void UpdateTweenIndexMapping(List<CustomTween> tweens)
        {
            // 새로운 트윈에 대해서만 인덱스 할당
            foreach (var tween in tweens)
            {
                if (!_tweenIndexMap.ContainsKey(tween.Id))
                {
                    _tweenIndexMap[tween.Id] = _tweenIndexMap.Count;
                }
            }
        }

        private void CleanupOldTweenStates(List<CustomTween> currentTweens)
        {
            var currentIds = new HashSet<int>(currentTweens.Select(t => t.Id));
            var keysToRemove = new List<int>();

            foreach (var id in _showDetailStates.Keys)
            {
                if (!currentIds.Contains(id))
                {
                    keysToRemove.Add(id);
                }
            }

            foreach (var key in keysToRemove)
            {
                _showDetailStates.Remove(key);
                _tweenIndexMap.Remove(key);
            }

            // Sequence 내부 트윈들의 상태도 정리
            var sequenceTweenKeysToRemove = new List<int>();
            foreach (var id in _sequenceTweenDetailStates.Keys)
            {
                var found = false;
                foreach (var tween in currentTweens)
                {
                    if (tween is Sequence sequence)
                    {
                        if (sequence.Tweens != null)
                        {
                            for (int i = 0; i < sequence.Tweens.Count; i++)
                            {
                                if (sequence.Tweens[i] != null && sequence.Tweens[i].Id == id)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (found) break;
                }

                if (!found)
                {
                    sequenceTweenKeysToRemove.Add(id);
                }
            }

            foreach (var key in sequenceTweenKeysToRemove)
            {
                _sequenceTweenDetailStates.Remove(key);
            }
        }

        private void DrawTweenItem(CustomTween tween)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField(tween.TweenName);
            DrawTargetInfo(tween);
            DrawStatusIndicator(tween);

            // Control buttons
            if (tween is Sequence sequence)
            {
                DrawSequenceControlButtons(sequence);
            }
            else
            {
                DrawControlButtons(tween);
            }

            // Show detail toggle
            if (!_showDetailStates.ContainsKey(tween.Id))
            {
                _showDetailStates[tween.Id] = false;
            }

            _showDetailStates[tween.Id] = EditorGUILayout.Foldout(_showDetailStates[tween.Id], "Show Details", true);

            if (_showDetailStates[tween.Id])
            {
                if (tween is Sequence seq)
                {
                    DrawSequenceDetails(seq);
                }
                else
                {
                    DrawTweenMemberVariables(tween);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTargetInfo(CustomTween tween)
        {
            if (tween is Sequence sequence)
            {
                if (sequence.TargetObject != null)
                {
                    EditorGUILayout.ObjectField(sequence.TargetObject, sequence.TargetObject.GetType(), true,
                        GUILayout.Width(300));
                }
            }
            else
            {
                if (tween.TargetObject != null)
                {
                    EditorGUILayout.ObjectField(tween.TargetObject, tween.TargetObject.GetType(), true,
                        GUILayout.Width(300));
                }
            }
        }

        private void DrawStatusIndicator(CustomTween tween)
        {
            string status;
            Color statusColor;

            // If it's a sequence, get the status of currently active tweens
            if (tween is Sequence sequence)
            {
                var sequenceStatus = GetCurrentStatus(sequence);
                status = sequenceStatus.status;
                statusColor = sequenceStatus.color;
            }
            else
            {
                var tweenStatus = GetCurrentStatus(tween);
                status = tweenStatus.status;
                statusColor = tweenStatus.color;
            }

            var prevColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(status, EditorStyles.boldLabel);
            GUI.color = prevColor;
        }

        private (string status, Color color) GetCurrentStatus(CustomTween tween)
        {
            if (tween.state == TweenState.Playing)
            {
                return ("Playing", Color.green);
            }

            if (tween.state == TweenState.Pause)
            {
                return ("Pause", Color.yellow);
            }

            if (tween.state == TweenState.Complete)
            {
                return ("Complete", Color.greenYellow);
            }

            if (tween.state == TweenState.Ready)
            {
                return ("Ready", Color.gray);
            }

            return (null, default);
        }

        private void DrawControlButtons(CustomTween tween)
        {
            EditorGUILayout.BeginHorizontal();

            // Play button
            GUI.enabled = tween.state != TweenState.Playing;
            if (GUILayout.Button("Play", GUILayout.Width(50)))
            {
                tween.PlayInternal();
            }

            // Pause button
            GUI.enabled = tween.state == TweenState.Playing && tween.state != TweenState.Pause;
            if (GUILayout.Button("Pause", GUILayout.Width(50)))
            {
                tween.PauseInternal();
            }

            // Replay button
            GUI.enabled = true;
            if (GUILayout.Button("Replay", GUILayout.Width(50)))
            {
                tween.ReplayInternal();
            }

            if (GUILayout.Button("Rewind", GUILayout.Width(50)))
            {
                tween.RewindInternal();
            }

            // Kill button
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Kill", GUILayout.Width(40)))
            {
                if (EditorUtility.DisplayDialog("Kill Tween", $"Kill {tween.TweenName} tween?", "Yes", "No"))
                {
                    tween.KillInternal();
                    _showDetailStates.Remove(tween.Id);
                    _tweenIndexMap.Remove(tween.Id);
                }
            }

            GUI.backgroundColor = Color.white;

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSequenceControlButtons(Sequence sequence)
        {
            EditorGUILayout.BeginHorizontal();

            // Play button
            GUI.enabled = sequence.state != TweenState.Playing;
            if (GUILayout.Button("Play", GUILayout.Width(50)))
            {
                sequence.PlayInternal();
            }

            // Pause button
            GUI.enabled = sequence.state == TweenState.Playing && sequence.state != TweenState.Pause;
            if (GUILayout.Button("Pause", GUILayout.Width(50)))
            {
                sequence.PauseInternal();
            }

            // Replay button
            GUI.enabled = true;
            if (GUILayout.Button("Replay", GUILayout.Width(50)))
            {
                sequence.ReplayInternal();
            }

            if (GUILayout.Button("Rewind", GUILayout.Width(50)))
            {
                sequence.RewindInternal();
            }

            // Kill button
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Kill", GUILayout.Width(40)))
            {
                if (EditorUtility.DisplayDialog("Kill Sequence", $"Kill this sequence?", "Yes", "No"))
                {
                    sequence.KillInternal();
                    _showDetailStates.Remove(sequence.Id);
                    _tweenIndexMap.Remove(sequence.Id);
                }
            }

            GUI.backgroundColor = Color.white;

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSequenceDetails(Sequence sequence)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Sequence Details", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            var tweenList = sequence.Tweens;
            var currentIndex = sequence.CurrentIndex;

            if (tweenList != null && tweenList.Count > 0)
            {
                EditorGUILayout.LabelField($"Current Index: {currentIndex}", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                // Draw table header
                DrawSequenceTableHeader();

                // Draw each tween in table format
                for (int i = 0; i < tweenList.Count; i++)
                {
                    var innerTween = tweenList[i];
                    if (innerTween == null) continue;

                    DrawSequenceTableRow(i, innerTween);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No tweens in sequence");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawSequenceTableHeader()
        {
            EditorGUILayout.BeginHorizontal("box");

            // Index column
            EditorGUILayout.LabelField("#", EditorStyles.boldLabel, GUILayout.Width(25));

            // Type column (Append/Join)
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(80),
                GUILayout.ExpandWidth(true));

            // Tween Name column
            EditorGUILayout.LabelField("Tween", EditorStyles.boldLabel, GUILayout.Width(120));

            // Target column
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel, GUILayout.Width(100));

            // Status column
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel, GUILayout.Width(70),
                GUILayout.ExpandWidth(true));

            // Progress column
            EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSequenceTableRow(int index, CustomTween tween)
        {
            if (!_sequenceTweenDetailStates.ContainsKey(tween.Id))
            {
                _sequenceTweenDetailStates[tween.Id] = false;
            }

            // 한 줄로 트윈 정보 표시
            var rect = EditorGUILayout.BeginHorizontal();

            // 배경색 설정 (마우스 오버 효과)
            if (Event.current.type == EventType.Repaint)
            {
                var isHovered = rect.Contains(Event.current.mousePosition);
                if (isHovered)
                {
                    EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
                }
            }

            // Foldout
            var foldoutRect = new Rect(rect.x, rect.y, 15, rect.height);
            _sequenceTweenDetailStates[tween.Id] =
                EditorGUI.Foldout(foldoutRect, _sequenceTweenDetailStates[tween.Id], "", GUIStyle.none);

            // Index
            EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(25));

            // Sequence Type
            var sequenceType = tween.sequenceType switch
            {
                SequenceType.Append => "Append",
                SequenceType.Wait => "Wait",
                SequenceType.Join => "Join",
                _ => ""
            };

            EditorGUILayout.LabelField(sequenceType, GUILayout.Width(80), GUILayout.ExpandWidth(true));

            // Tween Name
            EditorGUILayout.LabelField(tween.TweenName ?? "x", GUILayout.Width(120));

            // Target Object - Store the rect for exclusion from click detection
            Rect objectFieldRect;
            if (tween.TargetObject != null)
            {
                objectFieldRect =
                    GUILayoutUtility.GetRect(100, EditorGUIUtility.singleLineHeight, GUILayout.Width(100));
                EditorGUI.ObjectField(objectFieldRect, tween.TargetObject, tween.TargetObject.GetType(), true);
            }
            else
            {
                objectFieldRect =
                    GUILayoutUtility.GetRect(100, EditorGUIUtility.singleLineHeight, GUILayout.Width(100));
                EditorGUI.LabelField(objectFieldRect, "x");
            }

            // Status
            var (status, textColor) = GetCurrentStatus(tween);
            var prevColor = GUI.color;
            GUI.color = textColor;
            EditorGUILayout.LabelField(status, GUILayout.Width(70), GUILayout.ExpandWidth(true));
            GUI.color = prevColor;

            // Progress
            string progressText = GetProgressText(tween);
            EditorGUILayout.LabelField(progressText, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();

            // Click detection - exclude the ObjectField area
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition) && !objectFieldRect.Contains(Event.current.mousePosition))
            {
                _sequenceTweenDetailStates[tween.Id] = !_sequenceTweenDetailStates[tween.Id];
                Event.current.Use();
            }

            // Draw details if expanded
            if (_sequenceTweenDetailStates[tween.Id])
            {
                EditorGUI.indentLevel++;
                DrawTweenMemberVariables(tween);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        private void DrawTweenMemberVariables(CustomTween tween)
        {
            var fieldsToShow = new List<(string propertyName, Func<object> getValue)>
            {
                (nameof(tween.elapsedTime), () => tween.elapsedTime),
                (nameof(tween.duration), () => tween.duration),
                (nameof(tween.state), () => tween.state),
                (nameof(tween.easeType), () => tween.easeType),
                (nameof(tween.onStart), () => tween.onStart),
                (nameof(tween.onComplete), () => tween.onComplete),
                (nameof(tween.onRewind), () => tween.onRewind),
                (nameof(tween.autoKill), () => tween.autoKill)
            };

            EditorGUILayout.BeginVertical("box");

            foreach (var (propertyName, getValue) in fieldsToShow)
            {
                try
                {
                    var value = getValue();
                    string displayValue = FormatFieldValue(value);
                    EditorGUILayout.LabelField($"{propertyName}:", displayValue);
                }
                catch (Exception)
                {
                    EditorGUILayout.LabelField($"{propertyName}:", "Error reading value");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private string GetProgressText(CustomTween tween)
        {
            try
            {
                if (tween.duration > 0)
                {
                    var progress = Mathf.Clamp01(tween.elapsedTime / tween.duration);
                    return $"{progress:P0}";
                }

                return "0%";
            }
            catch
            {
                return "N/A";
            }
        }

        private string FormatFieldValue(object value)
        {
            return value switch
            {
                null => "null",
                float floatValue => floatValue.ToString("F3"),
                bool boolValue => boolValue.ToString(),
                Enum enumValue => enumValue.ToString(),
                Delegate => "Set",
                _ => value.ToString()
            };
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
    }
#endif
}