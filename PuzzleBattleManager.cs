using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // 🌟 슬라이더 및 UI 컴포넌트 제어용 필수 도구상자

// 🌟 [개발자님 기획 최종 구현]: 3매치 퍼즐 전장의 모든 아군/적군 실시간 데이터를 총괄 지휘하는 전용 사령관
public class PuzzleBattleManager : MonoBehaviour
{
    // ====== 1. [여기 추가] 다른 곳에서 호출할 수 있게 통로를 만듭니다 ======
    public static PuzzleBattleManager Instance { get; private set; }

    private void Awake()
    {
        // 내 자신을 Instance에 등록합니다.
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("--- 무한모드 최종 정산 시스템 ---")]
    public GameObject panel_InfiniteReward;  // 💡 형님이 만드신 결과창 패널(InfiniteRewardPanel 등)을 통째로 연결할 방
    public TextMeshProUGUI textFinalScore;   // Text_FinalScore 연결
    public TextMeshProUGUI textFinalTurns;   // Text_FinalTurns 연결
    public TextMeshProUGUI textRecordNotice; // Text_RecordNotice 연결
    public bool isTimeOver = false; // 💡 시간이 다 끝났음을 임시로 저장하는 안전핀

    // 🎯 [왕초보 구원] 재생 전에 꺼져 있어도 직속으로 조종할 수 있게 해주는 리모컨 방입니다!
    [Header("--- 재생전 OFF여도 강제 제어할 직속 회선 ---")]
    public GameObject btn_StartTouchTrigger_Direct;


    [Header("--- 배틀 핵심 UI 패널 록온 ---")]
    public GameObject panel_PuzzleBattle;    // 일반 스테이지 패널 (Panel_NMPuzzleBattle)
    public GameObject panel_InfiniteBattle;  // 💡 [추가] 무한모드 패널 (Panel_INPuzzleBattle)
    public GameObject enemyContainer;

    [Header("실시간 배틀 카드 장부")]
    public List<CharacterCard> liveCards = new List<CharacterCard>(); // 👈 여기에 실시간으로 담깁니다.

    [Header("--- 3매치 퍼즐 보드 직속 회선 연결 ---")]
    public Board puzzleBoardComponent;     // 보드.cs 스크립트 연결 방

    [Header("--- 턴 시스템 시스템 ---")]
    public int currentTurn = 0;        // 현재 누적된 턴 수
    public bool isUserAction = false; // 유저가 직접 드래그한 상태인지 체크하는 스위치

    [Header("콤보 시스템")]
    public int currentCombo = 0; // 현재 연속 콤보 수
    public float comboDamageMultiplier = 0.1f; // 1콤보당 추가될 데미지 배율 (0.1 = 10%)

    [Header("콤보 UI 설정")]
    // 유저님이 TextMeshPro를 사용 중이시므로 아래와 같이 선언합니다.
    public TMPro.TMP_Text comboText;

    [Header("--- NPC 전용 1~10위 순위판 UI ---")]
    public TextMeshProUGUI textNPCLeaderboard; // 💡 요 방이 상단에 있어야 맨 밑바닥 함수가 에러가 안 납니다!
    public GameObject panel_NPCLeaderboard_Popup; // 🎯 마을 순위판 팝업창 자체를 기억할 전원 제어 방!

