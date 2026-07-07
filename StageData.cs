using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NormalBattleManager : MonoBehaviour
{
    public static NormalBattleManager Instance { get; private set; }

    public enum GameState { Ready, PlayerTurn, Matching, EnemyTurn, GameClear, GameOver }

    [Header("--- 현재 일반모드 상태 ---")]
    public GameState currentState = GameState.Ready;

    [Header("--- 기획 및 스테이지 데이터 에셋 ---")]
    public StageData currentStageData; // 인스펙터에서 Stage_1, Stage_2 등 에셋을 넣는 주머니

    [Header("--- 필수 UI 패널 록온 ---")]
    public GameObject panel_NormalPuzzleBattle; // 일반배틀 전용 패널 (Panel_NMPuzzleBattle)
    public GameObject panel_NormalStageReward;  // 결과창 / 보상 팝업 패널
    public TextMeshProUGUI textNormalRecordNotice; // 결과 안내 텍스트

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 일반모드 매니저가 씬에 로드되거나 준비되면 초기화 격발
        InitNormalGame();
    }

    // 1. 일반모드 게임 초기화
    public void InitNormalGame()
    {
        currentState = GameState.Ready;

        if (currentStageData != null)
        {
            Debug.Log($"⚔️ [일반모드 독립회로 가동] 현재 스테이지: {currentStageData.stageName}");

            // 외부 스크립트나 타이머가 화면을 꺼버리는 부작용 방지 On 활성화
            if (panel_NormalPuzzleBattle != null)
            {
                panel_NormalPuzzleBattle.SetActive(true);
            }

            // 6x6 일반모드 보드 생성 가동 신호 송신
            if (Board.Instance != null)
            {
                Board.Instance.currentTurn = 0; // 진행 턴수 초기화
                Board.Instance.gameObject.SetActive(true);
                Board.Instance.InitializeNewBoard(); // 새 블록 배치
            }
        }
        else
        {
            Debug.LogError("⚠ [오류] NormalBattleManager에 StageData 에셋이 할당되지 않았습니다!");
        }

        SetState(GameState.PlayerTurn);
    }

    // 2. 상태 전환 제어타워
    public void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[일반모드 주파수] 상태 전환 -> {currentState}");

        switch (currentState)
        {
            case GameState.PlayerTurn:
                Debug.Log("🎮 플레이어 턴! 블록을 드래그하세요.");
                break;

            case GameState.Matching:
                // 블록 폭발 및 연쇄 연산 중 (조작 차단)
                break;

            case GameState.EnemyTurn:
                Debug.Log("👹 몬스터의 공격 턴입니다.");
                // (몬스터 반격 함수나 연출이 있다면 이곳에 추가)

                // 공격 연출이 끝나면 승리/패배 규칙 정산
                CheckNormalTurnEnd();
                break;

            case GameState.GameClear:
                Debug.Log("★ 스테이지 클리어! ★");
                if (panel_NormalStageReward != null)
                {
                    panel_NormalStageReward.SetActive(true);
                    if (textNormalRecordNotice != null) textNormalRecordNotice.text = "STAGE CLEAR!";
                }
                if (Board.Instance != null) Board.Instance.isGameActive = false;
                break;

            case GameState.GameOver:
                Debug.Log("☠ 제한 턴수 초과! 게임 오버 ☠");
                if (panel_NormalStageReward != null)
                {
                    panel_NormalStageReward.SetActive(true);
                    if (textNormalRecordNotice != null) textNormalRecordNotice.text = "GAME OVER (턴 초과)";
                }
                if (Board.Instance != null) Board.Instance.isGameActive = false;
                break;
        }
    }

    // 3. 🎯 [이사 완료] 일반모드 전용 턴수 차감 및 승리 규칙 정산 회로
    private void CheckNormalTurnEnd()
    {
        if (currentStageData != null)
        {
            int maxTurns = currentStageData.maxTurns;

            // Board.cs에 누적된 진행 턴수를 정직하게 가져옵니다.
            int playedTurn = Board.Instance != null ? Board.Instance.currentTurn : 0;
            int remainingTurns = maxTurns - playedTurn;

            Debug.Log($"📊 [일반모드 턴 정산] 진행: {playedTurn} / 제한: {maxTurns} (남은 턴: {remainingTurns})");

            // 🏆 [승리 조건]: 나중에 몬스터 체력 변수가 연결되면 주석을 해제하세요.
            // if (BaseMonster.Instance != null && BaseMonster.Instance.currentHP <= 0)
            // {
            //     SetState(GameState.GameClear);
            //     return;
            // }

            // 💀 [패배 조건]: 제한 턴수를 모두 소모했다면 게임 오버
            if (remainingTurns <= 0)
            {
                SetState(GameState.GameOver);
                return;
            }
        }

        // 패배나 승리 조건에 걸리지 않았다면 플레이어 턴으로 환원하여 잠금 해제
        SetState(GameState.PlayerTurn);
    }
}

[CreateAssetMenu(fileName = "NewStageData", menuName = "Stage/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("--- 스테이지 기본 정보 ---")]
    public int stageIndex;            // 스테이지 번호
    public string stageName;          // 스테이지 이름

    [Header("--- 일반모드 클리어 조건 ---")]
    public int maxTurns;              // 제한 턴수
    public int targetScore;           // 목표 점수

    [Header("--- 등장 몬스터 정보 ---")]
    public GameObject[] enemyPrefabs; // 이 스테이지에서 스폰할 몬스터 프리팹들
}
