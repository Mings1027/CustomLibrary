using System;
using Cysharp.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class Test : MonoBehaviour
{
    [ContextMenu("TestMethod")]
    private async UniTaskVoid TestMethod()
    {
        Profiler.BeginSample("Test");
        var result = await AsyncMethod();
        Profiler.EndSample();
        Debug.Log(result);
    }

    private async UniTask<int> AsyncMethod()
    {
        var delay = Random.Range(0, 10);
        Debug.Log(delay);
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
        return delay;
    }
}