using System;
using UnityEngine;

public class DisplayFPS : MonoBehaviour
{
    public enum ScreenCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public ScreenCorner corner = ScreenCorner.TopLeft;
    public int fontSize = 2; // Font size as a percentage of screen height
    public Color fontColor = Color.white;

    private float _deltaTime = 0.0f;

    private void Update()
    {
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        var w = Screen.width;
        var h = Screen.height;
        var style = new GUIStyle();
        var rect = new Rect(10, 10, w - 20, h - 20);

        switch (corner)
        {
            case ScreenCorner.TopLeft:
                style.alignment = TextAnchor.UpperLeft;
                break;
            case ScreenCorner.TopRight:
                style.alignment = TextAnchor.UpperRight;
                break;
            case ScreenCorner.BottomLeft:
                style.alignment = TextAnchor.LowerLeft;
                break;
            case ScreenCorner.BottomRight:
                style.alignment = TextAnchor.LowerRight;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        style.fontSize = h * fontSize / 100;
        style.normal.textColor = fontColor;
        
        var msec = _deltaTime * 1000.0f;
        var fps = 1.0f / _deltaTime;
        var text = $"{msec:0.0} ms ({fps:0.} fps)";
        GUI.Label(rect, text, style);
    }
}