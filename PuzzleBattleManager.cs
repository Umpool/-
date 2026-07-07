using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // 🌟 슬라이더 및 UI 컴포넌트 제어용 필수 도구상자

public class PuzzleBattleManager : MonoBehaviour
{
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

    [Header("ㅡ 일반 스테이지 기획 데이터 ㅡ")]
    public bool isNormalStageMode = false;   // 현재 일반 스테이지 모드인지 체크하는 스위치
    public string currentStageType = "";     // "Stage_A" 또는 "Stage_B" 기록용
    public int currentRound = 1;             // 현재 진행 중인 라운드 (1라운드부터 시작)
    public int maxRound = 5;                // 해당 스테이지의 총 라운드 수

    [Header("ㅡ 일반 스테이지 연결 UI ㅡ")]
    public GameObject panel_StageSelect;     // 스테이지 선택 창 (Panel_StageSelect)
    public GameObject panel_NMPuzzleBattle;  // 일반 스테이지 배틀 창 (Panel_NMPuzzleBattle)
    public GameObject panel_StageRewardPopup;// 3라운드마다 뜨는 보상 팝업창
    public GameObject panel_StageClearResult;// 보스 처치 후 뜨는 최종 결과창



    [Header("--- 파티창 설정 ---")]
    [SerializeField] private Transform NMPartyContainer; // 이 부분이 정확히 들어가 있는지 확인하세요.
    [SerializeField] private GameObject normalStagePuzzleBoard; 
    [SerializeField] private TMPro.TextMeshProUGUI normalStageTextUI;
    public TMPro.TextMeshProUGUI textStageRoundUI;
    // 🎯 [오늘의 미션]: 유니티 에디터에서 EnemyPrefab을 등록할 수 있는 주머니입니다!
    [Header("ㅡ 일반 스테이지용 적 프리팹 ㅡ")]
    public GameObject normalEnemyPrefab; // 일반 스테이지용 일반 적 프리팹
    public GameObject bossEnemyPrefab;   // 일반 스테이지용 보스 적 프리팹
    [Header("ㅡ 일반 스테이지 전용 전투 UI 전광판 ㅡ")]
public TMPro.TextMeshProUGUI textNormalSynergyDisplay; // Text_SynergyDisplay 연결용
public TMPro.TextMeshProUGUI textNormalComboUI;       // Combo 연결용



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
        UpdateTurnTextUI();

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
        Debug.Log($"🚀 [전투 개막] 모드 이름 판독 중: {gameMode}");
        // 🎯 [오늘의 미션]: 일반 모드가 작동 중일 때 들어오는 무한 모드 터치 신호를 차단합니다.
        if (isNormalStageMode && gameMode == "infinite")
        {
            Debug.Log("🛡️ [방어 성공] 일반 모드 중 무한 모드 강제 시작 신호가 가로채는 것을 완벽 차단했습니다.");
            return;
        }
        // 🎯 [스테이지 길이 세팅]: 들어온 gameMode 문자열에 따라 최대 라운드 수를 다르게 설정합니다.
        int maxStageRound = 5; // 💡 여기에 'int'를 붙여서 변수를 새로 정의해 줍니다!

        // 🎯 [화면 전환 및 스테이지 길이 세팅]: 이전 패널들을 끄고 배틀 패널을 켭니다.
        if (gameMode == "Stage_A")
        {
            maxStageRound = 5; // A버튼: 1-1 ~ 1-5 스테이지
            Debug.Log($"🎮 [일반 모드 A] 1-1부터 1-{maxStageRound}까지 진행됩니다.");

            // 🎬 [화면 전환 로직 실행]
            if (panel_NMPuzzleBattle != null) panel_NMPuzzleBattle.SetActive(true); // 배틀 패널 ON (자식 포함)

            // 🧹 기존 화면들 체크 해제 (OFF)
            // (※ 만약 아래 변수명에 빨간 줄이 가면, 상단에 선언해 두신 실제 마을/스테이지선택 패널 변수명으로 변경해 주세요)
            if (GameManager.Instance != null && GameManager.Instance.panel_Village != null) 
                GameManager.Instance.panel_Village.SetActive(false);             

            if (panel_StageSelect != null) 
                panel_StageSelect.SetActive(false); // 💡 앞에 GameManager 주소를 빼고 다이렉트로 연결!
            Debug.Log("🖥️ [화면 정리 완료] 마을 및 스테이지 선택창을 끄고 배틀 전장 화면을 켰습니다.");
        }
        else if (gameMode == "Stage_B")
        {
            maxStageRound = 7; // B버튼: 1-1 ~ 1-7 스테이지
            Debug.Log($"🎮 [일반 모드 B] 1-1부터 1-{maxStageRound}까지 진행됩니다.");

            // 🎬 [화면 전환 로직 실행]
            if (panel_NMPuzzleBattle != null) panel_NMPuzzleBattle.SetActive(true); // 배틀 패널 ON (자식 포함)

            // 🧹 기존 화면들 체크 해제 (OFF)
            if (GameManager.Instance != null && GameManager.Instance.panel_Village != null) 
                GameManager.Instance.panel_Village.SetActive(false);             

            if (panel_StageSelect != null) 
                panel_StageSelect.SetActive(false); // 💡 앞에 GameManager 주소를 빼고 다이렉트로 연결!
            Debug.Log("🖥️ [화면 정리 완료] 마을 및 스테이지 선택창을 끄고 배틀 전장 화면을 켰습니다.");
        }
    



