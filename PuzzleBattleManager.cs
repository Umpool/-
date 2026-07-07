using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [무한모드 전용 사령탑] 
/// 일반모드 코드를 완전히 도려내고, 오직 시간 제한 기반의 무한 퍼즐 배틀만 전담합니다.
/// </summary>
public class PuzzleBattleManager : MonoBehaviour
{
    public static PuzzleBattleManager Instance { get; private set; }

    // 무한모드 진행에 필요한 핵심 런타임 상태 정의
    public enum GameState { Ready, PlayerTurn, Matching, EnemyTurn, GameOver }

    [Header("--- 현재 무한모드 상태 ---")]
    public GameState currentState = GameState.Ready;

    [Header("--- 실시간 배틀 데이터 장부 ---")]
    public int currentTurn = 0;
    public int currentScore = 0;
    public bool isTimeOver = false; // 시간이 다 끝났음을 판정하는 안전핀

    [Header("--- 배틀 핵심 UI 패널 록온 ---")]
    public GameObject panel_PuzzleBattle;    // 퍼즐 블록들이 배치되는 메인 전장 패널
    public GameObject panel_InfiniteBattle;  // 무한모드 전용 상단 스코어 및 타이머 UI 패널
    public GameObject enemyContainer;        // 무한 몬스터가 생성되어 배치될 부모 그릇

    [Header("--- 아군 및 적군 실시간 HP 스캔 변수 그룹 ---")]
    public BaseMonster currentTargetMonster;            // 현재 플레이어가 타겟팅 중인 몬스터 주머니
    public Slider enemyHPBar;                           // 현재 전장에 배치된 몬스터의 HP 슬라이더 바
    public List<Slider> heroHPBars = new List<Slider>(); // 아군 파티원 5인의 실시간 HP 슬라이더 바 리스트
    public Board puzzleBoardComponent;                  // 연결될 보드 컴포넌트 리모컨


    [Header("--- 실시간 생존 영웅 카드 리스트 ---")]
    public List<CharacterCard> liveCards = new List<CharacterCard>();

    [Header("--- UI 관련 변수들 ---")]
    public TextMeshProUGUI textFinalScore;
    public TextMeshProUGUI textFinalTurns;
    public TextMeshProUGUI textRecordNotice;
    public TextMeshProUGUI textNPCLeaderboard;
    public GameObject panel_InfiniteReward;
    public GameObject panel_NPCLeaderboard_Popup;
    public GameObject btn_StartTouchTrigger_Direct;
    public TextMeshProUGUI turnTextUI;

