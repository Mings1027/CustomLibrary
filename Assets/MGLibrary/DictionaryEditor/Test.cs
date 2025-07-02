using UnityEngine;
using UnityEngine.Rendering;

namespace MGLibrary.DictionaryEditor
{
    public class Test : MonoBehaviour
    {
        [SerializeField] private Transform key, value;
        [SerializeField] private SerializedDictionary<Transform, Transform> testDic;
        public SerializedDictionary<Transform, Transform> TestDic => testDic;

        public void Add()
        {
            testDic.Add(key, value);
        }
    }
}