        // 🎯 [오늘의 미션]: 일반 스테이지 모드일 때는 무한모드 세탁기 코드를 완전히 건너뜁니다!
            // 🚀 [일반 스테이지 전용 3매치 가동 핵심 코드]
            if (isNormalStageMode)
            {
                // 1. 실시간 살아있는 카드 장부를 깨끗하게 비우고 새로 시작할 준비를 합니다.
                liveCards.Clear();

                // 2. 일반 스테이지 전용 UI 및 보드 활성화
                if (NMPartyContainer != null) NMPartyContainer.gameObject.SetActive(true);
                if (normalStagePuzzleBoard != null) normalStagePuzzleBoard.SetActive(true);

                // 3. 일반 스테이지 전용 텍스트 UI에 진행 상황 표시 (A/B 선택에 맞춤)
                if (normalStageTextUI != null)
                {
                    normalStageTextUI.text = $"STAGE 1-1 / 1-{maxStageRound}";
                    Debug.Log($"📝 [UI 연동] 일반 스테이지 UI 텍스트 세팅 완료 (목표: 1-{maxStageRound})");
                }

                // 4. 6x6 보드판 정품 리필 및 매치 엔진 가동
                if (Board.Instance != null)
                {
                    Debug.Log("🎲 [보드 초기화] 잠겨있던 문지기 스위치를 강제로 풀고 6x6 보드판 정품 리필을 가동합니다.");
                    Board.Instance.InitializeNewBoard();
                }
                else
                {
                    Debug.LogError("⚠️ [오류] Board.Instance를 찾을 수 없습니다! 씬에 Board 스크립트가 있는지 확인하세요.");
                }

                Debug.Log("🎮 [독점회로 가동] 일반모드 전용 셋업을 완료하고 무한모드 간섭을 차단했습니다.");
                // 💡 기존에 있던 return;을 과감히 제거하여 아래의 몬스터 이동(181줄~) 및 시너지 출력 코드까지 연달아 부드럽게 흐르도록 만듭니다!
            }
        // 🎯 [오늘의 최종 미션]: 무한모드 쪽에 생성된 몬스터들을 일반모드 EnemyContainer 상자 밑으로 쏙 배달해 줍니다!
        GameObject infEnemyContainer = GameObject.Find("Panel_INPuzzleBattle/EnemyContainer");
        GameObject nmEnemyContainerObj = GameObject.Find("Panel_NMPuzzleBattle/EnemyContainer");

        if (infEnemyContainer != null && nmEnemyContainerObj != null)
        {
            foreach (Transform child in infEnemyContainer.transform)
            {
                child.SetParent(nmEnemyContainerObj.transform, false);
            }
            Debug.Log("🚚 [배달 완료] 1라운드 일반 몬스터 카드를 일반 스테이지 상자로 안전하게 이사시켰습니다!");
        }

            // 일반모드 시너지 텍스트 출력
    if (textNormalSynergyDisplay != null) 
    {
        // (예시: 기존 시너지 매니저에서 텍스트를 받아와 출력합니다)
        textNormalSynergyDisplay.text = "현재 활성화된 시너지 효과 표시 구역"; 
    }
    
    
    
    // 일반모드 콤보 텍스트 출력
        if (textNormalComboUI != null)
        {
            // 콤보 변수 이름을 넣어 화면에 실시간 출력합니다.
            textNormalComboUI.text = $"COMBO!";
        }

