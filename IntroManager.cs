using UnityEngine;
using UnityEngine.UI;
using TMPro; // 최신 텍스트 사용을 위해 필수
using System.Collections;
using UnityEngine.EventSystems;

public class IntroManager : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 연결")]
    public GameObject introPanel;
    public GameObject titlePanel;
    public Slider loadingBar;
    public TextMeshProUGUI statusText; // Text 대신 TextMeshProUGUI로 변경

    [Header("타이틀 화면에서 보여줄 버튼들")]
    public GameObject[] visibleElements;

    private bool canProceed = false;

    void Start()
    {
        introPanel.SetActive(true);
        titlePanel.SetActive(false);
        loadingBar.value = 0f;
        statusText.text = "";
        StartCoroutine(LoadingSequence());
    }

IEnumerator LoadingSequence()
{
    float progress = 0f;
    float duration = 4.0f; // 전체 로딩 시간 (초 단위)

    while (progress < 1f)
    {
        progress += Time.deltaTime / duration;
        loadingBar.value = progress;

        // 진행도에 따라 텍스트 순차 변경
        if (progress < 0.25f)
            statusText.text = "당신을 위해 물을 끓이는 중.";
        else if (progress < 0.5f)
            statusText.text = "컵에 물을 붓는 중.";
        else if (progress < 0.75f)
            statusText.text = "물에 커피를 타는 중.";
        else
            statusText.text = "커피 완성★";

        yield return null; // 매 프레임마다 반복
    }

    // 로딩 완료 후 처리
    bool hasData = CheckSavedData();
    if (hasData)
    {
        statusText.text = "화면을 눌러주세요.";
        StartCoroutine(BlinkText(statusText));
        canProceed = true;
    }
    else
    {
        statusText.text = "저장된 데이터를 불러오는데 실패했습니다.";
        canProceed = false;
    }
}

    // 3. 텍스트 깜빡임 효과 (타입을 TextMeshProUGUI로 변경)
    IEnumerator BlinkText(TextMeshProUGUI text)
    {
        while (true)
        {
            for (float a = 1f; a >= 0f; a -= 0.05f)
            {
                text.color = new Color(text.color.r, text.color.g, text.color.b, a);
                yield return new WaitForSeconds(0.05f);
            }
            for (float a = 0f; a <= 1f; a += 0.05f)
            {
                text.color = new Color(text.color.r, text.color.g, text.color.b, a);
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    bool CheckSavedData()
    {
        return true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (canProceed)
        {
            // 1. 인트로/타이틀 전환
            introPanel.SetActive(false);
            titlePanel.SetActive(true);

            // 2. Visible Elements에 등록된 항목들 ON
            foreach (GameObject obj in visibleElements)
            {
                if (obj != null) obj.SetActive(true);
            }

            // 3. 본인 기능 정지 (이후 제어권은 GameManagers가 가짐)
            this.enabled = false;
        }
    }
}
