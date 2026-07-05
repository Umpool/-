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
    //여기서부터 드래그및 커서에 따라 움직이기 시작? 
    // 💡 InputManager 클래스 내부에 기존 변수들과 함께 추가해주세요.


private void HandlePurePointerInput()
    {
        if (Pointer.current == null) return;
    // 🌟 [사진 속 54번 라인부터 61번 라인까지 드래그해서 이 내용으로 교체하세요!]
    Vector2 currentPointerPos = Pointer.current.position.ReadValue();

    // 🔒 [유령 드래그 원천 차단]: 진짜 무한모드 전장 방(Panel_INPuzzleBattle)이 화면에 켜져있을 때만 드래그를 허용합니다!
    // 인트로 화면이나 타이틀 화면처럼 전장 방이 꺼져있을 때는 마우스를 아무리 움직여도 무조건 무시하고 도망갑니다.
    GameObject inPuzzlePanel = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle")?.gameObject;
    if (inPuzzlePanel == null || inPuzzlePanel.activeInHierarchy == false) return;


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
