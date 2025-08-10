using UnityEditor;
using UnityEngine;

namespace MGLibrary
{
    [CustomEditor(typeof(CountdownTimerTest))]
    public class CountdownTimerTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var timer = target as CountdownTimerTest;

            if (GUILayout.Button("Play"))
            {
                timer.Play();
            }

            if (GUILayout.Button("Pause"))
            {
                timer.Pause();
            }

            if (GUILayout.Button("Rewind"))
            {
                timer.Rewind();
            }

            if (GUILayout.Button("Restart"))
            {
                timer.Restart();
            }

            if (GUILayout.Button("ResetTime"))
            {
                timer.ResetTime();
            }
        }
    }
}