     // 🌟 [새로 추가] 던전 안에서 파티원들의 진짜 최대 체력 원본을 기억해 둘 딕셔너리 주머니
    private static Dictionary<int, int> partyMaxHpBackup = new Dictionary<int, int>();    private void Start()
    {
        currentTurn = 0;
        UpdateTurnTextUI();
            if (GameManager.Instance != null && GameManager.Instance.partyMembers != null)
    {
        foreach (var character in GameManager.Instance.partyMembers)
        {
            if (character == null) continue;

            // 만약 이 캐릭터의 최대 체력이 아직 기록된 적이 없다면 (즉, 던전에 완전히 처음 입장한 상태라면)
            if (!partyMaxHpBackup.ContainsKey(character.id))
            {
            // 1. 인스펙터에 적혀있던 원래 체력을 최대 체력 원본으로 저장
            partyMaxHpBackup[character.id] = character.hp;

            // 🚀 [여기에 한 줄 추가]: 던전에 처음 들어왔을 때만 체력을 원본 수치로 꽉 채워줍니다(풀피 세팅)!
            character.hp = partyMaxHpBackup[character.id];

            Debug.Log($"[최초 입장 확인] {character.characterName}의 최대 HP {character.hp}를 안전하게 기록하고 풀피로 시작합니다.");
            }
            else
            {
                // 🌟 이미 주머니에 원본이 기록되어 있다면 = 연속 배틀 중인 상태입니다!
                // 이때는 캐릭터의 피를 만지지 않고, 닳아있는 현재 HP 상태를 그대로 존중하여 유지합니다.
                Debug.Log($"[연속 전투 확인] {character.characterName}의 HP 상태를 리셋하지 않고 그대로 유지합니다. (현재 HP: {character.hp})");
            }
        }
    }

        // 🎯 1. [요청 사항] 재생 전에 꺼져(OFF) 있더라도 게임이 시작되면 무조건 가장 먼저 ON!
        if (btn_StartTouchTrigger_Direct != null)
        {
            btn_StartTouchTrigger_Direct.SetActive(true); // 👈 직속 회선으로 강제 ON!
            Debug.Log("🚀 [성공] 재생 전 OFF 상태였던 Btn_StartTouchTrigger를 Start에서 강제 ON 시켰습니다!");
        }

        // 🔒 2. [기존 안전장치] 게임 재생 버튼을 누르는 순간 GAMEOVER TXT는 무조건 강제로 OFF!
        if (panel_InfiniteBattle != null)
        {
            Transform gameover = panel_InfiniteBattle.transform.Find("GAMEOVER TXT");
            if (gameover != null)
            {
                gameover.gameObject.SetActive(false); // 👈 시작하자마자 OFF!
                Debug.Log("🔒 [보안 성공] 게임 시작 시 GAMEOVER TXT를 선제적으로 OFF 제어했습니다.");
            }
        }
    }

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
    [Header("--- 현재 배틀 필드 상황 ---")]
    // 중요! 어떤 모드의 몬스터든 이 주머니에 다 담을 수 있습니다.
    public BaseMonster currentTargetMonster;

    [Header("--- 아군 및 적군 HP 실시간 감시 주머니 ---")]
    public Slider enemyHPBar;              // 몬스터 체력바 슬라이더
    public List<Slider> heroHPBars = new List<Slider>(); // 아군 영웅 5인 체력바 슬라이더 리스트
    public TextMeshProUGUI turnTextUI;

    public int currentScore = 0;       // 🎯 무한모드 최종 대미지 스코어를 기억할 진짜 장부방 개설!

    [Header("--- 0.001초 초정밀 타이머 시스템 ---")] //타이머관련 코드
    public TextMeshProUGUI timeText;     // 유니티에서 Text_Timer를 연결할 리모컨 방
    public GameObject startTouchTriggerPanel;
    private float timeRemaining = 3f;  // 180초 (3분) 출발점 //3초라도안보이면
    private bool timerIsRunning = false; // 시계 ON/OFF 스위치
    // 🌟 [전투 정식 개시 스위치]: 무한 모드 버튼을 누르는 순간 GameManager에 의해 원격 가동됩니다!

