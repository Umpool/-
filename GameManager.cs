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
    public GameObject panel_PartyEditView; // [0-1번 기획]: 새로 배치할 대형 파티 편집 화면 패널

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
                    cardBtn.onClick.AddListener(() => OnClickWarehouseCharacterCard(member));
                }
            }

        }
    }
    void Update()
    {
        if (panel_Village != null && panel_Village.activeSelf && villageRectTransform != null)
        {
            HandleVillageInertiaDrag();
        }
    }

    public void GoToVillage()
    {
        if (Panel_Village != null) Panel_Village.SetActive(true);
        if (PartyListContainer != null)
        {
            PartyListContainer.SetActive(false);
            Debug.Log("[성공] 마을 도착! PartyListContainer를 깔끔하게 1회 OFF 했습니다.");
        }
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
                targetPosition = new Vector2(800f, -500f);
                Debug.Log("[빠른이동] 촌장님 좌표로 비행을 개시합니다.");
                break;
            case 1:
                targetPosition = new Vector2(1522f, 776f);
                Debug.Log("[빠른이동] 물약 상점의 진짜 물리 좌표로 정밀 추적 비행을 개시합니다.");
                break;
            case 2:
                targetPosition = new Vector2(-800f, 500f);
                Debug.Log("[빠른이동] 대장간 좌표로 비행을 개시합니다.");
                break;
            case 3:
                targetPosition = new Vector2(-1012f, -713f);
                Debug.Log("[빠른이동] 무한모드 NPC 진짜 좌표로 정밀 추적 비행을 개시합니다.");
                break;
            case 4:
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
    }

    public void OnClickInfiniteStageBackButton()
    {
        Debug.Log("[무한목록] 뒤로가기 클릭 -> 마을로 이동 및 Panel_InfiniteStage OFF");
        GameObject panelInfinite = GameObject.Find("Canvas")?.transform.Find("Panel_InfiniteStage")?.gameObject;
        if (panelInfinite != null) panelInfinite.SetActive(false);
        if (panel_Village) panel_Village.SetActive(true);
    }
    public void OnClickPartyMemberIcon(CharacterData clickedCharacter)
    {
        if (clickedCharacter == null) return;
        pendingRemoveCharacter = clickedCharacter;

        if (panel_PartyEditView != null && panel_PartyEditView.activeSelf)
        {
            GameObject confirmPopup2 = panel_PartyEditView.transform.Find("Panel_ConfirmRemove2")?.gameObject;
            if (confirmPopup2 != null)
            {
                confirmPopup2.SetActive(true);
                Debug.Log($"[신형 팝업 연동] {clickedCharacter.characterName} 제거 확인창 소환 완료!");
            }
            return;
        }
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
        if (partyMembers.Count >= 5)
        {
            ShowScreenNotice("파티는 최대 5명까지만 구성할 수 있습니다.");
            return;
        }
        Debug.Log($"[창고 가입] 파티 명단에 {targetData.characterName} 추가");
        allCharacters.Remove(targetData);
        partyMembers.Add(targetData);
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

        partyMembers.Sort((x, y) =>
        {
            bool xIsMain = (x.id == 1 || x.id == 2 || x.id == 3);
            bool yIsMain = (y.id == 1 || y.id == 2 || y.id == 3);
            if (xIsMain && !yIsMain) return -1;
            if (!xIsMain && yIsMain) return 1;
            return 0;
        });

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
                btnVillage.onClick.RemoveAllListeners();
                btnVillage.onClick.AddListener(() => OnClickPartyMemberIcon(member));
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
                    btnEdit.onClick.RemoveAllListeners();
                    btnEdit.onClick.AddListener(() => OnClickPartyMemberIcon(member));
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
            GameObject battleIconObj = Instantiate(partyMemberPrefab, battlePartyContainer.transform);
            CharacterCard cardComponent = battleIconObj.GetComponent<CharacterCard>();

            if (cardComponent != null)
            {
                // 🌟 핵심: 태어나자마자 전투 매니저 장부에 이름 올리기!
                PuzzleBattleManager puzzleManager = GetComponent<PuzzleBattleManager>();
                if (puzzleManager != null)
                {
                    puzzleManager.liveCards.Add(cardComponent);
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
    private void RefreshAllCharacterMenuCards() { }
} // GameManager 클래스 마감선
