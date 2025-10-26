using System;
using UnityEngine;

public class ElmonArrow : MonoBehaviour
{
    public Action OnMove;
    public event Action OnHit;

    private Collider2D _collider;
    public Vector3 EndPos { get; set; }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        _collider.enabled = true;
    }

    private void OnDisable()
    {
        ObjectPoolManager.Instance.Return(gameObject);
        OnMove = null;
        OnHit = null;
    }

    private void Update()
    {
        OnMove?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        OnHit?.Invoke();
    }

    public void DisableCollider()
    {
        _collider.enabled = false;
    }
}