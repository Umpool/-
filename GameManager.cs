using System.Collections.Generic;
using TMPro;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 패널 제어")]
    public GameObject panel_CharacterSelect; // 메인 캐릭터 선택창
    public GameObject panel_SubCharacterSelect; // 서브 캐릭터 선택창
    public GameObject panel_Village; // 마을 화면
    public GameObject panel_PartyEditView; // [0-1번 기획]: 새로 배치할 대형 파티 편집 화면 패널
    public GameObject popup_PartyAddConfirm; // 인스펙터에서 '파티에 추가 하시겠습니까?'


    [Header("🌟 캐릭터 정보 팝업 텍스트 연동 (새로 추가)")]
    public TextMeshProUGUI characterNameText; // '캐릭터 이름' 오브젝트 연결용
    public TextMeshProUGUI characterInfoText; // '캐릭터 정보' 오브젝트 연결용

    [Header("마을 파티 편집창 UI 연결")]
    public GameObject popup_PartyEditCharacterInfo; // 마을 편집창 전용 정보 팝업 (필요시 분리)
    public TextMeshProUGUI partyEditNameText;
    public TextMeshProUGUI partyEditInfoText;

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
    public TextMeshProUGUI txt_SynergyOutput; // [0-3번 기획]: 실시간 시너지 문자들을 뿜어낼 도화지 텍스트

    [Header("메인 및 서브 캐릭터 카드 버튼 리스트")]
    public List<CharacterCard> mainCharacterCards;
    public List<CharacterCard> subCharacterCards;
    public List<CharacterCard> warehouseCharacterCards; // [0-2번 기획]: 캐릭터 창고 내부에 나열될 프리팹 카드 목록

    [Header("🌟 중복/만석 안내 시스템 (최상위 Canvas 배치용)")]
    public GameObject popup_AlertWindow;      // 하이어라키의 '이미 파티에 있습니다.' 오브젝트만 연결!

    [Header("데이터 및 파티 관리")]
    public List<CharacterData> allCharacters; // 전체 캐릭터 풀 (창고 풀 데이터)
    public List<CharacterData> partyMembers; // 현재 파티 멤버 (최대 5명)
    public GameObject partyMemberPrefab; // 파티 아이콘 프리팹

    [Header("--- 마을 화면 UI 제어 ---")]
    public GameObject Panel_Village; // 마을 화면 패널
    public GameObject PartyListContainer; // 파티 목록 컨테이너

    [Header("--- 배틀 중 비활성화할 단축바 UI ---")]
    public GameObject QuickMoveButton; // 빠른 이동 버튼 오브젝트 ([V] 체크박스 제어용)

    [Header("--- 상단 단축바 리모컨 ---")]
    public GameObject panel_TopBar; // 만약 단축바 전체 패널 장부가 있다면 확인용

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

    private Coroutine villageMoveCoroutine;
    private Coroutine npcListToggleCoroutine;

    [Header("순수 코드 연동형 빠른 이동 시스템")]
    public GameObject obj_NPCListPanel; // 인스펙터에서 'NPC 목록판'을 연결할 칸

    [Header("배틀 전용 동적 파티창 설정")]
    public GameObject battlePartyContainer; // 인스펙터에서 PartyContainer_Battle을 연결할 칸

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        RefreshAllCharacterMenuCards();
    }

    void Start()
    {
        if (popup_PartyAddConfirm != null) popup_PartyAddConfirm.SetActive(false);
        // 🌟 [코드 제어] 게임 시작 시 알림창 팝업을 무조건 처음부터 OFF 상태로 초기화합니다.
        if (popup_AlertWindow != null) popup_AlertWindow.SetActive(false);
        if (txt_MainNoticeText != null) txt_MainNoticeText.gameObject.SetActive(false);
        if (panel_PartyEditView != null) panel_PartyEditView.SetActive(false);

        GameObject topMenuBar = GameObject.Find("Canvas")?.transform.Find("상단 단축바")?.gameObject;
        if (topMenuBar != null)
        {
            RectTransform topBarRect = topMenuBar.GetComponent<RectTransform>();
            if (topBarRect != null)
            {
                topBarRect.anchorMin = new Vector2(0f, 1f);
                topBarRect.anchorMax = new Vector2(1f, 1f);
                topBarRect.pivot = new Vector2(0.5f, 1f);
                topBarRect.anchoredPosition = new Vector2(0f, 0f);
                topBarRect.sizeDelta = new Vector2(0f, 80f);
            }

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
                    toggleRect.anchoredPosition = new Vector2(0f, 0f);
                    toggleRect.sizeDelta = new Vector2(200f, 80f);
                }

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
                        txtRect.sizeDelta = Vector2.zero;
                    }

                    Transform npcList = fastMoveTxt.Find("NPC 목록판");
                    if (npcList != null)
                    {
                        npcList.gameObject.SetActive(false);
                        RectTransform listRect = npcList.GetComponent<RectTransform>();
                        if (listRect != null)
                        {
                            listRect.anchorMin = new Vector2(0.5f, 1f);
                            listRect.anchorMax = new Vector2(0.5f, 1f);
                            listRect.pivot = new Vector2(0.5f, 1f);
                            listRect.anchoredPosition = new Vector2(0f, -80f);
                            listRect.sizeDelta = new Vector2(198.75f, 0f);
                        }
                    }
                }
            }
            topMenuBar.SetActive(false);
        }

        if (panel_PartyEditView != null)
        {
            panel_PartyEditView.transform.Find("Panel_ConfirmRemove2")?.gameObject.SetActive(false);
        }

        GameObject warehouseContainer = panel_PartyEditView?.transform.Find("캐릭터 창고창/WarehouseListContainer")?.gameObject;
        if (warehouseContainer != null)
        {
            RectTransform containerRect = warehouseContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                foreach (Transform child in warehouseContainer.transform)
                {
                    Destroy(child.gameObject);
                }
                containerRect.anchoredPosition = new Vector2(containerRect.anchoredPosition.x, 100f);
            }

            foreach (var member in allCharacters)
            {
                if (member == null) continue;
                GameObject cardObj = Instantiate(partyMemberPrefab, warehouseContainer.transform);
                PartyIcon partyIconScript = cardObj.GetComponent<PartyIcon>();

                if (partyIconScript != null)
                {
                    // 🌟 [칼조준 개조]: 여기는 창고/편집창 목록이므로 false를 명시해 HP바를 완전히 끕니다!
                    partyIconScript.Setup(member, false);
                }

                Button cardBtn = cardObj.GetComponentInChildren<Button>();
                if (cardBtn != null)
                {
                    cardBtn.onClick.RemoveAllListeners();
                    // 🌟 이 줄이 마을 전용 캐릭터 클릭 함수와 완벽하게 연결되었습니다!
                    // 212번 라인의 코드를 아래 한 줄로 정확하게 교체합니다.
                    cardBtn.onClick.AddListener(() => OnClickPartyEditwarehouseCharacter(member.id));
                }
            }

        }
    }
    void Update()
    {
        if (panel_Village != null && panel_Village.activeSelf && villageRectTransform != null)
        {
            // ---------------- [새로 추가된 키보드 이동 시스템] ----------------
            Vector2 keyboardInput = Vector2.zero;

            // 새로운 인풋 시스템(Input System) 패키지 기준 키 입력 감지
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed) { keyboardInput.y -= 1f; } // 이미지 반전 고려
                if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed) { keyboardInput.y += 1f; }
                if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed) { keyboardInput.x += 1f; }
                if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed) { keyboardInput.x -= 1f; }
            }

            // 키보드 입력이 있을 때만 마을 UI 좌표를 실시간으로 직접 이동시킵니다.
            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                float moveSpeed = 500f; // 키보드 이동 속도 (원하는 대로 변경 가능)
                Vector2 targetPos = villageRectTransform.anchoredPosition + (keyboardInput.normalized * moveSpeed * Time.deltaTime);
                
                // 283번째 줄에 이미 만들어두신 '화면 밖 이탈 방지 감금 함수'를 그대로 재활용하여 안전성 확보!
                villageRectTransform.anchoredPosition = RestrictPositionInsideVillage(targetPos);
            }
            // ------------------------------------------------------------------

            // 기존에 잘 돌아가던 마우스 드래그 관성 시스템 유지
            HandleVillageInertiaDrag();
        }
    }
    public void OnClickReturnToVillage()
    {
        // 1. 배틀 매니저 청소 (기존 코드 유지)
        if (PuzzleBattleManager.Instance != null)
        {
            PuzzleBattleManager.Instance.ResetBattleSystemForNextEntry();
        }

        // 2. 대소문자를 엄격하게 맞춘 원본 변수들을 사용하여 직접 켜고 끕니다.
        if (Panel_Village != null) Panel_Village.SetActive(true);
        if (Panel_INPuzzleBattle != null) Panel_INPuzzleBattle.SetActive(false);
        if (PartyListContainer != null) PartyListContainer.SetActive(false);

        Debug.Log("🏡 [성공] 마을 복귀 및 배틀 패널 완전 세탁 완료!");
    }

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

    public void OnClickToggleNPCListMenu()
    {
        if (obj_NPCListPanel != null)
        {
            bool isVisibleNow = obj_NPCListPanel.activeSelf;
            if (npcListToggleCoroutine != null) StopCoroutine(npcListToggleCoroutine);

            if (!isVisibleNow) npcListToggleCoroutine = StartCoroutine(AnimateNPCListDropdown(true));
            else npcListToggleCoroutine = StartCoroutine(AnimateNPCListDropdown(false));
        }
    }

    private System.Collections.IEnumerator AnimateNPCListDropdown(bool isOpen)
    {
        RectTransform panelRect = obj_NPCListPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        List<Transform> npcButtons = new List<Transform>();
        foreach (Transform child in obj_NPCListPanel.transform)
        {
            npcButtons.Add(child);
        }

        float buttonStepHeight = 80f;
        float delayTime = 0.05f;

        if (isOpen)
        {
            obj_NPCListPanel.SetActive(true);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
            foreach (var btn in npcButtons) btn.gameObject.SetActive(false);

            for (int i = 0; i < npcButtons.Count; i++)
            {
                if (npcButtons[i] == null) continue;
                npcButtons[i].gameObject.SetActive(true);
                float currentHeight = (i + 1) * buttonStepHeight;
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);
                yield return new WaitForSeconds(delayTime);
            }
        }
        else
        {
            for (int i = npcButtons.Count - 1; i >= 0; i--)
            {
                if (npcButtons[i] == null) continue;
                npcButtons[i].gameObject.SetActive(false);
                float currentHeight = i * buttonStepHeight;
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);
                yield return new WaitForSeconds(delayTime);
            }
            obj_NPCListPanel.SetActive(false);
        }
        npcListToggleCoroutine = null;
    }

    public void OnClickFastMoveToNPC(int npcIndex)
    {
        if (villageRectTransform == null) return;
        Vector2 targetPosition = Vector2.zero;

        switch (npcIndex)
        {
            case 0:
                // 촌장 (기존: -1468, 818 -> 반전: X는 플러스, Y는 마이너스)
                targetPosition = new Vector2(1468f, -818f);
                Debug.Log("[빠른이동] 촌장님 좌표로 비행을 개시합니다.");
                break;
            case 1:
                // 물약 (기존: -1522, -776 -> 반전: X는 플러스, Y는 플러스)
                targetPosition = new Vector2(1522f, 776f);
                Debug.Log("[빠른이동] 물약 상점의 진짜 물리 좌표로 정밀 추적 비행을 개시합니다.");
                break;
            case 2:
                // 대장간 (기존: 1534, -776 -> 반전: X는 마이너스, Y는 플러스)
                targetPosition = new Vector2(-1534f, 776f);
                Debug.Log("[빠른이동] 대장간 좌표로 비행을 개시합니다.");
                break;
            case 3:
                // 무한모드 (기존: 1012, 713 -> 반전: X는 마이너스, Y는 마이너스)
                targetPosition = new Vector2(-1012f, -713f);
                Debug.Log("[빠른이동] 무한모드 NPC 진짜 좌표로 정밀 추적 비행을 개시합니다.");
                break;
            case 4:
                // 일반스테이지 (기존: 1471, 713 -> 반전: X는 마이너스, Y는 마이너스)
                targetPosition = new Vector2(-1471f, -713f);
                Debug.Log("[빠른이동] 일반모드 NPC 진짜 좌표로 정밀 추적 비행을 개시합니다.");
                break;
        }

        villageVelocity = Vector2.zero;
        targetPosition = RestrictPositionInsideVillage(targetPosition);
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
            t = t * t * (3f - 2f * t);
            villageRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        villageRectTransform.anchoredPosition = targetPos;
        villageMoveCoroutine = null;

        if (obj_NPCListPanel != null) obj_NPCListPanel.SetActive(false);

        GameObject panelStageSelect = GameObject.Find("Canvas")?.transform.Find("Panel_StageSelect")?.gameObject;
        GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (panelStageSelect != null) panelStageSelect.SetActive(false);
        if (panelInfinite != null) panelInfinite.SetActive(false);

        switch (npcIndex)
        {
            case 3:
                OnClickEnterInfiniteStage();
                break;
            case 4:
                OnClickEnterNormalStage();
                break;
        }
    }
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
    public void OnClickCharacter(int charId)
    {
    selectedCharId = charId;
    CharacterData data = allCharacters.Find(x => x.id == charId);
    if (data == null) return;

    // 다른 방해 팝업들 즉시 물리적으로 전원 차단
    if (popup_CharacterInfo != null) popup_CharacterInfo.SetActive(false);
    if (popup_PartyEditCharacterInfo != null) popup_PartyEditCharacterInfo.SetActive(false);

    // 🌟 [교정 1] 철벽 만석 판정 : 파티원이 이미 5명이면 경고창을 띄우고 "즉시 함수 종료"
    if (partyMembers.Count >= 5)
    {
        if (popup_AlertWindow != null)
        {
            popup_AlertWindow.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeOutAlertWindow_SubCharSelect("정원이 가득찼습니다"));
        }
        return; // 🚀 여기서도 철저하게 리턴으로 잘라줍니다!
    }
        // [2] 중복 판정
    // 442번 라인 아래인 [2] 중복 판정 구간부터 아래 코드로 깔끔하게 변경해 줍니다.
    // [2] 중복 판정
    if (partyMembers.Contains(data))
    {
        if (popup_AlertWindow != null)
        {
            popup_AlertWindow.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeOutAlertWindow_SubCharSelect("이미 파티에 소속된 캐릭터입니다."));
        }
        return; // 🌟 void 함수이므로 false를 빼고 pure하게 return; 만 적어줍니다!
    }

    // 모든 검사를 통과했다면 기존 서브캐 정보창 정상 가동
    if (popup_AlertWindow != null) popup_AlertWindow.SetActive(false);
        if (popup_CharacterInfo != null)
    {
        // 하이어라키의 자식 경로를 스스로 추적하여 글씨 컴포넌트를 낚아챕니다.
        TMPro.TextMeshProUGUI nameTextComponent = popup_CharacterInfo.transform.Find("캐릭터 이름")?.GetComponent<TMPro.TextMeshProUGUI>();
        TMPro.TextMeshProUGUI infoTextComponent = popup_CharacterInfo.transform.Find("캐릭터 정보")?.GetComponent<TMPro.TextMeshProUGUI>();

        if (nameTextComponent != null) nameTextComponent.text = data.characterName;
        if (infoTextComponent != null) infoTextComponent.text = $"{data.description}\n(공격력: {data.attackPower} / 체력: {data.hp})";

        popup_CharacterInfo.SetActive(true);
        
        // 내부 확인3, 취소3 버튼 정렬 보장
        Transform confirm3Btn = popup_CharacterInfo.transform.Find("확인3");
        Transform cancel3Btn = popup_CharacterInfo.transform.Find("취소3");
        if (confirm3Btn != null) confirm3Btn.gameObject.SetActive(true);
        if (cancel3Btn != null) cancel3Btn.gameObject.SetActive(true);
    }
} // 🌟 OnClickCharacter 함수가 여기서 완전히 닫힙니다.

