using System;
using MGLibrary.ImprovedTimers;
using UnityEngine;
using UnityEngine.UI;

public class CountdownTimerTest : MonoBehaviour
{
    private CountdownTimer countdownTimer;
    [SerializeField] private float initTime;
    [SerializeField] private float newTime;
    [SerializeField] private Slider loadingBar;

    private void Awake()
    {
        countdownTimer = new CountdownTimer(initTime);
        loadingBar.value = initTime;
        countdownTimer.OnTimerPlay += () => Debug.Log("Timer Start");
        
        countdownTimer.OnProgress += UpdateLoadingBar;
        countdownTimer.OnTimerComplete += () => Debug.Log("Timer Stop");
    }

    private void OnDestroy()
    {
        countdownTimer.Dispose();
    }

    public void Play() => countdownTimer.Play();
    public void Pause() => countdownTimer.Pause();
    public void Rewind()
    {
        loadingBar.value = initTime;
        countdownTimer.Rewind();
    }

    public void Restart() => countdownTimer.Restart();
    public void ResetTime() => countdownTimer.ResetTime(newTime);

    private void UpdateLoadingBar(float progress)
    {
        loadingBar.value = progress;
    }
}