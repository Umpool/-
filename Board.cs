using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 🎯 [오류 해결]: PointerEventData를 컴퓨터가 인식할 수 있게 문을 열어줍니다!


// 유니티에게 이 스크립트가 마우스 클릭, 드래그, 떼기 신호를 직접 수신하겠다고 선언합니다.
// 이제 마우스 입력은 Update 리뉴얼 엔진이 전담하므로, 뒤에 붙은 인터페이스 단어들을 전부 떼어냅니다.
public class Board : MonoBehaviour
{
    // ---- [추가] 옛날 코드에서 가져온 블록 선택 및 되돌리기용 변수 ----
    
    private int prevFirstX, prevFirstY;   // 되돌리기를 위한 첫 번째 블록의 이전 좌표
    private int prevSecondX, prevSecondY; // 되돌리기를 위한 두 번째 블록의 이전 좌표
    
    // -----------------------------------------------------------------


    [Header("ㅡ 보드 기본 설정 ㅡ")]
public int width = 6; // 가로 6칸 고정
public int height = 6; // 세로 6칸 고정
private float blockSpacing = 105f; // [보완] 블록이 겹쳐서 오작동하는걸 막는 안전 간격
    public float blockPadding = 0.02f; 

    [Header("ㅡ 블록 원본 프리팩 (6색) ㅡ")]
    public GameObject[] blockPrefabs; 

    [Header("ㅡ 게임 상태 장부 ㅡ")]
    [System.NonSerialized] public GameObject[,] allBlocks; // 6x6 보드판 실제 배열 장부
    public bool isProcessing = false; // 블록이 움직이거나 터지는 중인지 체크 (조작 잠금)
    public Transform dragLayerParent; 

    [Header("ㅡ 턴 및 콤보 데이터 ㅡ")]
    public int currentTurn = 0;
    public int comboCount = 0;
        // ---- [복붙 시작] 옛날 코드에서 이사 온 안전 제어 스위치 ----
    private bool isSwapping = false;   // 블록 자리가 교체 중일 때 조작을 잠그는 스위치
    private bool isMatching = false;   // 블록이 터지고 채워지는 중일 때 조작을 잠그는 스위치
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


    private float[] comboDamageMultipliers = new float[] { 1.0f, 1.2f, 1.5f, 1.8f, 2.0f, 2.5f };

    private void Awake()
    {
        allBlocks = new GameObject[width, height];
    }

    public string GetBlockColor(GameObject block)
    {
        if (block == null) return "None";
        string blockName = block.name.ToLower();
        if (blockName.Contains("red") || blockName.Contains("적")) return "Red";
        if (blockName.Contains("yellow") || blockName.Contains("황")) return "Yellow";
        if (blockName.Contains("green") || blockName.Contains("녹")) return "Green";
        if (blockName.Contains("blue") || blockName.Contains("청")) return "Blue";
        if (blockName.Contains("purple") || blockName.Contains("자")) return "Purple";
        if (blockName.Contains("black") || blockName.Contains("흑")) return "Black";
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
        GameObject newBlock = Instantiate(blockPrefabs[prefabIndex], transform);
        // ---- [복붙 시작] 앵커 방식을 뿌리 뽑고 옛날 정밀 좌표식으로 교체 ----
        RectTransform rect = newBlock.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            // 옛날 코드의 정밀 격자 좌표 계산식을 여기에 직접 적용합니다.
            float startX = -((width - 1) * blockSpacing) / 2f;
            float startY = -((height - 1) * blockSpacing) / 2f;
            rect.anchoredPosition = new Vector2(startX + (x * blockSpacing), startY + (y * blockSpacing));
        }
        // ---- [복붙 끝] --------------------------------------------------

        string rawColor = GetBlockColor(blockPrefabs[prefabIndex]);
        