// 🌟[새로 추가] 파티에 추가 하시겠습니까? 팝업의 '취소4' 버튼 연동 함수
public void OnClickPartyAddConfirm_No()
{
    // 파티에 추가 하시겠습니까? 와 자식들 OFF
    if (popup_PartyAddConfirm != null) popup_PartyAddConfirm.SetActive(false);
}

    // ========================================================
    // 🏡 [여기에 새로 복사해서 붙여넣으세요!] 마을 편집창 창고 캐릭터 클릭 함수
    // ========================================================
// 📄 GameManager.cs - 481번 라인부터 함수 끝나는 중괄호까지 싹 덮어써 주세요!
public void OnClickPartyEditwarehouseCharacter(int charId)
{
    // 1. [광클 원천 차단막] 질문 팝업창이 이미 켜져 있다면 신호 즉시 파괴
    if (popup_PartyAddConfirm != null && popup_PartyAddConfirm.activeSelf) 
    {
        return; 
    }

    selectedCharId = charId;
    CharacterData data = allCharacters.Find(x => x.id == charId);
    if (data == null) return;

    // 2. [텍스트 출력] 캐릭터 이름과 설명 실시간 출력
    if (partyEditNameText != null) partyEditNameText.text = data.characterName;
    if (partyEditInfoText != null) 
    {
        partyEditInfoText.text = $"{data.description}\n(공격력: {data.attackPower} / 체력: {data.hp})";
    }

    // 🔥 3. [철벽 만석 판정]: 파티 멤버가 이미 5명이면 "정원이 가득찼습니다"를 띄우고 "즉시 종료"해야 합니다!
    if (partyMembers.Count >= 5)
    {
        if (popup_AlertWindow != null)
        {
            popup_AlertWindow.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeOutAlertWindow_PartyEdit("정원이 가득찼습니다"));
        }
        return; // 🌟 [가장 중요] 아래쪽 '추가하시겠습니까' 코드로 못 내려가게 여기서 완전히 도끼로 잘라버립니다!
    }

    // 4. [중복 판정]: 이미 파티에 있는 캐릭터라면 즉시 종료
    if (partyMembers.Contains(data))
    {
        if (popup_AlertWindow != null)
        {
            popup_AlertWindow.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeOutAlertWindow_PartyEdit("이미 파티에 소속된 캐릭터입니다."));
        }
        return; // 🌟 여기서도 철저하게 리턴으로 잘라줍니다!
    }

    // 5. [신규 가입 프리패스]: 만석도 아니고 중복도 아닐 때만 팝업창을 켭니다.
    if (popup_PartyAddConfirm != null)
    {
        popup_PartyAddConfirm.SetActive(true);
    }
}






    // ========================================================
    // 🛡️ [공용 마스터 판정기] 오직 AlertWindow 본체만 컨트롤하는 버전
    // ========================================================
    private bool CheckPartyAvailability(CharacterData data, bool isPartyEditView)
    {
        // 🚨 [조건 판정 전 필수 처리] 중복이나 만석 상황이라면, 화면의 다른 모든 정보/질문창은 예외 없이 즉시 완전 꺼버립니다!
        if (popup_CharacterInfo != null) popup_CharacterInfo.SetActive(false);
        if (popup_PartyEditCharacterInfo != null) popup_PartyEditCharacterInfo.SetActive(false);

        // 1️⃣ [최우선 순위] 정원 만석 검사 (메인 1명 + 서브 4명 = 총 5명 만석 판정)
        if (partyMembers.Count >= 5)
        {
            if (popup_AlertWindow != null)
            {
                TMPro.TextMeshProUGUI mainText = popup_AlertWindow.GetComponent<TMPro.TextMeshProUGUI>();
                if (mainText != null)
                {
                    
                    mainText.text = "정원이 가득찼습니다";
                }

                popup_AlertWindow.SetActive(true); 
                StopAllCoroutines(); 
                StartCoroutine(FadeOutAlertWindow());
            }
            return false; // 진행 차단
        }

        // 2️⃣ [차선 순위] 서브 캐릭터(ID 101번 이상) 중복 검사
        if (data.id >= 101 && partyMembers.Contains(data))
        {
            if (popup_AlertWindow != null)
            {
                TMPro.TextMeshProUGUI mainText = popup_AlertWindow.GetComponent<TMPro.TextMeshProUGUI>();
                if (mainText != null)
                {
                    
                    mainText.text = "이미 파티에 소속된 캐릭터입니다.";
                }

                popup_AlertWindow.SetActive(true); 
                StopAllCoroutines(); 
                StartCoroutine(FadeOutAlertWindow());
            }
            return false; // 진행 차단
        }

        return true; // 통과!
    }
