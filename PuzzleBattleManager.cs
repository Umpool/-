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
    [Header("--- 배틀 핵심 UI 패널 록온 ---")]
    public GameObject panel_PuzzleBattle; // 진짜 최종 3매치 퍼즐 배틀판 패널
    public GameObject enemyContainer;     // 몬스터들이 배치될 바구니

    [Header("--- 3매치 퍼즐 보드 직속 회선 연결 ---")]
    public Board puzzleBoardComponent;     // 보드.cs 스크립트 연결 방

    [Header("--- 턴 시스템 시스템 ---")]
    public int currentTurn = 0;        // 현재 누적된 턴 수
    public bool isUserAction = false; // 유저가 직접 드래그한 상태인지 체크하는 스위치

    private void Start()
    {
        currentTurn = 0;
        UpdateTurnTextUI();
    }

    public void OnUserDragBlock()
    {
        // 인풋매니저가 블록 드래그를 끝냈을 때 실행됩니다.
        Debug.Log("인풋매니저로부터 드래그 종료 신호 수신 완료!");
    }
    [Header("--- 현재 배틀 필드 상황 ---")]
    // 중요! 어떤 모드의 몬스터든 이 주머니에 다 담을 수 있습니다.
    public BaseMonster currentTargetMonster;

    [Header("--- 아군 및 적군 HP 실시간 감시 주머니 ---")]
    public Slider enemyHPBar;              // 몬스터 체력바 슬라이더
    public List<Slider> heroHPBars = new List<Slider>(); // 아군 영웅 5인 체력바 슬라이더 리스트
    public TextMeshProUGUI turnTextUI;

    // 🌟 [전투 정식 개시 스위치]: 무한 모드 버튼을 누르는 순간 GameManager에 의해 원격 가동됩니다!
    public void StartPuzzleBattle(string gameMode)
    {
        currentTurn = 0;
        UpdateTurnTextUI();

        Debug.Log($"[전투 차원 이동 완료] {gameMode} 모드로 3매치 퍼즐 배틀판 화면을 전개합니다!");

        // 1. 배틀판 도화지 화면 전체를 화사하게 켜줍니다!
        if (panel_PuzzleBattle != null) panel_PuzzleBattle.SetActive(true);

        // 2. 🌟 [데이터 연동 규칙]: 내 정예 파티원 5명 전투 모드 장착 및 적 몬스터 소환 연산 개시
        SetupBattleEntities();

        // 3. 🌟 [보드 엔진 발사]: 보드.cs 에게 6x6 정석 3매치 블록판 소환 명령을 내립니다!
        if (puzzleBoardComponent != null)
        {
            puzzleBoardComponent.SetupStage(6, 6); // 기획안 규격에 맞춰 가로 6 x 세로 6 강제 픽스!
            puzzleBoardComponent.CreateBoard();    // 블록 촤라락 소환 루틴 발사!
            Debug.Log("[보드 엔진 가동 완료] 6x6 정석 3매치 블록판이 화면에 완벽 생성되었습니다!");
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
        if (panel_PuzzleBattle != null)
        {
            // 🎯 [하이어라키 진짜 주소 정밀 타격]: 선명한 화면에서 확인한 진짜 부모 이름 'PartyContainer_Battle' 경로를 칼같이 포착합니다!
            Transform battlePartyListTrans = panel_PuzzleBattle.transform.Find("PartyContainer_Battle");

            if (battlePartyListTrans == null)
            {
                Debug.LogWarning("[구조 점검] Panel_INPuzzleBattle 아래에서 'PartyContainer_Battle' 상자를 찾지 못했습니다.");
                return;
            }

            // 5개의 자식 카드 슬롯 배정을 수집 보관합니다 (Battle_HeroSlot_0 ~ 4)
            List<Transform> heroCardSlots = new List<Transform>();
            foreach (Transform child in battlePartyListTrans)
            {
                // 이름이 슬라이더바가 아닌 진짜 카드 슬롯 본체들만 필터링 수집합니다
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
                Transform hpBarTrans = heroCardSlots[i].transform.Find("HP_Bar");
                if (hpBarTrans != null)
                {
                    Slider hpSlider = hpBarTrans.GetComponent<Slider>();
                    if (hpSlider != null)
                    {
                        float maxHP = currentHeroData.hp;
                        hpSlider.minValue = 0f;
                        hpSlider.maxValue = maxHP;
                        hpSlider.value = maxHP; // 전투 첫 진입이므로 생명력 만땅(100%) 충전 대령!

                        heroHPBars.Add(hpSlider); // 사령관의 실시간 피통 감시 바구니에 장착 완료!
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
