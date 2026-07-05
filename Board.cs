using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 🎯 [오류 해결]: PointerEventData를 컴퓨터가 인식할 수 있게 문을 열어줍니다!


public class Board : MonoBehaviour
{
    [Header("ㅡ 보드 기본 설정 ㅡ")]
    public int rows = 6;      // 가로 6칸
    public int cols = 6;      // 세로 6칸
    public float cellSize = 100f; // UI 블록 한 칸의 크기
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
    private bool isGameActive = false;


    private float[] comboDamageMultipliers = new float[] { 1.0f, 1.2f, 1.5f, 1.8f, 2.0f, 2.5f };

    private void Awake()
    {
        allBlocks = new GameObject[rows, cols];
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
    public void InitializeNewBoard()
    {
        ClearAllBoardObjects();

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
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
        Debug.Log("🎲 [성공] 자동 매칭이 방지된 6x6 보드가 배치되었습니다.");
    }

    private void SpawnBlockAt(int prefabIndex, int x, int y)
    {
        // 1. 블록을 생성하며 부모(PuzzleBoard)의 자식으로 입주시킵니다.
        GameObject newBlock = Instantiate(blockPrefabs[prefabIndex], transform);
        
        RectTransform rt = newBlock.GetComponent<RectTransform>();
        if (rt != null)
        {
            // ⭕ [36칸 보드에 블록들의 간격]
    float anchorMinX = (float)x / cols;
    float anchorMaxX = (float)(x + 1) / cols;
    float anchorMinY = (float)y / rows;
    float anchorMaxY = (float)(y + 1) / rows;

    rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
    rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);

    rt.offsetMin = new Vector2(5f, 5f);
    rt.offsetMax = new Vector2(-5f, -5f);
    rt.localScale = Vector3.one;
}



        string rawColor = GetBlockColor(blockPrefabs[prefabIndex]);
        newBlock.name = $"블록_{rawColor}_{Random.Range(10, 99)}";
        allBlocks[x, y] = newBlock;
    }


    public void ClearAllBoardObjects()
    {
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
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
    [Header("ㅡ 마우스 및 드래그 제어 ㅡ")]
    private GameObject selectedBlock = null; 
    private Vector2 clickStartPos;           
    private int startX, startY;              
    private bool isSwappingNow = false;       

    private void Update()
    {
        if (!isGameActive || isProcessing || isSwappingNow) return;
        HandleMouseInput();
    }

private void HandleMouseInput()
{
    // 마우스 왼쪽 버튼을 딱 누르는 타이밍을 감지합니다.
    if (Input.GetMouseButtonDown(0))
    {
        // 🎯 [UI 전용 레이캐스트]: 화면 맨 앞 투명 버튼을 통과하여 블록만 골라냅니다.
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // 찾은 오브젝트 이름이 "블록_"으로 시작할 때만 낚아챕니다.
            if (result.gameObject != null && result.gameObject.name.StartsWith("블록_"))
            {
                selectedBlock = result.gameObject;
                clickStartPos = Input.mousePosition;
                FindBlockIndex(selectedBlock, out startX, out startY);
                break;
            }
        }
    }

    // 2-2: 마우스 버튼을 뗐을 때 드래그 거리를 계산하여 블록 이동 처리
    if (Input.GetMouseButtonUp(0) && selectedBlock != null)
    {
        Vector2 clickEndPos = Input.mousePosition;
        Vector2 swipeDelta = clickEndPos - clickStartPos;

        // 드래그 거리(40픽셀)가 충분하면 스와이프 방향 계산
        if (swipeDelta.magnitude > 40f)
        {
            CalculateSwipeDirection(swipeDelta);
        }
        selectedBlock = null; // 선택 해제
    }
} // HandleMouseInput 함수 끝