//여기까지 1단락 








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

    public void OnClickConfirm3()
    {
        CharacterData data = allCharacters.Find(x => x.id == selectedCharId);
        if (data == null) return;

        // 🌟 [중복 체크 및 페이드아웃 알림창 가동 구간]
        if (partyMembers.Contains(data))
        {
            if (popup_AlertWindow != null)
            {
                // [수정] popup_AlertText 변수를 쓰지 않고, AlertWindow 본체에서 글자 컴포넌트를 직접 찾아 바꿉니다!
                TMPro.TextMeshProUGUI mainText = popup_AlertWindow.GetComponent<TMPro.TextMeshProUGUI>();
                if (mainText != null)
                {
                    mainText.color = new Color(1f, 1f, 1f, 1f); // 텍스트 컬러 흰색으로 변경
                    mainText.text = "이미 파티에 소속된 캐릭터입니다.";
                }

                popup_AlertWindow.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(FadeOutAlertWindow());
            }

            if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
            return;
        }

        // 🌟 [기존 시스템의 서브캐릭터 4인 제한 및 파티원 등록 구간]
        if (data.id >= 101 && GetSubCharacterCount() >= 4) return;

        partyMembers.Add(data);

        if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);

        // [기획 규칙] 메인 캐릭터(1~3) 등록 시 패널 전환
        if (data.id >= 1 && data.id <= 3)
        {
            if (panel_CharacterSelect) panel_CharacterSelect.SetActive(false);
            if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(true);
        }


        RefreshAllButtonsActiveState(); // 버튼들 숨김/등장 제어 (아래 참고)
        UpdatePartyUI(); // 파티창 새로고침
    }

    private int GetSubCharacterCount()
    {
        int count = 0;
        foreach (var member in partyMembers) { if (member.id >= 101) count++; }
        return count;
    }


    // 🌟 텍스트와 창이 중복 판정 시 즉시 켜지고, 눈 깜짝할 새 빠르게 사라지게 제어하는 함수
    // ========================================================
    // ⏳ [수정] 오직 최상위 popup_AlertWindow만 깔끔하게 페이드아웃 시키는 코루틴
    // ========================================================
    // ========================================================
    // ⏳ [오류 해결] System.Collections를 명시하여 제네릭 충돌을 수정한 코루틴
    // ========================================================
    private System.Collections.IEnumerator FadeOutAlertWindow_SubCharSelect(string message)
    {
        if (popup_AlertWindow == null) yield break;

        TMPro.TextMeshProUGUI mainText = popup_AlertWindow.GetComponent<TMPro.TextMeshProUGUI>();
        if (mainText == null) yield break;

        // 🎨 텍스트와 색상을 완벽한 흰색(오파시티 1)으로 즉시 강제 픽스!
        mainText.text = message;
        mainText.color = new Color(1f, 1f, 1f, 1f);

        yield return new WaitForSeconds(0.8f);

        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            mainText.color = new Color(1f, 1f, 1f, alpha); // 흰색 유지하며 투명해짐
            yield return null;
        }

        popup_AlertWindow.SetActive(false);
    }
        private System.Collections.IEnumerator FadeOutAlertWindow_PartyEdit(string message)
    {
        if (popup_AlertWindow == null) yield break;

        TMPro.TextMeshProUGUI mainText = popup_AlertWindow.GetComponent<TMPro.TextMeshProUGUI>();
        if (mainText == null) yield break;

        // 🎨 텍스트와 색상을 완벽한 검은색(오파시티 1)으로 즉시 강제 픽스!
        mainText.text = message;
        mainText.color = new Color(1f, 1f, 1f, 1f);

        yield return new WaitForSeconds(0.8f);

        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            mainText.color = new Color(1f, 1f, 1f, alpha); // 검은색 유지하며 투명해짐
            yield return null;
        }

        popup_AlertWindow.SetActive(false);
    }
            // ========================================================
    // 징검다리 코루틴: 옛날 이름으로 호출하는 코드(539, 558, 615 라인)를 
    // 안전하게 캐릭터 선택창용(흰색 글씨) 코루틴으로 연결해 주는 구원 투수 코드입니다!
    // ========================================================
    private System.Collections.IEnumerator FadeOutAlertWindow()
    {
        // 옛날 이름이 호출되면 자동으로 흰색 글씨 코루틴을 대신 실행해 줍니다.
        // 만약 이 함수들이 호출되는 곳이 서브캐 선택창이 맞다면 이대로 두시면 완벽합니다!
        TMPro.TextMeshProUGUI mainText = popup_AlertWindow != null ? popup_AlertWindow.GetComponent<TMPro.TextMeshProUGUI>() : null;
        string currentMsg = mainText != null ? mainText.text : "정원이 가득찼습니다";
        
        yield return StartCoroutine(FadeOutAlertWindow_SubCharSelect(currentMsg));
    }

    





    // 🌟 서브캐 버튼 목록의 UI를 데이터와 매칭하여 실시간 새로고침 (새로 추가)
    private void RefreshSubCharacterButtonsUI()
    {
        if (subCharacterCards == null) return;
        for (int i = 0; i < subCharacterCards.Count; i++)
        {
            if (i < allCharacters.Count)
            {
                subCharacterCards[i].gameObject.SetActive(true);
                subCharacterCards[i].SetupCard(allCharacters[i]); // 데이터 바인딩
            }
            else
            {
                subCharacterCards[i].gameObject.SetActive(false); // 남는 버튼 숨김
            }
        }
    }

    public void RefreshAllButtonsActiveState()
    {
        // 메인 캐릭터 버튼들 ON/OFF 제어 (ID: 1, 2, 3)
        for (int i = 0; i < mainCharacterCards.Count; i++)
        {
            if (mainCharacterCards[i] == null) continue;
            int targetId = i + 1;
            bool isAlreadyInParty = partyMembers.Exists(x => x.id == targetId);
            mainCharacterCards[i].gameObject.SetActive(!isAlreadyInParty);
        }

        // 서브 캐릭터 버튼들 ON/OFF 제어 (ID: 101 ~ 106)
        for (int i = 0; i < subCharacterCards.Count; i++)
        {
            if (subCharacterCards[i] == null) continue;
            int targetId = 101 + i;
            bool isAlreadyInParty = partyMembers.Exists(x => x.id == targetId);
            subCharacterCards[i].gameObject.SetActive(!isAlreadyInParty);
        }
    }




    public void OnClickCancel3()
    {
        if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);
    }

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