    // 💡 [StartPuzzleBattle 함수 전체를 아래 내용으로 덮어씌워 주세요]
    public void StartPuzzleBattle(string gameMode)
    {
        currentTurn = 0;
        UpdateTurnTextUI();

        // 먼저 두 패널을 모두 깔끔하게 꺼줍니다.
        if (panel_PuzzleBattle != null) panel_PuzzleBattle.SetActive(false);
        if (panel_InfiniteBattle != null) panel_InfiniteBattle.SetActive(false);
        GameObject realPartyList = GameObject.Find("Canvas")?.transform.Find("PartyListContainer")?.gameObject;
        if (realPartyList != null)
        {
            realPartyList.SetActive(false);
            Debug.Log("메인 Canvas에 있던 PartyListContainer를 완벽하게 전원 OFF 시켰습니다!");
        }
        if (panel_InfiniteBattle != null)
        {
            Transform triggerBtn = panel_InfiniteBattle.transform.Find("Btn_StartTouchTrigger");
            if (triggerBtn != null)
            {
                // 🎯 자식 글자 상자들까지 몽땅 대동해서 인스펙터 맨 위 체크박스를 강제로 [V] 상태로 ON 시켜버립니다!
                triggerBtn.gameObject.SetActive(true);
                Debug.Log("🚀 [형님 명령] Btn_StartTouchTrigger와 자식 오브젝트들을 화면 정중앙에 강제 ON 완공!");
            }
        }
        // 1. 모드 선택 판정
        // 💡 111번째 줄 무한 모드 판정 구역입니다!

        if (gameMode == "infinite" || gameMode == "Infinite")
        {
            // 1. ⏱ 180초 타이머 장부 꽉 채우기
            timeRemaining = 3f;
            timerIsRunning = false;

            // 2. 📂 큰 방 패널인 panel_InfiniteBattle을 무조건 가장 먼저 켭니다!
            if (panel_InfiniteBattle != null)
            {
                panel_InfiniteBattle.SetActive(true);
            }

            // 3. 🧼 메인 Canvas에 이사 가 있던 파티창 대장을 찾아 다이렉트로 꺼버립니다.
            GameObject realPartyList2 = GameObject.Find("Canvas")?.transform.Find("PartyListContainer")?.gameObject;
            if (realPartyList2 != null) realPartyList2.SetActive(false);

            // 4. 🚀 [형님 요청 완벽 반영] 재생 전에 에디터에서 체크박스가 꺼져(OFF) 있어도 코드로 무조건 가장 먼저 강제 ON!
            if (btn_StartTouchTrigger_Direct != null)
            {
                btn_StartTouchTrigger_Direct.SetActive(true); // 👈 이름으로 찾지 않고 직속 회선으로 즉시 켜버립니다!
                Debug.Log("🚀 [코드로 완벽 제어] 재생 전 OFF 상태였던 Btn_StartTouchTrigger 강제 ON 완공!");
            }
            else
            {
                Debug.LogWarning("⚠️ 유니티 인스펙터 창에서 btn_StartTouchTrigger_Direct 방에 오브젝트를 연결하지 않았습니다!");
            }

            // 5. 🛑 게임오버 결과창은 시작할 때 무조건 꺼져있어야 하므로 가려줍니다.
            if (panel_InfiniteBattle != null)
            {
                Transform gameover = panel_InfiniteBattle.transform.Find("GAMEOVER TXT");
                if (gameover != null) gameover.gameObject.SetActive(false);
            }

            Debug.Log("🏁 무한모드 전장 전개! 시작 트리거 팝업 자동 가동 완료!");
        }



        else
        {

            // 💡 일반 스테이지 패널을 켭니다!
            if (panel_PuzzleBattle != null) panel_PuzzleBattle.SetActive(true);
            Debug.Log("[일반 배틀 화면 전개 ON]");
        }

        // 2. 아군 소환 및 6x6 보드 엔진 가동 (공통 실행)
        SetupBattleEntities();
        if (puzzleBoardComponent != null)
        {
            puzzleBoardComponent.SetupStage(6, 6);
            puzzleBoardComponent.CreateBoard();
        }
    }
    public void UpdateTurnTextUI()
    {
        if (turnTextUI != null)
        {
            // 화면 텍스트 창에 현재 누적된 턴 숫자를 실시간으로 출력합니다.
            turnTextUI.text = $"{currentTurn}턴";
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

    if (activeBattlePanel != null)
    {
        // 🌟 이제 현재 켜져 있는 전장 패널 밑에서 'PartyContainer_Battle' 상자를 정확하게 조준 타격합니다!
        Transform battlePartyListTrans = activeBattlePanel.transform.Find("PartyContainer_Battle");
        if (battlePartyListTrans == null)
        {
            Debug.LogWarning($"[구조 점검] {activeBattlePanel.name} 아래에서 'PartyContainer_Battle' 상자를 찾지 못했습니다.");
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

    // 💡 [PuzzleBattleManager.cs 맨 밑바닥 괄호 직전에 그대로 붙여넣으세요]

    // 유니티가 매 프레임(초당 60~144번)마다 호출하여 0.001초 단위로 시간을 깎는 엔진입니다.
    // 📄 PuzzleBattleManager.cs 내부의 374번 줄 부근 Update 단락 교체

    private void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                // 🛑 3초가 끝났지만 즉시 결과창을 켜지 않고, 보드판이 끝날 때까지 대기시킵니다!
                timeRemaining = 0;
                timerIsRunning = false;
                DisplayTime(timeRemaining);

                isTimeOver = true; // "장부에 시간 종료라고 체크만 해둔다!"
                Debug.Log("⏳ [타임오버 원격 대기] 진행 중인 블록 연쇄 정산이 끝날 때까지 대기합니다...");
            }
        }
    }



    // 형님이 줏어오신 알고리즘을 0.001초(소수점 3자리) 폭풍 카운트다운으로 개조한 핵심 뷰어입니다.
    private void DisplayTime(float timeToDisplay)
    {
        if (timeToDisplay < 0) timeToDisplay = 0;

        // 분과 초를 정수로 쪼갭니다.
        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);

        // 🔥 [소수점 3자리 추출 공식]: 전체 초에서 정수 초를 빼면 순수 소수점 잔량만 남습니다. (예: 0.543초)
        // 여기에 1000을 곱해주면 0부터 999까지 초고속으로 달리는 밀리초(ms)가 완성됩니다!
        int milliseconds = Mathf.FloorToInt((timeToDisplay - Mathf.FloorToInt(timeToDisplay)) * 1000);

        if (timeText != null)
        {
            // {0:00}:{1:00}.{2:000} -> 분(2자리):초(2자리).밀리초(3자리) 형식으로 화면에 강제 출력!
            timeText.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }
    }
    // 유저가 "화면을 누르면 무한모드를 시작합니다"를 터치했을 때 실행될 최종 시동 함수입니다!
    public void OnClickRealStartInfiniteTimer()
    {
        // 1. 안내 팝업창을 화면에서 깔끔하게 꺼서 치워버립니다.
        if (startTouchTriggerPanel != null)
        {
            startTouchTriggerPanel.SetActive(false);
        }

        // 2. 🔥 이제 드디어 초정밀 타이머 시계 스위치를 ON 하고 가동합니다!
        timerIsRunning = true;
        Debug.Log("🏁 [무한 모드 스타트] 0.001초 카운트다운 폭풍 가동!");
    }
    public void ForceStopAndResetTimer() //화면이동시 타이머 초기화 
    {
        // 1. 🛑 타이머의 실시간 작동 스위치를 끕니다.
        timerIsRunning = false;

        // 2. ⏱️ 시간을 무한모드 기본 시간(180초)으로 완전히 초기화(리셋) 합니다.
        timeRemaining = 3f;

        // 3. 🖥️ 화면에 표시되는 타이머 텍스트 UI도 3분(03:00)으로 깔끔하게 새로고침 합니다.
        DisplayTime(timeRemaining);

        Debug.Log("⏱️ [타이머 강제 제어] 배틀 화면 탈출 감지! 타이머를 안전하게 멈추고 180초로 초기화했습니다.");
    }
    public void OnClickBackToVillageFromInfinite()
    {
        // [기존 필수 1] 켜져 있던 무한모드 결과창 패널(GAMEOVER TXT)을 시원하게 꺼버립니다.
        if (panel_InfiniteReward != null)
        {
            panel_InfiniteReward.SetActive(false);
        }

        // [기존 필수 2] 플레이가 끝난 무한모드 퍼즐판 패널(Panel_INPuzzleBattle)도 꺼줍니다.
        if (panel_InfiniteBattle != null)
        {
            panel_InfiniteBattle.SetActive(false);
        }

        // 🛠️ [변수명 일치 교정]: puzzleBoard -> puzzleBoardComponent
        if (puzzleBoardComponent != null)
        {
            puzzleBoardComponent.ForceStopAndClearBoard();
        }

        // [기존 필수 4] GameManager 싱글톤을 깨워서 마을 화면 패널을 다시 켜라고 명령합니다!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClickInfiniteStageBackButton();
            Debug.Log("무한모드 정산 완료! 결과창과 순위판을 모두 안전하게 초기화하고 마을로 복귀했습니다.");
        }
    }

    // 💡 [여기서부터 복사해서 맨 밑 괄호 직전에 그대로 붙여넣으세요]

    // 1. 3분 무한 모드가 끝났을 때 1위~10위까지 보이지 않는 장부를 계산해 저장하는 정산기
    // 🛠️ Board.cs에서 모든 연쇄가 끝났을 때 원격 호출하는 최종 정산 사령탑 단락
    public void OnTimerEnd()
    {
        timerIsRunning = false;

        // 📊 [최종 데이터 연동]: 중복 선언을 원천 박멸하고 정석 변수 방을 복원합니다!
        int finalScore = 0;
        int finalTurns = currentTurn; // 👈 현재 진행된 턴 수를 안전하게 복사하여 보관합니다.

        // 📍 [화면 상단 실시간 누적 데미지 문자열 원격 우회 연동]
        // 하이오라키 상자의 (ScoreText) 컴포넌트를 직접 조준하여 문자열을 낚아챕니다!
        TMPro.TextMeshProUGUI realScoreTMP = transform.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (realScoreTMP == null) realScoreTMP = GameObject.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();

        if (realScoreTMP != null)
        {
            // ✂️ 화면 상단판에 박혀있던 글자 "누적 데미지: 1,350" 문자열에서 오타 처리 후 오직 숫자 알맹이만 정제합니다.
            string scoreString = realScoreTMP.text.Replace("누적 대미지:", "").Replace("누적 대미지 :", "").Replace("누적 데미지:", "").Replace("누적 데미지 :", "").Replace(",", "").Trim();
            int.TryParse(scoreString, out finalScore);
        }
        else
        {
            // 상단 텍스트를 찾지 못했을 경우 예외 방지용 방어코드
            finalScore = currentScore;
        }

        // ✍️ [결과창 텍스트 출력]: 찌꺼기 없는 청정 화면에 최종 대미지를 예쁘게 출력합니다!
        if (textFinalScore != null)
        {
            textFinalScore.text = $"최종 대미지 : {finalScore:N0}";
        }

        // ⏱️ [턴 수 출력 연동]: 대미지 밑에 걸린 총 턴 수도 함께 갱신하여 유저에게 보여줍니다!
        // 🌟 만약 결과창에 턴 수를 표시할 전용 텍스트 변수(예: textFinalTurns 등)를 선언해두셨다면 
        // 아래 주석을 풀고 변수명만 맞춰서 연결해주시면 유니티 인스펙터 세팅 후 완벽히 작동합니다.
        /*
        if (textFinalTurns != null)
        {
            textFinalTurns.text = $"{finalTurns}턴 걸림";
        }
        */

        Debug.Log($"📊 [최종 정산 완료] 반영 대미지: {finalScore} | 소모한 총 턴 수: {finalTurns}턴");
    }




        // // 내부 저장소에서 1등부터 10등까지의 점수를 배열로 싹 긁어옵니다. (기존 랭킹 기능 100% 보존)
        int[] highScores = new int[10];
        for (int i = 0; i < 10; i++)
        {
            highScores[i] = PlayerPrefs.GetInt($"INF_RANK_{i + 1}", 0);
        }

        // // 현재 대미지 점수가 명예의 전당 몇 등인지 순위 검사 (기존 랭킹 기능 100% 보존)
        int currentRank = 0;
        for (int i = 0; i < 10; i++)
        {
            if (finalScore > highScores[i])
            {
                currentRank = i + 1;
                break;
            }
        }

        // -----------------------------------------------------------------
        // 🛠️ [기록 갱신 안내 가동]: 1~3순위 명예의 전당 진입 시 축하 문구 연출
        // -----------------------------------------------------------------
        if (currentRank >= 1 && currentRank <= 3)
        {
            if (textRecordNotice != null)
            {
                textRecordNotice.gameObject.SetActive(true);
                textRecordNotice.text = $"기록갱신! [{currentRank}위] 달성!";
            }
        }
        else
        {
            if (textRecordNotice != null) textRecordNotice.gameObject.SetActive(false);
        }




        // 💾 [탑 10 데이터 밀어내기 정산] 내 아래 등수들의 기록을 한 칸씩 밑으로 밀어냅니다.
        if (currentRank >= 1 && currentRank <= 10)
        {
            for (int i = 9; i >= currentRank; i--)
            {
                highScores[i] = highScores[i - 1];
            }
            highScores[currentRank - 1] = finalScore;

            for (int i = 0; i < 10; i++)
            {
                PlayerPrefs.SetInt($"INF_RANK_{i + 1}", highScores[i]);
            }
            PlayerPrefs.Save();
        }
        if (panel_InfiniteBattle != null)
        {
            Transform gameover = panel_InfiniteBattle.transform.Find("GAMEOVER TXT");
            if (gameover != null)
            {
                gameover.gameObject.SetActive(true);
                Debug.Log("🎉 [코드로 완벽 제어] 3분 종료! GAMEOVER TXT 결과창 강제 ON 대완공!");
            }
        }
    }

    // 2. 마을에서 NPC 순위보기 버튼을 누르면 1위부터 10위까지의 보이지 않는 장부를 긁어와 화면에 쾅 꽂아주는 함수
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
    // ✨ [추가] 콤보 글씨를 실시간으로 새로고침하고 1초 뒤 사라지게 만드는 함수
    // ✨ [속도감 업그레이드] 커지자마자 딜레이 없이 빠르게 스르륵 사라지는 고속 콤보 연출
    private Coroutine comboFadeCoroutine;

    public void UpdateComboTextUI()
    {
        if (comboText == null) return;

        if (currentCombo > 0)
        {
            comboText.text = "Combo\n" + currentCombo;

            Color textColor = comboText.color;
            textColor.a = 1f;
            comboText.color = textColor;

            if (comboFadeCoroutine != null)
            {
                StopCoroutine(comboFadeCoroutine);
            }
            comboFadeCoroutine = StartCoroutine(AnimateFastComboTextRoutine());
        }
        else
        {
            comboText.text = "";
        }
    }

    // 💥 쿵! 커진 직후 대기 없이 빠르게 녹아내리는 액션 연출 루틴
    private System.Collections.IEnumerator AnimateFastComboTextRoutine()
    {
        RectTransform rect = comboText.GetComponent<RectTransform>();
        Vector2 startPosition = rect != null ? rect.anchoredPosition : Vector2.zero;

        // --- [STEP 1: 0.12초 동안 엄청 크고 역동적으로 쿵! 튕기기] ---
        if (rect != null)
        {
            float bounceDuration = 0.12f; // 속도감을 위해 0.15초에서 더 단축
            float time = 0f;
            Vector3 targetScale = Vector3.one;
            Vector3 startScale = Vector3.one * 1.8f; // 순간 폭발력을 위해 1.8배까지 대폭 확대!

            while (time < bounceDuration)
            {
                time += UnityEngine.Time.deltaTime;
                rect.localScale = Vector3.Lerp(startScale, targetScale, time / bounceDuration);
                yield return null;
            }
            rect.localScale = targetScale;
        }

        // --- [STEP 2: 대기 시간(1초) 완전 삭제! 즉시 0.35초 동안 빠르게 스르륵 소멸] ---
        float fadeDuration = 0.35f; // 빠르게 사라지도록 0.5초에서 0.35초로 컷!
        float fadeTime = 0f;

        // 🛠️ [치유 코드 1]: 캐릭터 카드 색상을 절대 침범하지 않는 독립된 안전한 컬러 방 개설
        Color safeComboColor = (comboText != null) ? comboText.color : Color.white;

        while (fadeTime < fadeDuration)
        {
            fadeTime += UnityEngine.Time.deltaTime;
            float progress = fadeTime / fadeDuration;

            // 1. 투명도 고속 다운 연산 (원래 부드럽게 사라지는 연출 기능 100% 보존!)
            float alpha = UnityEngine.Mathf.Lerp(1f, 0f, progress);

            // 🛠️ [치유 코드 2]: 다른 프리팹을 오염시키지 않고 오직 콤보 글씨의 투명도만 안전하게 조작!
            if (comboText != null)
            {
                safeComboColor.a = alpha;
                comboText.color = safeComboColor;
            }

            // 2. 위로 가볍게 살짝 슝 솟구치며 사라지는 에어본 효과 추가 (원래 애니메이션 기능 100% 보존!)
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(startPosition.x, startPosition.y + (progress * 25f));
            }
            yield return null;
        }


        // --- [STEP 3: 완전히 끝나면 깔끔하게 청소 및 위치 리셋] ---
        comboText.text = "";
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = startPosition;
        }
    }
    // ✨ [추가] 몬스터가 턴 종료 시 살아있는 우리 캐릭터 카드를 무작위로 때리는 핵심 공격 회로
    public void MonsterAttackRandomPartyCard(float monsterDamage)
    {
        CharacterCard[] activeCards = liveCards.ToArray(); 
        
        if (activeCards.Length > 0)
        {
            int randomTargetIndex = Random.Range(0, activeCards.Length);
            CharacterCard targetCard = activeCards[randomTargetIndex];
            if (targetCard != null)
            {
                targetCard.TakeDamage(monsterDamage);
                Debug.Log($"💥 [몬스터 반격] {monsterDamage} 대미지를 입혔습니다!");
            }
        }
    }

    public void OnTimerEnd()
    {
        timerIsRunning = false;

        int finalScore = 0;
        int finalTurns = currentTurn; 

        // 📍 [화면 상단 실시간 누적 데미지 문자열 원격 우회 연동]
        TMPro.TextMeshProUGUI realScoreTMP = transform.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (realScoreTMP == null) realScoreTMP = GameObject.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();

        if (realScoreTMP != null)
        {
            string scoreString = realScoreTMP.text.Replace("누적 대미지:", "").Replace("누적 대미지 :", "").Replace("누적 데미지:", "").Replace("누적 데미지 :", "").Replace(",", "").Trim();
            int.TryParse(scoreString, out finalScore);
        }
        else
        {
            finalScore = currentScore;
        }

        // ✍️ [결과창 텍스트 출력]
        if (textFinalScore != null)
        {
            textFinalScore.text = $"최종 대미지 : {finalScore:N0}";
        }

        Debug.Log($"📊 [최종 정산 완료] 반영 대미지: {finalScore} | 소모한 총 턴 수: {finalTurns}턴");

        // 🏆 [무한 모드 랭킹 데이터 정산 연쇄 트리거 복원]
        // 559번 라인에서 에러가 터졌던 랭킹 갱신 로직을 중괄호 내부에 안전하게 격리 수용합니다.
        for (int i = 0; i < 5; i++)
        {
            int savedScore = PlayerPrefs.GetInt($"INF_RANK_{i + 1}", 0);
            if (finalScore > savedScore)
            {
                for (int j = 4; j > i; j--)
                {
                    PlayerPrefs.SetInt($"INF_RANK_{j + 1}", PlayerPrefs.GetInt($"INF_RANK_{j}", 0));
                }
                PlayerPrefs.SetInt($"INF_RANK_{i + 1}", finalScore);
                PlayerPrefs.Save();
                break;
            }
        }
    } // 👈 OnTimerEnd 함수가 완벽하게 마감되는 안전 괄호



