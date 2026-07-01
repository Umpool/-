using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필수적인 줄입니다!
using System.Collections;

public class TitleManager : MonoBehaviour
{
    [Header("타이틀 화면 패널")]
    public GameObject panel_Title;

    [Header("타이틀 하단 메뉴 버튼들")]
    public GameObject btn_Continue;
    public GameObject btn_Settings;
    public GameObject btn_Exit;

    [Header("설정창 팝업 직접 제어")]
    public GameObject panel_Settings; // 🌟 유니티 인스펙터에서 Panel_Settings 본체를 드래그해 넣을 칸입니다!

    [Header("타이틀 내부 팝업 관리")]
    public GameObject popup_DeleteConfirm;
    public GameObject confirm_Step1;
    public GameObject confirm_Step2;

    [Header("임시 데이터 연출 추가")]
    public TextMeshProUGUI noticeText; // 깜빡이게 할 Text (TMP) 오브젝트

    private Coroutine blinkCoroutine; // 깜빡임 작동을 제어할 리모컨 변수
    private Coroutine fadeCoroutine;  // 이어하기 전용 서서히 사라지는 리모컨

    void Start()
    {
        // 31~34번줄 영역: 독립 공용 팝업 및 알림 텍스트 초기화
        if (popup_DeleteConfirm) popup_DeleteConfirm.SetActive(false);
        if (confirm_Step1) confirm_Step1.SetActive(false);
        if (confirm_Step2) confirm_Step2.SetActive(false);
        if (noticeText) noticeText.gameObject.SetActive(false);

        // 37번줄 영역: 인스펙터 설정창도 우선 숨김
        if (panel_Settings) panel_Settings.SetActive(false);

        // 39번줄 영역: 타이틀 메인 메뉴 버튼 삼총사 활성화
        SetMainMenuButtonsActive(true);

        // 🌟 [개발자님의 명품 기획]: 일일이 하나씩 끄지 않고, 캔버스 전체를 자동 청소하는 시스템 가동!
        Transform canvasTransform = GameObject.Find("Canvas")?.transform;
        if (canvasTransform != null)
        {
            foreach (Transform child in canvasTransform)
            {
                // 이름이 "Panel_Title"인 타이틀 화면만 켜고, 나머지 모든 패널들은 예외 없이 전원 OFF!
                if (child.name == "Panel_Title")
                {
                    child.gameObject.SetActive(true);
                }
                else if (child.name == "popup_DeleteConfirm" || child.name == "GameManagers")
                {
                    // 엔진 핵심 오브젝트와 타이틀 팝업은 건드리지 않고 비켜갑니다.
                    continue;
                }
                else
                {
                    child.gameObject.SetActive(false); // 수십 수백개가 깔려있어도 여기서 전부 꺼집니다!
                }
            }
        }

        // 47~49번줄 영역: 아이폰 종료 버튼 리젝 방어 처리 유지
        #if UNITY_IOS
        if (btn_Exit != null) btn_Exit.SetActive(false);
        #endif
    }

    // 🌟 [기획안 완벽 추가]: PC, 안드로이드, 아이폰 환경을 자동 감지하는 스마트 휴식하기(종료) 함수!
    public void OnClickExitGame()
    {
        Debug.Log("[휴식하기] 플랫폼 감지 및 게임 종료 시도...");

        // A. 유니티 에디터(개발 중인 재생창)에서 테스트할 때 꺼지게 만드는 코드
#if UNITY_EDITOR
        Debug.Log("[휴식하기] 유니티 에디터 재생을 종료합니다.");
        UnityEditor.EditorApplication.isPlaying = false;

        // B. 아이폰(iOS) 환경일 때 실행될 영역 (만약 버튼을 강제로 노출시켰을 때를 대비한 안전 장치)
#elif UNITY_IOS
        Debug.LogWarning("[애플 정책 경고] 아이폰은 강제 종료가 불가능하므로 안내 문구를 출력합니다.");
        CleanUpPopupAndText();
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutNoticeRoutine("홈 버튼을 눌러 종료해 주세요."));

