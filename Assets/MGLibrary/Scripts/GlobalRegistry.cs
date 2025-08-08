using System;
using System.Collections.Generic;

namespace MGLibrary
{
    public struct Test
    {

    }
    public enum RegistryKey
    {
        Player,
        Camera,
    }

    public static class GlobalRegistry
    {
        private static readonly Dictionary<RegistryKey, object> RegistryDic = new();

        // Object 등록
        public static void RegisterObject(RegistryKey key, object obj)
        {
            if (RegistryDic.TryAdd(key, obj)) return;

            LogPrinter.LogWarning($"Object with key '{key}' is already registered. Replacing with new object.");
            RegistryDic[key] = obj;
        }

        // Object 해제
        public static bool UnregisterObject(RegistryKey key)
        {
            if (RegistryDic.Remove(key))
            {
                LogPrinter.Log($"Object with key '{key}' is unregistered.");
                return true;
            }

            LogPrinter.LogWarning($"Cannot unregister object with key '{key}' not found.");
            return false;
        }

        // Object 가져오기
        public static T GetObject<T>(RegistryKey key) where T : class
        {
            if (RegistryDic.TryGetValue(key, out var obj))
            {
                return obj as T;
            }

            LogPrinter.LogWarning($"Object with key '{key}' not found.");
            return null;
        }

        // 특정 키가 등록되어있는지 확인
        public static bool IsRegistered(RegistryKey key)
        {
            return RegistryDic.ContainsKey(key);
        }

        // 특정 타입으로 등록되어 있는지 확인
        public static bool IsRegistered<T>(RegistryKey key) where T : class
        {
            return RegistryDic.TryGetValue(key, out var obj) && obj is T;
        }

        public static Type GetObjectType(RegistryKey key)
        {
            if (RegistryDic.TryGetValue(key, out var obj))
            {
                return obj?.GetType();
            }

            return null;
        }

        // 등록된 모든 객체 제거 (씬 전환 등에 유용)
        public static void ClearAll()
        {
            var count = RegistryDic.Count;
            RegistryDic.Clear();
            LogPrinter.Log($"All {count} registered objects have been cleared.");
        }

        public static IEnumerable<RegistryKey> GetRegisteredKeys()
        {
            return RegistryDic.Keys;
        }
    }
}