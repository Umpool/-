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

[Header("화면 전환용 패널 등록")]
public GameObject panel_Intro;  // 유니티 인스펙터에서 Panel_Intro를 연결할 칸
public GameObject panel_Title;  // 유니티 인스펙터에서 Panel_Title을 연결할 칸

    [Header("타이틀 화면에서 보여줄 버튼들")]
    public GameObject[] visibleElements;

    private bool canProceed = false;

    void Awake()
    {
        // 🏁 [게임시작시]: 무엇보다 가장 먼저 Panel_Intro와 그 자식들을 통째로 눈에 보이게 켭니다.
        if (panel_Intro != null)
        {
            panel_Intro.SetActive(true);
        }
    }

    // 💡 기존의 로딩 시퀀스 연산은 유니티가 온전히 준비된 Start 타이밍에 안전하게 바통을 이어받아 출발시킵니다.
// 🌟 [교체 삽입]: 기존 void Start() 지운 자리에 이대로 붙여넣으세요.
    void Start()
    {
        introPanel.SetActive(true);
        titlePanel.SetActive(false);
        loadingBar.value = 0f;
        statusText.text = "";
        StartCoroutine(LoadingSequence());
    }


// 🌟 [최종 전환 스위치]: 기존 함수를 지운 자리에 이 코드를 그대로 붙여넣으세요.
public void OnClickIntroScreenChange()
{
    GameObject targetIntro = null;
    GameObject targetTitle = null;

    // 🔎 [하이어라키 수색]: 꺼진 채로 시작해서 눈을 감고 숨어있는 패널들을 정밀 추적합니다.
    GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
    foreach (GameObject obj in allObjects)
    {
        if (!obj.transform.parent) // 부모가 없는 최상위 독립 오브젝트들만 필터링합니다.
        {
            if (obj.name == "Panel_Intro") targetIntro = obj;
            if (obj.name == "Panel_Title") targetTitle = obj;
        }
    }

    // ❌ [인트로 끄기]: 찾아낸 인트로 화면을 비활성화하여 숨깁니다.
    if (targetIntro != null) targetIntro.SetActive(false);
    
    // ⭕ [타이틀 켜기]: 숨어있던 메인 타이틀 패널을 강제로 화면 위로 호출하여 켭니다!
    if (targetTitle != null) 
    {
        targetTitle.SetActive(true);
        Debug.Log("🎬 [성공] 독립형 인트로 OFF -> 타이틀 ON 완공 완료!");
    }
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

    // 💡 [클릭 연동 수선]: 인트로 화면 터치 즉시 타이틀 패널 가동 회선 확정!

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
    // 🔗 [인스펙터 버튼 연결용 징검다리 스위치]
    public void OnClickIntroFreePass()
    {
        // 버튼을 누르면 밑에 있는 진짜 클릭 함수(OnPointerClick)를 강제로 격발시킵니다!
        OnPointerClick(null);
    }
}