        // C. PC 빌드(윈도우/맥) 및 안드로이드(구글 플레이) 환경일 때 정상 종료 처리
#else
        Debug.Log("[휴식하기] 디바이스가 PC/안드로이드이므로 게임 프로그램을 정상 종료합니다.");
        Application.Quit();
#endif
    }

    // --- 모험 이어하기 버튼: 클릭시 --- (원본 100% 동일 유지)
    public void OnClickContinueAdventure()
    {
        Debug.Log("[모험 이어하기] 사용자의 데이터가 있는지 판정 중...");

        if (PlayerPrefs.HasKey("HasData"))
        {
            Debug.Log("[모험 이어하기] 1. 데이터가 있다면 마을화면으로 이동");
            if (panel_Title) panel_Title.SetActive(false);
            CleanUpPopupAndText();
            if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.panel_Village)
                    GameManager.Instance.panel_Village.SetActive(true);
                GameManager.Instance.UpdatePartyUI();
            }
        }
        else
        {
            Debug.Log("[모험 이어하기] 1-1. 데이터가 없다면 저장된 데이터가 없습니다! 출력후 점차사라지게하기");
            CleanUpPopupAndText();
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutNoticeRoutine("저장된 데이터가 없습니다!"));
        }
    }

    // --- 새로운 모험 버튼: 클릭시 --- (원본 100% 동일 유지)
    public void OnClickNewAdventure()
    {
        Debug.Log("[새로운 모험] 기존 데이터 판정 중...");
        SetMainMenuButtonsActive(false);

        if (PlayerPrefs.HasKey("HasData"))
        {
            Debug.Log("[새로운 모험] 1-1. 데이터가 있다면 off되어있던 Popup_DeleteConfirm가 on상태로 전환");
            if (popup_DeleteConfirm) popup_DeleteConfirm.SetActive(true);
            if (confirm_Step1) confirm_Step1.SetActive(true);
            if (confirm_Step2) confirm_Step2.SetActive(false);

            if (noticeText)
            {
                noticeText.text = "저장된 데이터가 있습니다.";
                noticeText.gameObject.SetActive(true);

                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkTextRoutine());
            }
        }
        else
        {
            Debug.Log("[새로운 모험] 1-2. 기존 데이터가 없을 때 (//메인 캐릭터 선택화면으로 이동)");

            MoveToCharacterSelect();
        }
    }

    // 확인1 클릭시 (기존 원본 유지)
    public void OnClickYes1()
    {
        if (confirm_Step1) confirm_Step1.SetActive(false);
        if (confirm_Step2) confirm_Step2.SetActive(true);
    }

    // 확인2 클릭시 (기존 원본 유지)
    public void OnClickYes2()
    {
        PlayerPrefs.DeleteAll();

        PlayerPrefs.SetInt("HasData", 1);
        PlayerPrefs.Save();

        CleanUpPopupAndText();
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
        MoveToCharacterSelect();
    }

    // 취소1 클릭시 (기존 원본 유지)
    public void OnClickNo1()
    {
        if (popup_DeleteConfirm) popup_DeleteConfirm.SetActive(false);
        if (confirm_Step1) confirm_Step1.SetActive(false);
        if (confirm_Step2) confirm_Step2.SetActive(false);

        CleanUpPopupAndText();
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }

        // [아이폰 기획 방어 규칙]: 취소하고 메인 메뉴 버튼을 켤 때도 
        // 아이폰 환경이라면 휴식하기 버튼은 꺼둔 상태를 칼같이 유지합니다.
        SetMainMenuButtonsActive(true);
#if UNITY_IOS
        if (btn_Exit != null) btn_Exit.SetActive(false);
#endif
    }

    // 취소2 클릭시 (기존 원본 유지)
    public void OnClickNo2()
    {
        if (confirm_Step2) confirm_Step2.SetActive(false);
        if (confirm_Step1) confirm_Step1.SetActive(true);
    }

    // 팝업이 완전히 닫힐 때 텍스트와 코루틴을 깔끔하게 정리하는 함수 (기존 원본 유지)
    private void CleanUpPopupAndText()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (noticeText) noticeText.gameObject.SetActive(false);
    }

    // 모험 이어하기 전용 문구 페이드아웃 루틴 (기존 유지)
    private IEnumerator FadeOutNoticeRoutine(string message)
    {
        if (noticeText == null) yield break;

        noticeText.text = message;
        noticeText.gameObject.SetActive(true);

        Color textColor = noticeText.color;
        textColor.a = 1.0f;
        noticeText.color = textColor;

        yield return new WaitForSeconds(1.0f);

        float duration = 1.0f;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textColor.a = Mathf.Lerp(1.0f, 0.0f, elapsed / duration);
            noticeText.color = textColor;
            yield return null;
        }

        noticeText.gameObject.SetActive(false);
        fadeCoroutine = null;
    }

    // 글자를 천천히 깜빡이게 만드는 마법의 함수 (코루틴 원본 유지)
    private IEnumerator BlinkTextRoutine()
    {
        float speed = 1.5f;
        Color textColor = noticeText.color;
        while (true)
        {
            float time = Mathf.PingPong(Time.time * speed, 1.0f);
            textColor.a = time;
            noticeText.color = textColor;
            yield return null;
        }
    }

    private void SetMainMenuButtonsActive(bool isActive)
    {
        if (btn_Continue) btn_Continue.SetActive(isActive);
        if (btn_Settings) btn_Settings.SetActive(isActive);
        if (btn_Exit) btn_Exit.SetActive(isActive);
    }

    private void MoveToCharacterSelect()
    {
        if (panel_Title) panel_Title.SetActive(false);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGameFromTitle();
        }
    }

    // 🌟 [설정 버튼 클릭 시]: 설정창을 열고 하단 메뉴 버튼들을 숨깁니다.
    public void OnClickOpenSettings() //세팅버튼 클릭시
    {
        Debug.Log("[타이틀] 설정 버튼 클릭 -> 설정창 ON / 타이틀 화면 패널 OFF");

        // 1. 🌟 [정정 기획 반영]: 타이틀 화면 패널 자체를 화면에서 완전히 끕니다(OFF).
        if (panel_Title) panel_Title.SetActive(false);

        // 2. 인스펙터에 연결된 설정창 패널 본체를 켭니다 (자식인 뒤로가기도 세트로 자동 ON!)
        if (panel_Settings) panel_Settings.SetActive(true);
    }


    // 🌟 [설정창 내부 닫기 버튼 클릭 시]: 설정창을 닫고 하단 메뉴 버튼들을 복구합니다.
    public void OnClickCloseSettings()
    {
        Debug.Log("[설정창] 뒤로가기 클릭 -> 설정창 OFF / 타이틀 화면 및 버튼 전체 복구 ON");

        // 1. 켜져있던 설정창 패널 본체와 자식들을 완전히 끕니다(OFF).
        if (panel_Settings) panel_Settings.SetActive(false);

        // 2. 🌟 [정정 기획 반영]: 꺼져있던 타이틀 화면 패널(panel_Title)을 다시 활성화하여 켭니다(ON)!
        if (panel_Title) panel_Title.SetActive(true);

        // 3. 타이틀의 메인 메뉴 버튼 삼총사도 원래 위치에 완벽하게 복구하여 켭니다(ON).
        SetMainMenuButtonsActive(true);

#if UNITY_IOS
        if (btn_Exit != null) btn_Exit.SetActive(false);
#endif
    }



}