        // [핵심 기획 반영] 프리팹 원본 이름 뒤에 현재 연동 중인 x와 y 좌표를 선명하게 박아줍니다!
        newBlock.name = blockPrefabs[prefabIndex].name + "_" + x + "_" + y;
        
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
        yield return new WaitForSeconds(0.2f);
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
    // 🎯 [옛날 정품 d-2 엔진 이식] 인스펙터나 프리팹 세팅에 간섭받지 않는 무적의 클릭&드래그 시스템
    // 🎯 [완전 융합] 최신 Update 마우스 엔진에 옛날 d-2 정품 드래그/대각선 차단 공식을 주입합니다.
    private void Update()
    {
        // 블록이 움직이거나 계산 중일 때는 마우스 입력을 원천 차단합니다.
        // ---- [복붙 시작] 블록이 움직이거나 매칭 계산 중일 때 마우스 입력 철저히 차단 ----
        if (isProcessing || isSwappingNow || isSwapping || isMatching) return;
        // ---- [복붙 끝] -----------------------------------------------------------

        // 1. 마우스 왼쪽 버튼을 누르는 순간
        if (Input.GetMouseButtonDown(0))
        {
            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = Input.mousePosition };
            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject != null && (result.gameObject.name.StartsWith("Block_") || result.gameObject.name.StartsWith("블록_")))
                {
                    selectedBlock = result.gameObject;
                    clickStartPos = Input.mousePosition; // 누른 시점의 픽셀 좌표 기억
                    FindBlockIndex(selectedBlock, out startX, out startY); // 격자 주소 분석
                    break;
                }
            }
        }

        // 2. 마우스 왼쪽 버튼을 떼는 순간 (드래그 방향 분석)
        if (Input.GetMouseButtonUp(0) && selectedBlock != null)
        {
            Vector2 clickEndPos = Input.mousePosition;
            Vector2 swipeDelta = clickEndPos - clickStartPos;

            // 최소 40픽셀 이상 확실히 움직였을 때만 드래그로 인정합니다.
            if (swipeDelta.magnitude > 40f)
            {
                CalculateSwipeDirection(swipeDelta);
            }
            selectedBlock = null; // 선택 초기화
        }
    }


        // 2. 마우스 왼쪽 버튼을 떼는 순간
        if (Input.GetMouseButtonUp(0) && selectedBlock != null)
        {
            Vector2 clickEndPos = Input.mousePosition;
            Vector2 swipeDelta = clickEndPos - clickStartPos;

            Debug. Log($"👋 [4단계 마우스 뗌] 블록 [{selectedBlock.name}] 드래그 누적 거리: {swipeDelta.magnitude} 픽셀");

            if (swipeDelta.magnitude > 40f)
            {
                Debug.Log($"🚀 [5단계 격발] 거리가 40픽셀 이상({swipeDelta.magnitude}px)이므로 CalculateSwipeDirection 함수를 원격 호출합니다!");
                CalculateSwipeDirection(swipeDelta);
            }
            else
            {
                Debug.LogWarning($"⚠️ [드래그 취소] 움직인 거리가 {swipeDelta.magnitude}픽셀로 너무 짧아 스와이프를 취소합니다. (최소 40픽셀 필요)");
            }

            selectedBlock = null; // 선택 초기화
        }
    }


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
    // 🎯 [d-2 정품 + 최신 턴/콤보 융합] 블록 이동 및 1턴 소모
    // ---- [복붙 시작] 앵커 방식을 완전히 제거하고 정밀 UI 픽셀 위치로 자리 교체 및 턴 소모 ----
    private IEnumerator SwapBlocksRoutine(int x1, int y1, int x2, int y2)
    {
        isSwapping = true;
        isUserTurn = true; // 🎯 기획 반영: 유저가 직접 드래그했음을 기록하여 자동 연쇄 폭발과 구분합니다!

        GameObject b1 = allBlocks[x1, y1];
        GameObject b2 = allBlocks[x2, y2];

        if (b1 != null && b2 != null)
        {
            // 🌟 옛날 코드의 완벽한 픽셀 목적지 좌표 계산식 가동
            float startX = -((width - 1) * blockSpacing) / 2f;
            float startY = -((height - 1) * blockSpacing) / 2f;

            Vector2 posA = new Vector2(startX + (x1 * blockSpacing), startY + (y1 * blockSpacing));
            Vector2 posB = new Vector2(startX + (x2 * blockSpacing), startY + (y2 * blockSpacing));

            // 두 블록을 서로의 목적지로 동시에 부드럽게 슬라이딩시킵니다.
            StartCoroutine(MoveBlockSmoothlyUI(b1, posB));
            yield return StartCoroutine(MoveBlockSmoothlyUI(b2, posA));
        }

        // 데이터 장부(배열) 정보 동기화 교체
        allBlocks[x1, y1] = b2;
        allBlocks[x2, y2] = b1;

        if (b1 != null) b1. name = $"블록_{GetBlockColor(b1)}_{x2}_{y2}";
        if (b2 != null) b2. name = $"블록_{GetBlockColor(b2)}_{x1}_{y1}";

        // 자리가 바뀌었으니 3매치가 맞았는지 판정하러 이동합니다.
        yield return StartCoroutine(JudgeMatchAndProcess(x1, y1, x2, y2));
    }
    // ---- [복붙 끝] ----------------------------------------------------------------------


    // 🎯 [d-2 정품 + 복귀 기능] 3매치 판정 및 실패 시 6배속 되돌리기
    private IEnumerator JudgeMatchAndProcess(int x1, int y1, int x2, int y2)
    {
        isProcessing = true;
        List<GameObject> matches = FindAllMatches();

        if (matches.Count > 0)
        {
            yield return StartCoroutine(DestroyAndRefillRoutine(matches));
        }
        else
        {
            // ❌ 3매치 실패 시 원상복구 (역방향 6배속)
            GameObject b1 = allBlocks[x1, y1], b2 = allBlocks[x2, y2];
            if (b1 != null && b2 != null)
            {
                yield return StartCoroutine(MoveBlocks(b1, b2, x1, y1, x2, y2, 6f));
            }
            // 데이터 원상복구
            allBlocks[x1, y1] = b2; allBlocks[x2, y2] = b1;
            if (b1 != null) b1.name = $"블록_{GetBlockColor(b1)}_{x1}_{y1}";
            if (b2 != null) b2.name = $"블록_{GetBlockColor(b2)}_{x2}_{y2}";
        }
        isSwappingNow = false; isProcessing = false;
        yield return StartCoroutine(CheckPostProcessAndDeadlock());
    }

    // 🛠️ 공통 이동/복귀 로직 (옛날 정품 뼈대)
    private IEnumerator MoveBlocks(GameObject b1, GameObject b2, int x1, int y1, int x2, int y2, float speed)
    {
        RectTransform rt1 = b1.GetComponent<RectTransform>(), rt2 = b2.GetComponent<RectTransform>();
        rt1.SetAsLastSibling();
        Vector2 sMin1 = rt1.anchorMin, sMax1 = rt1.anchorMax, sMin2 = rt2.anchorMin, sMax2 = rt2.anchorMax;
        Vector2 tMin1 = new Vector2((float)x2 / width, (float)y2 / height), tMax1 = new Vector2((float)(x2 + 1) / width, (float)(y2 + 1) / height);
        Vector2 tMin2 = new Vector2((float)x1 / width, (float)y1 / height), tMax2 = new Vector2((float)(x1 + 1) / width, (float)y1 / height);
        
        float t = 0f;
        while (t < 1f) {
            t += Time.deltaTime * speed;
            rt1.anchorMin = Vector2.Lerp(sMin1, tMin1, t); rt1.anchorMax = Vector2.Lerp(sMax1, tMax1, t);
            rt2.anchorMin = Vector2.Lerp(sMin2, tMin2, t); rt2.anchorMax = Vector2.Lerp(sMax2, tMax2, t);
            yield return null;
        }
        rt1.anchorMin = tMin1; rt1.anchorMax = tMax1; rt2.anchorMin = tMin2; rt2.anchorMax = tMax2;
    }

    // 🎯 [복구] 3매치 정방향 사후 판정 및 6배속 복귀 엔진
    // 🎯 [복구 완료] 3매치 정방향 사후 판정 및 6배속 복귀 엔진
    private IEnumerator JudgeMatchAndProcess(int x1, int y1, int x2, int y2)
    {
        isProcessing = true;
        List<GameObject> matchedBlocks = FindAllMatches();

        if (matchedBlocks.Count > 0)
        {
            yield return StartCoroutine(DestroyAndRefillRoutine(matchedBlocks));
        }
        else
        {
            // ❌ [원상복구 엔진] 매치 실패 시 원래 자리로 부드럽게 되돌리기
            GameObject block1 = allBlocks[x1, y1];
            GameObject block2 = allBlocks[x2, y2];

            if (block1 != null && block2 != null)
            {
                RectTransform rt1 = block1.GetComponent<RectTransform>();
                RectTransform rt2 = block2.GetComponent<RectTransform>();

                if (rt1 != null) rt1.SetAsLastSibling();

                // 6배속 복귀 애니메이션 (보정값 적용)
                float backTime = 0f;
                while (backTime < 1f)
                {
                    backTime += Time.deltaTime * 6f;
                    Vector2 targetPos1 = new Vector2((float)x1 / width, (float)y1 / height);
                    Vector2 targetPos2 = new Vector2((float)x2 / width, (float)y2 / height);
                    
                    if (rt1 != null) {
                        rt1.anchorMin = Vector2.Lerp(rt1.anchorMin, targetPos1, backTime);
                        rt1.anchorMax = Vector2.Lerp(rt1.anchorMax, targetPos1 + new Vector2(1f/width, 1f/height), backTime);
                    }
                    if (rt2 != null) {
                        rt2.anchorMin = Vector2.Lerp(rt2.anchorMin, targetPos2, backTime);
                        rt2.anchorMax = Vector2.Lerp(rt2.anchorMax, targetPos2 + new Vector2(1f/width, 1f/height), backTime);
                    }
                    yield return null;
                }
            }

            // 💡 [데이터 원상복구] 스왑 실패 시 배열 정보 다시 교환
            allBlocks[x1, y1] = block2;
            allBlocks[x2, y2] = block1;
        }

        isSwappingNow = false;
        isProcessing = false;
        yield return StartCoroutine(CheckPostProcessAndDeadlock());
    }


    // 🎯 [완전 융합] 현재 코드의 콤보 배율/연쇄 폭발 장치를 100% 보존하면서 옛날 점수 연동을 이식한 엔진
    // 🎯 [완전 복구] 콤보 배율과 연쇄 폭발을 보존한 옛날 d-2 정품 파괴/리필 통합 엔진
    // 🎯 [완전 융합] 콤보 및 연쇄 폭발을 처리하는 통합 엔진 (리팩토링 버전)
    // 🎯 [오류 해결 완료 버전] 콤보와 연쇄 폭발을 에러 없이 완벽 처리하는 통합 엔진
    // 🎯 [완전 복구] 콤보와 연쇄 폭발을 에러 없이 완벽 처리하는 통합 엔진
    // 🎯 [d-2 정품 연쇄 폭발 + 최신 콤보 시스템 융합]
    private IEnumerator DestroyAndRefillRoutine(List<GameObject> matches)
    {
        while (matches.Count > 0)
        {
            // 1. 최신 기획 반영: 폭발할 때마다 콤보 수치 상승 및 UI 반영
            comboCount++;
            UpdateComboTextUI(); 

            if (PuzzleBattleManager.Instance != null)
            {
                PuzzleBattleManager.Instance.UpdateTurnTextUI();
            }

            // 2. 옛날 d-2 정품 방식: 안전하게 장부(배열) 비우고 화면에서 블록 제거
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

            // 블록이 팡 터지는 연출을 위한 잠깐의 대기시간
            yield return new WaitForSeconds(0.15f);

            // 3. 옛날 d-2 정품 방식: 기존 블록을 아래로 떨구고, 천장에서 새 블록 소환
            yield return StartCoroutine(DropExistingBlocksRoutine());
            yield return StartCoroutine(RefillNewBlocksRoutine());

            // 4. [무한 콤보 핵심]: 다 떨어져 내린 후 또 3개가 맞았는지 보드판 전수 조사!
            matches = FindAllMatches();
        }

        // 연속 폭발이 완전히 끝났을 때 콤보 초기화 및 판막힘(데드락) 최종 검사
        comboCount = 0;
        yield return StartCoroutine(CheckPostProcessAndDeadlock());
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
                        // 새 블록 생성 및 보드판의 자식으로 등록
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
                        newBlock. name = $"블록_{ rawColor}_{ x}_{ y}";
                        
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
        }
        
        // 블록들이 바닥에 안착할 때까지 안전하게 0.2초 대기
        yield return new WaitForSeconds( 0.2f);
    }
    // ---- [복붙 끝] ----------------------------------------------------

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
    private IEnumerator ResolveDeadlockRoutine()
    {
        isProcessing = true;
        Debug.LogWarning("🚨 [데드락 발동] 움직일 조합 없음! 위에서 아래로 순서대로 소멸 후 재배치합니다.");

        // 1. 옛날 정품 d-2 방식: 12시(맨 윗줄 y=height-1)부터 6시(맨 아랫줄 y=0) 방향으로 시간차 삭제
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (allBlocks[x, y] != null)
                {
                    Destroy(allBlocks[x, y]);
                    allBlocks[x, y] = null;
                }
            }
            // 한 줄 지울 때마다 0.05초씩 쉬어서 스르륵 사라지는 그라데이션 느낌 연출
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(0.2f);

        // 2. 새로운 블록들을 천장에서 대량 낙하시켜 빈칸 채우기
        yield return StartCoroutine(RefillNewBlocksRoutine());
        
        isProcessing = false;

        // 3. 새로 채워진 보드판에 자동으로 터지는 블록이 있는지 2차 연쇄 추적 점검
        yield return StartCoroutine(CheckPostProcessAndDeadlock());
    }


    private IEnumerator CheckPostProcessAndDeadlock()
    {
        if (!CheckPossibleMatchesExist())
        {
            yield return StartCoroutine(ResolveDeadlockRoutine());
        }
    }

    public void ShutdownAndCleanupBoard()
    {
        isGameActive = false;
        isProcessing = true;

        ClearAllBoardObjects();

        List<GameObject> leakObjects = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child != null && child.gameObject.name.StartsWith("블록_"))
            {
                leakObjects.Add(child.gameObject);
            }
        }

        for (int i = 0; i < leakObjects.Count; i++)
        {
            Destroy(leakObjects[i]);
        }
        leakObjects.Clear();

        comboCount = 0;
        UpdateComboTextUI();
        Debug.Log("✨ [성공] 보드판 2차 유령 찌꺼기 추적 소멸 완수.");
    }
} // ⭕ 클래스를 안전하게 닫아주는 영광의 웅장한 닫는 중괄호!!
