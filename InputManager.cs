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

    private string startBlockColor = ""; // 💡 [추가] 처음 누른 블록의 색상 저장


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
    //여기서부터 드래그및 커서에 따라 움직이기 시작? 
    // 💡 InputManager 클래스 내부에 기존 변수들과 함께 추가해주세요.
    private bool isTrackingSwipe = false;
    private GameObject selectedBlock = null;
    public float swipeResistance = 20f; // 이 거리 이상 움직여야 슬라이드 시작으로 인정

private void HandlePurePointerInput()
    {
        if (Pointer.current == null) return;
        Vector2 currentPointerPos = Pointer.current.position.ReadValue();

        // 1. 마우스를 꾹 누르는 순간 (기존 원본 로직 100% 유지)
        if (Pointer.current.press.wasPressedThisFrame)
        {
            startPosition = currentPointerPos;
            isDragging = true;
            OnInputStart?.Invoke(currentPointerPos);
        }
        // 2. 손가락을 떼는 순간
        else if (Pointer.current.press.wasReleasedThisFrame)
        {
            if (isDragging)
            {
                // 💡 [초고속 씹힘 방지 보완] 누른 좌표와 뗀 좌표의 실제 거리 차이를 구합니다.
                float dragDistance = Vector2.Distance(startPosition, currentPointerPos);

                // 유저가 화면에서 최소 15픽셀 이상만 움직였다면 무조건 드래그로 인정합니다!
                if (dragDistance > 15f)
                {
                    // 💡 상하좌우 방향 계산
                    string dragDirection = CalculateSimpleDirection(startPosition, currentPointerPos);
                    Debug.Log($"🎯 드래그 성공! 방향: [{dragDirection}]");

                    // 🔥 [기존 1턴 소모 기능] 배틀 매니저에게 정직하게 신호를 보냅니다.
                    if (PuzzleBattleManager.Instance != null)
                    {
                        PuzzleBattleManager.Instance.OnUserDragBlock();
                    }
                }

                // 기존 해제 로직 유지
                isDragging = false;
                OnInputEnd?.Invoke(currentPointerPos);
            }
        }
    }

    // 💡 괄호 에러를 막기 위해 함수 내부에 안전하게 포함시킨 보조 방향 계산기입니다.
    private string CalculateSimpleDirection(Vector2 start, Vector2 end)
    {
        Vector2 diff = end - start;
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            return diff.x > 0 ? "RIGHT" : "LEFT";
        }
        else
        {
            return diff.y > 0 ? "UP" : "DOWN";
        }
    }

    // 기존 원본에 있던 필수 함수입니다.
    public Vector2 GetStartPosition() => startPosition;

}
