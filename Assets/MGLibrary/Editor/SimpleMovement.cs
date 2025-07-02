using UnityEngine;

public class SimpleMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    void Update()
    {
        // WASD 키 입력 받기
        float horizontal = Input.GetAxis("Horizontal"); // A, D 키 (또는 화살표 좌우)
        float vertical = Input.GetAxis("Vertical");     // W, S 키 (또는 화살표 상하)
        
        // 이동 벡터 계산
        Vector3 movement = new Vector3(horizontal, 0, vertical);
        
        // Transform을 이용한 이동 (deltaTime 곱해서 프레임 독립적으로)
        transform.Translate(movement * moveSpeed * Time.deltaTime);
    }
}