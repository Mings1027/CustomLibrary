using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MGLibrary.CustomTween.Editor
{
    [CustomEditor(typeof(TweenBuilder))]
    public class TweenBuilderEditor : UnityEditor.Editor
    {
        private SerializedProperty playOnStartProp;
        private SerializedProperty autoKillProp;

        private TweenBuilder tweenBuilder;

        private readonly Color green = Color.green;
        private readonly Color gray = Color.gray;
        private readonly Color yellow = Color.yellow;

        private void OnEnable()
        {
            playOnStartProp = serializedObject.FindProperty("playOnStart");
            autoKillProp = serializedObject.FindProperty("autoKill");

            tweenBuilder = (TweenBuilder)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawControlButtons();

            DrawSettings();

            DrawTweenList();

            bool hasNoneTweenType = false;
            foreach (var tweenData in tweenBuilder.TweenDataList)
            {
                if (tweenData.tweenType == TweenType.None && tweenData.sequenceType != SequenceType.Wait)
                {
                    hasNoneTweenType = true;
                    break;
                }
            }

            if (!hasNoneTweenType)
            {
                if (GUILayout.Button("Add Tween"))
                {
                    // Direct access instead of serialized property
                    tweenBuilder.TweenDataList.Add(new TweenData());
                    EditorUtility.SetDirty(tweenBuilder);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawControlButtons()
        {
            if (!Application.isPlaying) return;

            if (tweenBuilder == null || tweenBuilder.CurrentTween == null) return;

            var curState = tweenBuilder.CurrentTween.state;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            // Play Button
            GUI.enabled = curState != TweenState.Playing;
            if (GUILayout.Button("Play"))
            {
                tweenBuilder.Play();
            }

            // Pause Button
            GUI.enabled = curState == TweenState.Playing;
            if (GUILayout.Button("Pause"))
            {
                tweenBuilder.Pause();
            }

            // Replay Button
            GUI.enabled = true;
            if (GUILayout.Button("Replay"))
            {
                tweenBuilder.Replay();
            }

            // Rewind Button
            if (GUILayout.Button("Rewind"))
            {
                tweenBuilder.Rewind();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Status display
            EditorGUILayout.Space(5);

            var status = GetCurrentStatus(tweenBuilder.CurrentTween);
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = status.color;
            EditorGUILayout.LabelField(status.text, style);

            EditorGUILayout.EndVertical();
        }

        private (string text, Color color) GetCurrentStatus(CustomTween tween)
        {
            return tween.state switch
            {
                TweenState.Playing => ("Playing", Color.green),
                TweenState.Pause => ("Pause", Color.yellow),
                TweenState.Complete => ("Complete", Color.greenYellow),
                TweenState.Ready => ("Ready", gray),
                _ => (null, default)
            };
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = playOnStartProp.boolValue ? green : gray;

            if (GUILayout.Button("Play On Start"))
            {
                playOnStartProp.boolValue = !playOnStartProp.boolValue;
            }

            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = autoKillProp.boolValue ? green : gray;

            if (GUILayout.Button("Auto Kill"))
            {
                autoKillProp.boolValue = !autoKillProp.boolValue;
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTweenList()
        {
            // Direct access to the list
            var tweenDataList = tweenBuilder.TweenDataList;

            for (var i = 0; i < tweenDataList.Count; i++)
            {
                DrawTweenElement(i, tweenDataList[i]);
            }
        }

        private void DrawTweenElement(int index, TweenData tweenData)
        {
            var tweenDataList = tweenBuilder.TweenDataList;
            var isWaitOperation = tweenData.sequenceType == SequenceType.Wait;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawTweenHeader(index, tweenData, tweenDataList, isWaitOperation);

            if (!isWaitOperation)
            {
                DrawTargetSelection(tweenData);
                DrawTweenTypeSelector(tweenData);
            }

            if (tweenData.isExpanded)
            {
                DrawExpandedContent(tweenData, tweenDataList, isWaitOperation);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTweenHeader(int index, TweenData tweenData, List<TweenData> tweenDataList,
            bool isWaitOperation)
        {
            EditorGUILayout.BeginHorizontal();

            var title = isWaitOperation
                ? $"Wait ({tweenData.duration}s)"
                : $"{tweenData.tweenType}";

            tweenData.isExpanded = EditorGUILayout.Foldout(tweenData.isExpanded, title, true);

            GUILayout.FlexibleSpace();

            DrawMoveButtons(index, tweenDataList);
            DrawDeleteButton(index, tweenDataList);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMoveButtons(int index, List<TweenData> tweenDataList)
        {
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            // Move Up Button
            EditorGUI.BeginDisabledGroup(index == 0);
            if (GUILayout.Button("▲", buttonStyle, GUILayout.Width(30), GUILayout.Height(22)))
            {
                (tweenDataList[index], tweenDataList[index - 1]) = (tweenDataList[index - 1], tweenDataList[index]);
                EditorUtility.SetDirty(tweenBuilder);
            }

            EditorGUI.EndDisabledGroup();

            // Move Down Button
            EditorGUI.BeginDisabledGroup(index == tweenDataList.Count - 1);
            if (GUILayout.Button("▼", buttonStyle, GUILayout.Width(30), GUILayout.Height(22)))
            {
                (tweenDataList[index], tweenDataList[index + 1]) = (tweenDataList[index + 1], tweenDataList[index]);
                EditorUtility.SetDirty(tweenBuilder);
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawDeleteButton(int index, List<TweenData> tweenDataList)
        {
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f, 1f);
            if (GUILayout.Button("✕", buttonStyle, GUILayout.Width(30), GUILayout.Height(22)))
            {
                tweenDataList.RemoveAt(index);
                EditorUtility.SetDirty(tweenBuilder);
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawTargetSelection(TweenData tweenData)
        {
            EditorGUILayout.BeginHorizontal();

            var buttonText = tweenData.isSelf ? "SELF" : "OTHER";
            var tooltip = tweenData.isSelf
                ? "Will animate on this gameObject"
                : "Will animate the assigned gameObject";
            var buttonContent = new GUIContent(buttonText, tooltip);

            GUI.backgroundColor = tweenData.isSelf ? green : yellow;

            if (GUILayout.Button(buttonContent, GUILayout.Width(50)))
            {
                tweenData.isSelf = !tweenData.isSelf;
                if (!tweenData.isSelf)
                {
                    tweenData.Reset();
                }

                EditorUtility.SetDirty(tweenBuilder);
            }

            GUI.backgroundColor = Color.white;

            if (!tweenData.isSelf)
            {
                EditorGUI.BeginChangeCheck();
                var newTarget =
                    (Transform)EditorGUILayout.ObjectField(tweenData.targetTransform, typeof(Transform), true);
                if (EditorGUI.EndChangeCheck())
                {
                    tweenData.targetTransform = newTarget;
                    EditorUtility.SetDirty(tweenBuilder);
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawExpandedContent(TweenData tweenData, List<TweenData> tweenDataList, bool isWaitOperation)
        {
            var hasValidTarget = isWaitOperation || tweenData.isSelf || tweenData.targetTransform != null;
            if (!hasValidTarget) return;

            if (!isWaitOperation && tweenData.tweenType == TweenType.None)
            {
                return;
            }

            DrawSequenceTypeSelector(tweenData, tweenDataList);

            EditorGUILayout.Space(5);
            DrawTweenSettings(tweenData);
            EditorGUILayout.Space(5);

            if (!isWaitOperation)
            {
                DrawEventToggles(tweenData);
            }
        }

        private void DrawTweenTypeSelector(TweenData tweenData)
        {
            if (!tweenData.isSelf) return;
            EditorGUI.BeginChangeCheck();
            var newTweenType = (TweenType)EditorGUILayout.EnumPopup("Tween Type", tweenData.tweenType);
            if (EditorGUI.EndChangeCheck())
            {
                tweenData.tweenType = newTweenType;
                EditorUtility.SetDirty(tweenBuilder);
            }
        }

        private void DrawSequenceTypeSelector(TweenData tweenData, List<TweenData> tweenDataList)
        {
            if (tweenDataList.Count > 1)
            {
                EditorGUI.BeginChangeCheck();
                var newSequenceType = (SequenceType)EditorGUILayout.EnumPopup("Sequence", tweenData.sequenceType);
                if (EditorGUI.EndChangeCheck())
                {
                    tweenData.sequenceType = newSequenceType;
                    EditorUtility.SetDirty(tweenBuilder);
                }
            }
        }

        private void DrawTweenSettings(TweenData tweenData)
        {
            // Direct property access with change checking
            EditorGUI.BeginChangeCheck();

            tweenData.duration = EditorGUILayout.FloatField("Duration", tweenData.duration);
            tweenData.easeType = (EaseType)EditorGUILayout.EnumPopup("Ease Type", tweenData.easeType);
            tweenData.loopCount = EditorGUILayout.IntField("Loop Count", tweenData.loopCount);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                var buttonText = tweenData.isFrom ? "FROM" : "TO";
                var tooltip = tweenData.isFrom ? "Set End Value" : "Set Start Value";
                var buttonContent = new GUIContent(buttonText, tooltip);
                if (GUILayout.Button(buttonContent, GUILayout.Width(70)))
                {
                    tweenData.isFrom = !tweenData.isFrom;
                }

                switch (tweenData.tweenType)
                {
                    case TweenType.Move:
                        if (tweenData.useValue)
                        {
                            tweenData.endValueV3 = EditorGUILayout.Vector3Field("", tweenData.endValueV3);
                        }
                        else
                        {
                            tweenData.endValueTransform = (Transform)EditorGUILayout.ObjectField("",
                                tweenData.endValueTransform, typeof(Transform), true);
                        }

                        var valueText = tweenData.useValue ? "value" : "Target";
                        var valueContent = new GUIContent(valueText);
                        if (GUILayout.Button(valueContent, GUILayout.Width(50)))
                        {
                            tweenData.useValue = !tweenData.useValue;
                        }

                        break;
                    case TweenType.Scale:
                    case TweenType.Rotation:
                    case TweenType.LocalRotation:
                        tweenData.endValueV3 = EditorGUILayout.Vector3Field("", tweenData.endValueV3);
                        break;

                    case TweenType.MoveX:
                    case TweenType.MoveY:
                    case TweenType.MoveZ:
                        tweenData.endValueFloat = EditorGUILayout.FloatField("", tweenData.endValueFloat);
                        break;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(tweenBuilder);
            }
        }

        public static void DrawHorizontalLine(Color color, int thickness = 1)
        {
            var rect = EditorGUILayout.GetControlRect(false, thickness);
            EditorGUI.DrawRect(rect, color);
        }

        private void DrawEventToggles(TweenData tweenData)
        {
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            DrawHorizontalLine(gray);
            EditorGUILayout.BeginHorizontal();

            DrawEventToggle("OnStart", ref tweenData.hasOnStart);
            DrawEventToggle("OnPlay", ref tweenData.hasOnPlay);
            DrawEventToggle("OnUpdate", ref tweenData.hasOnUpdate);
            DrawEventToggle("OnComplete", ref tweenData.hasOnComplete);
            DrawEventToggle("OnRewind", ref tweenData.hasOnRewind);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            DrawActiveEventFields(tweenData);
        }

        private void DrawEventToggle(string displayName, ref bool toggleValue)
        {
            GUI.backgroundColor = toggleValue ? green : gray;

            if (GUILayout.Button(displayName))
            {
                toggleValue = !toggleValue;
                EditorUtility.SetDirty(tweenBuilder);
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawActiveEventFields(TweenData tweenData)
        {
            var hasAnyEvents = tweenData.hasOnStart || tweenData.hasOnPlay || tweenData.hasOnUpdate ||
                               tweenData.hasOnComplete || tweenData.hasOnRewind;

            if (hasAnyEvents)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                if (tweenData.hasOnStart)
                {
                    EditorGUILayout.PropertyField(
                        new SerializedObject(tweenBuilder).FindProperty("tweenDataList")
                            .GetArrayElementAtIndex(tweenBuilder.TweenDataList.IndexOf(tweenData))
                            .FindPropertyRelative("onStart"),
                        new GUIContent("On Start"));
                }

                if (tweenData.hasOnPlay)
                {
                    EditorGUILayout.PropertyField(
                        new SerializedObject(tweenBuilder).FindProperty("tweenDataList")
                            .GetArrayElementAtIndex(tweenBuilder.TweenDataList.IndexOf(tweenData))
                            .FindPropertyRelative("onPlay"),
                        new GUIContent("On Play"));
                }

                if (tweenData.hasOnUpdate)
                {
                    EditorGUILayout.PropertyField(
                        new SerializedObject(tweenBuilder).FindProperty("tweenDataList")
                            .GetArrayElementAtIndex(tweenBuilder.TweenDataList.IndexOf(tweenData))
                            .FindPropertyRelative("onUpdate"),
                        new GUIContent("On Update"));
                }

                if (tweenData.hasOnComplete)
                {
                    EditorGUILayout.PropertyField(
                        new SerializedObject(tweenBuilder).FindProperty("tweenDataList")
                            .GetArrayElementAtIndex(tweenBuilder.TweenDataList.IndexOf(tweenData))
                            .FindPropertyRelative("onComplete"),
                        new GUIContent("On Complete"));
                }

                if (tweenData.hasOnRewind)
                {
                    EditorGUILayout.PropertyField(
                        new SerializedObject(tweenBuilder).FindProperty("tweenDataList")
                            .GetArrayElementAtIndex(tweenBuilder.TweenDataList.IndexOf(tweenData))
                            .FindPropertyRelative("onRewind"),
                        new GUIContent("On Rewind"));
                }

                EditorGUILayout.EndVertical();
            }
        }
    }
}