    private void Awake()
    {
        // 싱글톤 인스턴스 등록 및 중복 방지 방어선 구축
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 무한모드 전용 엔진 공장 초기화
    /// </summary>
    public void InitGame()
    {
        currentState = GameState.Ready;
        currentTurn = 0;
        currentScore = 0;
        isTimeOver = false;

        Debug.Log("🧼 [무한모드 전용 엔진] 배틀 데이터 및 스코어 초기화 완공!");

        // 무한모드 전용 UI 레이아웃 강제 교체
        if (panel_InfiniteBattle != null) panel_InfiniteBattle.SetActive(true);
        if (panel_PuzzleBattle != null) panel_PuzzleBattle.SetActive(true);

        // 무한모드 전용 무한 리필 몬스터 엔진 가동
        if (InfiniteMonster.Instance != null)
        {
            InfiniteMonster.Instance.ResetAndRespawnMonster();
        }

        // 초기화 완료 후 플레이어에게 턴 주파수 이양
        SetState(GameState.PlayerTurn);
    }
    /// <summary>
    /// 무한모드 상태 제어 타워
    /// </summary>
    public void SetState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.PlayerTurn:
                // 플레이어가 블록을 드래그할 수 있도록 잠금 해제 신호 송신
                break;

            case GameState.Matching:
                // 블록 폭발 및 연쇄 콤보 연산 중 (조작 원천 차단)
                break;

            case GameState.EnemyTurn:
                Debug.Log("👹 [무한모드] 적 반격 턴! 파티원 카드 무작위 타격");

                // ⚔️ 무한모드 전용 몬스터 공격 연동 구역 (있을 경우 활성화)
                // if (InfiniteMonster.Instance != null) { InfiniteMonster.Instance.AttackRandomHero(); }

                // 반격 연출이 끝나면 즉시 다음 턴 준비
                CheckTurnEnd();
                break;

            case GameState.GameOver:
                Debug.Log("☠️ 무한모드 타임오버! 게임이 종료되었습니다.");
                if (Board.Instance != null) Board.Instance.isGameActive = false;
                break;
        }
    }

    /// <summary>
    /// 무한모드 턴 정산 회로
    /// </summary>
    private void CheckTurnEnd()
    {
        // 무한모드는 제한 턴수가 없으므로, 상태가 타임오버(GameOver)가 아니라면
        // 적의 반격 연산이 끝난 직후 즉시 플레이어 턴으로 환원시킵니다.
        if (!isTimeOver && currentState != GameState.GameOver)
        {
            SetState(GameState.PlayerTurn);
        }
    }
    // 던전 안에서 파티원들의 진짜 최대 체력 원본을 기억해 둘 딕셔너리 안전 장부
    private static Dictionary<int, int> partyMaxHpBackup = new Dictionary<int, int>();

    private void Start()
    {
        // 런타임 장부 깨끗하게 비우고 게임 초기화 격발
        currentTurn = 0;
        InitGame();

        // 게임 매니저에 등록된 실제 출전 파티원들의 데이터를 스캔하여 체력 동기화
        if (GameManager.Instance != null && GameManager.Instance.partyMembers != null)
        {
            foreach (var character in GameManager.Instance.partyMembers)
            {
                if (character == null) continue;

                // 처음 입장하는 캐릭터라면 진짜 원본 최대 체력을 안전 장부에 백업
                if (!partyMaxHpBackup.ContainsKey(character.id))
                {
                    partyMaxHpBackup[character.id] = character.hp;
                    character.hp = partyMaxHpBackup[character.id];
                    Debug.Log($"[무한던전 입장] {character.characterName} HP 원본 저장 완료: {character.hp}");
                }
                else
                {
                    // 연속 전투 상태라면 안전 장부에 저장된 원래 체력을 그대로 유지
                    Debug.Log($"[무한던전 연속] {character.characterName} 실시간 HP 대기 상태 유지: {character.hp}");
                }
            }
        }
    }
    /// <summary>
    /// 인풋 매니저가 유저의 블록 조작을 끝냈을 때 실행되는 신호 탑
    /// </summary>
    public void OnUserDragBlock()
    {
        // 🛡️ [런타임 안전 방어선] 배틀 패널이 실제로 화면에 켜져 있을 때만 인풋 연산을 실행합니다.
        if (gameObject.activeInHierarchy == false)
        {
            return;
        }

        Debug.Log("🎯 [무한사령탑] 인풋 매니저로부터 드래그 종료 신호 수신 완료!");
    }

    /// <summary>
    /// UI 버튼을 클릭하여 무한모드 전용 던전으로 즉시 진입하는 게이트 함수
    /// </summary>
    public void StartInfiniteStageViaButton(string modeName)
    {
        if (modeName == "infinite")
        {
            // 게임 매니저의 글로벌 상태 코드를 무한모드 규격인 '2'번으로 록온합니다.
            if (GameManager.Instance != null) GameManager.Instance.stageMode = 2;

            // 유니티 표준 규격에 맞추어 보드 컴포넌트를 탐색한 뒤 무한모드 타이머를 가동합니다.
            Board mainBoard = FindAnyObjectByType<Board>();
            if (mainBoard != null)
            {
                mainBoard.OnClickRealStartInfiniteTimer();
            }
        }
    }
    /// <summary>
    /// 외부 시스템에서 배틀 진입 신호를 보낼 때 무한모드 엔진을 켜주는 메인 게이트
    /// </summary>
    public void StartPuzzleBattle(string gameMode)
    {
        if (!gameMode.Equals("2"))
        {
            Debug.LogWarning($"⚠️ [무한매니저] 무한모드(모드'2')가 아니므로 작동을 거부합니다.");
            return;
        }

        Debug.Log("♾️ [무한모드 엔진 가동] 무한 퍼즐 배틀 월드로 진입합니다.");

        // 1. 캔버스 및 패널 상태 동기화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCanvasState(GameManager.CanvasState.PuzzleBattle);
        }
        if (panel_PuzzleBattle != null) panel_PuzzleBattle.SetActive(true);

        // 2. 몬스터 스폰 및 보드 활성화
        if (InfiniteMonster.Instance != null)
        {
            InfiniteMonster.Instance.SpawnInfiniteMonster();
        }
        if (Board.Instance != null) Board.Instance.isGameActive = true;

        // 3. UI 상태 초기화 (주석 요구사항 반영)
        if (btn_StartTouchTrigger_Direct != null) btn_StartTouchTrigger_Direct.SetActive(false);

        // 게임오버 텍스트 초기화 (주석 요구사항 반영)
        GameObject goText = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle/GAMEOVER TXT")?.gameObject;
        if (goText != null) goText.SetActive(false);

        SetState(GameState.PlayerTurn);
    }

    public void OnTimerEnd(int finalScore)
    {
        isTimeOver = true;
        SetState(GameState.GameOver);

        // 1. 결과 UI 표시
        if (panel_InfiniteReward != null) panel_InfiniteReward.SetActive(true);
        if (textFinalScore != null) textFinalScore.text = $"최종 점수 : {finalScore:N0}";
        if (textFinalTurns != null) textFinalTurns.text = $"걸린 턴수 : {currentTurn} 턴";

        // 2. 랭킹 시스템 정산 회로 (주석 요구사항 반영)
        int[] highScores = new int[10];
        for (int i = 0; i < 10; i++) highScores[i] = PlayerPrefs.GetInt($"INF_RANK_{i + 1}", 0);

        int currentRank = 0;
        for (int i = 0; i < 10; i++)
        {
            if (finalScore > highScores[i])
            {
                for (int j = 9; j > i; j--) highScores[j] = highScores[j - 1];
                highScores[i] = finalScore;
                currentRank = i + 1;
                break;
            }
        }

        // 데이터 저장
        for (int i = 0; i < 10; i++) PlayerPrefs.SetInt($"INF_RANK_{i + 1}", highScores[i]);
        PlayerPrefs.Save();

        // 3. 순위 진입 축하 연출
        if (currentRank >= 1 && currentRank <= 3 && textRecordNotice != null)
        {
            textRecordNotice.gameObject.SetActive(true);
            textRecordNotice.text = $"🔥 기록갱신! [{currentRank} 위] 달성! 🔥";
        }

        // 4. 게임오버 전용 UI 활성화
        GameObject goText = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle/GAMEOVER TXT")?.gameObject;
        if (goText != null) goText.SetActive(true);
    }
    public void RefreshNPCLeaderboardUI()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("무한모드 랭킹보드 (Top 10)\n");

        for (int i = 1; i <= 10; i++)
        {
            int score = PlayerPrefs.GetInt($"INF_RANK_{i}", 0);
            sb.AppendLine($"{i} 위 : {score:N0} 대미지");
        }

        if (textNPCLeaderboard != null)
        {
            textNPCLeaderboard.text = sb.ToString();
        }

        if (panel_NPCLeaderboard_Popup != null)
        {
            panel_NPCLeaderboard_Popup.SetActive(true);
        }

        Debug.Log("[NPC 순위판] 탑텐 데이터를 긁어와 새로고침 완료!");
    }


    /// <summary>
    /// 다음 판 진입을 위해 연출 UI 및 스타트 버튼을 초기 상태로 복구하는 함수
    /// </summary>
    public void ResetBattleSystemForNextEntry()
    {
        GameObject goText = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle/GAMEOVER TXT")?.gameObject;
        if (goText != null) goText.SetActive(false);

        if (btn_StartTouchTrigger_Direct != null)
        {
            btn_StartTouchTrigger_Direct.SetActive(true);
            Debug.Log("🧹 [PuzzleBattleManager] 다음 진입을 위한 전장 청소 완수!");
        }
    }
