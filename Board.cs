using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 🎯 [오류 해결]: PointerEventData를 컴퓨터가 인식할 수 있게 문을 열어줍니다!


// 유니티에게 이 스크립트가 마우스 클릭, 드래그, 떼기 신호를 직접 수신하겠다고 선언합니다.
// 이제 마우스 입력은 Update 리뉴얼 엔진이 전담하므로, 뒤에 붙은 인터페이스 단어들을 전부 떼어냅니다.
public class Board : MonoBehaviour
{
    // ---- [추가] 옛날 코드에서 가져온 Block 선택 및 되돌리기용 변수 ----
    
    private int prevFirstX, prevFirstY;   // 되돌리기를 위한 첫 번째 Block의 이전 좌표
    private int prevSecondX, prevSecondY; // 되돌리기를 위한 두 번째 Block의 이전 좌표
    
    // -----------------------------------------------------------------


    [Header("ㅡ 보드 기본 설정 ㅡ")]
public int width = 6; // 가로 6칸 고정
public int height = 6; // 세로 6칸 고정
private float blockSpacing = 105f; // [보완] Block이 겹쳐서 오작동하는걸 막는 안전 간격
    public float blockPadding = 0.02f; 

    [Header("ㅡ Block 원본 프리팩 (6색) ㅡ")]
    public GameObject[] blockPrefabs; 

    [Header("ㅡ 게임 상태 장부 ㅡ")]
    [System.NonSerialized] public GameObject[,] allBlocks; // 6x6 보드판 실제 배열 장부
    public bool isProcessing = false; // Block이 움직이거나 터지는 중인지 체크 (조작 잠금)
    public Transform dragLayerParent; 

    [Header("ㅡ 턴 및 콤보 데이터 ㅡ")]
    public int currentTurn = 0;
    public int comboCount = 0;
        // ---- [복붙 시작] 옛날 코드에서 이사 온 안전 제어 스위치 ----
    private bool isSwapping = false;   // Block 자리가 교체 중일 때 조작을 잠그는 스위치
    private bool isMatching = false;   // Block이 터지고 채워지는 중일 때 조작을 잠그는 스위치
    private bool isUserTurn = false;   // 유저가 직접 움직였을 때만 턴을 깎기 위한 판별 스위치
    // ---- [복붙 끝] -----------------------------------------


    [SerializeField] private Transform puzzleBoard; // <-- 이 줄을 변수 모여있는 곳에 추가


    [Header("ㅡ 이사 온 부드러운 콤보 시스템 ㅡ")]
    public int currentCombo = 0;             
    public float comboDamageMultiplier = 0.1f; 
    public TMPro.TMP_Text comboText; 
    private Coroutine comboFadeCoroutine;

    [Header("ㅡ 이사 온 초정밀 타이머 UI ㅡ")]
    public TMPro.TextMeshProUGUI TimeText; 

    [Header("ㅡ 이사 온 시작 팝업창 UI ㅡ")]
    public GameObject startTouchTriggerPanel; 

    [Header("ㅡ 진입 및 타이머 설정 ㅡ")]
    public float limitTime = 180f; // 무한모드 3분(180초) 제한시간
    public bool isGameActive = false;

    [Header("ㅡ 이사 온 게임오버 팝업 UI ㅡ")]
    public GameObject gameOverTxtPanel; // 다이렉트 주머니!



    private float[] comboDamageMultipliers = new float[] { 1.0f, 1.2f, 1.5f, 1.8f, 2.0f, 2.5f };

    private void Awake()
    {
        allBlocks = new GameObject[width, height];
    }

    public string GetBlockColor(GameObject block)
    {
        if (block == null) return "None";
        
        string blockName = block.name.ToLower();
        
        if (blockName.Contains("red"))     return "Red";
        if (blockName.Contains("yellow"))  return "Yellow";
        if (blockName.Contains("green"))   return "Green";
        if (blockName.Contains("blue"))    return "Blue";
        if (blockName.Contains("purple"))  return "Purple";
        if (blockName.Contains("black"))   return "Black";
        
        return "Unknown";
    }

