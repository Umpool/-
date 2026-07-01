using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 패널 제어")]
    public GameObject panel_CharacterSelect; // 메인 캐릭터 선택창
    public GameObject panel_SubCharacterSelect; // 서브 캐릭터 선택창
    public GameObject panel_Village; // 마을 화면
    public GameObject panel_PartyEditView; // 🌟 [0-1번 기획]: 새로 배치할 대형 파티 편집 화면 패널

    [Header("팝업 및 컨테이너 제어")]
    public GameObject popup_CharacterInfo; // 독립된 완전 공용 캐릭터 정보 팝업
    public GameObject panel_ConfirmRemove; // 최종 출발 / 제거 공용 팝업 패널
    public GameObject partyListContainer; // 하단 파티 목록 컨테이너 (항상 유지)

    [Header("팝업 내부 버튼 직접 제어")]
    public GameObject btn_Confirm3; // 완전 공용으로 쓸 확인3 버튼 오브젝트
    public GameObject btn_Cancel3; // 완전 공용으로 쓸 취소3 버튼 오브젝트

    [Header("출발하기용 팝업 요소 (출발하시겠습니까?)")]
    public GameObject obj_StartWindow; // 출발하시겠습니까? 문구 그룹
    public GameObject btn_Confirm5; // 확인5 (최종출발)
    public GameObject btn_Cancel5; // 취소5 (출발취소)

    [Header("파티제거용 팝업 요소 (파티에서 제거하시겠습니까?)")]
    public GameObject obj_RemoveWindow; // 파티에서 제거하시겠습니까? 문구 그룹
    public GameObject btn_RemoveYes; // 예 버튼
    public GameObject btn_RemoveNo; // 아니오 버튼

    [Header("팝업 내부 텍스트 및 경고 연동")]
    public TextMeshProUGUI txt_CharacterName;
    public TextMeshProUGUI txt_CharacterInfo;
    public TextMeshProUGUI txt_MainNoticeText; // 조건 미달 시 알림 텍스트 띄울 TMP
    public TextMeshProUGUI txt_SynergyOutput; // 🌟 [0-3번 기획]: 실시간 시너지 문자들을 뿜어낼 도화지 텍스트

    [Header("메인 및 서브 캐릭터 카드 버튼 리스트")]
    public List<CharacterCard> mainCharacterCards;
    public List<CharacterCard> subCharacterCards;
    public List<CharacterCard> warehouseCharacterCards; // 🌟 [0-2번 기획]: 캐릭터 창고 내부에 나열될 프리팹 카드 목록

    [Header("데이터 및 파티 관리")]
    public List<CharacterData> allCharacters; // 전체 캐릭터 풀 (창고 풀 데이터)
    public List<CharacterData> partyMembers; // 현재 파티 멤버 (최대 5명)
    public GameObject partyMemberPrefab; // 파티 아이콘 프리팹

    [Header("상태 설정")]
    public GameState currentGameState;
    public int stageMode;

    private int selectedCharId; // 현재 클릭해서 상세보기 중인 캐릭터 ID
    private CharacterData pendingRemoveCharacter; // 제거 대기 캐릭터 데이터 임시 보관
    private Coroutine noticeFadeCoroutine; // 경고창 페이드아웃 제어용

    [Header("관성 가동 앤 카메라 완벽 감금 시스템")]
    public RectTransform villageRectTransform; // 인스펙터에서 Panel_Village를 연결할 칸
    private bool isVillageDragging = false;
    private Vector2 dragMouseStartPos;
    private Vector2 dragPanelStartPos;

    private Vector2 villageVelocity; // 실시간 이동 속도
    private Vector2 lastPanelPos; // 직전 프레임의 위치
    private float inertiaDecel = 0.008f; // 관성이 줄어드는 속도

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        RefreshAllCharacterMenuCards();
    }

    void Start()
    {
        if (txt_MainNoticeText != null) txt_MainNoticeText.gameObject.SetActive(false);
        if (panel_PartyEditView != null) panel_PartyEditView.SetActive(false);

        // ======================================================================================
        // 🌟 [4단계 상단 네비게이션바 코딩 자동화 강제 조립 시스템]
        // ======================================================================================
        GameObject topMenuBar = GameObject.Find("Canvas")?.transform.Find("상단 단축바")?.gameObject;
        if (topMenuBar != null)
        {
            // [1단계]: '상단 단축바' 앵커 강제 세팅 (Top-Stretch: 화면 꼭대기 가로 꽉 채우기)
            RectTransform topBarRect = topMenuBar.GetComponent<RectTransform>();
            if (topBarRect != null)
            {
                topBarRect.anchorMin = new Vector2(0f, 1f);
                topBarRect.anchorMax = new Vector2(1f, 1f);
                topBarRect.pivot = new Vector2(0.5f, 1f);
                topBarRect.anchoredPosition = new Vector2(0f, 0f);
                topBarRect.sizeDelta = new Vector2(0f, 80f); // 세로 길이를 80픽셀 명품 띠 모양으로 고정!
            }

            // [2단계]: 'Btn_FastMoveToggle' 앵커 강제 세팅 (Top-Right: 우측 상단 고정 및 위치 정돈)
            Transform toggleBtn = topMenuBar.transform.Find("Btn_FastMoveToggle");
            if (toggleBtn != null)
            {
                toggleBtn.gameObject.SetActive(true);
                RectTransform toggleRect = toggleBtn.GetComponent<RectTransform>();
                if (toggleRect != null)
                {
                    toggleRect.anchorMin = new Vector2(1f, 1f);
                    toggleRect.anchorMax = new Vector2(1f, 1f);
                    toggleRect.pivot = new Vector2(1f, 1f);
                    toggleRect.anchoredPosition = new Vector2(0f, 0f); // 오른쪽 구석에서 안쪽으로 50픽셀 이쁘게 밀당
                    toggleRect.sizeDelta = new Vector2(200f, 80f);       // 마우스 센서 크기 가로/세로 규격 고정
                }

                // [3단계]: '빠른이동' 글씨 도화지 앵커 강제 세팅 (Stretch-Stretch: 사방 꽉 채워서 마우스 클릭 패스)
                Transform fastMoveTxt = toggleBtn.Find("빠른이동");
                if (fastMoveTxt != null)
                {
                    fastMoveTxt.gameObject.SetActive(true);
                    RectTransform txtRect = fastMoveTxt.GetComponent<RectTransform>();
                    if (txtRect != null)
                    {
                        txtRect.anchorMin = new Vector2(0f, 0f);
                        txtRect.anchorMax = new Vector2(1f, 1f);
                        txtRect.pivot = new Vector2(0.5f, 0.5f);
                        txtRect.anchoredPosition = Vector2.zero;
                        txtRect.sizeDelta = Vector2.zero; // 0,0,0,0 완전 밀착
                    }

                    // [4단계]: ★핵심★ 'NPC 목록판' 앵커, 위치, 피벗 강제 조준 세팅
                    Transform npcList = fastMoveTxt.Find("NPC 목록판");
                    if (npcList != null)
                    {
                        npcList.gameObject.SetActive(false); // 시작할 때는 잠시 숨겨두기
                        RectTransform listRect = npcList.GetComponent<RectTransform>();
                        if (listRect != null)
                        {
                            // 앵커 가이드를 무조건 '가운데 상단(Top-Center)'으로 리셋!
                            listRect.anchorMin = new Vector2(0.5f, 1f);
                            listRect.anchorMax = new Vector2(0.5f, 1f);

                            // 못질(피벗) 위치를 '1(천장 벽)'로 강제 픽스하여 상하방 전개 무빙축 완성!
                            listRect.pivot = new Vector2(0.5f, 1f);

                            // 위치 보정: 촌장 글씨가 빠른이동 단추를 가리지 않게, 버튼 높이(80) 바로 밑 경계선 위치인 Y: -80f로 강제 정렬!
                            listRect.anchoredPosition = new Vector2(0f, -80f);
                            listRect.sizeDelta = new Vector2(198.75f, 0f); // 세로 길이는 연산 코드가 자식 수 맞춰 자동 조절하므로 0출발!
                        }
                    }
                }
            }

            // 인트로 화면 단계에서는 상단바를 완전히 감추어 둡니다.
            topMenuBar.SetActive(false);
            // 🌟 [개발자님 기획 반영]: 파티 편집 화면에 처음 들어올 때 팝업창 세트가 눈치 없이 미리 켜져 있지 않도록 선제 차단(OFF)합니다!
            if (panel_PartyEditView != null) panel_PartyEditView.transform.Find("Panel_ConfirmRemove2")?.gameObject.SetActive(false);

        }
    }



    public void RefreshAllCharacterMenuCards()
    {
        foreach (var card in mainCharacterCards)
        {
            if (card != null) card.RefreshUI();
        }
        foreach (var card in subCharacterCards)
        {
            if (card != null) card.RefreshUI();
        }
        foreach (var card in warehouseCharacterCards)
        {
            if (card != null) card.RefreshUI();
        }

        // 🌟 이번에 기존 기능 아래에 안전하게 안착한 창고창 바둑판 연쇄 생성 엔진!
        GameObject warehouseContainer = panel_PartyEditView?.transform.Find("캐릭터 창고창/WarehouseListContainer")?.gameObject;
        if (warehouseContainer != null)
        {
            RectTransform containerRect = warehouseContainer.GetComponent<RectTransform>();
            if (containerRect != null)
                foreach (Transform child in warehouseContainer.transform) Destroy(child.gameObject);
            foreach (var member in allCharacters)
            {
                // 기존 X 위치는 그대로 유지하고, Y 위치만 100f로 강제 고정합니다.
                containerRect.anchoredPosition = new Vector2(containerRect.anchoredPosition.x, 100f);
                if (member == null) continue;
                GameObject cardObj = Instantiate(partyMemberPrefab, warehouseContainer.transform);
                PartyIcon partyIconScript = cardObj.GetComponent<PartyIcon>();
                if (partyIconScript != null) partyIconScript.Setup(member);
                Button cardBtn = cardObj.GetComponentInChildren<Button>();
                if (cardBtn != null)
                {
                    cardBtn.onClick.RemoveAllListeners();
                    cardBtn.onClick.AddListener(() => OnClickWarehouseCharacterCard(member));
                }
            }
        }
    } // 🌟 함수의 최종 닫는 중괄호


    void Update()
    {
        // 오직 마을 화면 패널이 활성화 상태일 때만 마찰력 관성 드래그 공식만 실시간 가동합니다!
        if (panel_Village != null && panel_Village.activeSelf && villageRectTransform != null)
        {
            HandleVillageInertiaDrag();
        }
    } // 🌟 Update 함수가 완전히 끝나는 마감 중괄호




    private void HandleVillageInertiaDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isVillageDragging = true;
            villageVelocity = Vector2.zero;
            dragMouseStartPos = Input.mousePosition;
            dragPanelStartPos = villageRectTransform.anchoredPosition;
            lastPanelPos = dragPanelStartPos;
        }

        if (Input.GetMouseButton(0) && isVillageDragging)
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 mouseDelta = currentMousePos - dragMouseStartPos;
            Vector2 targetPos = dragPanelStartPos + (mouseDelta * 1.5f);

            targetPos = RestrictPositionInsideVillage(targetPos);

            villageRectTransform.anchoredPosition = targetPos;
            villageVelocity = (targetPos - lastPanelPos) / Time.deltaTime;
            lastPanelPos = targetPos;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isVillageDragging = false;
        }

        if (!isVillageDragging && villageVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 nextPos = villageRectTransform.anchoredPosition + (villageVelocity * Time.deltaTime);
            Vector2 restrictedPos = RestrictPositionInsideVillage(nextPos);

            if (nextPos.x != restrictedPos.x) villageVelocity.x = 0;
            if (nextPos.y != restrictedPos.y) villageVelocity.y = 0;

            villageRectTransform.anchoredPosition = restrictedPos;
            villageVelocity = Vector2.Lerp(villageVelocity, Vector2.zero, inertiaDecel);
        }
    }

    private Vector2 RestrictPositionInsideVillage(Vector2 pos)
    {
        float limitX = (villageRectTransform.rect.width - 1920f) / 2f;
        float limitY = (villageRectTransform.rect.height - 1080f) / 2f;
        pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
        pos.y = Mathf.Clamp(pos.y, -limitY, limitY);
        return pos;
    }

    [Header("🌟 순수 코드 연동형 빠른 이동 시스템")]
    public GameObject obj_NPCListPanel; // 인스펙터에서 'NPC 목록판'을 연결할 칸

    // ① 상단 [빠른이동] 버튼 클릭 시: 목록창 토글 가동
    // 🌟 [빠른이동 버튼 클릭 시]: 에디터 상태가 어떻든 상관없이, 무조건 켜져 있으면 끄고 꺼져 있으면 켭니다!
    // 🌟 [빠른이동 버튼 클릭 시]: 캔버스 내부의 NPC 목록판 방 주소를 오차 없이 타겟 조준하여 켰다 껐다 뒤집습니다!
    // 🌟 [빠른이동 버튼 클릭 시]: 복잡한 경로 탐색 없이 인스펙터에 연결된 목록판 전원을 다이렉트로 칼같이 토글합니다!
    // 🌟 [빠른이동 버튼 클릭]: 뚝 끊기지 않고 위에서 아래로 스르륵 피어나는 그라데이션 페이드 연출 가동!


    // 🌟 [양방향 도미노 앤 목록판 크기 동적 연동 시스템]
    // 센서 빠른이동 애니메이션 시작점
    // 🌟 [양방향 도미노 앤 목록판 크기 동적 연동 시스템]
    private Coroutine npcListToggleCoroutine;

    public void OnClickToggleNPCListMenu()
    {
        if (obj_NPCListPanel != null)
        {
            bool isVisibleNow = obj_NPCListPanel.activeSelf;
            if (npcListToggleCoroutine != null) StopCoroutine(npcListToggleCoroutine);

            if (!isVisibleNow)
            {
                npcListToggleCoroutine = StartCoroutine(AnimateNPCListDropdown(true));
            }
            else
            {
                npcListToggleCoroutine = StartCoroutine(AnimateNPCListDropdown(false));
            }
        }
    }

    // 🌟 [명품 감속 곡선 탑재형 드롭다운 가변 연산 코루틴]
    // 🌟 [열고 닫히는 개별 속도 및 칼반응 딜레이 완전 보정형 코루틴]
    // 🌟 [개발자님 맞춤 수리: 담백하고 부드러운 순수 감속 곡선 및 칼반응 딜레이 마스터 코루틴]
    // 🌟 [개발자님 요청 직전 원본 복구선]: 위에서 아래로 순차 개폐되는 정석 도미노 코루틴 엔진
    private System.Collections.IEnumerator AnimateNPCListDropdown(bool isOpen)
    {
        RectTransform panelRect = obj_NPCListPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        // 1. 목록판 내부의 실제 자식 버튼 5개를 순서대로 수집합니다.
        List<Transform> npcButtons = new List<Transform>();
        foreach (Transform child in obj_NPCListPanel.transform)
        {
            npcButtons.Add(child);
        }

        // 💡 5명 완전체 짤림 버그를 격파했던 개당 80f 높이 간격 수치와 0.05초 칼박자 핑퐁 타이밍입니다!
        float buttonStepHeight = 80f;
        float delayTime = 0.05f;

        if (isOpen)
        {
            // ---------------- [1번 기획: 위에서 아래로 순차적 ON] ----------------
            obj_NPCListPanel.SetActive(true);

            // 시작할 때는 부모 크기를 0으로 축소하고 자식들도 전부 숨깁니다.
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
            foreach (var btn in npcButtons) btn.gameObject.SetActive(false);

            for (int i = 0; i < npcButtons.Count; i++)
            {
                if (npcButtons[i] == null) continue;

                npcButtons[i].gameObject.SetActive(true); // 자식 눈뜨기

                // 현재 켜진 자식의 수(i + 1)에 비례해서 부모 목록판 크기를 실시간으로 늘려줍니다!
                float currentHeight = (i + 1) * buttonStepHeight;
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

                yield return new WaitForSeconds(delayTime);
            }
        }
        else
        {
            // ---------------- [2번 기획: 아래에서 위로 역순 순차적 OFF] ----------------
            for (int i = npcButtons.Count - 1; i >= 0; i--)
            {
                if (npcButtons[i] == null) continue;

                npcButtons[i].gameObject.SetActive(false); // 자식 눈감기(OFF)

                // 여전히 남아있는 자식의 수(i)에 맞춰 부모 목록판 크기를 실시간으로 줄여줍니다!
                float currentHeight = i * buttonStepHeight;
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

                yield return new WaitForSeconds(delayTime);
            }

            // 모든 자식이 다 꺼지면 최종적으로 부모 껍데기 전원을 차단합니다.
            obj_NPCListPanel.SetActive(false);
        }

        npcListToggleCoroutine = null;
    }



    // ② 목록 안의 NPC 이름 버튼 클릭 시: 해당 좌표로 마을 패널 자체가 자동 비행 추적!
    // 🌟 [개발자님 5인 체제 기획 완벽 반영 마스터 함수]
    // 🌟 [5인 마스터 빠른이동 제어판]: 각 NPC가 배치된 영토의 진짜 물리 좌표계를 완벽 보정했습니다!
    public void OnClickFastMoveToNPC(int npcIndex)
    {
        if (villageRectTransform == null) return;

        Vector2 targetPosition = Vector2.zero;

        // 💡 [좌표 보정 안내]: 유니티 씬 뷰에서 마을 도화지를 누르고 마우스로 움직였을 때, 
        // 우측 인스펙터창 Rect Transform의 Anchored Position (X, Y) 값을 확인 후 아래 숫자를 내 맵에 맞게 수정해주시면 칼조준 안착됩니다!
        switch (npcIndex)
        {
            case 0: // 0번: 촌장님 위치
                targetPosition = new Vector2(800f, -500f);
                Debug.Log("[빠른이동] 촌장님 좌표로 비행을 개시합니다.");
                break;
            case 1: // 1번: 물약 상점 위치 정밀 역산 보정 주입!
                // 🌟 [정답 코드]: 물약 오브젝트의 수치(-1522, -776) 부호를 반대로 뒤집은 (1522f, 776f)를 정확하게 주입해줍니다!
                targetPosition = new Vector2(1522f, 776f);
                Debug.Log("[빠른이동] 물약 상점의 진짜 물리 좌표로 정밀 추적 비행을 개시합니다.");
                break;

            case 2: // 2번: 대장간 위치
                targetPosition = new Vector2(-800f, 500f);
                Debug.Log("[빠른이동] 대장간 좌표로 비행을 개시합니다.");
                break;
            case 3: // 3번: 무한 모드 진입 NPC (INF) 정밀 역산 보정값 주입!
                // 🌟 [정답 코드]: 무한모드 NPC 오브젝트 수치(1012, 713)의 부호를 반대로 뒤집어 입력합니다!
                targetPosition = new Vector2(-1012f, -713f);
                Debug.Log("[빠른이동] 무한모드 NPC 진짜 좌표로 정밀 추적 비행을 개시합니다.");
                break;

            case 4: // 4번: 일반 모드 진입 NPC (NOM) 정밀 역산 보정값 주입!
                // 🌟 [정답 코드]: 일반모드 NPC 오브젝트 수치(1471, 713)의 부호를 반대로 뒤집어 입력합니다!
                targetPosition = new Vector2(-1471f, -713f);
                Debug.Log("[빠른이동] 일반모드 NPC 진짜 좌표로 정밀 추적 비행을 개시합니다.");
                break;

        }

        villageVelocity = Vector2.zero; // 비행 출발 시 돌던 드래그 속도 완전 초기화
        targetPosition = RestrictPositionInsideVillage(targetPosition); // 이탈 방지 브레이크 통과

        if (villageMoveCoroutine != null) StopCoroutine(villageMoveCoroutine);
        villageMoveCoroutine = StartCoroutine(SmoothInertiaMoveRoutine(targetPosition, npcIndex));
    }

    private System.Collections.IEnumerator SmoothInertiaMoveRoutine(Vector2 targetPos, int npcIndex)
    {
        float duration = 0.5f;
        float elapsed = 0.0f;
        Vector2 startPos = villageRectTransform.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // 스무스 감속 곡선

            villageRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        villageRectTransform.anchoredPosition = targetPos;
        villageMoveCoroutine = null;

        // 🌟 [기획안 연동]: 자식 버튼을 클릭하여 이동이 완료되는 즉시, 목록판과 자식들만 깔끔하게 오프(OFF) 청소합니다!
        // 안착 완료 시 목록창을 자동 OFF 하여 시야 확보
        if (obj_NPCListPanel != null) obj_NPCListPanel.SetActive(false);

        // 🌟 [개발자님 기획 완벽 반영]: 화면이 흉물스럽게 겹치지 않도록, 새 방을 열기 직전에 스테이지 관련 화면 일당들을 세트로 일제히 먼저 끕니다!
        GameObject panelStageSelect = GameObject.Find("Canvas")?.transform.Find("Panel_StageSelect")?.gameObject;
        GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;

        if (panelStageSelect != null) panelStageSelect.SetActive(false);
        if (panelInfinite != null) panelInfinite.SetActive(false);

        // 🌟 [NPC의 클릭시 연쇄 적용 스위치]: 청소가 끝난 무결점 도화지 위에, 해당 NPC 고유의 화면만 단독 ON 가동!
        switch (npcIndex)
        {
            case 0:
                // 촌장님 클릭 시가 적용될 공간
                break;
            case 1:
                // 물약상점 클릭 시가 적용될 공간
                break;
            case 2:
                // 대장간 클릭 시가 적용될 공간
                break;
            case 3:
                // 무한모드 NPC 안착 즉시 ➡️ 경쟁 방들이 꺼진 상태이므로 오직 '무한 모드 화면'만 단독 ON!
                OnClickEnterInfiniteStage();
                break;
            case 4:
                // 일반모드 NPC 안착 즉시 ➡️ 경쟁 방들이 꺼진 상태이므로 오직 '일반 스테이지 화면'만 단독 ON!
                OnClickEnterNormalStage();
                break;
        }
    } // 🌟 코루틴 함수가 완벽하게 마무리 닫히는 대중괄호

    private Coroutine villageMoveCoroutine;

    // --- [0. 메인 캐릭터 선택창 시작시] ---
    public void StartNewGameFromTitle()
    {

        Debug.Log("[메인 캐릭터 선택창] 시작시 설정 가동");
        if (panel_CharacterSelect) panel_CharacterSelect.SetActive(true);
        if (partyListContainer) partyListContainer.SetActive(true);
        if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
        if (btn_Confirm3) btn_Confirm3.SetActive(false);
        if (btn_Cancel3) btn_Cancel3.SetActive(false);
        if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(false);
        if (panel_Village) panel_Village.SetActive(false);
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);

        partyMembers.Clear();
        UpdatePartyUI();
    }

    // --- [1. 각 캐릭터 버튼을 클릭시] ---
    public void OnClickCharacter(int charId)
    {
        selectedCharId = charId;
        CharacterData data = allCharacters.Find(x => x.id == selectedCharId);
        if (data != null)
        {
            if (txt_CharacterName) txt_CharacterName.text = data.characterName;
            if (txt_CharacterInfo) txt_CharacterInfo.text = $"{data.description}\n\nHP: {data.hp} | ATK: {data.attackPower} | DEF: {data.defense}";
            if (popup_CharacterInfo) popup_CharacterInfo.SetActive(true);
            if (btn_Confirm3) btn_Confirm3.SetActive(true);
            if (btn_Cancel3) btn_Cancel3.SetActive(true);
        }
    }

    // --- [새로 추가한 작업]: 타이틀 복귀 원본 로직 보존
    public void OnClickMainToTitleBackButton()
    {
        Debug.Log("[메인 선택창] 뒤로가기 클릭 -> 타이틀 화면 완전 복구");
        if (panel_CharacterSelect) panel_CharacterSelect.SetActive(false);
        if (partyListContainer) partyListContainer.SetActive(false);
        if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);

        TitleManager titleScript = FindAnyObjectByType<TitleManager>();
        if (titleScript != null)
        {
            if (titleScript.panel_Title) titleScript.panel_Title.SetActive(true);
            if (titleScript.btn_Continue) titleScript.btn_Continue.SetActive(true);
            if (titleScript.btn_Settings) titleScript.btn_Settings.SetActive(true);
            if (titleScript.btn_Exit) titleScript.btn_Exit.SetActive(true);
#if UNITY_IOS
            if (titleScript.btn_Exit != null) titleScript.btn_Exit.SetActive(false);
#endif
        }
    }

    // --- [1-1. 공용 확인3 클릭시 (메인/서브 파티 이동)] ---
    public void OnClickConfirm3()
    {
        CharacterData data = allCharacters.Find(x => x.id == selectedCharId);
        if (data == null) return;

        if (panel_CharacterSelect && panel_CharacterSelect.activeSelf)
        {
            partyMembers.Add(data);
            allCharacters.Remove(data);
            UpdatePartyUI();
            if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
            if (btn_Confirm3) btn_Confirm3.SetActive(false);
            if (btn_Cancel3) btn_Cancel3.SetActive(false);
            if (panel_CharacterSelect) panel_CharacterSelect.SetActive(false);
            StartSubCharacterSelect();
        }
        else if (panel_SubCharacterSelect && panel_SubCharacterSelect.activeSelf)
        {
            if (partyMembers.Count >= 5)
            {
                ShowScreenNotice("파티원이 가득 찼습니다.");
                if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
                return;
            }
            partyMembers.Add(data);
            allCharacters.Remove(data);
            UpdatePartyUI();
            if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
        }
    }

    public void OnClickCancel3() { if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false); }

    // --- [0. 서브 캐릭터 선택창 시작시] ---
    public void StartSubCharacterSelect()
    {
        Debug.Log("[서브 캐릭터 선택창] 시작시 설정 가동");
        if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(true);
        if (partyListContainer) partyListContainer.SetActive(true);
        if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
        if (btn_Confirm3) btn_Confirm3.SetActive(false);
        if (btn_Cancel3) btn_Cancel3.SetActive(false);
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);
    }

    // 🌟 [1번 기획 완벽 주입]: 일반/무한 모드 진입 시 하단 파티창(PartyListContainer) 활성화 고수
    public void OnClickEnterNormalStage()
    {
        Debug.Log("[마을] 일반 모드 클릭 -> Panel_StageSelect와 자식들 ON");
        stageMode = 1;
        GameObject panelStageSelect = GameObject.Find("Canvas")?.transform.Find("Panel_StageSelect")?.gameObject;
        if (panelStageSelect != null)
        {
            panelStageSelect.SetActive(true);
        }
    }

    public void OnClickNormalStageBackButton()
    {
        Debug.Log("[일반목록] 뒤로가기 클릭 -> 마을로 이동 및 Panel_StageSelect와 자식들 OFF");
        GameObject panelStageSelect = GameObject.Find("Canvas")?.transform.Find("Panel_StageSelect")?.gameObject;
        if (panelStageSelect != null) panelStageSelect.SetActive(false);
        if (panel_Village) panel_Village.SetActive(true);
    }

    public void OnClickEnterInfiniteStage()
    {
        Debug.Log("[마을] 무한 모드 클릭 -> Panel_InfiniteStage와 자식들 ON");
        stageMode = 2;
        GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (panelInfinite != null)
        {
            panelInfinite.SetActive(true);
        }
    }

    public void OnClickInfiniteStageBackButton()
    {
        Debug.Log("[무한목록] 뒤로가기 클릭 -> 마을로 이동 및 Panel_InfiniteStage와 자식들 OFF");
        GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (panelInfinite != null) panelInfinite.SetActive(false);
        if (panel_Village) panel_Village.SetActive(true);
    }
    // 🌟 [파티원 아이콘 클릭]: 편집창 전용 신형 팝업창(Panel_ConfirmRemove2)을 화면 정중앙에 탁 켜줍니다!
    public void OnClickPartyMemberIcon(CharacterData clickedCharacter)
    {
        if (clickedCharacter == null) return;

        // 임시 저장 주머니에 내가 방금 클릭해서 뺄 영웅 데이터를 안전하게 킵해둡니다.
        pendingRemoveCharacter = clickedCharacter;

        if (panel_PartyEditView != null && panel_PartyEditView.activeSelf)
        {
            // 🌟 [정밀 주소 추적]: 개발자님이 새로 복사해서 넣어두신 'Panel_ConfirmRemove2' 팝업창 본체를 정확히 찾아옵니다!
            GameObject confirmPopup2 = panel_PartyEditView.transform.Find("Panel_ConfirmRemove2")?.gameObject;
            if (confirmPopup2 != null)
            {
                confirmPopup2.SetActive(true); // "제거하시겠습니까?" 팝업창 전원 ON!
                Debug.Log($"[신형 팝업 연동] {clickedCharacter.characterName} 제거 확인창 소환 완료!");
            }
            return;
        }

        // (기존 캐릭터 최초 선택 화면일 때 가동되던 낡은 1호점 팝업창 규칙 수리 보존)
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(true);
    }


    public void OnClickRemoveYes()
    {
        if (pendingRemoveCharacter == null) return;
        Debug.Log($"[제거 확정] {pendingRemoveCharacter.characterName} 파티 해제 후 원위치 복구");
        CharacterData target = pendingRemoveCharacter;
        partyMembers.Remove(target);
        allCharacters.Add(target);
        UpdatePartyUI();

        // 🌟 [새로 추가되는 복귀선]: 캐릭터 선택창에서 팝업창 [예]를 누르는 즉시 아래쪽 캐릭터 창고창 목록까지 실시간으로 싹 갱신하여 정렬 나열해 줍니다!
        RefreshAllCharacterMenuCards();
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);
        pendingRemoveCharacter = null;

        if (target.id == 1 || target.id == 2 || target.id == 3)
        {
            Debug.Log("[기획 2-1] 파티창에서 메인캐릭터가 사라짐 -> 메인 선택창으로 복귀");
            if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(false);
            if (panel_CharacterSelect) panel_CharacterSelect.SetActive(true);
            if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
        }
    }

    public void OnClickRemoveNo()
    {
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);
        pendingRemoveCharacter = null;
    }

    // --- [3. 뒤로가기 버튼 클릭시 (원본 유지)] ---
    public void OnClickBackButton()
    {
        Debug.Log("[뒤로가기] 클릭 -> 메인 캐릭터 선택화면으로 이동");
        CharacterData mainChar = partyMembers.Find(x => x.id == 1 || x.id == 2 || x.id == 3);
        if (mainChar != null)
        {
            partyMembers.Remove(mainChar);
            allCharacters.Add(mainChar);
            UpdatePartyUI();
        }
        if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(false);
        if (panel_CharacterSelect) panel_CharacterSelect.SetActive(true);
        if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
    }

    // --- [4. 출발하기 버튼 클릭시 (원본 유지)] ---
    public void OnClickStartAdventureButton()
    {
        Debug.Log("[출발하기] 인원 및 조건 검사");
        bool hasMain = partyMembers.Exists(x => x.id == 1 || x.id == 2 || x.id == 3);
        int subCount = partyMembers.Count - (hasMain ? 1 : 0);
        if (hasMain && subCount >= 4)
        {
            Debug.Log("[출발 완료] 5인 조건 완벽 충족 -> 오직 출발하기 팝업 세트만 출력");
            if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(true);
            if (obj_RemoveWindow) obj_RemoveWindow.SetActive(false);
            if (btn_RemoveYes) btn_RemoveYes.SetActive(false);
            if (btn_RemoveNo) btn_RemoveNo.SetActive(false);
            if (obj_StartWindow) obj_StartWindow.SetActive(true);
            if (btn_Confirm5) btn_Confirm5.SetActive(true);
            if (btn_Cancel5) btn_Cancel5.SetActive(true);
        }
        else
        {
            if (!hasMain) ShowScreenNotice("메인 캐릭터가 없습니다. 메인 캐릭터를 선택해주세요.");
            else if (subCount < 4) ShowScreenNotice("서브 캐릭터가 4명이되도록 서브 캐릭터를 선택해주세요.");
        }
    }

    public void OnClickConfirm5()
    {
        Debug.Log("[출발 승인 예 클릭] 마을 화면으로 최종 진입합니다. 모든 팝업 및 자식 일괄 OFF");
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);
        if (obj_StartWindow) obj_StartWindow.SetActive(false);
        if (btn_Confirm5) btn_Confirm5.SetActive(false);
        if (btn_Cancel5) btn_Cancel5.SetActive(false);
        if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(false);
        if (panel_CharacterSelect) panel_CharacterSelect.SetActive(false);
        if (obj_NPCListPanel != null && obj_NPCListPanel.transform.parent != null)
        {
            obj_NPCListPanel.transform.parent.gameObject.SetActive(true); // Btn_FastMoveToggle(부모) ON
        }
        if (partyListContainer) partyListContainer.SetActive(false);

        GameObject panelStageSelect = GameObject.Find("Canvas")?.transform.Find("Panel_StageSelect")?.gameObject;
        if (panelStageSelect != null) panelStageSelect.SetActive(false);
        GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (panelInfinite != null) panelInfinite.SetActive(false);
        if (panel_Village) panel_Village.SetActive(true);
        // 🌟 [지우지 말고 여기에 딱 2줄만 얹어주세요!]: 왕부모 상자인 '상단 단축바'의 전원을 강제로 켭니다.
        GameObject topMenuBar = GameObject.Find("Canvas")?.transform.Find("상단 단축바")?.gameObject;
        if (topMenuBar != null) topMenuBar.SetActive(true);
    }

    public void OnClickCancel5()
    {
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);
        if (obj_StartWindow) obj_StartWindow.SetActive(false);
        if (btn_Confirm5) btn_Confirm5.SetActive(false);
        if (btn_Cancel5) btn_Cancel5.SetActive(false);
    }
    // 🌟🌟🌟 [실시간 파티 편집 보관 창고 인아웃 제어 스위치 공간] 🌟🌟🌟

    // 🌟 [2번 기획]: 파티편집 메뉴 클릭 시 호출될 원격 오픈 함수
    // 🌟 [파티편집1, 2 버튼 클릭 시]: 대형 편집 화면 패널을 켜고 자식들을 일괄 ON 합니다!
    public void OnClickOpenPartyEditView()
    {
        Debug.Log("[스테이지 UI] 파티편집 클릭 -> Panel_PartyEditView 및 자식들 일괄 ON");

        // 1. 인스펙터에 연결된 대형 파티 편집 패널 본체를 시원하게 켭니다!
        // (부모 패널이 켜지면서 내부에 세팅해두신 자식 창고창 버튼들도 세트로 자동 ON 상태가 됩니다!)
        if (panel_PartyEditView != null)
        {
            panel_PartyEditView.SetActive(true);
        }

        // 2. 기획 규칙: 하단 파티 목록창(PartyListContainer)도 화면에 항상 선명하게 유지 노출합니다.
        if (partyListContainer)
        {
            partyListContainer.SetActive(true);
        }

        // 3. UI 데이터 동기화 및 실시간 시너지 문자 표기 강제 갱신
        UpdatePartyUI();
    }


    // 🌟 파티 편집 완료(닫기) 버튼 클릭 시 호출될 함수
    // 🌟 [파티 편집창 내부 '뒤로가기' 버튼 클릭 시]: 패널과 자식들을 일괄 OFF 합니다!
    public void OnClickClosePartyEditView()
    {
        Debug.Log("[파티편집] 뒤로가기 클릭 -> Panel_Party Edit View 및 자식들 일괄 OFF");

        // 1. 인스펙터에 연결된 대형 파티 편집 패널 본체를 안전하게 끕니다!
        // (부모 패널이 꺼지면서 내부에 속한 시너지 리스트 등 자식들도 세트로 자동 OFF 상태가 됩니다.)
        if (panel_PartyEditView != null)
        {
            panel_PartyEditView.SetActive(false);
        }
    }


    // 🌟 [4번 기획]: 캐릭터 보관 창고창 리스트 카드를 마우스 클릭 시 호출될 실시간 가입 스위치!
    public void OnClickWarehouseCharacterCard(CharacterData targetData)
    {
        if (targetData == null) return;

        // 파티 최대 결성 인원 상한선 예외 처리 체크 (5명 제한)
        if (partyMembers.Count >= 5)
        {
            ShowScreenNotice("파티는 최대 5명까지만 구성할 수 있습니다.");
            return;
        }

        Debug.Log($"[창고 가입] 파티 명단에 {targetData.characterName} 추가 완료");
        allCharacters.Remove(targetData); // 보관소 명단 풀에서 제외
        partyMembers.Add(targetData);      // 가용 실시간 파티 목록에 등록
        UpdatePartyUI();                  // UI 리렌더링 및 실시간 시너지 재연산 가동
    }

    // 🌟 [5번 기획 부품]: 현재 파티원들의 고유 속성을 분석해 시너지 텍스트를 실시간 빌드해주는 연산 장치! 
    private string CalculateCurrentSynergyText()
    {
        if (partyMembers == null || partyMembers.Count == 0)
        {
            return "현재 구성된 파티가 없어 활성화된 시너지가 없습니다.";
        }

        // 파티원들의 시너지 명단을 인원수별로 수집 분류합니다.
        Dictionary<string, int> synergyCounts = new Dictionary<string, int>();

        foreach (var member in partyMembers)
        {
            if (member == null || string.IsNullOrEmpty(member.synergyName)) continue;

            if (synergyCounts.ContainsKey(member.synergyName))
                synergyCounts[member.synergyName]++;
            else
                synergyCounts[member.synergyName] = 1;
        }

        // 텍스트 조립기 가동
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("📜 <b><color=#FFE600>[실시간 파티 조합 시너지 현황]</color></b>\n");

        bool hasAnySynergy = false;

        foreach (var kvp in synergyCounts)
        {
            string sName = kvp.Key;
            int count = kvp.Value;

            // 💡 [나중에 확장할 때]: "2명 이상 조합될 때만 글자 표기해줘!" 같은 세부 규칙도 여기서 쉽게 커스텀 가능합니다.
            sb.AppendLine($"▶ <b>{sName}</b> (참여 파티원 수: {count}명)");
            hasAnySynergy = true;
        }

        if (!hasAnySynergy) return "현재 결성된 고유 조합 시너지가 없습니다.";
        return sb.ToString();
    }

    // --- [화면 경고 텍스트 페이드아웃 시스템 (원본 유지)] --- 
    public void ShowScreenNotice(string message)
    {
        if (txt_MainNoticeText == null) return;
        if (noticeFadeCoroutine != null) StopCoroutine(noticeFadeCoroutine);
        noticeFadeCoroutine = StartCoroutine(NoticeFadeOutRoutine(message));
    }

    private System.Collections.IEnumerator NoticeFadeOutRoutine(string message)
    {
        txt_MainNoticeText.text = message;
        txt_MainNoticeText.gameObject.SetActive(true);
        Color textColor = txt_MainNoticeText.color;
        textColor.a = 1.0f;
        txt_MainNoticeText.color = textColor;
        yield return new WaitForSeconds(1.5f);
        float duration = 1.0f;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textColor.a = Mathf.Lerp(1.0f, 0.0f, elapsed / duration);
            txt_MainNoticeText.color = textColor;
            yield return null;
        }
        txt_MainNoticeText.gameObject.SetActive(false);
        noticeFadeCoroutine = null;
    }

    // 🌟 [개발자님 기획 100% 무결점 구현]: 3매치 블록이 터질 때 대미지 수치를 수거해 실시간으로 몬스터 피를 깎아내립니다! [PDF: 0.1.10]
    public void DamageEnemy(int amount)
    {
        // 1. 퍼즐 전장 전담 매니저 두뇌를 원격으로 서치해 수거합니다.
        PuzzleBattleManager puzzleManager = GetComponent<PuzzleBattleManager>();
        if (puzzleManager != null && puzzleManager.enemyHPBar != null)
        {
            // 2. 실시간 대미지 반영: 몬스터 체력바의 현재 value(잔량)에서 터진 점수만큼 슥 깎아버립니다!
            float currentHP = puzzleManager.enemyHPBar.value; // 현재 적 피통 수거
            puzzleManager.enemyHPBar.value = Mathf.Max(0f, currentHP - (amount / 100f)); // 최하 0까지 안전하게 감산 깎기

            Debug.Log($"[몬스터 타격 폭발!] 3매치 연쇄 파괴로 적에게 {amount}의 대미지 적중! 몬스터 남은 HP 게이지: {puzzleManager.enemyHPBar.value}");

            // 3. 만약 몬스터 피가 완전히 0이 되어 사망했다면? ➡️ 승리 보상 패널(RewardPanel) 가동!
            if (puzzleManager.enemyHPBar.value <= 0f)
            {
                Debug.Log("[전투 승리 선포] 무한 모드 몬스터 격파 완료! 보상 수령 창을 전개합니다.");

                // 보드.cs 하단에 잠들어 있던 보상 창 끄기 켜기 스위치로 마스터 바통을 연쇄 터치 터치 발사합니다!
                Board boardComponent = puzzleManager.panel_PuzzleBattle?.GetComponentInChildren<Board>();
                if (boardComponent != null) boardComponent.OnClickRewardConfirmButton();
            }
        }
        else
        {
            Debug.LogWarning("[대미지 통신 지연] 아직 유니티 인스펙터창에 몬스터 HP_Bar 회선선이 물려있지 않아 수치만 수거 중입니다: " + amount);
        }
    }


    // --- [파티 실시간 UI 렌더링 시스템] --- 
    public void UpdatePartyUI() 
    {
        // 🌟 [개발자님 명품 UI 자동화 버그 제로 기획 주입]: 
        // 유니티 캔버스에 있는 상단 단축바를 찾아와서, 오직 마을 패널이 켜져 있을 때만(true/false) 완벽하게 연동되도록 동기화시킵니다!
        GameObject topMenuBar = GameObject.Find("Canvas")?.transform.Find("상단 단축바")?.gameObject;
        if (topMenuBar != null && panel_Village != null)
        {
            // 마을 화면이 활성화(activeSelf) 상태면 true가 되어 상단바도 켜지고, 꺼지면 자동으로 함께 꺼집니다!
            topMenuBar.SetActive(panel_Village.activeSelf);
        }


        if (partyListContainer == null) return;

        // 메인 캐릭터 무조건 1번 슬롯 강제 정렬 정석 공식 완벽 보존 
        partyMembers.Sort((x, y) =>
        {
            bool xIsMain = (x.id == 1 || x.id == 2 || x.id == 3);
            bool yIsMain = (y.id == 1 || y.id == 2 || y.id == 3);
            if (xIsMain && !yIsMain) return -1;
            if (!xIsMain && yIsMain) return 1;
            return 0;
        });

        // 1. 원래 마을 화면 하단 바구니에 있던 낡은 아이콘들을 깨끗하게 청소합니다.
        foreach (Transform child in partyListContainer.transform) Destroy(child.gameObject);

        // 🌟 [새로 개설된 복사본 주소 록온선!]: 드디어 편집창 내부 전용으로 복사한 바구니 상자를 정확히 찾아옵니다!
        GameObject editPartyContainer = panel_PartyEditView?.transform.Find("PartyListContainer_Edit")?.gameObject;
        if (editPartyContainer != null)
        {
            // 편집창 전용 복사본 바구니 내부도 깨끗하게 선제 청소!
            foreach (Transform child in editPartyContainer.transform) Destroy(child.gameObject);
        }

        // 2. 🌟 [양방향 동시 복사 연동 가동]: 현재 가입된 5명의 명단을 루프 돌리며 양쪽에 동시에 심어줍니다!
        foreach (var member in partyMembers)
        {
            // (A) 원래 쓰던 마을 화면 하단 바구니에 영웅 아이콘 생성 및 데이터 세팅
            GameObject iconVillage = Instantiate(partyMemberPrefab, partyListContainer.transform);
            PartyIcon partyIconScriptVillage = iconVillage.GetComponent<PartyIcon>();
            if (partyIconScriptVillage != null)
            {
                partyIconScriptVillage.Setup(member); // 기존 데이터 세팅 기능 완벽 보존!
            }
            Button btnVillage = iconVillage.GetComponentInChildren<Button>();
            if (btnVillage != null)
            {
                btnVillage.onClick.RemoveAllListeners();
                btnVillage.onClick.AddListener(() => OnClickPartyMemberIcon(member));
            }

            // (B) 🔥 대형 파티 편집창 내부에 새로 구운 'PartyListContainer_Edit' 복사본 바구니에도 똑같이 영웅 아이콘 복사 연쇄 생성!
            if (editPartyContainer != null)
            {
                GameObject iconEdit = Instantiate(partyMemberPrefab, editPartyContainer.transform);
                PartyIcon partyIconScriptEdit = iconEdit.GetComponent<PartyIcon>();
                if (partyIconScriptEdit != null)
                {
                    partyIconScriptEdit.Setup(member); // 편집창 복사본 아이콘에도 똑같이 데이터 주입!
                }
                Button btnEdit = iconEdit.GetComponentInChildren<Button>();
                if (btnEdit != null)
                {
                    btnEdit.onClick.RemoveAllListeners();
                    // 편집창에서 이 아이콘을 누르면 원터치로 파티에서 즉시 탈퇴 복구되도록 세팅!
                    btnEdit.onClick.AddListener(() => OnClickPartyMemberIcon(member));
                }
            }



        }




        RefreshAllCharacterMenuCards();
    }
    // ======================================================================================
    // 🌟 [개발자님 기획 100% 독립 구현: 파티 편집창 2호기 팝업 전용 마스터 제어국] 🌟
    // ======================================================================================

    // 🌟 [2호기 전용 '예' 버튼 클릭 시]: 상단 파티창에서 캐릭터를 지우고 중앙 창고 바둑판으로 이동시킨 뒤, 2호기 팝업창 세트 전체를 완전히 OFF 시킵니다!
    public void OnClickEditRemoveYes()
    {
        if (pendingRemoveCharacter == null) return;
        CharacterData target = pendingRemoveCharacter;

        // 1. 장착되어 있던 파티 명단 주머니에서 빼버리고, 창고 대기 데이터 주머니로 영웅 수거 반납!
        if (partyMembers.Contains(target)) partyMembers.Remove(target);
        if (!allCharacters.Contains(target)) allCharacters.Add(target);

        // 2. 상단 파티 슬롯 화면(PartyListContainer_Edit)과 중앙 거대 창고창 바둑판 리스트 화면을 동시에 새로고침 나열합니다.
        UpdatePartyUI();
        RefreshAllCharacterMenuCards();

        // 3. 🌟 [1호기와 완벽 분리]: 사용이 마감된 파티 편집창 내부의 'Panel_ConfirmRemove2' 패널 세트 전체를 깔끔하게 차단(OFF) 소멸시킵니다.
        if (panel_PartyEditView != null)
        {
            GameObject confirmPopup2 = panel_PartyEditView.transform.Find("Panel_ConfirmRemove2")?.gameObject;
            if (confirmPopup2 != null) confirmPopup2.SetActive(false);
        }

        pendingRemoveCharacter = null;
        Debug.Log("[2호기 아니오 버튼 완벽 작동] 파티원 유지 및 팝업창 단독 소멸 완료!");
    }

    // 🌟 이번에 문서 맨 밑바닥 방에 안전하게 새로 용접되어 안착한 무한 모드 진입 엔진!
    // ======================================================================================
    // 🌟 [무한 스테이지 모드 버튼 클릭 시]: 무한 스테이지 창을 끄고, 진짜 3매치 퍼즐 전투판을 화사하게 켜줍니다!
    // ======================================================================================
    // ======================================================================================
    // 🌟 [개발자님 기획 200% 정밀 보정 완료]: 무한모드 개시 즉시, 진짜 최종 퍼즐 전투판 'Panel_INPuzzleBattle'로 차원 이동시킵니다!
    // ======================================================================================
    // 🌟 [개발자님 기획 100% 칼반영]: 마을에서 무한모드 버튼을 누르면 무한모드 스테이지 대기방과 자식들을 통째로 ON 시킵니다!
        // ======================================================================================
    // 🌟 [개발자님 정석 기획 1번 흐름]: 마을에서 버튼 클릭 시, 무한 대기방 패널과 자식들 일제히 ON!
    // ======================================================================================
    public void OnClickInfiniteStage()
    {
        // 마을 화면 패널(Panel_Village) 전원을 OFF 숨김 처리합니다.
        if (panel_Village != null) panel_Village.SetActive(false);

        // 캔버스 아래 대기 중인 1차 목적지 'Panel_InfiniteStage' 패널을 찾아 전원 ON!
        GameObject infStagePanel = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (infStagePanel != null)
        {
            infStagePanel.SetActive(true); // 부모가 켜지며 내부 자식 단추들까지 세트로 동시 ON 완료!
            stageMode = 2; // 무한모드 정석 식별 상태 값 주입
            Debug.Log("[1단계 성공] 마을 종료 ➡️ 무한 스테이지 대기화면(Panel_InfiniteStage) 및 자식들 통합 ON 완공!");
        }
        else
        {
            Debug.LogError("[1단계 오류] 캔버스 아래에서 'Panel_InfiniteStage' 오브젝트를 찾지 못했습니다!");
        }

    }


    // ======================================================================================
    // 🌟 [개발자님 정석 기획 2번 흐름]: 대기방 안에서 Btn_Stage_I 클릭 시, 진짜 퍼즐 전투판과 자식들 일제히 ON!
    // ======================================================================================
    public void OnClickInfiniteStageModeStart()
    {
        // 1차 무한 스테이지 대기방 패널(Panel_InfiniteStage) 본체 전원을 OFF 숨김 처리합니다.
        GameObject infStagePanel = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (infStagePanel != null) infStagePanel.SetActive(false);

        // 2차 최종 목적지인 진짜 3매치 퍼즐 배틀판 'Panel_INPuzzleBattle' 오브젝트를 찾아 전원 ON!
        GameObject realPuzzlePanel = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle")?.gameObject;
        if (realPuzzlePanel != null)
        {
            realPuzzlePanel.SetActive(true);

            Transform puzzleBoardTrans = realPuzzlePanel.transform.Find("PuzzleBoard");
            if (puzzleBoardTrans != null)
            {
                puzzleBoardTrans.gameObject.SetActive(true);

                // ==========================================================
                // 🌟 [최종 고민 해결]: 화면에 3매치 판을 실시간으로 그려내는 핵심 코드 엔진 가동!
                // ==========================================================
                Board boardComponent = puzzleBoardTrans.GetComponent<Board>();
                if (boardComponent != null)
                {
                    boardComponent.SetupStage(6, 6); // 6x6 크기로 정밀 세팅
                    boardComponent.CreateBoard();    // 화면에 블록 프리팹들 촤라락 복사 생성!
                    InitializeDynamicBattlePartyUI();
                    Debug.Log("[퍼즐 소환 완료] 6x6 격자 보석 블록이 화면에 정상 출력되었습니다.");
                }
            }
            }
        else
        {
            Debug.LogError("[2단계 오류] 캔버스 아래에서 진짜 퍼즐판 'Panel_INPuzzleBattle' 오브젝트를 찾지 못했습니다!");
        }
    }
    [Header("⚔️ 배틀 전용 동적 파티창 설정")]
    public GameObject battlePartyContainer; // 인스펙터에서 PartyContainer_Battle을 연결할 칸

    // [기획 요구사항 반영]: 배틀방 진입 시 실제 캐릭터 데이터와 연동된 배틀용 슬롯+HP바를 동적 소환하는 엔진 
    public void InitializeDynamicBattlePartyUI()
    {
        Debug.Log("[전투UI 개시] 배틀 전용 실시간 데이터 연동형 파티창 소환을 시작합니다.");

        // 1. 혹시 남아있을지 모를 배틀 전용 파티창 내부를 깨끗하게 대청소합니다. 
        if (battlePartyContainer != null)
        {
            foreach (Transform child in battlePartyContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("[배틀 UI 경고] battlePartyContainer 장치가 인스펙터에 연결되지 않았습니다.");
            return;
        }

        // 2. 현재 편성된 내 파티 영웅 명단을 루프 돌리며 배틀 전용 아이콘+HP바를 실시간 생성합니다! 
        foreach (var member in partyMembers)
        {
            if (member == null) continue; 

        // 마을 파티창 프리팹(partyMemberPrefab)을 배틀용 컨테이너 하위에 복사 생성합니다! 
        GameObject battleIconObj = Instantiate(partyMemberPrefab, battlePartyContainer.transform);

            // 아이콘에 붙어있는 스크립트를 깨워 캐릭터 데이터(이름, 테마색, 체력바 등)를 강제 연동합니다. 
            PartyIcon partyIconScript = battleIconObj.GetComponent<PartyIcon>();
            if (partyIconScript != null)
            {
                partyIconScript.Setup(member); // 우리가 앞서 다듬은 Setup 함수가 실행되며 발밑 HP바가 동적 형성됩니다! 
            }

            // 🌟 [배틀 최적화]: 전투 중 실수로 영웅 아이콘을 눌러 파티에서 해제되는 버그를 막기 위해 버튼을 잠금 처리합니다.
            Button btn = battleIconObj.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners(); // 기존의 '클릭 시 파티 탈퇴' 팝업 회선을 차단 
                                                  // 필요하다면 나중에 이곳에 '영웅 필살기 터치 버튼' 회선을 연결할 수 있습니다.
            }
        }
        Debug.Log("[배틀 UI 완공] 실제 캐릭터 데이터와 100% 일치하는 실시간 HP바 장착 영웅들이 화면에 배치되었습니다.");
    }

} // 🌟 GameManager 클래스 전체 문서가 마감되는 진짜 최종 마지막 닫는 대중괄호선 (절대 지우시면 안 됩니다!)
