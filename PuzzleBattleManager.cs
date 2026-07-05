using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // 🌟 슬라이더 및 UI 컴포넌트 제어용 필수 도구상자




    [Header("--- 무한모드 최종 정산 시스템 ---")]
    public GameObject panel_InfiniteReward;  // 💡 형님이 만드신 결과창 패널(InfiniteRewardPanel 등)을 통째로 연결할 방
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


    // 💡 [StartPuzzleBattle 함수 전체를 아래 내용으로 덮어씌워 주세요]
    public void StartPuzzleBattle(string gameMode)
    {
        currentTurn = 0;
        UpdateTurnTextUI();

        // 1. [다이렉트 화면 전환]: 무한모드 패널은 무조건 켜고, 일반 패널은 무조건 끕니다.
        if (panel_PuzzleBattle != null) panel_PuzzleBattle.SetActive(false);
        if (panel_InfiniteBattle != null) panel_InfiniteBattle.SetActive(true);

        // 2. [불필요한 UI 및 결과창 빛의 속도로 청소]
        GameObject realPartyList = GameObject.Find("Canvas")?.transform.Find("PartyListContainer")?.gameObject;
        if (realPartyList != null) realPartyList.SetActive(false);

        if (panel_InfiniteBattle != null)
        {
            Transform gameover = panel_InfiniteBattle.transform.Find("GAMEOVER TXT");
            if (gameover != null) gameover.gameObject.SetActive(false); // 결과창은 칼같이 끔
        }

        // 🔒 [형님의 대원칙 반영 완공 2단계]: 이제 진짜 전투 시작이니 보드 스크립트 전원을 켭니다!
        if (puzzleBoardComponent != null)
        {
            puzzleBoardComponent.enabled = true; // 1. 오직 전투 가동 시점에만 스크립트 전원 ON!
            puzzleBoardComponent.InitializeNewBoard();   // 2. 군더더기 없이 보드 스크립트 내부 정석대로 즉시 보드 생성!
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

     // 🌟 PuzzleBattleManager.cs 내부 OnClickBackToVillageFromInfinite() 함수 끝자락 교체 구역
     if (GameManager.Instance != null)
     {
        // 1. 기존의 무한모드 화면 정돈 명령 가동
        GameManager.Instance.OnClickInfiniteStageBackButton();
        
        // 2. 🔓 [형님이 검거하신 진짜 정답 치트키 작동!]
        // 창고에 잠들어 있던 빠른 이동 버튼 부활 사령탑 함수를 원격으로 강력하게 깨웁니다!
        GameManager.Instance.ExitBattleStage();
        
        Debug.Log("🎪 [대완공] ExitBattleStage 함수 원격 가동! 빠른 이동 버튼이 완벽하게 ON 복구되었습니다.");
       }
      }
    



     // 💡 [여기서부터 복사해서 맨 밑 괄호 직전에 그대로 붙여넣으세요]

     // 1. 3분 무한 모드가 끝났을 때 1위~10위까지 보이지 않는 장부를 계산해 저장하는 정산기
     // 🛠️ Board.cs에서 모든 연쇄가 끝났을 때 원격 호출하는 최종 정산 사령탑 단락
    public void OnTimerEnd()
     {
      timerIsRunning = false;

     // 🛠 최종 대미지 및 턴수 연동 장부 개설
     int finalScore = 0;
     int finalTurns = currentTurn;

     // 💡 하이어라키 세상을 뒤져 ScoreText(상단 텍스트판)의 컴포넌트를 조준 사격합니다.
     TMPro.TextMeshProUGUI realScoreTMP = transform.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
     if (realScoreTMP == null) realScoreTMP = GameObject.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();

     if (realScoreTMP != null)
     {
        // 🎯 [정규식 특수 안전망]: "대미지", "데미지", 공백(" "), 콜론(":") 등 글자는 몽땅 소멸시키고
        // 오직 순수한 숫자 알맹이(예: 800, 1040)만 정확하게 추출하여 finalScore에 주입합니다!
        string cleanNumbers = System.Text.RegularExpressions.Regex.Replace(realScoreTMP.text, @"[^\d]", "");
        int.TryParse(cleanNumbers, out finalScore);
     }
     else
     {
        finalScore = currentScore;
     }

     // 🖥️ [인스펙터 연동 1단계]: 최종 대미지 결과창 텍스트 박스에 3자리 콤마(,N0)를 찍어 출력합니다.
     if (textFinalScore != null)
     {
        textFinalScore.text = $"최종 대미지 : {finalScore:N0}";
     }

     // 🖥️ [인스펙터 연동 2단계]: 묶여있던 최종 걸린 턴수 데이터를 글자로 주입하고 컴포넌트를 강제 ON 합니다!
     if (textFinalTurns != null)
     {
        textFinalTurns.text = $"걸린 턴수 : {finalTurns} 턴";
        textFinalTurns.gameObject.SetActive(true);
     }

    }
      // (※ 이 바로 아래에 배치되어 있는 int[] highScores = new int[10]; 로직부터 명예의 전당 Top 10 밀어내기 및 GAMEOVER TXT 활성화 코드는 절대로 지우지 말고 그대로 매끄럽게 이어붙이시면 성공입니다!)





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
        // 💾 [624번 라인 PlayerPrefs.Save(); 바로 아랫줄부터 드래그해서 교체하세요!]
        PlayerPrefs.Save();
    } // 🔒 1. highScores 기록을 밀어내던 if (currentRank >= 1 ...) 문을 완전히 닫아줍니다.

    // 🌟 [무한모드 패널 제어 및 교차 편집]
    if (panel_InfiniteBattle != null)
    {
        // [A] 결과창 텍스트 박스(GAMEOVER TXT)를 확실하게 ON 합니다.
        Transform gameover = panel_InfiniteBattle.transform.Find("GAMEOVER TXT");
        if (gameover != null)
        {
            gameover.gameObject.SetActive(true); 
        }
    }

    // [B] 게임오버 순간이므로 스타트 트리거 버튼은 완벽하게 끕니다.
    if (btn_StartTouchTrigger_Direct != null)
    {
        btn_StartTouchTrigger_Direct.SetActive(false);
        Debug.Log("🏁 [최적화 완공] GAMEOVER TXT는 ON, 트리거 버튼은 OFF 교차 편집 완료!");
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