    public float GetComboMultiplier()
    {
        int index = Mathf.Clamp(comboCount, 0, comboDamageMultipliers.Length - 1);
        return comboDamageMultipliers[index];
    }
    // 💡 [2단계 핵심]: 게임 시작 시 3매치가 미리 터지는 것을 방지하는 안전 생성 엔진
    // 🎯 [완전 보강] 옛날 코드(d-2)의 정밀 격자 좌표 시스템을 이식한 보드 초기화 엔진
    // 🎯 [ width / height 장부 완벽 연동 ] 현재 코드의 변수 명칭을 100% 보존한 초기화 엔진
    public void InitializeNewBoard()
    {
        // 🔓 [왕초보 특제: 두 번째 판 마우스 차단벽 원천 붕괴 락온]
        // 버튼을 눌러 새 판을 까는 바로 그 순간, 마우스를 꽉 잠그고 있던 유령 스위치들을 완전히 강제 해제합니다!
        isGameActive = true;     // 1. 게임 활성화 상태 ON!
        isProcessing = false;    // 2. 블록 연산 중 잠금 해제(false)!
        isMatching = false;      // 3. 매칭 계산 중 잠금 해제(false)!

        ClearAllBoardObjects();
        
        // width(가로)와 height(세로) 장부 크기 그대로 안전하게 반복문을 돌립니다.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 시작하자마자 3매치가 미리 터지는 버그 방지 목록 계산
                List<int> allowedIndices = new List<int>();
                for (int i = 0; i < blockPrefabs.Length; i++)
                {
                    if (x >= 2 && GetBlockColor(allBlocks[x - 1, y]) == GetBlockColor(blockPrefabs[i]) &&
                        GetBlockColor(allBlocks[x - 2, y]) == GetBlockColor(blockPrefabs[i])) continue;
                    if (y >= 2 && GetBlockColor(allBlocks[x, y - 1]) == GetBlockColor(blockPrefabs[i]) &&
                        GetBlockColor(allBlocks[x, y - 2]) == GetBlockColor(blockPrefabs[i])) continue;
                    
                    allowedIndices.Add(i);
                }

                int randomIndex = allowedIndices[Random.Range(0, allowedIndices.Count)];
                SpawnBlockAt(randomIndex, x, y);
            }
        }
        Debug.Log("🎲 [성공] 변수 충돌이 해결된 6x6 보드가 배치되었습니다.");
    }

    // 🎯 [ width / height 장부 완벽 연동 ] 현재 코드의 앵커 시스템과 이름 규칙을 일치시킨 생성 엔진
    private void SpawnBlockAt(int prefabIndex, int x, int y)
    {
        // 앵커식을 제거하고 픽셀 좌표와 영문 "Block_" 규칙을 적용하여 블록 생성 및 정렬
        GameObject newBlock = Instantiate(blockPrefabs[prefabIndex], transform);
        RectTransform rect = newBlock.GetComponent<RectTransform>();
        
        if (rect != null)
        {

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            float startX = -((width - 1) * blockSpacing) / 2f;
            float startY = -((height - 1) * blockSpacing) / 2f;
            rect.anchoredPosition = new Vector2(startX + (x * blockSpacing), startY + (y * blockSpacing));
        }

        string rawColor = GetBlockColor(blockPrefabs[prefabIndex]);
        newBlock.name = "Block_" + rawColor + "_" + x + "_" + y;
        allBlocks[x, y] = newBlock;
    }



    

    private IEnumerator DropExistingBlocksRoutine()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allBlocks[x, y] == null)
                {
                    for (int k = y + 1; k < height; k++)
                    {
                        if (allBlocks[x, k] != null)
                        {
                            allBlocks[x, y] = allBlocks[x, k];
                            allBlocks[x, k] = null;
                            
                    // ---- [복붙 시작] 옛날 방식의 정밀 UI 픽셀 좌표 계산 및 이동 ----
                    float startX = -((width - 1) * blockSpacing) / 2f;
                    float startY = -((height - 1) * blockSpacing) / 2f;
                    Vector2 targetUIPos = new Vector2(startX + (x * blockSpacing), startY + (y * blockSpacing));
                    StartCoroutine(MoveBlockSmoothlyUI(allBlocks[x, y], targetUIPos));
                    // ---- [복붙 끝] ---------------------------------------------

                            string originalName = allBlocks[x, y].name;
                            int lastUnderscore = originalName.LastIndexOf('_');
                            int secondLastUnderscore = originalName.Substring(0, lastUnderscore).LastIndexOf('_');
                            string colorPrefix = originalName.Substring(0, secondLastUnderscore);
                            allBlocks[x, y].name = colorPrefix + "_" + x + "_" + y;
                            break;
                        }
                    }
                }
            }
        }
        yield return new WaitForSeconds(0.15f); //빈칸 채우는 시간
    }



    public void ClearAllBoardObjects()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allBlocks[x, y] != null)
                {
                    Destroy(allBlocks[x, y]);
                    allBlocks[x, y] = null;
                }
            }
        }
    }

    public void StartInfiniteMode()
    {
    currentTurn = 0;
    comboCount = 0;
    isGameActive = true;
    StartCoroutine(InfiniteTimerRoutine());
    }

    private IEnumerator InfiniteTimerRoutine()
    {
        float timer = limitTime;
        while (timer > 0)
        {
            if (!isGameActive) yield break;
            timer -= Time.deltaTime;
            
            // 이사 온 0.001초 출력 뷰어 가동
            DisplayTime(timer); 

            yield return null;
        }
        isGameActive = false;
        isProcessing = true;
        Debug.Log("⏱️ [종료] 3분 제한시간 도달! 무한모드가 강제 종료됩니다.");
        ShutdownAndCleanupBoard();
    }

    public void StartNormalMode()
    {
        InitializeNewBoard();
        currentTurn = 0;
        comboCount = 0;
        isGameActive = true;
        Debug.Log("⚔️ [시작] 일반모드가 가동되었습니다. 보스를 처치하세요!");
    }