        // 🛡️ [질문자님 제보 특제 안전핀 장착]
        // 오직 인스펙터 버튼 매개변수 칸에 'infinite'라고 적혀있을 때만 무한모드 세탁기를 돌립니다!
        if (gameMode == "infinite")
        {
            // 1. 대장 컴퓨터에게 무한모드(2) 상태임을 각인
            if (GameManager.Instance != null)
            {
                GameManager.Instance.stageMode = 2;
            }

            // 2. 무한모드 전용 몬스터 0점 세탁 및 풀피 부활
            if (InfiniteMonster.Instance != null)
            {
                InfiniteMonster.Instance.ResetAndRespawnMonster();
            }

            // 3. 내부 UI 전광판 무한모드용 완전 리셋
            currentTurn = 0;
            currentScore = 0;
            isTimeOver = false;
            UpdateTurnTextUI();

            Debug.Log("🧼 [무한모드 전용] 세탁기 및 무선 안테나 리셋 정산 완공!");
        }
        else
        {
            // 🏰 [일반 모드 구역]: 매개변수가 infinite가 아니라면 기존 일반 스테이지 규칙을 그대로 따릅니다.
            // (만약 기존에 일반 모드용 turn이나 score 세팅이 따로 있었다면 여기에 복구해주시면 됩니다.)
            Debug.Log("⚔️ [일반 모드 전용] 원래 기획 흐름대로 부작용 없이 안전하게 전투를 시작합니다.");
        }

        // 4. [공통 가동 엔진]: 일반 모드든 무한모드든 퍼즐판 자체는 신선하게 새로 굴려야 합니다!
        if (puzzleBoardComponent != null)
        {
            puzzleBoardComponent.StopAllCoroutines();

            // 잠겨있던 문지기 스위치를 공통으로 안전하게 열어줍니다.
            // 🔓 맨 앞을 public으로 바꿔서 외부(PuzzleBattleManager)에서도 마음대로 리셋할 수 있게 문을 엽니다!
            puzzleBoardComponent.enabled = false;
            puzzleBoardComponent.gameObject.SetActive(false);
            puzzleBoardComponent.gameObject.SetActive(true);
            puzzleBoardComponent.enabled = true;


            // 새 블록 정품 배치 가동
            puzzleBoardComponent.InitializeNewBoard();
            
        }
        

        // 5. 버튼 및 UI 트리거 마감 처리
        if (panel_InfiniteBattle != null)
        {
            Transform gameover = panel_InfiniteBattle.transform.Find("GAMEOVER TXT");
            if (gameover != null) gameover.gameObject.SetActive(false);
        }

