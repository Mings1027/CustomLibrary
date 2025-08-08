using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class CoroutineTest : MonoBehaviour
{
    [SerializeField] private int count;
    private void Start()
    {
        StartCoroutine(RunCo());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            count++;
        }
    }

    private IEnumerator RunCo()
    {
         count = 0;
        while (count <= 100)
        {
            yield return new WaitForSeconds(1);
            count++;
            Debug.Log("실행중...");
        }
    }
}