public void OnClickRealStartInfiniteTimer()
{
    if (startTouchTriggerPanel != null)
    {
        startTouchTriggerPanel.SetActive(false);
    }
    StartInfiniteMode();
}

    private void DisplayTime(float timeToDisplay)
    {
        if (TimeText == null) return;
        if (timeToDisplay < 0) timeToDisplay = 0;

        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);
        int milliseconds = Mathf.FloorToInt((timeToDisplay - Mathf.FloorToInt(timeToDisplay)) * 1000);

        TimeText.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }
    [Header("ㅡ 마우스 및 드래그 제어 (리뉴얼 엔진) ㅡ")]
    private GameObject selectedBlock = null;
    private Vector2 clickStartPos;
    private int startX, startY;
    private bool isSwappingNow = false;

    // 🎯 [완전 구현] 인스펙터/프리팹 투명 가림막을 무력화하는 무적의 픽셀 좌표 추적 시스템
    private void Update()
    {
        // Block이 움직이거나 매칭 계산 중일 때 마우스 입력 철저히 차단
        if (isProcessing || isSwappingNow || isSwapping || isMatching) return;

        // 1. 마우스 왼쪽 버튼을 누르는 순간 (클릭)
        if (Input.GetMouseButtonDown(0))
        {
            // 📡 [마우스 클릭 감지 스파이 로그 장착]
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log($"[클릭 감지 신호 수신] 🖱️ 화면 마우스 위치: {Input.mousePosition} | 🌍 유니티 월드 변환 좌표: {mouseWorldPos}");


            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = Input.mousePosition };
            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject != null && (result.gameObject.name.StartsWith("Block_")))
                {
                    selectedBlock = result.gameObject;

                    // ◀ 마우스 클릭(드래그 시작) 시 즉시 최상단 레이어로 이동
                    if (selectedBlock.TryGetComponent<RectTransform>(out var selectedRT))
                    {
                        selectedRT.SetAsLastSibling();
                    }

                    clickStartPos = Input.mousePosition;
                    FindBlockIndex(selectedBlock, out startX, out startY);
                    break;
                }
            }

        } // <- 🎯 GetMouseButtonDown(0) 조건문이 완전히 끝나는 닫는 괄호

        // 2. 마우스 왼쪽 버튼을 떼는 순간 (드래그 완료 판정) - foreach 바깥으로 정상 탈출!
        if (Input.GetMouseButtonUp(0) && selectedBlock != null)
        {
            Vector2 clickEndPos = Input.mousePosition;
            Vector2 swipeDelta = clickEndPos - clickStartPos;

            // 드래그 누적 거리가 최소 40픽셀 이상 확실히 움직였을 때만 격발
            if (swipeDelta.magnitude > 40f)
            {
                CalculateSwipeDirection(swipeDelta);
            }
            selectedBlock = null; // 조작 대상 초기화
        }
    } // <- 🎯 Update() 함수 전체가 예쁘게 마무리되는 닫는 괄호


    private void FindBlockIndex(GameObject block, out int x, out int y)
{
    x = -1; y = -1;
    if (block == null) return;

    // 대소문자 무시를 위해 소문자로 변환 후 언더바(_) 기준으로 쪼갭니다.
    string[] nameParts = block.name.ToLower().Split('_');

    if (nameParts.Length >= 3)
    {
        // 맨 뒤에서 2번째 칸과 맨 마지막 칸에서 순수 숫자만 골라내어 x, y에 주입합니다.
        int.TryParse(nameParts[nameParts.Length - 2], out x);
        int.TryParse(nameParts[nameParts.Length - 1], out y);
    }
}


    // 🎯 [기획 규칙 완벽 이식] 드래그 방향에 따라 정확히 인접한 칸(Target)을 지정하는 조작 엔진
    // 🎯 [완전 수정] 대각선 이동을 철저히 차단하는 철통 방어 조작 엔진
    // 🎯 [완전 보강] 대각선 이동을 철저히 차단하는 철통 방어 조작 엔진
    // 🎯 [d-2 정품 이식] 옛날 버전의 확실한 단방향 정밀 스와이프 및 대각선 방어 수식
    private void CalculateSwipeDirection(Vector2 delta)
    {
        int targetX = startX;
        int targetY = startY;

        // 옛날 d-2 코드의 가장 직관적이고 확실한 가로/세로 축 가리기 공식
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            // 가로 움직임이 크면 오직 좌우로만 딱 1칸 이동 인정
            targetX += delta.x > 0 ? 1 : -1;
        }
        else
        {
            // 세로 움직임이 크면 오직 상하로만 딱 1칸 이동 인정 (대각선 미끄러짐 원천 차단)
            targetY += delta.y > 0 ? 1 : -1;
        }

        // 6x6 보드 격자판 안쪽의 안전한 범위일 때만 실제 자리 교체 가동
        if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
        {
            StartCoroutine(SwapBlocksRoutine(startX, startY, targetX, targetY));
        }
        else
        {
            Debug.LogWarning($"⚠ [벽 차단] ({targetX}, {targetY})는 보드판 바깥 영역이라 조작을 취소합니다.");
        }
    }



    // 🎯 [완전 복구] 가로/세로 3개 이상 연속된 컬러 매칭을 한 치의 오차도 없이 적출하는 정품 탐색기
    private List<GameObject> FindAllMatches()
    {
        List<GameObject> matches = new List<GameObject>();

        // 1. 가로축 3연속 매칭 추적 검사 (width - 2까지만 안전하게 순회)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                GameObject b1 = allBlocks[x, y];
                GameObject b2 = allBlocks[x + 1, y];
                GameObject b3 = allBlocks[x + 2, y];

                if (b1 != null && b2 != null && b3 != null)
                {
                    // 대소문자를 무시하고 추출한 문자열 색상이 연속으로 일치하는지 비교합니다.
                    if (GetBlockColor(b1) == GetBlockColor(b2) && GetBlockColor(b2) == GetBlockColor(b3))
                    {
                        if (!matches.Contains(b1)) matches.Add(b1);
                        if (!matches.Contains(b2)) matches.Add(b2);
                        if (!matches.Contains(b3)) matches.Add(b3);
                    }
                }
            }
        }

        // 2. 세로축 3연속 매칭 추적 검사 (height - 2까지만 안전하게 순회)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                GameObject b1 = allBlocks[x, y];
                GameObject b2 = allBlocks[x, y + 1];
                GameObject b3 = allBlocks[x, y + 2];

                if (b1 != null && b2 != null && b3 != null)
                {
                    if (GetBlockColor(b1) == GetBlockColor(b2) && GetBlockColor(b2) == GetBlockColor(b3))
                    {
                        if (!matches.Contains(b1)) matches.Add(b1);
                        if (!matches.Contains(b2)) matches.Add(b2);
                        if (!matches.Contains(b3)) matches.Add(b3);
                    }
                }
            }
        }

        return matches;
    }
    
    // 🎯 [완전 복구] 날아가버렸던 드래그 자리 교체 및 1턴 소모 전담 엔진
    // 🎯 [d-2 정품 + 최신 턴/콤보 융합] Block 이동 및 1턴 소모
    // ---- [복붙 시작] 앵커 방식을 완전히 제거하고 정밀 UI 픽셀 위치로 자리 교체 및 턴 소모 ----
    // ✅ 기존 최신 기획 기능(이름 규칙, 유저 턴 기록 등)을 보존하며 옛날 복귀만 이식한 코드입니다!
    private IEnumerator SwapBlocksRoutine(int x1, int y1, int x2, int y2)
    {
        isSwapping = true;
        isUserTurn = true; // 🎯 [기존 기능 보존] 유저 턴 기록

        GameObject b1 = allBlocks[x1, y1];
        GameObject b2 = allBlocks[x2, y2];

    if (b1 != null && b2 != null)
    {
        // 뽄형님이 추가한 레이어 최상단 고정 기능!
        if (b1.TryGetComponent<RectTransform>(out var rt1)) rt1.SetAsLastSibling();
        if (b2.TryGetComponent<RectTransform>(out var rt2)) rt2.SetAsLastSibling();

        // // 🌟 계산 공식으로 부드러운 위치 계산
        float startX = -((width - 1) * blockSpacing) / 2f;
        float startY = -((height - 1) * blockSpacing) / 2f;


            Vector2 posA = new Vector2(startX + (x1 * blockSpacing), startY + (y1 * blockSpacing));
            Vector2 posB = new Vector2(startX + (x2 * blockSpacing), startY + (y2 * blockSpacing));

            StartCoroutine(MoveBlockSmoothlyUI(b1, posB));
            yield return StartCoroutine(MoveBlockSmoothlyUI(b2, posA));
        }

        // 데이터 교체
        allBlocks[x1, y1] = b2;
        allBlocks[x2, y2] = b1;

        // 🎯 [기존 기능 보존] 영문 "Block_" 이름 규칙 적용
        if (b1 != null) b1.name = $"Block_{GetBlockColor(b1)}_{x2}_{y2}";
        if (b2 != null) b2.name = $"Block_{GetBlockColor(b2)}_{x1}_{y1}";

        currentTurn++;
        if (PuzzleBattleManager.Instance != null)
        {
            PuzzleBattleManager.Instance.currentTurn = currentTurn;
            PuzzleBattleManager.Instance.UpdateTurnTextUI();
        }


        yield return StartCoroutine(JudgeMatchAndProcess(x1, y1, x2, y2));
    }

    // 🛠️ 공통 이동/복귀 로직 (옛날 정품 뼈대)
    // ✅ 이 함수 전체를 복사해서 기존의 복잡한 앵커식 MoveBlocks 함수 자리에 통째로 덮어씌우세요!
    private IEnumerator MoveBlocks(GameObject b1, GameObject b2, int x1, int y1, int x2, int y2, float speed)
    {
        if (b1 == null || b2 == null) yield break;

        RectTransform rt1 = b1.GetComponent<RectTransform>();
        RectTransform rt2 = b2.GetComponent<RectTransform>();

        if (rt1 == null || rt2 == null) yield break;

        // 드래그하는 블록이 다른 블록 뒤로 숨지 않게 맨 앞으로 레이어를 올려줍니다.
        rt1.SetAsLastSibling();
        rt2.SetAsLastSibling();

        // [옛날 d-2 정품 방식 이식]: 앵커(anchor) 연산을 완전히 무시하고, 순수 UI 픽셀 위치로 목적지를 잡습니다.
        float startX = -((width - 1) * blockSpacing) / 2f;
        float startY = -((height - 1) * blockSpacing) / 2f;

        Vector2 startPos1 = rt1.anchoredPosition;
        Vector2 startPos2 = rt2.anchoredPosition;

        // b1은 (x2, y2) 위치로 가고, b2는 (x1, y1) 위치로 이동해야 합니다.
        Vector2 targetPos1 = new Vector2(startX + (x2 * blockSpacing), startY + (y2 * blockSpacing));
        Vector2 targetPos2 = new Vector2(startX + (x1 * blockSpacing), startY + (y1 * blockSpacing));

        float t = 0f;
        while (t < 1f)
        {
            // speed 배속을 적용하여 부드럽게 두 블록을 동시에 픽셀 이동시킵니다.
            t += Time.deltaTime * speed;
            
            if (rt1 != null) rt1.anchoredPosition = Vector2.Lerp(startPos1, targetPos1, t);
            if (rt2 != null) rt2.anchoredPosition = Vector2.Lerp(startPos2, targetPos2, t);
            
            yield return null;
        }

        // 목적지 좌표에 소수점 오차가 나지 않도록 칼같이 고정해줍니다.
        if (rt1 != null) rt1.anchoredPosition = targetPos1;
        if (rt2 != null) rt2.anchoredPosition = targetPos2;
    }


    // 🎯 [복구] 3매치 정방향 사후 판정 및 6배속 복귀 엔진
    // 🎯 [복구 완료] 3매치 정방향 사후 판정 및 6배속 복귀 엔진
    // 🎯 [완전 수리] "Block_" 이름 규칙 적용 및 실패 시 조작 잠금 스위치를 철저히 해제하는 정품 엔진
    // ✅ 이 아래 부분을 복사해서 JudgeMatchAndProcess 함수 전체에 그대로 덮어씌우세요!
    private IEnumerator JudgeMatchAndProcess(int x1, int y1, int x2, int y2)
    {
        isProcessing = true;
        List<GameObject> matchedBlocks = FindAllMatches();

        if (matchedBlocks.Count > 0)
        {
            // [3매치 성공] 파괴 및 리필 엔진 가동
            yield return StartCoroutine(DestroyAndRefillRoutine(matchedBlocks));

            // 🌟 [데드락 추적 보안]: 모든 연쇄 폭발과 리필이 끝난 "최종 시점"에 움직일 조합이 있는지 검사합니다!
            yield return StartCoroutine(CheckPostProcessAndDeadlock());

            // 성공 처리가 완전히 끝났으므로 조작 잠금 장치를 해제합니다.
            isSwapping = false;
            isMatching = false;
            isSwappingNow = false;
            isProcessing = false;
        }
        else
        {
                    // 매칭 실패 시 콤보 데이터를 칼같이 0으로 초기화하는 방어선 장착!
        comboCount = 0;
        UpdateComboTextUI();
            // [3매치 실패] 🚨 서로 바꿨던 블록 대상을 컴퓨터 장부(allBlocks)에서 다시 정확히 추적합니다.
            GameObject block1 = allBlocks[x2, y2]; // 교체되어 x2, y2에 가 있는 블록
            GameObject block2 = allBlocks[x1, y1]; // 교체되어 x1, y1에 가 있는 블록

            if (block1 != null && block2 != null)
            {
                // 옛날 d-2 정품 공식의 화면 정중앙 기준 UI 픽셀 목적지 계산 가동
                float startX = -((width - 1) * blockSpacing) / 2f;
                float startY = -((height - 1) * blockSpacing) / 2f;

                // 실패했으므로 block1은 다시 원래 고유 터전인 (x1, y1) 주소로, block2는 (x2, y2) 주소로 되돌려 보냅니다.
                Vector2 originalPos1 = new Vector2(startX + (x1 * blockSpacing), startY + (y1 * blockSpacing));
                Vector2 originalPos2 = new Vector2(startX + (x2 * blockSpacing), startY + (y2 * blockSpacing));

                // 화면상에서 부드럽게 원위치 슬라이딩 연출을 실시간 실행합니다.
                StartCoroutine(MoveBlockSmoothlyUI(block1, originalPos1));
                yield return StartCoroutine(MoveBlockSmoothlyUI(block2, originalPos2));
            }

            // 🌟 [d-2 정품 완벽 복구 핵심]: 화면 이동이 완전히 끝난 후 컴퓨터 내부 데이터 장부를 안전하게 원상복구합니다.
            allBlocks[x1, y1] = block1;
            allBlocks[x2, y2] = block2;

            // 이름 뒤에 붙어있던 격자 위치 인덱스 데이터명도 깔끔하게 원래 정보로 되돌려놓습니다.
            if (block1 != null) block1.name = $"Block_{GetBlockColor(block1)}_{x1}_{y1}";
            if (block2 != null) block2.name = $"Block_{GetBlockColor(block2)}_{x2}_{y2}";

            // 컴포넌트 유실 방지를 위한 내부 시스템 좌표 동기화 전송
            if (block1 != null) block1.SendMessage("SetGridPosition", new Vector2Int(x1, y1), SendMessageOptions.DontRequireReceiver);
            if (block2 != null) block2.SendMessage("SetGridPosition", new Vector2Int(x2, y2), SendMessageOptions.DontRequireReceiver);

            // 실패 연출과 장부 정리가 완료되었으므로 잠겨있던 스위치들을 시원하게 해제합니다.
            isSwapping = false;
            isMatching = false;
            isSwappingNow = false;
            isProcessing = false;
        }
    }


    



    // 🎯 [완전 융합] 현재 코드의 콤보 배율/연쇄 폭발 장치를 100% 보존하면서 옛날 점수 연동을 이식한 엔진
    // 🎯 [완전 복구] 콤보 배율과 연쇄 폭발을 보존한 옛날 d-2 정품 파괴/리필 통합 엔진
    // 🎯 [완전 융합] 콤보 및 연쇄 폭발을 처리하는 통합 엔진 (리팩토링 버전)
    // 🎯 [오류 해결 완료 버전] 콤보와 연쇄 폭발을 에러 없이 완벽 처리하는 통합 엔진
    // 🎯 [완전 복구] 콤보와 연쇄 폭발을 에러 없이 완벽 처리하는 통합 엔진
    // 🎯 [d-2 정품 연쇄 폭발 + 최신 콤보 시스템 융합]
    // 🎯 [3단계 수리 완결판] 무한 락 루프 방지 및 InfiniteMonster 타격/이름 규칙 보강
    private IEnumerator DestroyAndRefillRoutine(List<GameObject> matches)
    {
        while (matches.Count > 0)
        {
            // 1. 최신 기획 반영: 폭발할 때마다 콤보 수치 상승 및 UI 반영
            comboCount++;
            UpdateComboTextUI();

            // 유저가 직접 드래그해서 첫 번째 매치가 터진 순간에만 실행됩니다!
if (isUserTurn)
{
    // 🔔 [Monster 타격 연동 코드]: 터진 블록의 총 개수당 100 대미지 계산
if (InfiniteMonster.Instance != null)
{
    // 1. [기획 규칙]: 동시 파괴된 블록 개수(matches.Count)에 따른 보너스 배율 계산
    float countMultiplier = 1.0f;
    if (matches.Count == 4)       countMultiplier = 1.5f; // 4개 동시 파괴 시 1.5배!
    if (matches.Count >= 5)       countMultiplier = 2.0f; // 5개 이상 대량 파괴 시 2.0배 폭발 대미지!

    // 2. [최종 대미지 연산]: (기본 대미지) * 콤보 배율 * 파괴 개수 배율
    float baseDamage = matches.Count * 100f;
    float finalDamage = baseDamage * GetComboMultiplier() * countMultiplier;

    // 3. 무한모드 몬스터에게 대미지 주입 및 로그 출력
    InfiniteMonster.Instance.TakeDamage(finalDamage);
    Debug.Log($"💣 [대폭발] 터진 블록: {matches.Count}개({countMultiplier}배) | 콤보: {comboCount}콤보({GetComboMultiplier()}배) | 최종 대미지: {finalDamage}!");
}
    isUserTurn = false; // 첫 연쇄 이후 플래그 초기화
}

            // 2. 옛날 d-2 정품 방식: 안전하게 장부(배열) 비우고 화면에서 Block 제거
            foreach (GameObject block in matches)
            {
                if (block != null)
                {
                    int x, y;
                    FindBlockIndex(block, out x, out y);
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        allBlocks[x, y] = null;
                    }
                    Destroy(block);
                }
            }

            // 블록터지는 시간
            yield return new WaitForSeconds(0.05f);

            // 3. 옛날 d-2 정품 방식: 기존 Block을 아래로 떨구고, 천장에서 새 Block 리필
            yield return StartCoroutine(DropExistingBlocksRoutine());
            yield return StartCoroutine(RefillNewBlocksRoutine());

            // 새블록떨어지고 판정하는시간
            yield return new WaitForSeconds(0.03f);

            // 4. [무한 콤보 핵심]: 다 떨어져 내린 후 또 3개가 맞았는지 보드판 전수 조사!
            matches = FindAllMatches();
        }

        // 모든 폭발 처리가 완벽히 끝났으므로 다음 드래그가 가능하도록 모든 조작 잠금을 해제합니다.
        isSwapping = false;
        isMatching = false;
        isSwappingNow = false;
        isProcessing = false;
    }







    // ---- [복붙 시작] 앵커식을 완전히 제거하고 픽셀 좌표로 리필하는 엔진 ----
    private IEnumerator RefillNewBlocksRoutine()
    {
        for ( int x = 0; x < width; x++)
        {
            // 아래(0)에서 위(height-1)로 올라가며 빈칸 탐색
            for ( int y = 0; y < height; y++)
            {
                if ( allBlocks[ x, y] == null)
                {
                    int randomIndex = Random. Range( 0, blockPrefabs. Length);
                    GameObject prefabToSpawn = blockPrefabs[ randomIndex];
                    
                    if ( prefabToSpawn != null)
                    {
                        // 새 Block 생성 및 보드판의 자식으로 등록
                        GameObject newBlock = Instantiate( prefabToSpawn, transform);
                        RectTransform rt = newBlock. GetComponent< RectTransform>();
                        
                        if ( rt != null)
                        {
                            rt. anchorMin = new Vector2( 0.5f, 0.5f);
                            rt. anchorMax = new Vector2( 0.5f, 0.5f);
                            rt. pivot = new Vector2( 0.5f, 0.5f);
                            rt. localScale = Vector3. one;
                            rt. localRotation = Quaternion. identity;
                            
                            // 🌟 옛날 코드 공식: 화면 바깥 천장(height 위치)에서 생성 시작점 잡기
                            float startX = -((width - 1) * blockSpacing) / 2f;
                            float startY = -((height - 1) * blockSpacing) / 2f;
                            rt. anchoredPosition = new Vector2( startX + ( x * blockSpacing), startY + ( height * blockSpacing));
                        }
                        
                        string rawColor = GetBlockColor( blockPrefabs[ randomIndex]);
                        newBlock. name = $"Block_{ rawColor}_{ x}_{ y}";
                        
                        // 데이터 장부에 저장
                        allBlocks[ x, y] = newBlock;
                        
                        // 🌟 옛날 코드 공식: 실제로 자리를 잡고 멈출 바닥 목표 픽셀 좌표 계산
                        float targetX = -((width - 1) * blockSpacing) / 2f + ( x * blockSpacing);
                        float targetY = -((height - 1) * blockSpacing) / 2f + ( y * blockSpacing);
                        Vector2 targetUIPos = new Vector2( targetX, targetY);
                        
                        // 부드럽게 떨어지는 옛날 UI 이동 엔진 가동
                        StartCoroutine( MoveBlockSmoothlyUI( newBlock, targetUIPos));
                    }
                }
            }
        
        
        // Block들이 바닥에 안착할 때까지 안전하게 0.2초 대기
        yield return new WaitForSeconds( 0.15f);
    }
    // ---- [복붙 끝] ----------------------------------------------------
    }

    public void UpdateComboTextUI()
    {
        if (comboText == null) return;

        if (comboCount > 0)
        {
            comboText.text = "Combo\n" + comboCount;
            Color textColor = comboText.color;
            textColor.a = 1f; 
            comboText.color = textColor;

            if (comboFadeCoroutine != null) StopCoroutine(comboFadeCoroutine);
            comboFadeCoroutine = StartCoroutine(AnimateFastComboTextRoutine());
        }
        else
        {
            comboText.text = "";
        }
    }

    private IEnumerator AnimateFastComboTextRoutine()
    {
        if (comboText == null) yield break;

        RectTransform rect = comboText.GetComponent<RectTransform>();
        Vector2 startPosition = rect != null ? rect.anchoredPosition : Vector2.zero;

        if (rect != null)
        {
            float bounceDuration = 0.12f; 
            float time = 0f;
            Vector3 targetScale = Vector3.one;
            Vector3 startScale = Vector3.one * 1.8f; 

            while (time < bounceDuration)
            {
                time += Time.deltaTime;
                rect.localScale = Vector3.Lerp(startScale, targetScale, time / bounceDuration);
                yield return null;
            }
            rect.localScale = targetScale;
        }

        float fadeDuration = 0.35f; 
        float fadeTime = 0f;
        Color safeComboColor = comboText.color;

        while (fadeTime < fadeDuration)
        {
            fadeTime += Time.deltaTime;
            float progress = fadeTime / fadeDuration;

            float alpha = Mathf.Lerp(1f, 0f, progress);
            safeComboColor.a = alpha;
            comboText.color = safeComboColor;

            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(startPosition.x, startPosition.y + (progress * 25f));
            }
            yield return null;
        }

        comboText.text = "";
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = startPosition;
        }
    }

    
    // 🎯 [완전 보강] 옛날 코드(d-2)의 철통 안전 검사식을 width/height에 맞게 이식한 데드락 탐색 엔진
    // 🎯 [중괄호 오류 완벽 수정] 안전하게 정돈된 데드락 탐색 엔진
    private bool CheckPossibleMatchesExist()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allBlocks[x, y] == null) continue;

                // 1. 오른쪽 칸 탐색
                if (x + 1 < width)
                {
                    if (allBlocks[x + 1, y] != null)
                    {
                        if (SimulateSwapAndCheckMatch(x, y, x + 1, y)) return true;
                    }
                }

                // 2. 위쪽 칸 탐색
                if (y + 1 < height)
                {
                    if (allBlocks[x, y + 1] != null)
                    {
                        if (SimulateSwapAndCheckMatch(x, y, x, y + 1)) return true;
                    }
                }
            }
        }
        return false;
    }

    // 🎯 데드락 임시 스와이프 검사기
    private bool SimulateSwapAndCheckMatch(int x1, int y1, int x2, int y2)
    {
        GameObject temp = allBlocks[x1, y1];
        allBlocks[x1, y1] = allBlocks[x2, y2];
        allBlocks[x2, y2] = temp;

        List<GameObject> testMatches = FindAllMatches();
        bool hasMatch = testMatches.Count > 0;

        allBlocks[x2, y2] = allBlocks[x1, y1];
        allBlocks[x1, y1] = temp;

        return hasMatch;
    }


    // 🎯 [d-2 정품 이식] 움직일 조합이 없을 때 12시->6시 방향으로 부드럽게 판을 밀고 리필하는 엔진
    // ✅ [데드락 리뉴얼]: 중앙 확산형 연출 코루틴 (상세 로직은 하단 설명 참고)
    // ✅ 데드락 이후 상대방 블록 좌표가 -1로 깨지던 버그를 완벽하게 수리한 최종 종결 패치입니다!
    private IEnumerator ResolveDeadlockRoutine()
    {
        isProcessing = true;
        
        // 1. 소멸: 중앙(2.5, 2.5) 기준으로 거리 계산 후 가까운 순으로 제거
        List<KeyValuePair<GameObject, float>> blocksToDestroy = new List<KeyValuePair<GameObject, float>>();
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (allBlocks[x, y] != null) {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(2.5f, 2.5f));
                    blocksToDestroy.Add(new KeyValuePair<GameObject, float>(allBlocks[x, y], dist));
                    allBlocks[x, y] = null;
                }
            }
        }
        blocksToDestroy.Sort((a, b) => a.Value.CompareTo(b.Value));
        
        float lastDist = -1f;
        foreach (var pair in blocksToDestroy) {
            if (lastDist >= 0f && Mathf.Abs(pair.Value - lastDist) > 0.1f) yield return new WaitForSeconds(0.03f);
            Destroy(pair.Key);
            lastDist = pair.Value;
        }
        yield return new WaitForSeconds(0.2f); // 대기

        // 2. 생성: 중앙 기준으로 거리 계산 후 가까운 순으로 생성 및 팝업 연출
        List<Vector2Int> spawnCoords = new List<Vector2Int>();
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) spawnCoords.Add(new Vector2Int(x, y));
        
        spawnCoords.Sort((a, b) => {
            float d1 = Vector2.Distance(a, new Vector2(2.5f, 2.5f));
            float d2 = Vector2.Distance(b, new Vector2(2.5f, 2.5f));
            return d1.CompareTo(d2);
        });

        float lastSpawnDist = -1f;
        foreach (Vector2Int coord in spawnCoords) {
            int randomIndex = Random.Range(0, blockPrefabs.Length);
            GameObject newBlock = Instantiate(blockPrefabs[randomIndex], transform);
            RectTransform rt = newBlock.GetComponent<RectTransform>();
            
            float sx = -((width - 1) * blockSpacing) / 2f;
            float sy = -((height - 1) * blockSpacing) / 2f;
            rt.anchoredPosition = new Vector2(sx + (coord.x * blockSpacing), sy + (coord.y * blockSpacing));
            
            // 프리팹 분석 및 위치 데이터 고정 (이름표 동기화 핵심)
            string rawColor = GetBlockColor(blockPrefabs[randomIndex]);
            newBlock.name = $"Block_{rawColor}_{coord.x}_{coord.y}";
            newBlock.SendMessage("SetGridPosition", new Vector2Int(coord.x, coord.y), SendMessageOptions.DontRequireReceiver);
            
            StartCoroutine(AnimateScaleUpUI(newBlock));
            allBlocks[coord.x, coord.y] = newBlock;

            float currDist = Vector2.Distance(coord, new Vector2(2.5f, 2.5f));
            if (lastSpawnDist >= 0f && Mathf.Abs(currDist - lastSpawnDist) > 0.1f) yield return new WaitForSeconds(0.03f);
            lastSpawnDist = currDist;
        }
        
        // 매치 체크
        List<GameObject> checkMatchesAfterDeadlock = FindAllMatches();
        if (checkMatchesAfterDeadlock.Count > 0)
        {
            yield return StartCoroutine(DestroyAndRefillRoutine(checkMatchesAfterDeadlock));
        }
        else
        {
            isProcessing = false;
        }
    }
   //데드락은 여기까지 

    // 🌟 0에서 1로 스케일이 커지는 팝업 연출
    private IEnumerator AnimateScaleUpUI(GameObject target) {
        float elapsed = 0f, duration = 0.2f;
        while (elapsed < duration) {
            if (target == null) yield break;
            target.transform.localScale = Vector3.one * (elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (target != null) target.transform.localScale = Vector3.one;
    }

    private IEnumerator CheckPostProcessAndDeadlock() {
        if (!CheckPossibleMatchesExist()) yield return StartCoroutine(ResolveDeadlockRoutine());
    }


    // ✅ 917번째 줄부터 파일 맨 끝까지 이 코드로 통째로 안전 덮어쓰기 하세요!
    // ✅ 917번째 줄부터 파일 맨 마지막 끝 줄까지 이 코드로 통째로 안전 덮어쓰기 하세요!
    // 🎯 [재시작 트리거 시스템]: 게임오버 화면 터치 시 보드판을 부활시킵니다.
    // ✅ 여기서부터 복사해서 파일 끝까지 덮어쓰기 하세요!
        // ✅ [정품 연동 핵심]: 인스펙터에 조립 완료된 매니저에게 정산 명령을 위임합니다.
        // ✅ 사진 속 929번째 줄부터 파일 맨 마지막 끝 줄까지 이 코드로 통째로 안전 덮어쓰기 하세요!
        // ✅ 뽄형님이 짚어주신 시작/종료 담당 오브젝트 규칙을 완벽하게 적용한 최종 종결 코드입니다!
    public void ShutdownAndCleanupBoard()
    {
        isGameActive = false;
        isProcessing = true;
        
        ClearAllBoardObjects(); // 보드판 블록 찌꺼기 완벽 청소
        comboCount = 0;
        UpdateComboTextUI();
        Debug.Log("✨ [성공] 보드판 소멸 완수.");

        // 🎯 [게임오버 발동]: 시간이 종료되었으므로 매니저의 정산 기능을 깨웁니다.
        if (PuzzleBattleManager.Instance != null)
        {
            PuzzleBattleManager.Instance.currentTurn = currentTurn;
            PuzzleBattleManager.Instance.OnTimerEnd(); // 이 함수 안에서 최종 대미지와 턴수를 정산합니다.

            // ⚡ [스위치 작동 1]: 게임이 끝났으므로 '시작 담당 리모컨'은 확실하게 꺼줍니다!
            if (PuzzleBattleManager.Instance.btn_StartTouchTrigger_Direct != null)
            {
                PuzzleBattleManager.Instance.btn_StartTouchTrigger_Direct.SetActive(false);
            }

            // ⚡ [스위치 작동 2]: 대신 '종료 담당 결과창(GAMEOVER TXT)' 본체를 화면에 확실하게 켭니다!
            if (PuzzleBattleManager.Instance.panel_InfiniteReward != null)
            {
                PuzzleBattleManager.Instance.panel_InfiniteReward.SetActive(true);
            }
        }
    }

    // 🎯 [재시작 트리거]: 결과창 화면을 터치했을 때 다시 태초의 상태로 부활시키는 함수
    public void RestartGameByTouch()
    {
        if (PuzzleBattleManager.Instance != null)
        {
            // ⚡ [스위치 작동 3]: 다시 게임을 시작해야 하므로 떠 있던 '종료 담당 결과창'은 깨끗이 끕니다.
            if (PuzzleBattleManager.Instance.panel_InfiniteReward != null)
            {
                PuzzleBattleManager.Instance.panel_InfiniteReward.SetActive(false);
            }
                
            PuzzleBattleManager.Instance.currentTurn = 0;
            PuzzleBattleManager.Instance.UpdateTurnTextUI();
            
            // ⚡ [스위치 작동 4]: 다음 판 첫 터치(드래그) 시작을 감지할 수 있도록 '시작 담당 리모컨'을 다시 켭니다!
            if (PuzzleBattleManager.Instance.btn_StartTouchTrigger_Direct != null)
            {
                PuzzleBattleManager.Instance.btn_StartTouchTrigger_Direct.SetActive(true);
            }
        }

        // 보드 제어용 변수들도 첫 게임 시작 상태로 깔끔하게 영점 조절합니다.
        isGameActive = true;
        isProcessing = false;
        isSwapping = false;
        isMatching = false;
        comboCount = 0;
        currentTurn = 0;

        // 가운데서부터 사방으로 피어나는 정품 새 보드판 배치 가동!
        InitializeNewBoard();
        Debug.Log("🔄 [순환 완공] 게임오버 창이 닫히고 스타트 트리거 버튼이 켜지며 무한모드가 재시작됩니다!");
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
} // 🚨 파일의 맨 마지막을 닫아주는 전체 마침표 중괄호입니다! 이 밑에는 아무것도 적지 마세요.