public void OnClickEnterNormalStage()
{
    Debug.Log("[마을] 일반 모드 클릭 -> Panel_StageSelect와 자식들 ON");
    stageMode = 1;
    GameObject panelStageSelect = GameObject.Find("Canvas")?.transform.Find("Panel_StageSelect")?.gameObject;
    if (panelStageSelect != null) panelStageSelect.SetActive(true);

    // 🌟 [교체식 추가] 버튼을 눌러 스테이지 진입 패널을 켜는 순간, 체크박스를 강제로 ON 합니다.
    PuzzleBattleManager battleMgr = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle")?.GetComponent<PuzzleBattleManager>();
    if (battleMgr != null && battleMgr.btn_StartTouchTrigger_Direct != null)
    {
        battleMgr.btn_StartTouchTrigger_Direct.SetActive(true);
    }
}

    public void OnClickNormalStageBackButton()
    {
        Debug.Log("[일반목록] 뒤로가기 클릭 -> 마을로 이동 및 Panel_StageSelect OFF");
        GameObject panelStageSelect = GameObject.Find("Canvas")?.transform.Find("Panel_StageSelect")?.gameObject;
        if (panelStageSelect != null) panelStageSelect.SetActive(false);
        if (panel_Village) panel_Village.SetActive(true);
    }

