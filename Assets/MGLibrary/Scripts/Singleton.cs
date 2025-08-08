using UnityEngine;

namespace MGLibrary
{
    [DisallowMultipleComponent]
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isAppQuit = true;

        public static T Instance
        {
            get
            {
                if (_isAppQuit) return null;

                if (_instance != null) return _instance;
                
                var finds = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (finds.Length > 0)
                {
                    _instance = finds[0];
                    DontDestroyOnLoad(_instance.gameObject);
                }

                if (finds.Length > 1)
                {
                    for (var i = 0; i < finds.Length; i++)
                    {
                        Destroy(finds[i].gameObject);
                    }
                }

                if (_instance != null) return _instance;

                var newGameObject = new GameObject(typeof(T).Name);
                DontDestroyOnLoad(newGameObject);
                _instance = newGameObject.AddComponent<T>();

                return _instance;
            }
        }

        private void OnApplicationQuit()
        {
            _isAppQuit = true;
        }
    }
}