        if (btn_StartTouchTrigger_Direct != null)
        {
            btn_StartTouchTrigger_Direct.SetActive(false); // 터치 문구 숨기기
        }
    }





    // 🌟 [개발자님 최신 계층구조 200% 정밀 반영]: 하단 아군 영웅 5명의 카드와 HP 바 주소를 1대1 유기적 연동시킵니다!
    private void SetupBattleEntities()
    {
        Debug.Log("[배틀 연산 1단계] 내 정예 파티원 데이터 수거 및 HP 회선 연결 시작!");
        if (GameManager.Instance == null || GameManager.Instance.partyMembers == null) return;

        // 🌟 [핵심 수정]: 일반 스테이지 패널이나 무한 모드 패널 중 현재 하이어라키에서 실제로 '켜져 있는 패널'을 대장 부모로 선정합니다!
        GameObject activeBattlePanel = null;
        if (panel_InfiniteBattle != null && panel_InfiniteBattle.activeSelf) activeBattlePanel = panel_InfiniteBattle;
        else if (panel_PuzzleBattle != null && panel_PuzzleBattle.activeSelf) activeBattlePanel = panel_PuzzleBattle;
else if (panel_NMPuzzleBattle != null && panel_NMPuzzleBattle.activeSelf) activeBattlePanel = panel_NMPuzzleBattle;
        if (activeBattlePanel != null)
        {
            // 🌟 이제 현재 켜져 있는 전장 패널 밑에서 'PartyContainer_Battle' 상자를 정확하게 조준 타격합니다!
        // 🎯 [244번째 줄부터 249번째 줄까지 드래그해서 이 코드로 교체합니다]
        Transform battlePartyListTrans = null;
        if (activeBattlePanel != null)
        {
            battlePartyListTrans = activeBattlePanel.transform.Find("PartyContainer_Battle");
        }

        // 🎯 [오늘의 미션]: 만약 위에서 상자를 못 찾았더라도, 일반 스테이지 전용 주머니가 세팅되어 있다면 그걸 다이렉트로 사용합니다!
        if (battlePartyListTrans == null && NMPartyContainer != null)
        {
            battlePartyListTrans = NMPartyContainer;
        }

        if (battlePartyListTrans == null)
        {
            Debug.LogWarning("[구조 점검] 아군 파티를 나열할 PartyContainer_Battle 상자를 찾지 못했습니다.");
            return;
        }


            // ====== 🚀 이 아래의 자식 슬롯 수집 및 슬라이더 퍼센트 계산(While/Offset) 코드는 방금 수정한 그대로 완벽히 유지해 줍니다! ======
            List<Transform> heroCardSlots = new List<Transform>();
            foreach (Transform child in battlePartyListTrans)
            {
                if (child.name.Contains("Battle_HeroSlot"))
                {
                    heroCardSlots.Add(child);
                }
            }
            // 🎯 [파티창 엔진 연동]: 수집된 자식 슬롯들에 실제 캐릭터 데이터를 쌩쌩하게 주입합니다.
            for (int i = 0; i < heroCardSlots.Count; i++)
            {
                var partyIconScript = heroCardSlots[i].GetComponent<PartyIcon>(); 
                if (partyIconScript != null)
                {
                    // GameManager의 실제 파티원 명단(partyMembers) 데이터를 순서대로 매칭합니다.
                    if (GameManager.Instance != null && GameManager.Instance.partyMembers != null && i < GameManager.Instance.partyMembers.Count)
                    {
                        var heroData = GameManager.Instance.partyMembers[i];
                        
                        // 공유해주신 PartyIcon.cs의 Setup 함수를 깨워 데이터와 배틀 활성화(true) 신호를 보냅니다!
                        partyIconScript.Setup(heroData, isBattle: true);
                        heroCardSlots[i].gameObject.SetActive(true);
                    }
                }
            }
            Debug.Log("🛡️ [파티창 연동 완료] 기존 로직을 보존한 상태로 PartyIcon 스펙 주입을 마쳤습니다!");

            heroHPBars.Clear();
            int activePartyCount = GameManager.Instance.partyMembers.Count;

            for (int i = 0; i < heroCardSlots.Count; i++)
            {
                if (i >= activePartyCount)
                {
                    // 선택된 실제 영웅 데이터 개수를 초과하는 남는 슬롯 카드는 전원 OFF 숨김 처리
                    heroCardSlots[i].gameObject.SetActive(false);
                    continue;
                }

                CharacterData currentHeroData = GameManager.Instance.partyMembers[i];
                heroCardSlots[i].gameObject.SetActive(true);

                // 🌟 [PartyIcon 연동 비주얼 입히기]: 보내주신 PartyIcon.cs의 Setup 함수를 깨워 0.00초 만에 진짜 내 영웅 카드 그래픽 옷을 입혀줍니다!
                PartyIcon partyIconScript = heroCardSlots[i].GetComponent<PartyIcon>();
                if (partyIconScript != null)
                {
                    partyIconScript.Setup(currentHeroData);
                }
                

                // 🌟 [HP 바 연동]: 자식 밑에 매달려 대기 중인 슬라이더 'HP_Bar'를 추적해 캐릭터 고유 체력 영점을 강제 동기화시킵니다!
                // 📄 211번 라인 부근 기존 Slider 연결 코드 구역을 찾아 아래 코드로 완전히 교체합니다!
                Transform hpBarTrans = heroCardSlots[i].transform.Find("HP_Bar");
                if (hpBarTrans != null)
                {
                    Slider hpSlider = hpBarTrans.GetComponent<Slider>();
                    if (hpSlider != null)
                    {
                        // 🌟 1. [퍼센트 작동 환경 구축] 슬라이더의 작동 범위를 무조건 0부터 1까지(비율)로 고정합니다.
                        hpSlider.minValue = 0f;
                        hpSlider.maxValue = 1f;

                        // 🌟 2. static 주머니에 저장해 둔 이 영웅의 진짜 '최대 체력 원본' 가져오기
                        int maxHP = 100; // 원본을 못 찾을 때를 대비한 안전 장치 수치
                        if (partyMaxHpBackup.ContainsKey(currentHeroData.id))
                        {
                            maxHP = partyMaxHpBackup[currentHeroData.id];
                        }

                        // 🌟 3. 현재 체력과 최대 체력을 1:1 비교하여 정밀한 비율(0.0 ~ 1.0) 계산
                        float hpPercent = (float)currentHeroData.hp / maxHP;

                        // 🌟 4. 비율 수치를 슬라이더에 주입하여 풀피일 땐 100% 빈틈없이 초록색으로 꽉 채웁니다!
                        hpSlider.value = hpPercent;

                        // 🌟 5. [인스펙터 잠금 해제 및 4대 레이아웃 강제 동기화 대완공]
                        // 프리팹 수치와 상관없이 부모(HP_Bar), 배경, 게이지의 모든 영역을 1:1로 강제 밀착시킵니다.
                        RectTransform hpBarRect = hpBarTrans.GetComponent<RectTransform>();
                        RectTransform backgroundRect = hpBarTrans.Find("Background")?.GetComponent<RectTransform>();
                        RectTransform fillArea = hpBarTrans.Find("Fill Area")?.GetComponent<RectTransform>();
                        RectTransform fill = fillArea?.Find("Fill")?.GetComponent<RectTransform>();

                        // ① 최상위 부모인 HP_Bar의 높이를 날씬한 일자형(12)으로 강제 픽스
                        if (hpBarRect != null)
                        {
                            var size = hpBarRect.sizeDelta;
                            size.y = 12f; // 뚱뚱하던 높이를 날씬하게 깎음
                            hpBarRect.sizeDelta = size;
                        }

                        // ② 빨간색 배경(Background)을 부모 크기에 100% 꽉 차게 자석 정렬
                        if (backgroundRect != null)
                        {
                            backgroundRect.anchorMin = Vector2.zero;
                            backgroundRect.anchorMax = Vector2.one;
                            backgroundRect.offsetMin = Vector2.zero;
                            backgroundRect.offsetMax = Vector2.zero;
                        }

                        // ③ 게이지 영역(Fill Area)을 부모 크기에 100% 꽉 차게 자석 정렬
                        if (fillArea != null)
                        {
                            fillArea.anchorMin = Vector2.zero;
                            fillArea.anchorMax = Vector2.one;
                            fillArea.offsetMin = Vector2.zero;
                            fillArea.offsetMax = Vector2.zero;
                        }

                        // ④ 초록색 피(Fill)의 여백과 앵커를 완벽하게 일치시켜 오차 박멸
                        if (fill != null)
                        {
                            fill.anchorMin = Vector2.zero;
                            fill.anchorMax = Vector2.one;
                            fill.offsetMin = Vector2.zero;
                            fill.offsetMax = Vector2.zero; // 🚀 마이너스로 삐져나가던 붉은 잔상 강제 소멸
                        }

                        // ⑤ 자동 정렬 시스템(Layout Group)의 강제 찌그러트림 간섭을 최종 차단
                        UnityEngine.UI.LayoutElement hpLayout = hpSlider.GetComponent<UnityEngine.UI.LayoutElement>();
                        if (hpLayout != null)
                        {
                            hpLayout.ignoreLayout = true;
                        }

                        // 원래의 매니저 주머니 등록 코드로 부드럽게 이어집니다.
                        heroHPBars.Add(hpSlider);
                    }
                }


            }
            Debug.Log($"[아군 진형 연동 완공] 총 {heroHPBars.Count}명의 파티원이 실시간 생명력 게이지를 장착 완료했습니다!");
        }
    }

    // 🔔 [여기 추가] 인게임 중 스폰된 캐릭터들이 스스로의 HP바를 등록하러 오는 입구입니다.
    public void RegisterHeroHPBar(Slider heroSlider)
    {
        if (heroSlider == null) return;

        /* 
           🔒 [전투 화면 전용 안전장치]
           현재 퍼즐 배틀 패널 오브젝트가 화면에 활성화(True)되어 있을 때만 
           영웅의 HP 바를 배틀 시스템 주머니에 등록합니다!
           
           ※ 주의: 만약 씬에 배치된 퍼즐 배틀 패널 오브젝트 이름이 다르면 
           아래 'gameObject' 대신 해당 패널 변수명을 적어주셔도 됩니다.
        */
        if (gameObject.activeInHierarchy == false)
        {
            // 전투 패널이 꺼져 있다면 (예: 마을, 로비 등) 등록하지 않고 즉시 차단합니다.
            return;
        }

        if (heroHPBars == null)
        {
            heroHPBars = new List<Slider>();
        }

        if (!heroHPBars.Contains(heroSlider))
        {
            heroHPBars.Add(heroSlider);
            Debug.Log($"[전투 전용 자동 연동] 영웅 HP 바 등록 완료! (현재 {heroHPBars.Count}개)");
        }
    }

    public void OnClickBackToVillageFromInfinite()
    {
        // 🧼 [왕초보 특제: 마을로 가기 버튼 클릭 즉시 완벽 세탁기 가동]
        currentTurn = 0;       // 1. 내부 턴수 즉시 0으로 초기화
        currentScore = 0;      // 2. 내부 점수 즉시 0으로 초기화
        if (InfiniteMonster.Instance != null)
        {
            InfiniteMonster.Instance.ResetAndRespawnMonster(); // ◀ 새 함수 이름으로 교체!
        }
        isTimeOver = false;    // 3. 마우스를 가로막던 제한시간 종료 안전핀 즉시 해제 (False)
        UpdateTurnTextUI();    // 4. 화면에 보이는 인게임 턴수 글자도 즉시 "0 턴"으로 초기화

        // 5. 다음 판 진입 시 버그 없이 바로 굴러가도록 보드판 엔진 가동 및 새 블록 정품 배치!
        if (puzzleBoardComponent != null)
        {
            puzzleBoardComponent.gameObject.SetActive(true);
            puzzleBoardComponent.enabled = true;
            puzzleBoardComponent.InitializeNewBoard();
        }

        // 6. 무한모드 패널 강제 OFF (숨기기)
        GameObject infinitePanel = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle")?.gameObject;
        if (infinitePanel != null)
        {
            infinitePanel.SetActive(false);
        }

        // 7. 무한모드 결과창 패널 강제 OFF
        if (panel_InfiniteReward != null)
        {
            panel_InfiniteReward.SetActive(false);
        }

        // 8. 대장 컴퓨터에게 마을 이동 및 단축바 부활 명령 전송
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClickInfiniteStageBackButton();
            GameManager.Instance.ExitBattleStage();

            Debug.Log("★ [마을 복귀 및 대완공] 마을로 가기 버튼을 누른 타이밍에 모든 데이터 초기화 및 보드판 세탁 완료!");
        }
        // 5. 다음 판 진입 시 버그 없이 바로 굴러가도록 보드판 엔진 가동 및 새 블록 정품 배치!
        if (puzzleBoardComponent != null)
        {
            // 🛑 [질문자님 특제 차단벽 붕괴 스위치]: 백그라운드에서 아직도 무언가를 기다리며 마우스를 잠그고 있던 
            // 모든 연쇄 폭발, 스왑, 데드락 판정용 스위치 주머니를 강제로 완전히 열어버립니다(false)!
            puzzleBoardComponent.StopAllCoroutines(); // 1. 유령 코루틴 완전 사형
            puzzleBoardComponent.isGameActive = false; // 3. 인게임 활성화 플래그 완전 초기화

            // 🔎 만약 Board 스크립트 내부에 swap이나 match 플래그가 매개변수로 존재한다면 
            // 원격으로 직접 머리채 잡고 false로 문질러 닦아줍니다!
            // (reflection이나 직접 접근 대신 오브젝트 컴포넌트를 껐다 켜서 완벽히 공장 초기화 시킵니다.)
            puzzleBoardComponent.enabled = false;

            puzzleBoardComponent.gameObject.SetActive(true);
            puzzleBoardComponent.enabled = true; // 4. 마우스 클릭 하드웨어 센서 완벽 개방!
            puzzleBoardComponent.InitializeNewBoard(); // 5. 새 블록 정품 리필 완공!
        }

    }

    public void OnTimerEnd()
    {
        // 1. 상태 동결
        isTimeOver = true;
        if (puzzleBoardComponent != null) { puzzleBoardComponent.enabled = false; puzzleBoardComponent.StopAllCoroutines(); }

        // 2. 몬스터 누적 대미지 데이터 연동 (핵심)
        int finalScore = 0;
        if (InfiniteMonster.Instance != null)
        {
            finalScore = Mathf.FloorToInt(InfiniteMonster.Instance.totalDamageDealt);
        }
        else
        {
            // 백업: TMP에서 숫자 추출
            TMPro.TextMeshProUGUI realScoreTMP = transform.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (realScoreTMP != null)
            {
                string cleanNumbers = System.Text.RegularExpressions.Regex.Replace(realScoreTMP.text, @"[^\d]", "");
                int.TryParse(cleanNumbers, out finalScore);
            }
        }

        // 🎯 448번째 줄 구역을 이 코드로 교체해 줍니다.
        if (liveCards.Count == 0)
        {
            Debug.Log("모든 몬스터 처치! 다음 웨이브 준비");

            if (isNormalStageMode)
            {
                OnClearCurrentRound(); // 우리가 만든 라운드 정산 함수 실행!
                return;
            }
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


    // ✨ [추가] 몬스터가 턴 종료 시 살아있는 우리 캐릭터 카드를 무작위로 때리는 핵심 공격 회로
    public void MonsterAttackRandomPartyCard(float monsterDamage)
    {
        // 1. 화면에 생성되어 배치된 모든 캐릭터 카드(CharacterCard) 목록을 전수 조사하여 수거합니다.
        // 기존 FindObjectsOfType 코드는 아예 삭제합니다!
        CharacterCard[] activeCards = liveCards.ToArray(); // 👈 실시간 장부를 그대로 가져오므로 꼬일 일이 전혀 없습니다.



        // 2. 만약 살아 움직이는 파티원 카드가 화면에 한 장이라도 존재한다면 공격을 감행합니다.
        if (activeCards.Length > 0)
        {
            // 3. 무작위로 타겟 카드를 한 장 선정합니다 (예: 4명 중 1명 로또 타격)
            int randomTargetIndex = Random.Range(0, activeCards.Length);
            CharacterCard targetCard = activeCards[randomTargetIndex];

            if (targetCard != null)
            {
                // 4. 선정된 그 카드의 체력만 정직하게 쾅! 깎아내립니다.
                targetCard.TakeDamage(monsterDamage);
                Debug.Log($"💥 [몬스터 반격] 적이 파티원 [{(targetCard.GetComponent<PartyIcon>() != null ? targetCard.GetComponent<PartyIcon>().myData.characterName : "이름 없음")}]을(를) 공격하여 {monsterDamage} 대미지를 입혔습니다!");

            }
        }
        else
        {
            Debug.Log("💀 화면에 살아있는 파티원 카드가 없어 몬스터가 공격할 대상을 찾지 못했습니다.");
        }
    }

    // ✨ [리모컨 스위치] 몬스터가 턴 종료 시 살아있는 파티원 카드를 무작위로 때리는 명령장치
    public void Remote_MonsterAttackRandomCard(float damage)
    {
        // 1. 현재 전투 화면에 생성되어 배치된 모든 캐릭터 카드(CharacterCard) 목록을 전수 조사합니다.
        // 기존 FindObjectsOfType 코드는 아예 삭제합니다!
        CharacterCard[] activeCards = liveCards.ToArray(); // 👈 매장부에 기록된 데이터만 정직하게 꺼내 씁니다.



        // 2. 살아 움직이는 파티원 카드가 화면에 존재한다면 무작위 타격을 가합니다.
        if (activeCards.Length > 0)
        {
            // 3. 무작위 타겟 선정 (예: 4명 중 1명 로또 타격)
            int randomTargetIndex = Random.Range(0, activeCards.Length);
            CharacterCard targetCard = activeCards[randomTargetIndex];

            if (targetCard != null)
            {
                // 4. 리모컨 신호 발송! 지목당한 그 카드의 수신기(TakeDamage)를 작동시킵니다.
                targetCard.TakeDamage(damage);
                Debug.Log($"💥 [리모컨 작동] 몬스터가 파티원 [{(targetCard.GetComponent<PartyIcon>() != null ? targetCard.GetComponent<PartyIcon>().myData.characterName : "이름 없음")}] 카드를 저격하여 {damage} 대미지를 입혔습니다!");

            }
        }
        else
        {
            Debug.Log("💀 [경고] 화면에 살아있는 파티원 카드가 없어 리모컨이 타겟을 찾지 못했습니다.");
        }
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
    public void UpdateTurnTextUI()
    {
        if (turnTextUI != null)
        {
            turnTextUI.text = $"{currentTurn} 턴";
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
    // 🎯 1. Stage_A 버튼을 눌렀을 때 실행될 함수 (1-1 ~ 1-5)
    public void OnClickStartStageA()
    {
        isNormalStageMode = true;
        currentStageType = "Stage_A";
        currentRound = 1;
        maxRound = 5;

        // 화면 전환: 선택창 끄고 일반 배틀창 켜기
        if (panel_StageSelect != null) panel_StageSelect.SetActive(false);
        if (panel_NMPuzzleBattle != null) panel_NMPuzzleBattle.SetActive(true);

        // 🎯 [오늘의 미션]: 배틀창이 켜질 때 화면을 가리고 있는 마을 패널을 강제로 꺼줍니다!
        GameObject village = GameObject.Find("Panel_Village");
        if (village != null) village.SetActive(false);
        // 첫 라운드 몬스터 생성 시작!
        SpawnStageRoundMonsters();
    }
    // 🎯 [오늘의 신규 코드]: 마을에서 모험 시작 버튼을 누르면 코드가 알아서 화면을 켜줍니다!
    public void OnClickStartAdventure()
    {
        // Panel_StageSelect 오브젝트가 비어있지 않다면
        if (panel_StageSelect != null)
        {
            // 인스펙터의 맨 위 네모 체크박스를 켜서 화면과 자식들을 통째로 활성화합니다!
            panel_StageSelect.SetActive(true);

            Debug.Log("🧙‍♂️ 코드로 Panel_StageSelect와 자식 오브젝트들을 모두 켰습니다!");
        }
    }



    // 🎯 [오늘의 미션]: 통째로 누락되었던 Stage_B 버튼용 함수를 새로 추가해 줍니다!
    public void OnClickStartStageB()
    {
        isNormalStageMode = true;
        currentStageType = "Stage_B";
        currentRound = 1;
        maxRound = 7;

        // 화면 전환: 선택창 끄고 일반 배틀창 켜기
        if (panel_StageSelect != null) panel_StageSelect.SetActive(false);
        if (panel_NMPuzzleBattle != null) panel_NMPuzzleBattle.SetActive(true);
        // 🎯 [오늘의 미션]: 배틀창이 켜질 때 화면을 가리고 있는 마을 패널을 강제로 꺼줍니다!
        GameObject village = GameObject.Find("Panel_Village");
        if (village != null) village.SetActive(false);
        // 첫 라운드 몬스터 생성 시작!
        SpawnStageRoundMonsters();
    }

    // 🎯 3. 라운드별 몬스터 배치 및 기획 규칙 계산 함수

    //일반 스테이지 모드
    // 🎯 파일 맨 아래에 있는 진짜 SpawnStageRoundMonsters 함수 수정 구역
    // 🎯 [수정 위치]: 파일 맨 아래 SpawnStageRoundMonsters 함수를 이 코드로 싹 교체해 줍니다!
    private void SpawnStageRoundMonsters()
    {
        Debug.Log($"⚔ 일반 스테이지 {currentStageType} : {currentRound} 라운드 시작!");

        // 라운드 전광판 UI가 연결되어 있다면 글자 갱신 (예: 1-1, 1-2)
        if (textStageRoundUI != null)
        {
            textStageRoundUI.text = $"1-{currentRound}";
        }

        // 안전 우회용 함수를 먼저 실행하여 보드판 공간을 생성합니다.
        StartPuzzleBattle("normal");

        // 👹 1. 마지막 라운드: 보스 몬스터 1마리 등장 규칙!
        if (currentRound == maxRound)
        {
            Debug.Log("👹 [경고] 마지막 라운드입니다! 보스 몬스터가 출현합니다!");

            // 보스 프리팹이 등록되어 있다면 그것을 쓰고, 없으면 일반 적 프리팹을 씁니다.
            GameObject prefabToSpawn = (bossEnemyPrefab != null) ? bossEnemyPrefab : normalEnemyPrefab;

            if (prefabToSpawn != null)
            {
                StartPuzzleBattle("infinite"); // 기존 만능 소환 함수로 보스 배치!
            }
        }
        // 👾 2. 일반 라운드: n라운드 숫자에 맞춰 n마리 등장 및 자동 정렬 규칙!
        else
        {
            int monsterCount = currentRound;
            Debug.Log($"👾 일반 몬스터가 {monsterCount}마리 등장합니다. (n:1 비율)");

            if (normalEnemyPrefab != null)
            {
                // 현재 라운드 숫자(monsterCount)만큼 반복해서 소환합니다 (1라운드엔 1마리, 2라운드엔 2마리...)
                for (int i = 0; i < monsterCount; i++)
                {
                    StartPuzzleBattle("infinite"); // 기존 만능 소환 함수로 일반 적 배치!
                }
            }
        }


        // 🎯 3. 소환된 적 카드들을 화면에 예쁘게 정렬해 주는 기존 시스템 가동!
        SetupBattleEntities();

        // 🎯 4. 공간이 다 만들어진 후에 안전하게 보드판 내부 오브젝트들을 리셋합니다.
        if (Board.Instance != null) Board.Instance.InitializeNewBoard();
            
        }




    // 🎯 4. 몬스터를 모두 처치했을 때 검사하는 함수
    public void OnClearCurrentRound()
    {
        // 마지막 라운드(보스)를 깼다면 최종 결과창 출력!
        if (currentRound == maxRound)
        {
            Debug.Log("🏆 보스를 처치했습니다! 스테이지 클리어!");
            if (panel_StageClearResult != null) panel_StageClearResult.SetActive(true);
            return;
        }

        // 3라운드마다 보상 팝업창 띄우기 규칙 (3, 6...)
        if (currentRound % 3 == 0)
        {
            Debug.Log($"🎁 {currentRound}라운드 돌파 보상 팝업 가동!");
            if (panel_StageRewardPopup != null) panel_StageRewardPopup.SetActive(true);
        }
        else
        {
            // 보상 라운드가 아니라면 자동으로 다음 라운드 진행
            ProceedToNextRound();
        }
    }

    // 🎯 5. [계속진행] 버튼 및 다음 라운드 이동 전담 함수
    public void OnClickContinueStage()
    {
        // 보상 팝업창을 끄고 다음 라운드로!
        if (panel_StageRewardPopup != null) panel_StageRewardPopup.SetActive(false);
        ProceedToNextRound();
    }

    private void ProceedToNextRound()
    {
        currentRound++;
        SpawnStageRoundMonsters();
    }

    // 🎯 6. 결과창의 [마을로이동] 버튼용 함수
    public void OnClickGoToVillage()
    {
        isNormalStageMode = false;

        // 🎯 [오늘의 미션]: 마을로 돌아갈 때는 라운드 표시 글자를 깔끔하게 지워줍니다.
        if (textStageRoundUI != null)
        {
            textStageRoundUI.text = "";
        }

        // 최종 결과창 끄고, 일반 배틀창도 끄고, 마을 화면 켜기
        if (panel_StageClearResult != null) panel_StageClearResult.SetActive(false);
        if (panel_NMPuzzleBattle != null) panel_NMPuzzleBattle.SetActive(false);

        // 유니티 계층구조(Hierarchy)에 있는 Panel_Village 오브젝트를 찾아서 켜줍니다.
        GameObject village = GameObject.Find("Panel_Village");
        if (village != null) village.SetActive(true);
    }
}








     
    
