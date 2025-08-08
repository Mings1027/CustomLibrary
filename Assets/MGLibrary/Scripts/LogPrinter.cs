using System.Runtime.CompilerServices;
using UnityEngine;

namespace MGLibrary
{
    public static class LogPrinter
    {
        public static void Log(string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Debug.Log($"[{System.IO.Path.GetFileName(file)}:{line} - {member}] {message}");
        }

        public static void Log(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Debug.Log($"[{System.IO.Path.GetFileName(file)}:{line} - {member}] {message}");
        }

        public static void LogInfo(string message) => Debug.Log($"[INFO] {message}");
        public static void LogWarning(string message) => Debug.LogWarning($"[WARNING] {message}");
        public static void LogError(string message) => Debug.LogError($"[ERROR] {message}");
    }
}