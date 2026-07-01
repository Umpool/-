using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // 아무것도 가로막지 않고 클릭하는 순간 무조건 발사되는 순수 포인터 이벤트
    public static event Action<Vector2> OnInputStart;
    public static event Action<Vector2> OnInputEnd;

    public static Vector2 MovementInput { get; private set; }
    public static InputManager Instance { get; private set; }

    private Vector2 startPosition;
    private bool isDragging;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 1. 키보드 WASD 감지 (순수 데이터 수집)
        Vector2 keyboardInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) keyboardInput.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) keyboardInput.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) keyboardInput.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) keyboardInput.x += 1;
        }
        MovementInput = keyboardInput.normalized;

        // 2. 묻지도 따지지도 않고 모든 클릭 신호를 전송하는 엔진 가동
        HandlePurePointerInput();
    }

    private void HandlePurePointerInput()
    {
        if (Pointer.current == null) return;
        Vector2 currentPointerPos = Pointer.current.position.ReadValue();

        // 꾹 누르는 순간: UI 버튼이든 블록이든 신경 안 쓰고 무조건 신호 발사!
        if (Pointer.current.press.wasPressedThisFrame)
        {
            startPosition = currentPointerPos;
            isDragging = true;
            OnInputStart?.Invoke(currentPointerPos);
        }
        // 손가락을 떼는 순간: 무조건 해제 신호 발사!
        else if (Pointer.current.press.wasReleasedThisFrame)
        {
            if (isDragging)
            {
                isDragging = false;
                OnInputEnd?.Invoke(currentPointerPos);
            }
        }
    }

    public Vector2 GetStartPosition() => startPosition;
}