public void OnClickEnterInfiniteStage()
{
    Debug.Log("[마을] 무한 모드 클릭 -> Panel_InfiniteStage와 자식들 ON");
    stageMode = 2;
    GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
    if (panelInfinite != null) panelInfinite.SetActive(true);

    // 🌟 [교체식 추가] 무한모드 버튼을 눌렀을 때도 똑같이 체크박스를 강제로 ON 합니다.
    PuzzleBattleManager battleMgr = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle")?.GetComponent<PuzzleBattleManager>();
    if (battleMgr != null && battleMgr.btn_StartTouchTrigger_Direct != null)
    {
        battleMgr.btn_StartTouchTrigger_Direct.SetActive(true);
    }
}

    public void OnClickInfiniteStageBackButton()
    {
        Debug.Log("[무한목록] 뒤로가기 클릭 -> 마을로 이동 및 Panel_InfiniteStage OFF");
        GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (panelInfinite != null) panelInfinite.SetActive(false);
        if (panel_Village) panel_Village.SetActive(true);
    }
// [기존 함수 보완] 파티창의 아이콘을 클릭했을 때 작동
public void OnClickPartyMemberIcon(CharacterData clickedCharacter)
{
    if (clickedCharacter == null) return;
    pendingRemoveCharacter = clickedCharacter; // 제거 대기 캐릭터 임시 보관

    // 캐릭터 정보도 함께 출력해 줍니다.
    if (partyEditNameText != null) partyEditNameText.text = clickedCharacter.characterName;
if (partyEditInfoText != null) partyEditInfoText.text = $"{clickedCharacter.description}\n(공격력: {clickedCharacter.attackPower} / 체력: {clickedCharacter.hp})";
    // 현재 파티 편집창이 켜져 있는 상태라면 신형 제거 팝업(Panel_ConfirmRemove2)을 ON
    if (panel_PartyEditView != null && panel_PartyEditView.activeSelf)
    {
        GameObject confirmPopup2 = panel_PartyEditView.transform.Find("Panel_ConfirmRemove2")?.gameObject;
        if (confirmPopup2 != null)
        {
            confirmPopup2.SetActive(true); // Panel_ConfirmRemove2 와 자식들 ON
            Debug.Log($"[신형 팝업] {clickedCharacter.characterName} 제거 확인창 ON");
        }
        return;
    }
    if (panel_ConfirmRemove != null)
    {
        panel_ConfirmRemove.SetActive(true); // 🌟 기존 기능 그대로 유지

        // 🚀 [여기만 추가] 부모 패널 안에 있는 모든 자식 오브젝트들을 확실하게 ON 시킵니다.
        foreach (Transform child in panel_ConfirmRemove.transform)
        {
            child.gameObject.SetActive(true);
        }
    }
}

// [새로 추가] Panel_ConfirmRemove2 내부의 '예' 버튼과 연동할 함수
public void OnClickPartyEditRemoveYes()
{
    if (pendingRemoveCharacter != null)
    {
        Debug.Log($"[파티 편집 제거] {pendingRemoveCharacter.characterName} 파티에서 제외");
        partyMembers.Remove(pendingRemoveCharacter);
        pendingRemoveCharacter = null;
    }

    // 1. 파티창 UI 새로고침 (제거된 데이터가 반영되어 자동으로 창고나 목록 정렬 상태 유지)
    UpdatePartyUI();
    RefreshAllButtonsActiveState();

    // 2. Panel_ConfirmRemove2와 자식들 OFF
    GameObject confirmPopup2 = panel_PartyEditView?.transform.Find("Panel_ConfirmRemove2")?.gameObject;
    if (confirmPopup2 != null) confirmPopup2.SetActive(false);
}

