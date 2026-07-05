using UnityEngine;
using System;
using UnityEngine.InputSystem;

/// <summary>
/// [마우스 및 키보드 입력 총괄 사령탑]
/// 게임 내 모든 입력 신호를 수집하여 다른 시스템으로 전달하는 중앙 집중형 클래스입니다.
/// </summary>
public class InputManager : MonoBehaviour
{
    // =========================================================================
    // 📢 [이벤트] 입력 신호 수신/발신 (Action)
    // =========================================================================
    public static event Action<Vector2> OnInputStart; // 클릭(시작) 좌표 쏘아보내기
    public static event Action<Vector2> OnInputEnd;   // 클릭 해제(끝) 좌표 쏘아보내기

    // =========================================================================
    // 💾 [데이터 변수] 현재 상태 보관
    // =========================================================================
    public static Vector2 MovementInput { get; private set; } // WASD 방향키 입력값
    public static InputManager Instance { get; private set; } // 싱글톤 인스턴스
    private Vector2 startPosition;                             // 드래그 시작 좌표 기억
    private bool isDragging;                                   // 드래그 상태 체크 스위치

    // =========================================================================
    // ⚙️ [시스템 초기화]
    // =========================================================================
    void Awake()
    {
        // 싱글톤 패턴: 인스턴스 고정 및 씬 전환 시 파괴 방지(DontDestroyOnLoad)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // 중복 인스턴스 파괴
        }
    }

    // =========================================================================
    // 🔄 매 프레임마다 유저의 실시간 입력 신호를 감시하는 사령탑
    // =========================================================================
    void Update()
    {
        // 1. 키보드 방향키(WASD, 화살표) 입력 감지 및 데이터 수집
        Vector2 keyboardInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            // W 또는 ↑ 키를 누르면 위쪽(Y축 +1)으로 이동값 추가
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) keyboardInput.y += 1;
            // S 또는 ↓ 키를 누르면 아래쪽(Y축 -1)으로 이동값 추가
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) keyboardInput.y -= 1;
            // A 또는 ← 키를 누르면 왼쪽(X축 -1)으로 이동값 추가
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) keyboardInput.x -= 1;
            // D 또는 → 키를 누르면 오른쪽(X축 +1)으로 이동값 추가
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) keyboardInput.x += 1;
        }

        // 대각선 이동 시 속도가 빨라지지 않도록 입력값을 일정하게 규격화(정규화)하여 저장
        MovementInput = keyboardInput.normalized;

        // 2. 묻지도 따지지도 않고 모든 클릭 신호를 전송하는 엔진 가동
        HandlePurePointerInput();
    }



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

    // =========================================================================
    // 🧭 드래그 방향 계산기 (상하좌우 판단)
    // =========================================================================
    private string CalculateSimpleDirection(Vector2 start, Vector2 end)
    {
        // 마우스가 누른 시작점과 뗀 끝점의 실제 거리 차이를 구합니다.
        Vector2 diff = end - start;

        // 가로(X축) 이동 거리가 세로(Y축) 이동 거리보다 더 크다면? -> 좌우 이동으로 판단
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            // 오른쪽으로 움직였으면 "RIGHT", 왼쪽이면 "LEFT" 글자를 뱉어냅니다.
            return diff.x > 0 ? "RIGHT" : "LEFT";
        }
        // 세로 이동 거리가 더 크다면? -> 상하 이동으로 판단
        else
        {
            // 위쪽으로 움직였으면 "UP", 아래쪽이면 "DOWN" 글자를 뱉어냅니다.
            return diff.y > 0 ? "UP" : "DOWN";
        }
    }

    // =========================================================================
    // 📍 처음 누른 마우스 좌표 보내주기 스위치
    // =========================================================================
    public Vector2 GetStartPosition() => startPosition;

} // 🔒 InputManager 클래스 전체가 끝나는 최종 대문 괄호