/// <summary>
/// 무한모드 제한 시간이 종료되었을 때 타이머에 의해 강제 격발되는 최종 정산 함수
/// </summary>
    public void OnTimerEnd(int finalScore)
    {
    // 중복 정산 방지 및 게임 상태 변경
    isTimeOver = true;
    SetState(GameState.GameOver);

    // 1. UI 패널 및 결과 표시
    if (panel_InfiniteReward != null) panel_InfiniteReward.SetActive(true);
    if (textFinalScore != null) textFinalScore.text = $"최종 점수 : {finalScore:N0}";
    if (textFinalTurns != null) textFinalTurns.text = $"걸린 턴수 : {currentTurn} 턴";

    // 2. 랭킹 시스템 정산 회로 (Top 10 계산)
    int[] highScores = new int[10];
    for (int i = 0; i < 10; i++)
    {
        highScores[i] = PlayerPrefs.GetInt($"INF_RANK_{i + 1}", 0);
    }

    int currentRank = 0;
    for (int i = 0; i < 10; i++)
    {
        if (finalScore > highScores[i])
        {
            // 아래 순위 기록들을 한 칸씩 뒤로 밀어내기
            for (int j = 9; j > i; j--)
            {
                highScores[j] = highScores[j - 1];
            }
            highScores[i] = finalScore;
            currentRank = i + 1;
            break;
        }
    }



    // 4. 게임오버 전용 시각 텍스트 및 시작 차단 연출
    GameObject goText = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle/GAMEOVER TXT")?.gameObject;
    if (goText != null) goText.SetActive(true);

    if (btn_StartTouchTrigger_Direct != null) btn_StartTouchTrigger_Direct.SetActive(false);
    }

// 🎯 [정밀 UI 픽셀 위치 이동 부품]: 블록들이 꼬이거나 아래로 밀려 내려가지 않게 막아주는 방어선 코드
    private IEnumerator MoveBlockSmoothlyUI(GameObject target, Vector2 targetPosition)
    {
        if (target == null) yield break;
        RectTransform rt = target.GetComponent<RectTransform>();
        if (rt == null) yield break;

        rt.SetAsLastSibling(); // 드래그 중인 블록이 다른 블록 뒤로 숨지 않게 레이어 맨 앞으로 이동
        Vector2 startPos = rt.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.15f; // 쾌속 슬라이딩 스피드 0.15초 고정

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // 부드러운 감속 연출 효과
            if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        if (rt != null) rt.anchoredPosition = targetPosition;
    }

    public void UpdateTurnTextUI()
    {
        if (turnTextUI != null)
        {
            turnTextUI.text = $"{currentTurn} 턴";
        }
    }
}


