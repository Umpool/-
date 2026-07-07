using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // 🌟 슬라이더 및 UI 컴포넌트 제어용 필수 도구상자

public class PuzzleBattleManager : MonoBehaviour
{
    // 1. 모드 정의 스위치 삭제 (무조건 무한모드로만 작동하므로 필요 없음)
    public enum GameState { Ready, PlayerTurn, Matching, EnemyTurn, GameClear, GameOver }
    public GameState currentState = GameState.Ready;

    // 2. 기존 InitGame을 무한모드 전용으로 원상복구
    public void InitGame()
    {
        currentState = GameState.Ready;
        currentTurn = 0;
        currentScore = 0;
        isTimeOver = false;

        Debug.Log("🧼 [무한모드 전용 엔진] 세탁기 및 배틀 시스템 완공!");

        // 무한모드 전용 UI 패널 강제 On
        if (panel_InfiniteBattle != null) panel_InfiniteBattle.SetActive(true);
        if (panel_PuzzleBattle != null) panel_PuzzleBattle.SetActive(false);

        if (InfiniteMonster.Instance != null)
        {
            InfiniteMonster.Instance.ResetAndRespawnMonster();
        }

        SetState(GameState.PlayerTurn);
    }

    // 3. 상태 관리 함수 단순화
    public void SetState(GameState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case GameState.PlayerTurn:
                break;
            case GameState.Matching:
                break;
            case GameState.EnemyTurn:
                Debug.Log("👹 [무한모드] 적 반격 턴! 영웅 카드 무작위 타격");
                // 무한모드 전용 무작위 공격 실행 (기존 코드 연동)
                // 예: MonsterAttackRandomPartyCard(10f);
                CheckTurnEnd();
                break;
        }
    }


    // 턴 소모 및 승리/패배 규칙 체크
    // 🔔 [4] 일반모드 승리/패배 규칙 체크 (하나로 완벽하게 합쳐진 버전)
    private void CheckTurnEnd()
    {
        // 무한모드는 제한 턴수가 없으므로, 적 턴 연산이 끝나면 즉시 플레이어 턴으로 돌려놓습니다.
        SetState(GameState.PlayerTurn);
    }







    [Header("--- 무한모드 최종 정산 시스템 ---")]
    public GameObject panel_InfiniteReward;  // 결과창 패널(InfiniteRewardPanel 등)을 통째로 연결할 방
    public TextMeshProUGUI textFinalScore;   // Text_FinalScore 연결
    public TextMeshProUGUI textFinalTurns;   // Text_FinalTurns 연결
    public TextMeshProUGUI textRecordNotice; // Text_RecordNotice 연결
    public bool isTimeOver = false; // 💡 시간이 다 끝났음을 임시로 저장하는 안전핀

    [Header("--- 배틀 핵심 UI 패널 록온 ---")]
    public GameObject panel_PuzzleBattle;    // 일반 스테이지 패널 (Panel_NMPuzzleBattle)
    public GameObject panel_InfiniteBattle;  // 💡 [추가] 무한모드 패널 (Panel_INPuzzleBattle)
    public GameObject enemyContainer;

    [Header("실시간 배틀 카드 장부")]
    public List<CharacterCard> liveCards = new List<CharacterCard>(); // 👈 여기에 실시간으로 담깁니다.

    [Header("--- NPC 전용 1~10위 순위판 UI ---")]
    public TextMeshProUGUI textNPCLeaderboard; // 💡 요 방이 상단에 있어야 맨 밑바닥 함수가 에러가 안 납니다!
    public GameObject panel_NPCLeaderboard_Popup; // 🎯 마을 순위판 팝업창 자체를 기억할 전원 제어 방!

    [Header("--- 현재 배틀 필드 상황 ---")]
    // 중요! 어떤 모드의 몬스터든 이 주머니에 다 담을 수 있습니다.
    public BaseMonster currentTargetMonster;

    [Header("--- 아군 및 적군 HP 실시간 감시 주머니 ---")]
    public Slider enemyHPBar;              // 몬스터 체력바 슬라이더
    public List<Slider> heroHPBars = new List<Slider>(); // 아군 영웅 5인 체력바 슬라이더 리스트
    public TextMeshProUGUI turnTextUI;

    public int currentTurn = 0;
    public int currentScore = 0;
    public GameObject btn_StartTouchTrigger_Direct;

    public Board puzzleBoardComponent;

    // 🔓 [보호 수준 완전 개방] 외부(PuzzleBattleManager)에서 액세스하여 리셋할 수 있도록 public으로 변경합니다!
    public bool isSwapping = false;
    public bool isMatching = false;
    public bool isSwappingNow = false;


    [Header("--- 파티창 설정 ---")]
    [SerializeField] private Transform NMPartyContainer; // 이 부분이 정확히 들어가 있는지 확인하세요.





    public static PuzzleBattleManager Instance { get; private set; }

    private void Awake()
    {
        // 내 자신을 Instance에 등록합니다.
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }





    // 🌟 [새로 추가] 던전 안에서 파티원들의 진짜 최대 체력 원본을 기억해 둘 딕셔너리 주머니
    private static Dictionary<int, int> partyMaxHpBackup = new Dictionary<int, int>();
    private void Start()
    {
        currentTurn = 0;
        InitGame();

        if (GameManager.Instance != null && GameManager.Instance.partyMembers != null)
        {
            foreach (var character in GameManager.Instance.partyMembers)
            {
                if (character == null) continue;

                if (!partyMaxHpBackup.ContainsKey(character.id))
                {
                    partyMaxHpBackup[character.id] = character.hp;
                    character.hp = partyMaxHpBackup[character.id];
                    Debug.Log($"[최초 입장] {character.characterName} HP: {character.hp}");
                }
                else
                {
                    Debug.Log($"[연속 전투] {character.characterName} HP 유지: {character.hp}");
                }
            } // foreach 종료
        } // if 종료
    } // Start() 최종 종료





    public void OnUserDragBlock()
    {
        // 💡 [안전 장치 추가] 현재 이 배틀 매니저 스크립트가 붙어있는 오브젝트(배틀 화면)가 
        // 하이어라키 창에서 실제로 '켜져 있을 때'만 내부 로직을 실행하도록 막아줍니다.
        if (gameObject.activeInHierarchy == false)
        {
            return; // 배틀 화면이 꺼져있다면 아래 코드를 실행하지 않고 즉시 함수를 빠져나갑니다!
        }

        // -------------------------------------------------------------
        // 여기서부터는 배틀 화면이 정상적으로 켜져 있을 때만 실행됩니다.
        // -------------------------------------------------------------

        // 인풋매니저가 블록 드래그를 끝냈을 때 실행됩니다.
        Debug.Log("인풋매니저로부터 드래그 종료 신호 수신 완료!");

        // (나중에 여기에 매칭 검사하고 턴 누적하는 코드가 추가될 예정입니다)
    }

    // 배틀 진입
    public void StartPuzzleBattle(string gameMode)
    {
        // 1. 일반모드 신호(Stage_A, Stage_B 등)가 들어오면 이 매니저는 즉시 작동을 거부하고 나갑니다.
        if (!gameMode.Equals("2"))
        {
            Debug.LogWarning($"⚠️ [무한매니저] 무한모드(모드'2')가 아니므로 작동을 취소합니다. 입력된 모드: {gameMode}");
            return;
        }

        Debug.Log("♾️ [무한모드 엔진 가동] 무한 퍼즐 배틀을 시작합니다.");

        // 2. 무한모드 전용 UI 및 게임 판 세팅 (일반모드용 UI On/Off 코드는 싹 지웠습니다)
        GameManager.Instance.UpdateCanvasState(GameManager.CanvasState.PuzzleBattle);
        panel_PuzzleBattle.SetActive(true);

        // 3. 무한모드 전용 몬스터 및 타이머 스폰 루틴 독점 격발
        InfiniteMonster.Instance.SpawnInfiniteMonster();
        isProcessing = false;

        // 4. 상태 머신을 플레이어 턴으로 전환하여 퍼즐 조작 개시
        SetState(GameState.PlayerTurn);
    }





        // 이 밑으로는 기존 무한모드용 코드들이 그대로 유지됩니다 (손대지 않음)


        // 3. UI 갱신 및 랭킹 정산 (기존 기능 보존)
        if (textFinalScore != null) textFinalScore.text = $"최종 점수 : {finalScore:N0}";
        if (textFinalTurns != null) textFinalTurns.text = $"걸린 턴수 : {currentTurn} 턴";

        // Top 10 랭킹 계산
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
        for (int i = 0; i < 10; i++) PlayerPrefs.SetInt($"INF_RANK_{i + 1}", highScores[i]);
        PlayerPrefs.Save();

        // 4. UI 팝업 및 마무리
        if (currentRank >= 1 && currentRank <= 3 && textRecordNotice != null)
        {
            textRecordNotice.gameObject.SetActive(true);
            textRecordNotice.text = $"기록갱신! [{currentRank} 위] 달성!";
        }
        if (panel_InfiniteBattle != null) panel_InfiniteBattle.transform.Find("GAMEOVER TXT")?.gameObject.SetActive(true);
        if (btn_StartTouchTrigger_Direct != null) btn_StartTouchTrigger_Direct.SetActive(false);
    }




    // 🔒 2. 가장 중요! OnTimerEnd() 함수 전체를 확실하게 닫아주는 최종 바깥 중괄호입니다!

    // =========================================================================
    // ⚔️ 여기서부터 상단 함수와 완전히 분리된 깨끗한 독립형 새 함수가 시작됩니다!
    // =========================================================================

    // // 2. 마을에서 NPC 순위보기 버튼을 누르면 1위부터 10위까지의 보이지 않는 장부를 긁어와 화면에 쾅 꽂아주는 함수
    public void RefreshNPCLeaderboardUI()
    {


        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("무한모드 랭킹보드 (Top 10)\n");

        for (int i = 1; i <= 10; i++)
        {
            int score = PlayerPrefs.GetInt($"INF_RANK_{i}", 0);
            sb.AppendLine($"{i}위 : {score:N0} 대미지");
        }

        if (textNPCLeaderboard != null)
        {
            textNPCLeaderboard.text = sb.ToString();
        }

        if (panel_NPCLeaderboard_Popup != null)
        {
            panel_NPCLeaderboard_Popup.SetActive(true);
        }

        Debug.Log("[NPC 순위판] 보이지 않는 장부에서 탑텐 데이터를 긁어와 새로고침 완료!");
    }


    public void ResetBattleSystemForNextEntry()
    {
        // 하이어라키에 실제 존재하는 대문자 이름의 오브젝트를 찾아서 안전하게 꺼버립니다.
        GameObject goText = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle/GAMEOVER TXT")?.gameObject;
        if (goText != null)
        {
            goText.SetActive(false);
        }

        // 트리거 시작 버튼은 기존에 쓰고 계시던 리모컨 이름 그대로 켜줍니다.
        if (btn_StartTouchTrigger_Direct != null)
        {
            btn_StartTouchTrigger_Direct.SetActive(true);
            Debug.Log("🧹 [PuzzleBattleManager] 배틀 데이터 초기화 완수!");
        }
    }

    public void StartInfiniteStageViaButton(string modeName)
    {
        if (modeName == "infinite")
        {
            // 🌟 [에러 완벽 해결]: 내부에서 사용 중인 정확한 모드 시작 신호를 호출합니다.
            if (GameManager.Instance != null) GameManager.Instance.stageMode = 2;

            // 🌟 [경고 완벽 해결]: 유니티 6 표준 규격인 FindAnyObjectByType으로 변경했습니다!
            Board mainBoard = FindAnyObjectByType<Board>();
            if (mainBoard != null)
            {
                mainBoard.OnClickRealStartInfiniteTimer();
            }
        }

    }





     
    