// [새로 추가] Panel_ConfirmRemove2 내부의 '아니오' 버튼과 연동할 함수
public void OnClickPartyEditRemoveNo()
{
    pendingRemoveCharacter = null;

    // Panel_ConfirmRemove2와 자식들 OFF
    GameObject confirmPopup2 = panel_PartyEditView?.transform.Find("Panel_ConfirmRemove2")?.gameObject;
    if (confirmPopup2 != null) confirmPopup2.SetActive(false);
}



    public void OnClickRemoveYes()
    {
        // 🌟 기존에 이미 구현되어 있던 pendingRemoveCharacter를 그대로 활용합니다.
        CharacterData data = pendingRemoveCharacter;
        if (data != null)
        {
            partyMembers.Remove(data);

            // [기획 규칙] 메인 캐릭터가 제거 되었을 시 메인캐창으로 강제 복귀
            if (data.id >= 1 && data.id <= 3)
            {
                if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(false);
                if (panel_CharacterSelect) panel_CharacterSelect.SetActive(true);
            }
        }

        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);

        // 🌟 602줄의 규칙에 맞춰 제거 처리가 끝났으므로 null로 초기화합니다.
        pendingRemoveCharacter = null;

        RefreshAllButtonsActiveState();
        UpdatePartyUI();
    }


    public void OnClickRemoveNo()
    {
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);
        pendingRemoveCharacter = null;
    }

    public void OnClickBackButton()
    {
        Debug.Log("[뒤로가기] 클릭 -> 메인 캐릭터 선택화면으로 이동");

        // 1. 파티에서 메인 캐릭터(ID: 1, 2, 3)를 정확하게 찾아내기
        CharacterData mainChar = partyMembers.Find(x => x.id == 1 || x.id == 2 || x.id == 3);
        if (mainChar != null)
        {
            // 2. 파티 리스트에서만 안전하게 제거 (allCharacters.Add는 데이터가 꼬이므로 삭제합니다)
            partyMembers.Remove(mainChar);

            // 3. 파티창 UI 실시간 새로고침
            UpdatePartyUI();
        }

        // 4. 기획 흐름대로 각 화면 패널 ON/OFF 제어
        if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(false);
        if (panel_CharacterSelect) panel_CharacterSelect.SetActive(true);
        if (popup_CharacterInfo) popup_CharacterInfo.SetActive(false);

        // 🌟 [핵심] 파티에서 빠진 메인 캐릭터의 나열되었던 버튼을 다시 ON 시켜주는 코드 추가!
        RefreshAllButtonsActiveState();
    }


    public void OnClickStartAdventureButton()
    {
        Debug.Log("[출발하기] 인원 및 조건 검사");
        bool hasMain = partyMembers.Exists(x => x.id == 1 || x.id == 2 || x.id == 3);
        int subCount = partyMembers.Count - (hasMain ? 1 : 0);

        if (hasMain && subCount >= 4)
        {
            Debug.Log("[출발 완료] 5인 조건 완벽 충족 -> 출발하기 팝업 출력");
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
            else if (subCount < 4) ShowScreenNotice("서브 캐릭터가 4명이 되도록 서브 캐릭터를 선택해주세요.");
        }
    }

    public void OnClickConfirm5()
    {
        Debug.Log("[출발 승인] 마을로 최종 진입. 모든 팝업 OFF");
        if (panel_ConfirmRemove) panel_ConfirmRemove.SetActive(false);
        if (obj_StartWindow) obj_StartWindow.SetActive(false);
        if (btn_Confirm5) btn_Confirm5.SetActive(false);
        if (btn_Cancel5) btn_Cancel5.SetActive(false);
        if (panel_SubCharacterSelect) panel_SubCharacterSelect.SetActive(false);
        if (panel_CharacterSelect) panel_CharacterSelect.SetActive(false);

        if (obj_NPCListPanel != null && obj_NPCListPanel.transform.parent != null)
        {
            obj_NPCListPanel.transform.parent.gameObject.SetActive(true);
        }
        if (partyListContainer) partyListContainer.SetActive(false);

        GameObject panelStageSelect = GameObject.Find("Canvas")?.transform.Find("Panel_StageSelect")?.gameObject;
        if (panelStageSelect != null) panelStageSelect.SetActive(false);
        GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (panelInfinite != null) panelInfinite.SetActive(false);
        if (panel_Village) panel_Village.SetActive(true);

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

    public void OnClickOpenPartyEditView()
    {
        Debug.Log("[스테이지 UI] 파티편집 클릭 -> Panel_PartyEditView 및 자식들 ON");
        if (panel_PartyEditView != null) panel_PartyEditView.SetActive(true);
        if (panel_TopBar != null) panel_TopBar.SetActive(true);
        if (partyListContainer) partyListContainer.SetActive(true);
        if (panel_PartyEditView != null)
        if (popup_PartyAddConfirm != null) popup_PartyAddConfirm.SetActive(false);
{
    Transform confirmPopup2 = panel_PartyEditView.transform.Find("Panel_ConfirmRemove2");
    if (confirmPopup2 != null) confirmPopup2.gameObject.SetActive(false);
}

        UpdatePartyUI();
    }

    public void OnClickClosePartyEditView()
    {
        Debug.Log("[파티편집] 뒤로가기 클릭 -> Panel_PartyEditView 및 자식들 OFF");
        if (panel_PartyEditView != null) panel_PartyEditView.SetActive(false);
        if (PartyListContainer != null) PartyListContainer.SetActive(false);
        if (panel_TopBar != null) panel_TopBar.SetActive(true);
    }

    public void ClosePartyEditView()
    {
        if (panel_PartyEditView != null) panel_PartyEditView.SetActive(false);
        if (PartyListContainer != null) PartyListContainer.SetActive(false);
    }

public void OnClickWarehouseCharacterCard(CharacterData targetData)
{
    if (targetData == null) return;

    // 1. [캐릭터 이름 및 정보 출력] 클릭 즉시 좌측이나 하단 텍스트에 스탯 출력
    if (partyEditNameText != null) partyEditNameText.text = targetData.characterName;
if (partyEditInfoText != null) partyEditInfoText.text = $"{targetData.description}\n(공격력: {targetData.attackPower} / 체력: {targetData.hp})";
    // 2. [판정] 이미 구현된 마스터 판정기 활용 (만석 및 중복 검사)
    // 파티 편집창(isPartyEditView = true) 기준으로 검사 진행
    if (!CheckPartyAvailability(targetData, true)) 
    {
        return; // 만석이거나 중복이면 팝업 경고가 뜨고 아래 로직(파티 추가)은 차단됨
    }

    // 3. [파티에 추가] 판정 통과 시 파티 리스트에 등록하고 UI 갱신
    Debug. Log($"[파티 편집] 파티 명단에 {targetData.characterName} 추가 완료");
    partyMembers.Add(targetData);
    
    // 4. [파티창으로 이동] 전체 파티 UI를 새로고침하여 파티창 컨테이너에 배치시킴
    UpdatePartyUI();
}

    private string CalculateCurrentSynergyText()
    {
        if (partyMembers == null || partyMembers.Count == 0)
        {
            return "현재 구성된 파티가 없어 활성화된 시너지가 없습니다.";
        }

        Dictionary<string, int> synergyCounts = new Dictionary<string, int>();
        foreach (var member in partyMembers)
        {
            if (member == null || string.IsNullOrEmpty(member.synergyName)) continue;
            if (synergyCounts.ContainsKey(member.synergyName)) synergyCounts[member.synergyName]++;
            else synergyCounts[member.synergyName] = 1;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine(" <b><color=#FFE600>[실시간 파티 조합 시너지 현황] 📜</color></b>\n");
        bool hasAnySynergy = false;

        foreach (var kvp in synergyCounts)
        {
            string sName = kvp.Key;
            int count = kvp.Value;
            sb.AppendLine($"▶ <b>{sName}</b> (참여 파티원 수: {count}명)");
            hasAnySynergy = true;
        }

        if (!hasAnySynergy) return "현재 결성된 고유 조합 시너지가 없습니다.";
        return sb.ToString();
    }
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

    public void DamageEnemy(int amount)
    {
        PuzzleBattleManager puzzleManager = GetComponent<PuzzleBattleManager>();
        if (puzzleManager != null && puzzleManager.enemyHPBar != null)
        {
            float currentHP = puzzleManager.enemyHPBar.value;
            puzzleManager.enemyHPBar.value = Mathf.Max(0f, currentHP - (amount / 100f));
            Debug.Log($"[몬스터 타격!] 대미지 {amount} 적중! 남은 HP 게이지: {puzzleManager.enemyHPBar.value}");

            if (puzzleManager.enemyHPBar.value <= 0f)
            {
                Debug.Log("[전투 승리 선포] 무한 모드 몬스터 격파! 보상 창 전개.");
                Board boardComponent = puzzleManager.panel_PuzzleBattle?.GetComponentInChildren<Board>();
                if (boardComponent != null) boardComponent.OnClickRewardConfirmButton();
            }
        }
        else
        {
            Debug.LogWarning("[대미지 통신 지연] 유니티 인스펙터에 몬스터 HP_Bar 미연결: " + amount);
        }
    }

    public void UpdatePartyUI()
    {
        GameObject topMenuBar = GameObject.Find("Canvas")?.transform.Find("상단 단축바")?.gameObject;
        if (topMenuBar != null && panel_Village != null)
        {
            GameObject mainCharSelect = GameObject.Find("Canvas")?.transform.Find("Panel_CharacterSelect")?.gameObject;
            GameObject subCharSelect = GameObject.Find("Canvas")?.transform.Find("Panel_SubCharacterSelect")?.gameObject;
            bool isSelectingCharacter = (mainCharSelect != null && mainCharSelect.activeSelf) || (subCharSelect != null && subCharSelect.activeSelf);
            topMenuBar.SetActive(!isSelectingCharacter);
        }

        if (partyListContainer == null) return;

        partyMembers.Sort((x, y) => x.id.CompareTo(y.id));

        foreach (Transform child in partyListContainer.transform) Destroy(child.gameObject);

        GameObject editPartyContainer = panel_PartyEditView?.transform.Find("PartyListContainer_Edit")?.gameObject;
        if (editPartyContainer != null)
        {
            foreach (Transform child in editPartyContainer.transform) Destroy(child.gameObject);
        }

        foreach (var member in partyMembers)
        {
            GameObject iconVillage = Instantiate(partyMemberPrefab, partyListContainer.transform);
            PartyIcon partyIconScriptVillage = iconVillage.GetComponent<PartyIcon>();
            if (partyIconScriptVillage != null) partyIconScriptVillage.Setup(member);

            Button btnVillage = iconVillage.GetComponentInChildren<Button>();
            if (btnVillage != null)
            {
                CharacterData cachedMemberVillage = member; // 데이터가 밀리지 않게 가둡니다.
                btnVillage.onClick.AddListener(() => OnClickPartyMemberIcon(cachedMemberVillage));
            }

            if (editPartyContainer != null)
            {
                GameObject iconEdit = Instantiate(partyMemberPrefab, editPartyContainer.transform);
                PartyIcon partyIconScriptEdit = iconEdit.GetComponent<PartyIcon>();
                if (partyIconScriptEdit != null) partyIconScriptEdit.Setup(member, false);

                PuzzleBattleManager puzzleManager = GetComponent<PuzzleBattleManager>();
                CharacterCard cardComponentEdit = iconEdit.GetComponent<CharacterCard>();

                if (cardComponentEdit != null && puzzleManager != null && puzzleManager.liveCards != null)
                {
                    // 🌟 여기서는 Clear()를 쓰지 않고, 그냥 차곡차곡 순서대로 5명을 장부에 더하기만 합니다!
                    puzzleManager.liveCards.Add(cardComponentEdit);
                }


                Button btnEdit = iconEdit.GetComponentInChildren<Button>();
                if (btnEdit != null)
                {
                    CharacterData cachedMemberEdit = member; // 데이터가 밀리지 않게 가둡니다.
                    btnEdit.onClick.AddListener(() => OnClickPartyMemberIcon(cachedMemberEdit));
                }
            }
        }
        RefreshAllCharacterMenuCards();
    }

    public void OnClickEditRemoveYes()
    {
        if (pendingRemoveCharacter == null) return;
        CharacterData target = pendingRemoveCharacter;

        if (partyMembers.Contains(target)) partyMembers.Remove(target);
        if (!allCharacters.Contains(target)) allCharacters.Add(target);

        UpdatePartyUI();
        RefreshAllCharacterMenuCards();

        if (panel_PartyEditView != null)
        {
            GameObject confirmPopup2 = panel_PartyEditView.transform.Find("Panel_ConfirmRemove2")?.gameObject;
            if (confirmPopup2 != null) confirmPopup2.SetActive(false);
        }
        pendingRemoveCharacter = null;
        Debug.Log("[2호기 작동 완료] 파티원 유지 및 팝업창 단독 소멸 완료!");
    }

    public void OnClickInfiniteStage()
    {
        if (panel_Village != null) panel_Village.SetActive(false);
        GameObject infStagePanel = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (infStagePanel != null)
        {
            infStagePanel.SetActive(true);
            stageMode = 2;
            Debug.Log("[1단계 성공] 대기화면(Panel_InfiniteStage) 통합 ON 완공!");
        }
        else
        {
            Debug.LogError("[1단계 오류] 'Panel_InfiniteStage' 오브젝트를 찾지 못했습니다!");
        }
    }

    public void OnClickInfiniteStageModeStart()
    {
        GameObject infStagePanel = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (infStagePanel != null) infStagePanel.SetActive(false);

        GameObject realPuzzlePanel = GameObject.Find("Canvas")?.transform.Find("Panel_INPuzzleBattle")?.gameObject;
        if (realPuzzlePanel != null)
        {
            realPuzzlePanel.SetActive(true);
            Transform puzzleBoardTrans = realPuzzlePanel.transform.Find("PuzzleBoard");
            if (puzzleBoardTrans != null)
            {
                puzzleBoardTrans.gameObject.SetActive(true);
                Board boardComponent = puzzleBoardTrans.GetComponent<Board>();
                if (boardComponent != null)
                {
                    boardComponent.SetupStage(6, 6);
                    boardComponent.CreateBoard();
                    InitializeDynamicBattlePartyUI();
                    Debug.Log("[퍼즐 소환 완료] 6x6 격자 보석 블록 출력 완료.");
                }
            }
        }
        else
        {
            Debug.LogError("[2단계 오류] 'Panel_INPuzzleBattle' 오브젝트를 찾지 못했습니다!");
        }
    }

    public void InitializeDynamicBattlePartyUI()
    {
        Debug.Log("[전투UI 개시] 배틀 전용 파티창 소환 시작.");

        if (battlePartyContainer != null)
        {
            // 🌟 [버그 박멸 핵심 공식]: 새로운 배틀 카드를 찍어내기 전에, 
            // 기존 컨테이너 자식으로 남아있던 모든 구형 카드 잔상들을 완벽하게 싹 청소합니다!
            foreach (Transform child in battlePartyContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("[배틀 UI 경고] battlePartyContainer 장치가 미연결 상태입니다.");
            return;
        }

        // 🌟 청소가 끝난 빈 방에 정확히 현재 파티원(최대 5명) 수만큼만 새로 복사합니다!
        foreach (var member in partyMembers)
        {
            if (member == null) continue;
            GameObject battleIconObj = Instantiate(partyMemberPrefab, battlePartyContainer.transform);
            PartyIcon partyIconScript = battleIconObj.GetComponent<PartyIcon>();

            if (partyIconScript != null)
            {
                // 아까 구현해둔 공식! 배틀 모드이므로 true를 주입해 HP바 인스펙터 체크박스를 ON 합니다!
                partyIconScript.Setup(member, true);
            }

            Button btn = battleIconObj.GetComponentInChildren<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
        }
        Debug.Log("[배틀 UI 완공] 잔상 청소 후 HP바 장착 영웅 배치 완료.");
    }




    public void EnterBattleStage()
    {
        if (QuickMoveButton != null)
        {
            QuickMoveButton.SetActive(false);
            Debug.Log(" [배틀 시작] 빠른 이동 버튼 OFF.");
        }
    }

    public void ExitBattleStage()
    {
        if (QuickMoveButton != null)
        {
            QuickMoveButton.SetActive(true);
            Debug.Log(" [배틀 종료] 빠른 이동 버튼 ON 복구.");
        }
    }

    // 빈 껍데기 함수 정의 (에러 방지용)
    private void RefreshAllCharacterMenuCards()
    {
        RefreshSubCharacterButtonsUI(); // 파티 해제(제거) 시에도 UI 갱신
    }
    // ========================================================
    // 🏡 [버그 완전 소멸] 마을 편집창 [예] 버튼을 눌러 최종 파티원으로 등록하는 함수
    // ========================================================
    public void OnClickPartyEditConfirm()
    {
        CharacterData data = allCharacters.Find(x => x.id == selectedCharId);
        if (data == null) return;

        // [최종 철벽] 예 버튼을 누르는 순간 만석이거나 중복인지 최종 필터링
        if (partyMembers.Count >= 5 || (data.id >= 101 && partyMembers.Contains(data)))
        {
            if (popup_PartyEditCharacterInfo != null) popup_PartyEditCharacterInfo.SetActive(false);
            if (popup_AlertWindow != null)
            {
                popup_AlertWindow.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(FadeOutAlertWindow_PartyEdit(partyMembers.Count >= 5 ? "정원이 가득찼습니다" : "이미 파티에 소속된 캐릭터입니다."));
            }
            return;
        }

        // 정상 데이터 추가 처리
        partyMembers.Add(data);

        if (popup_PartyEditCharacterInfo != null) popup_PartyEditCharacterInfo.SetActive(false);

        // 💡 [이 자리에 기존 UI 갱신 코드를 넣으세요!] 
        // 예: UpdatePartyUI(); 등 하단 슬롯 프리팹을 새로고침하는 함수 이름 입력
    }
    // 🌟[새로 추가] 파티에 추가 하시겠습니까? 팝업의 '확인4' 버튼 연동 함수
public void OnClickPartyAddConfirm_Yes()
{
    CharacterData data = allCharacters.Find(x => x.id == selectedCharId);
    if (data != null)
    {
        Debug.Log($"[파티 편집 가입] {data.characterName} 파티 추가 완료!");
        partyMembers.Add(data); // 파티 리스트에 데이터 추가
    }

    // 1. 파티창 UI 실시간 새로고침 (파티창 컨테이너로 캐릭터 이동 처리)
    UpdatePartyUI();
    RefreshAllButtonsActiveState();

    // 2. 파티에 추가 하시겠습니까? 와 자식들 OFF
    if (popup_PartyAddConfirm != null)
    {
        popup_PartyAddConfirm.SetActive(false);
    }}

}
