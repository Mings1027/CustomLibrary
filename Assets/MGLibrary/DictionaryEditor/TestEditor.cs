using UnityEditor;
using UnityEngine;
using System.Linq;

namespace MGLibrary.DictionaryEditor
{
    [CustomEditor(typeof(Test))]
    public class TestEditor : Editor
    {
        private bool showDictionary = true;
        
        public override void OnInspectorGUI()
        {
            Test test = (Test)target;
            
            // Key, Value 필드 표시
            EditorGUILayout.LabelField("Add New Entry", EditorStyles.boldLabel);
            
            SerializedProperty keyProp = serializedObject.FindProperty("key");
            SerializedProperty valueProp = serializedObject.FindProperty("value");
            
            EditorGUILayout.BeginHorizontal();
            
            // Key 필드 - 고정 너비로 충분한 공간 확보
            EditorGUILayout.LabelField("Key", GUILayout.Width(30));
            EditorGUILayout.PropertyField(keyProp, GUIContent.none, GUILayout.MinWidth(120));
            
            // 화살표 - 중앙 정렬을 위한 공간
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("→", EditorStyles.boldLabel, GUILayout.Width(20));
            GUILayout.FlexibleSpace();
            
            // Value 필드 - 고정 너비로 충분한 공간 확보
            EditorGUILayout.LabelField("Value", GUILayout.Width(40));
            EditorGUILayout.PropertyField(valueProp, GUIContent.none, GUILayout.MinWidth(120));

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Add 버튼
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Add to Dictionary", GUILayout.Height(25)))
            {
                test.Add();
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(test);
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            
            // Dictionary 내용 표시
            showDictionary = EditorGUILayout.Foldout(showDictionary, $"Dictionary Contents ({test.TestDic?.Count ?? 0} items)", true);
            
            if (showDictionary && test.TestDic != null && test.TestDic.Count > 0)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                var keysToRemove = new System.Collections.Generic.List<Transform>();
                
                foreach (var kvp in test.TestDic)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Key 표시 (읽기 전용)
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(kvp.Key, typeof(Transform), true, GUILayout.MinWidth(150));
                    EditorGUI.EndDisabledGroup();
                    
                    // 화살표
                    EditorGUILayout.LabelField("→", EditorStyles.boldLabel, GUILayout.Width(20));
                    
                    // Value 표시 (편집 가능)
                    Transform newValue = (Transform)EditorGUILayout.ObjectField(kvp.Value, typeof(Transform), true, GUILayout.MinWidth(150));
                    if (newValue != kvp.Value)
                    {
                        test.TestDic[kvp.Key] = newValue;
                        if (!Application.isPlaying)
                        {
                            EditorUtility.SetDirty(test);
                        }
                    }
                    
                    // 삭제 버튼
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(18)))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // 구분선
                    if (test.TestDic.Keys.ToList().IndexOf(kvp.Key) < test.TestDic.Count - 1)
                    {
                        EditorGUILayout.Space(2);
                        DrawUILine(Color.gray, 1);
                        EditorGUILayout.Space(2);
                    }
                }
                
                // 삭제할 키들 제거
                foreach (var key in keysToRemove)
                {
                    test.TestDic.Remove(key);
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(test);
                    }
                }
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space();
                
                // Clear All 버튼
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Clear All", GUILayout.Height(20)))
                {
                    if (EditorUtility.DisplayDialog("Clear Dictionary", 
                        "Are you sure you want to clear all dictionary entries?", 
                        "Yes", "No"))
                    {
                        test.TestDic.Clear();
                        if (!Application.isPlaying)
                        {
                            EditorUtility.SetDirty(test);
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            else if (showDictionary)
            {
                EditorGUILayout.HelpBox("Dictionary is empty", MessageType.Info);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}