    private void FindBlockIndex(GameObject target, out int xPos, out int yPos)
    {
        xPos = -1; yPos = -1;
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (allBlocks[x, y] == target)
                {
                    xPos = x; yPos = y;
                    return;
                }
            }
        }
    }

    private void CalculateSwipeDirection(Vector2 delta)
    {
        int targetX = startX;
        int targetY = startY;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            targetX += delta.x > 0 ? 1 : -1;
        }
        else
        {
            targetY += delta.y > 0 ? 1 : -1;
        }

        if (targetX >= 0 && targetX < rows && targetY >= 0 && targetY < cols)
        {
            StartCoroutine(SwapBlocksRoutine(startX, startY, targetX, targetY));
        }
    }

    private IEnumerator SwapBlocksRoutine(int x1, int y1, int x2, int y2)
    {
        isSwappingNow = true;
        currentTurn++; 

        if (PuzzleBattleManager.Instance != null)
        {
            PuzzleBattleManager.Instance.UpdateTurnTextUI();
        }

        Debug.Log($"⏳ [턴 카운트] 유저 조작 감지! 현재 누적 턴수: {currentTurn}턴");

        GameObject block1 = allBlocks[x1, y1];
        GameObject block2 = allBlocks[x2, y2];

        if (dragLayerParent != null) block1.transform.SetParent(dragLayerParent);
        else block1.transform.SetAsLastSibling();

        Vector2 pos1 = new Vector2(x1 * cellSize, y1 * cellSize);
        Vector2 pos2 = new Vector2(x2 * cellSize, y2 * cellSize);

        float duration = 0.25f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (block1 != null) block1.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(pos1, pos2, t);
            if (block2 != null) block2.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(pos2, pos1, t);
            yield return null;
        }

        allBlocks[x1, y1] = block2;
        allBlocks[x2, y2] = block1;

        if (block1 != null) block1.transform.SetParent(transform);

        yield return StartCoroutine(JudgeMatchAndProcess(x1, y1, x2, y2));
    }

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
            comboCount = 0; 
            UpdateComboTextUI();
            Debug.Log($"❌ 매치 실패! 콤보 카운트 리셋. 원위치로 복귀합니다.");

            GameObject block1 = allBlocks[x2, y2]; 
            GameObject block2 = allBlocks[x1, y1];

            block1.transform.SetAsLastSibling();

            Vector2 posCurrent1 = new Vector2(x2 * cellSize, y2 * cellSize);
            Vector2 posCurrent2 = new Vector2(x1 * cellSize, y1 * cellSize);

            float duration = 0.2f; 
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (block1 != null) block1.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(posCurrent1, posCurrent2, t);
                if (block2 != null) block2.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(posCurrent2, posCurrent1, t);
                yield return null;
            }

            allBlocks[x1, y1] = block1;
            allBlocks[x2, y2] = block2;
        }

        isSwappingNow = false;
        isProcessing = false;
    }

    private List<GameObject> FindAllMatches()
    {
        List<GameObject> matches = new List<GameObject>();

        for (int y = 0; y < cols; y++)
        {
            for (int x = 0; x < rows - 2; x++)
            {
                GameObject b1 = allBlocks[x, y];
                GameObject b2 = allBlocks[x + 1, y];
                GameObject b3 = allBlocks[x + 2, y];

                if (b1 == null || b2 == null || b3 == null) continue;

                if (GetBlockColor(b1) == GetBlockColor(b2) && GetBlockColor(b2) == GetBlockColor(b3))
                {
                    if (!matches.Contains(b1)) matches.Add(b1);
                    if (!matches.Contains(b2)) matches.Add(b2);
                    if (!matches.Contains(b3)) matches.Add(b3);
                }
            }
        }

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols - 2; y++)
            {
                GameObject b1 = allBlocks[x, y];
                GameObject b2 = allBlocks[x, y + 1];
                GameObject b3 = allBlocks[x, y + 2];

                if (b1 == null || b2 == null || b3 == null) continue;

                if (GetBlockColor(b1) == GetBlockColor(b2) && GetBlockColor(b2) == GetBlockColor(b3))
                {
                    if (!matches.Contains(b1)) matches.Add(b1);
                    if (!matches.Contains(b2)) matches.Add(b2);
                    if (!matches.Contains(b3)) matches.Add(b3);
                }
            }
        }

        return matches;
    }
    private IEnumerator DestroyAndRefillRoutine(List<GameObject> matches)
    {
        while (matches.Count > 0)
        {
            comboCount++; 
            UpdateComboTextUI(); // 이사 온 콤보 UI 실시간 격발
            
            float currentMultiplier = GetComboMultiplier();
            Debug.Log($"💥 [폭발] {matches.Count}개 파괴! 현재 {comboCount}콤보 (배율: {currentMultiplier}배)");

            foreach (GameObject block in matches)
            {
                if (block != null)
                {
                    FindBlockIndex(block, out int x, out int y);
                    if (x != -1 && y != -1) allBlocks[x, y] = null;
                    Destroy(block);
                }
            }

            yield return new WaitForSeconds(0.15f);

            yield return StartCoroutine(DropExistingBlocksRoutine());
            yield return StartCoroutine(RefillNewBlocksRoutine());

            matches = FindAllMatches();
        }

        // 하늘에서 다 떨어지고 안정화되었을 때 데드락(판막힘) 최종 전수 조사 기동
        yield return StartCoroutine(CheckPostProcessAndDeadlock());
    }

    private IEnumerator DropExistingBlocksRoutine()
    {
        float duration = 0.2f; 
        List<Coroutine> activeMoves = new List<Coroutine>();

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (allBlocks[x, y] == null)
                {
                    for (int ku = y + 1; ku < cols; ku++)
                    {
                        if (allBlocks[x, ku] != null)
                        {
                            allBlocks[x, y] = allBlocks[x, ku];
                            allBlocks[x, ku] = null;

                            Vector2 targetPos = new Vector2(x * cellSize, y * cellSize);
                            activeMoves.Add(StartCoroutine(SmoothMoveBlock(allBlocks[x, y], targetPos, duration)));
                            break;
                        }
                    }
                }
            }
        }
        foreach (var move in activeMoves) yield return move;
    }

    private IEnumerator RefillNewBlocksRoutine()
    {
        float duration = 0.25f;
        List<Coroutine> activeMoves = new List<Coroutine>();

        for (int x = 0; x < rows; x++)
        {
            int missingCount = 0;
            
            // 아래(0)에서부터 위(cols-1)로 올라가며 빈칸을 찾습니다.
            for (int y = 0; y < cols; y++)
            {
                if (allBlocks[x, y] == null)
                {
                    missingCount++;
                    int randomIndex = Random.Range(0, blockPrefabs.Length);
                    
                    // 블록 생성
                    GameObject newBlock = Instantiate(blockPrefabs[randomIndex], puzzleBoard);
                    RectTransform rt = newBlock.GetComponent<RectTransform>();
                    
                    // 위에서 정방향으로 차곡차곡 쌓이도록 시작 Y 위치 계산
                    float startY = (cols + missingCount) * cellSize;
                    rt.anchoredPosition = new Vector2(x * cellSize, startY);
                    
                    string rawColor = GetBlockColor(blockPrefabs[randomIndex]);
                    newBlock.name = $"블록_{rawColor}_{Random.Range(10, 99)}";
                    
                    // 데이터 배열에 저장
                    allBlocks[x, y] = newBlock;
                    
                    // 목표 위치로 부드럽게 떨어뜨리기
                    Vector2 targetPos = new Vector2(x * cellSize, y * cellSize);
                    activeMoves.Add(StartCoroutine(SmoothMoveBlock(newBlock, targetPos, duration)));
                } // [if문 끝]
            } // [y축 for문 끝]
        } // [x축 for문 끝]

        // ✨ 중요: 모든 블록 생성이 끝난 '후'에 움직임이 멈출 때까지 기다려줍니다.
        foreach (var move in activeMoves) 
        {
            yield return move;
        }
    } // [RefillNewBlocksRoutine 함수 최종 끝!]

        private IEnumerator SmoothMoveBlock(GameObject target, Vector2 targetAnchoredPos, float time)
    {
        if (target == null) yield break;

        RectTransform rt = target.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 startPos = rt.anchoredPosition;
        float elapsed = 0f;

        // 지정된 시간(duration) 동안 부드럽게 목표 격자 좌표로 이동시키는 반복문
        while (elapsed < time)
        {
            if (target == null) yield break; // 중간에 블록이 터지면 탈출
            
            elapsed += Time.deltaTime;
            // ✨ [핵심 수정]: Lerp(선형 보간)를 이용해 화면 좌표를 오차 없이 정밀하게 이동시킵니다.
            rt.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, elapsed / time);
            yield return null;
        }

        // 최종 이동이 끝난 후 미세한 오차를 없애기 위해 확실하게 목표 좌표를 꽂아줍니다.
        if (target != null)
        {
            rt.anchoredPosition = targetAnchoredPos;
        }
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

    private bool CheckPossibleMatchesExist()
    {
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (allBlocks[x, y] == null) continue;

                if (x < rows - 1 && allBlocks[x + 1, y] != null)
                {
                    if (SimulateSwapAndCheckMatch(x, y, x + 1, y)) return true;
                }
                if (y < cols - 1 && allBlocks[x, y + 1] != null)
                {
                    if (SimulateSwapAndCheckMatch(x, y, x, y + 1)) return true;
                }
            }
        }
        return false; 
    }

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

    private IEnumerator ResolveDeadlockRoutine()
    {
        isProcessing = true;
        Debug.LogWarning("🚨 [데드락 감지] 움직일 조합이 없습니다! 판을 비우고 하늘에서 새로 떨어집니다.");

        ClearAllBoardObjects();
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(RefillNewBlocksRoutine());

        isProcessing = false;
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
