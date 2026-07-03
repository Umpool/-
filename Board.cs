using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Board : MonoBehaviour
{
    [Header("블록 원본(프리팹) 등록")]
    public GameObject[] blockPrefabs; // Red, Yellow, Green, Blue, Purple, Black 순서 등록
    public GameObject rewardPanel;

    [Header("판 크기 설정")]
    public int width = 6;  // 기획안 2번: 6x6 사이즈 고정
    public int height = 6;

    // 🌟 [해결]: 블록들이 서로 걸쳐서 엉뚱하게 클릭되는 현상을 막기 위해 격자 간격을 105로 시원하게 넓혔습니다!
    private float blockSpacing = 105f;

    private GameObject[,] allBlocks;
    private GameObject selectedBlock;

    private bool isSwapping = false;
    private bool isMatching = false;
    private bool isUserTurn = false; // 기획안 3-1, 3-2: 유저 조작 여부 판별 스위치

    public void SetupStage(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
    }

    void OnEnable()
    {
        InputManager.OnInputStart += HandleInputStart;
        InputManager.OnInputEnd += HandleInputEnd;
    }

    void OnDisable()
    {
        InputManager.OnInputStart -= HandleInputStart;
        InputManager.OnInputEnd -= HandleInputEnd;
    }

    // 최종 수정된 UI 전용 보드 소환 엔진
    public void CreateBoard()
    {
        // 1. 기존 잔여 블록 UI 완전 청소
        foreach (Transform child in transform) { Destroy(child.gameObject); }

        allBlocks = new GameObject[width, height];

        // 2. 6x6 보드 배치 루프 시작
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int randomIndex = GetValidBlockIndex(x, y);
                GameObject prefabToSpawn = blockPrefabs[randomIndex];

                if (prefabToSpawn != null)
                {
                    // 월드 인스턴스가 아닌 UI 복사 방식으로 뻥튀기 원천 차단
                    GameObject block = Instantiate(prefabToSpawn);
                    block.transform.SetParent(this.transform, false);

                    RectTransform rect = block.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.localScale = Vector3.one;
                        rect.localRotation = Quaternion.identity;
                        rect.anchoredPosition = GetUIAnchoredPosition(x, y);
                    }

                    // 기획안 조건: 영문 이름 식별용 정렬 이름 부여
                    block.name = prefabToSpawn.name + "_" + x + "_" + y;
                    allBlocks[x, y] = block;
                }
            }
        }

        // 배치 완료 후 혹시 모를 데드락 검사
        if (CheckIsDeadlock())
        {
            StartCoroutine(HandleDeadlockRoutine());
        }
    }

    // UI 공간 전용 정밀 격자 좌표 계산식
    private Vector2 GetUIAnchoredPosition(int x, int y)
    {
        float startX = -((width - 1) * blockSpacing) / 2f;
        float startY = -((height - 1) * blockSpacing) / 2f;
        return new Vector2(startX + (x * blockSpacing), startY + (y * blockSpacing));
    } //파트111111111111111111111111111111111
      // 시작 시 강제 3매치 버그 방지 공식
    int GetValidBlockIndex(int x, int y)
    {
        List<int> validIndices = new List<int>();
        for (int i = 0; i < blockPrefabs.Length; i++) validIndices.Add(i);

        if (x >= 2)
        {
            GameObject l1 = allBlocks[x - 1, y];
            GameObject l2 = allBlocks[x - 2, y];
            if (l1 != null && l2 != null && GetBlockColorName(l1).Equals(GetBlockColorName(l2)))
                validIndices.Remove(GetBlockTypeIndexByName(GetBlockColorName(l1)));
        }
        if (y >= 2)
        {
            GameObject d1 = allBlocks[x, y - 1];
            GameObject d2 = allBlocks[x, y - 2];
            if (d1 != null && d2 != null && GetBlockColorName(d1).Equals(GetBlockColorName(d2)))
                validIndices.Remove(GetBlockTypeIndexByName(GetBlockColorName(d1)));
        }

        if (validIndices.Count == 0) return Random.Range(0, blockPrefabs.Length);
        return validIndices[Random.Range(0, validIndices.Count)];
    }

    // 기획안 조건: 대소문자 무시 이름 기반 색상 구분 검사기
    string GetBlockColorName(GameObject block)
    {
        if (block == null) return "none";
        string bName = block.name.ToLower();
        if (bName.Contains("red")) return "red";
        if (bName.Contains("yellow")) return "yellow";
        if (bName.Contains("green")) return "green";
        if (bName.Contains("blue")) return "blue";
        if (bName.Contains("purple")) return "purple";
        if (bName.Contains("black")) return "black";
        return "none";
    }

    int GetBlockTypeIndexByName(string colorName)
    {
        if (colorName.Equals("red")) return 0;
        if (colorName.Equals("yellow")) return 1;
        if (colorName.Equals("green")) return 2;
        if (colorName.Equals("blue")) return 3;
        if (colorName.Equals("purple")) return 4;
        if (colorName.Equals("black")) return 5;
        return 0;
    }

    // 🌟 [1번 버그 완벽 수정]: 클릭 판정을 더 넓고 관대하게 낚아채는 모던 포인터 센서 장착
    public void HandleInputStart(Vector2 screenPos)
    {
        if (isSwapping || isMatching || Camera.main == null) return;

        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // 이제 테두리나 겹친 구역에 상관없이 이름에 'Block'이 들어간 타겟을 부드럽게 실시간 선점합니다.
            if (result.gameObject.name.Contains("Block"))
            {
                selectedBlock = result.gameObject;
                return;
            }
        }
    }

    public void HandleInputEnd(Vector2 screenPos)
    {
        if (selectedBlock == null || isSwapping || isMatching) return;

        Vector2 startScreenPos = InputManager.Instance.GetStartPosition();
        Vector2 swipeDir = screenPos - startScreenPos;

        if (swipeDir.magnitude < 40f) // 해상도 안전 드래그 거리 판정식
        {
            selectedBlock = null;
            return;
        }

        string[] nameParts = selectedBlock.name.Split('_');
        int currentX = int.Parse(nameParts[nameParts.Length - 2]);
        int currentY = int.Parse(nameParts[nameParts.Length - 1]);

        int targetX = currentX;
        int targetY = currentY;

        if (Mathf.Abs(swipeDir.x) > Mathf.Abs(swipeDir.y))
            targetX += swipeDir.x > 0 ? 1 : -1;
        else
            targetY += swipeDir.y > 0 ? 1 : -1;

        if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
        {
            GameObject targetBlock = allBlocks[targetX, targetY];
            if (targetBlock != null)
            {
                isUserTurn = true; // 유저가 직접 조작했음을 기록
                StartCoroutine(SwapBlocksRoutine(currentX, currentY, targetX, targetY));
            }
        }
        selectedBlock = null;
    }//파트2222222222222222222222222222222
     // 기획안 5, 5-1번 충족: 매치 실패 시 부드럽게 원래 위치 원위치 복귀 연출 코루틴
    private IEnumerator SwapBlocksRoutine(int ax, int ay, int bx, int by)
    {
        isSwapping = true;
        GameObject blockA = allBlocks[ax, ay];
        GameObject blockB = allBlocks[bx, by];

        RectTransform rectA = blockA.GetComponent<RectTransform>();
        RectTransform rectB = blockB.GetComponent<RectTransform>();

        Vector2 posA = GetUIAnchoredPosition(ax, ay);
        Vector2 posB = GetUIAnchoredPosition(bx, by);

        // 자리가 바뀌는 애니메이션 (0.2초 동안 부드럽게 이동)
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;
            rectA.anchoredPosition = Vector2.Lerp(posA, posB, t);
            rectB.anchoredPosition = Vector2.Lerp(posB, posA, t);
            yield return null;
        }

        rectA.anchoredPosition = posB;
        rectB.anchoredPosition = posA;

        // 배열 데이터 교체
        allBlocks[ax, ay] = blockB;
        allBlocks[bx, by] = blockA;

        // 이름 뒤의 X, Y 좌표 텍스트 갱신
        string namePrefixA = blockA.name.Substring(0, blockA.name.LastIndexOf('_'));
        namePrefixA = namePrefixA.Substring(0, namePrefixA.LastIndexOf('_'));
        string namePrefixB = blockB.name.Substring(0, blockB.name.LastIndexOf('_'));
        namePrefixB = namePrefixB.Substring(0, namePrefixB.LastIndexOf('_'));

        blockA.name = namePrefixA + "_" + bx + "_" + by;
        blockB.name = namePrefixB + "_" + ax + "_" + ay;

        // 기획안 6번: 교체 후 보드판 매치 점검 가동
        bool hasMatches = CheckHasMatches();

        if (hasMatches)
        {
            isSwapping = false;
            StartCoroutine(CheckAndDestroyMatchesRoutine());
        }
        else
        {

            // [유저 기획 반영] 드래그로 블록 파괴에 실패했으므로 즉시 콤보를 0으로 리셋합니다.
            if (PuzzleBattleManager.Instance != null)
            {
                PuzzleBattleManager.Instance.currentCombo = 0;
                PuzzleBattleManager.Instance.UpdateComboTextUI(); // 화면 글씨도 지우기
            }
            // [수정] 올바른 매개변수(ax, ay, bx, by)로 원래 자리 되돌리기 애니메이션 구동
            yield return StartCoroutine(SwapBlocksRoutine(ax, ay, bx, by));

            isSwapping = false;
            isUserTurn = false; // 매치 실패 시 턴 증가 플래그 리셋
        } // 👈 꼬여있던 else 문을 닫아주는 중괄호입니다!
    }

        bool CheckHasMatches()
    {
        // 가로 방향 3매치 검사
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                GameObject b1 = allBlocks[x, y];
                GameObject b2 = allBlocks[x + 1, y];
                GameObject b3 = allBlocks[x + 2, y];
                if (b1 != null && b2 != null && b3 != null)
                {
                    string c1 = GetBlockColorName(b1);
                    string c2 = GetBlockColorName(b2);
                    string c3 = GetBlockColorName(b3);
                    if (!c1.Equals("none") && c1.Equals(c2) && c2.Equals(c3)) return true;
                }
            }
        }
        // 세로 방향 3매치 검사
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                GameObject b1 = allBlocks[x, y];
                GameObject b2 = allBlocks[x, y + 1];
                GameObject b3 = allBlocks[x, y + 2];
                if (b1 != null && b2 != null && b3 != null)
                {
                    string c1 = GetBlockColorName(b1);
                    string c2 = GetBlockColorName(b2);
                    string c3 = GetBlockColorName(b3);
                    if (!c1.Equals("none") && c1.Equals(c2) && c2.Equals(c3)) return true;
                }
            }
        }
        return false;
    }

    // 기획안 4번, 6번: 3개 이상 연속 매치 블록 일괄 소멸 및 연쇄 수거
    IEnumerator CheckAndDestroyMatchesRoutine()
    {
        isMatching = true;
        List<GameObject> matchesList = new List<GameObject>();

        // 가로 매치 리스트 수거
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                GameObject b1 = allBlocks[x, y];
                GameObject b2 = allBlocks[x + 1, y];
                GameObject b3 = allBlocks[x + 2, y];
                if (b1 != null && b2 != null && b3 != null)
                {
                    string c1 = GetBlockColorName(b1); string c2 = GetBlockColorName(b2); string c3 = GetBlockColorName(b3);
                    if (!c1.Equals("none") && c1.Equals(c2) && c2.Equals(c3))
                    {
                        if (!matchesList.Contains(b1)) matchesList.Add(b1);
                        if (!matchesList.Contains(b2)) matchesList.Add(b2);
                        if (!matchesList.Contains(b3)) matchesList.Add(b3);
                    }
                }
            }
        }

        // 세로 매치 리스트 수거
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                GameObject b1 = allBlocks[x, y];
                GameObject b2 = allBlocks[x, y + 1];
                GameObject b3 = allBlocks[x, y + 2];
                if (b1 != null && b2 != null && b3 != null)
                {
                    string c1 = GetBlockColorName(b1); string c2 = GetBlockColorName(b2); string c3 = GetBlockColorName(b3);
                    if (!c1.Equals("none") && c1.Equals(c2) && c2.Equals(c3))
                    {
                        if (!matchesList.Contains(b1)) matchesList.Add(b1);
                        if (!matchesList.Contains(b2)) matchesList.Add(b2);
                        if (!matchesList.Contains(b3)) matchesList.Add(b3);
                    }
                }
            }
        }//파트33333333333333333
            if (matchesList.Count > 0)
            {
                // 유저가 직접 움직여서 발생한 첫 매치일 때만 정직하게 1턴을 소모합니다.
                if (isUserTurn)
                {
                    if (PuzzleBattleManager.Instance != null)
                    {
                        PuzzleBattleManager.Instance.currentTurn++;
                        PuzzleBattleManager.Instance.UpdateTurnTextUI();
                    }
                    isUserTurn = false; // 첫 정산 완료 후 플래그를 꺼서 자동 연쇄는 턴을 안 씁니다.
                }

                // [유저 기획 반영] 블록이 파괴될 때마다 정직하게 1콤보씩 차곡차곡 누적합니다.
                if (PuzzleBattleManager.Instance != null)
                {
                    PuzzleBattleManager.Instance.currentCombo++;
                    PuzzleBattleManager.Instance.UpdateComboTextUI(); // 실시간 콤보 글씨 갱신
                }

                int comboCount = PuzzleBattleManager.Instance != null ? PuzzleBattleManager.Instance.currentCombo : 1;

                // 1콤보부터 대미지 배율이 조금씩 상승하는 시스템 (1콤보=100%, 2콤보=110%, 3콤보=120%...)
                float comboDamageMultiplier = PuzzleBattleManager.Instance != null ? PuzzleBattleManager.Instance.comboDamageMultiplier : 0.1f;
                float finalMultiplier = 1f + ((comboCount - 1) * comboDamageMultiplier);

                // 터진 블록 1개당 100 대미지 계산 후 최종 콤보 배율 곱하기!
                float baseDamage = matchesList.Count * 100f;
                float damageDealt = baseDamage * finalMultiplier;

                Debug.Log($"🔥 {comboCount} 콤보 달성! (배율: {finalMultiplier * 100}%) -> 몬스터에게 {damageDealt} 대미지!");

                // 계산된 최종 연쇄 대미지로 무한 몬스터를 때립니다.
                if (InfiniteMonster.Instance != null)
                {
                    InfiniteMonster.Instance.TakeDamage(damageDealt);
                }
            }
            else
            {
                Debug.Log("[기획 3-2번 성공] 자동 연쇄 낙하 폭발 발생! 턴 수 증가는 면제됩니다.");
            }

            // 외부 전투 시스템 타격 데미지 원격 통신 발송
            int dmg = matchesList.Count * 10;
            if (GameManager.Instance != null) GameManager.Instance.DamageEnemy(dmg);

            // 보드 데이터 맵에서 제거 및 오브젝트 파괴
            foreach (GameObject b in matchesList)
            {
                if (b != null)
                {
                    string[] parts = b.name.Split('_');
                    int bx = int.Parse(parts[parts.Length - 2]);
                    int by = int.Parse(parts[parts.Length - 1]);
                    allBlocks[bx, by] = null;
                    Destroy(b);
                }
            }

            yield return new WaitForSeconds(0.15f);
            yield return StartCoroutine(DropBlocksRoutine());  // 기획안 4-1번: 상 방향 블록으로 빈칸 채우기
            yield return StartCoroutine(RefillBoardRoutine()); // 기획안 4-1번: 천장에서 새 블록 비처럼 생성

            // 또 매칭되는 블록이 있다면 콤보 재귀 반복 (이때는 유저 조작이 아니므로 턴 안 올라감)
            if (CheckHasMatches())
            {
                yield return StartCoroutine(CheckAndDestroyMatchesRoutine());
            }
            else if (CheckIsDeadlock()) // 터질 게 없으면 데드락 최종 점검
            {
                yield return StartCoroutine(HandleDeadlockRoutine());
            }
        }

    

    // 🌟 [2번 버그 수정 포인트]: 위에서 떨어질 때 보석 이름에서 색상 접두사 단어가 유실되는 현상 원천 차단
    IEnumerator DropBlocksRoutine()
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

                            Vector2 targetUIPos = GetUIAnchoredPosition(x, y);
                            StartCoroutine(MoveBlockSmoothlyUI(allBlocks[x, y], targetUIPos));

                            // 원본 블록의 순수 이름(예: Block_Blue)을 칼같이 추적하여 결합합니다.
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

    // 🌟 [2번 버그 수정 포인트]: 새 블록이 리필될 때도 원본 프리팹 이름을 안전하게 보존
    IEnumerator RefillBoardRoutine()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allBlocks[x, y] == null)
                {
                    int randomBlockIndex = Random.Range(0, blockPrefabs.Length);
                    GameObject prefabToSpawn = blockPrefabs[randomBlockIndex];

                    if (prefabToSpawn != null)
                    {
                        GameObject block = Instantiate(prefabToSpawn);
                        block.transform.SetParent(this.transform, false);

                        RectTransform rect = block.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            rect.localScale = Vector3.one;
                            rect.localRotation = Quaternion.identity;
                            rect.anchoredPosition = GetUIAnchoredPosition(x, height);
                        }

                        Vector2 targetUIPos = GetUIAnchoredPosition(x, y);
                        StartCoroutine(MoveBlockSmoothlyUI(block, targetUIPos));

                        // 원본 프리팹의 온전한 이름 단어 뒤에만 인덱스를 결합시킵니다.
                        block.name = prefabToSpawn.name + "_" + x + "_" + y;
                        allBlocks[x, y] = block;
                    }
                }
            }
        }
        yield return new WaitForSeconds(0.2f);
    }

    // UI RectTransform 전용 초정밀 부드러운 스무스 이동 엔진 코루틴
    IEnumerator MoveBlockSmoothlyUI(GameObject block, Vector2 targetAnchoredPos)
    {
        if (block == null) yield break;
        RectTransform rect = block.GetComponent<RectTransform>();
        if (rect == null) yield break;

        float time = 0;
        Vector2 startPos = rect.anchoredPosition;

        while (time < 1f)
        {
            if (block == null) yield break;
            time += Time.deltaTime * 6f;
            rect.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, time);
            yield return null;
        }
        rect.anchoredPosition = targetAnchoredPos;
    }

    // 기획안 데드락 (ㄱ) 규칙: 상, 하, 좌, 우 움직여서 3개가 터지는 조합이 한 군데라도 있는지 가상 연산 검사
    bool CheckIsDeadlock()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                GameObject temp = allBlocks[x, y];
                allBlocks[x, y] = allBlocks[x + 1, y];
                allBlocks[x + 1, y] = temp;

                bool matchFound = CheckHasMatches();

                temp = allBlocks[x, y];
                allBlocks[x, y] = allBlocks[x + 1, y];
                allBlocks[x + 1, y] = temp;

                if (matchFound) return false; // 하나라도 움직여서 터질 가능성이 있다면 데드락 아님!
            }
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                GameObject temp = allBlocks[x, y];
                allBlocks[x, y] = allBlocks[x, y + 1];
                allBlocks[x, y + 1] = temp;

                bool matchFound = CheckHasMatches();

                temp = allBlocks[x, y];
                allBlocks[x, y] = allBlocks[x, y + 1];
                allBlocks[x, y + 1] = temp;

                if (matchFound) return false;
            }
        }
        Debug.Log("[데드락 발동] 유저가 움직여서 터뜨릴 수 있는 조합이 단 한 개도 없습니다!");
        return true;
    }

    // 기획안 데드락 (ㄴ, ㄷ) 규칙: 12시에서 6시 아래 방향 시간차 그라데이션 소멸 후 무더기 리필 하단 안착
    IEnumerator HandleDeadlockRoutine()
    {
        isSwapping = true;

        // 기획안 (ㄴ): Y축 역순(12시 맨 윗줄부터 6시 맨 아랫줄까지) 돌며 시간차 파괴
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
            yield return new WaitForSeconds(0.08f); // 위에서 아래로 쓸려내려가듯 소멸하는 그라데이션 연출 시간차
        }

        yield return new WaitForSeconds(0.3f);

        // 기획안 (ㄷ): 12시 천장 위 방향에서 새로운 블록들을 대량 낙하시켜 가장 하단부터 36칸 재배치
        yield return StartCoroutine(RefillBoardRoutine());

        // 무한 폭발 루프 방지용 재점검 루프 가동
        while (CheckHasMatches())
        {
            yield return StartCoroutine(CheckAndDestroyMatchesRoutine());
            yield return StartCoroutine(DropBlocksRoutine());
            yield return StartCoroutine(RefillBoardRoutine());
        }
        isSwapping = false;
    }

    public void OnClickRewardConfirmButton()
    {
        if (GameManager.Instance != null && GameManager.Instance.stageMode == 2)
        {
            GameManager.Instance.currentGameState = GameState.RewardSelect;
            Debug.Log("[보드 판정] 무한 모드 단판 종료 확인! 판 OFF 선포.");
        }
    }
}
