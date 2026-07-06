using UnityEngine;
using UnityEngine.UI;

public class ShortcutManager : MonoBehaviour
{
    // 1. 상태 장부에 서브 캐릭터 선택창(SubCharSelect)을 새로 추가했습니다.
    public enum GameScreenState
    {
        Intro,          // 인트로 (화면 클릭 대기)
        LobbyMenu,      // 메인 타이틀 로비 메뉴
        CharSelect,     // 메인 캐릭터 선택창
        SubCharSelect,  // 서브 캐릭터 선택창 (추가 🎯)
        ConfirmPopup    // 예/아니오 팝업창
    }

    [Header("ㅡ 현재 활성화된 화면 지정 ㅡ")]
    public GameScreenState currentScreen = GameScreenState.Intro;

    [Header("ㅡ 1. 인트로 화면 연동 버튼 ㅡ")]
    public Button introTouchButton;

    [Header("ㅡ 2. 타이틀 로비 메뉴 버튼 ㅡ")]
    public Button newGameButton;
    public Button continueButton;
    public Button settingButton;
    public Button restButton;

    [Header("ㅡ 3. 메인 캐릭터 선택창 버튼 ㅡ")]
    public Button warriorButton;
    public Button mageButton;
    public Button archerButton;
    public Button mainCharBackButton; // 메인 선택창 뒤로가기 (추가 🎯)

    [Header("ㅡ 4. 서브 캐릭터 선택창 버튼 ㅡ")]
    public Button subCharBackButton;  // 서브 선택창 뒤로가기 (추가 🎯)
    public Button[] subCharacterButtons = new Button[6]; // 서브캐 1번~6번 배열 (추가 🎯)
    public Button startAdventureButton; // 🎯 [추가] 출발하기 버튼 연동 슬롯

    [Header("ㅡ 5. 예/아니오 팝업창 버튼 ㅡ")]
    public Button yesButton;
    public Button noButton;





    private void Start()
    {
        // 🎯 [입력 먹통 정석 해결]: 재생하자마자 게임 창의 마우스 입력 권한을 100% 강제로 뺏어옵니다.
        #if UNITY_EDITOR
        var gameViewType = System.Type.GetType("UnityEditor.GameView, UnityEditor");
        if (gameViewType != null)
        {
            var gameView = UnityEditor.EditorWindow.GetWindow(gameViewType);
            if (gameView != null) gameView.Focus(); // 윈도우 포커스 획득
        }
        #endif

        // 인트로 상태로 시작점을 명확하게 고정합니다.
        currentScreen = GameScreenState.Intro;

        // ====================================================================
        // 🎯 [정석 자동화]: 버튼마다 일일이 인스펙터 클릭 연결 안 해도 되게 코드로 강제 주입!
        // ====================================================================
        
        // 1. 인트로 화면 자동 연결
        if (introTouchButton != null)
            introTouchButton.onClick.AddListener(() => OnIntroScreenClicked());

        // 2. 타이틀 로비 메뉴 자동 연결
        if (newGameButton != null) newGameButton.onClick.AddListener(() => StartNewAdventure());
        if (continueButton != null) continueButton.onClick.AddListener(() => ContinueGame());
        if (settingButton != null) settingButton.onClick.AddListener(() => OpenSettings());
        if (restButton != null) restButton.onClick.AddListener(() => RestInVillage());

        // 3. 메인 캐릭터 선택창 자동 연결
        if (warriorButton != null) warriorButton.onClick.AddListener(() => SelectClass("Warrior"));
        if (mageButton != null) mageButton.onClick.AddListener(() => SelectClass("Mage"));
        if (archerButton != null) archerButton.onClick.AddListener(() => SelectClass("Archer"));
        if (mainCharBackButton != null) mainCharBackButton.onClick.AddListener(() => GoBackToLobby());

        // 4. 서브 캐릭터 선택창 및 출발하기 자동 연결
        if (subCharBackButton != null) subCharBackButton.onClick.AddListener(() => GoBackToMainChar());
        if (startAdventureButton != null) startAdventureButton.onClick.AddListener(() => StartBattleStage());
    }





    // 💡 [추가]: 유니티가 내부적으로 포커스 변화를 정상 인식하도록 돕는 정석 툴 내부 이벤트 함수
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && currentScreen == GameScreenState.Intro)
        {
            Debug.Log("<color=yellow>[포커스 확인]</color> 게임 창이 활성화되어 단축키 입력 준비가 완료되었습니다.");
        }

    }




    private void Update()
    {

        // 🚨 [통합 뒤로가기 엔진]: 메인 또는 서브 캐릭터 선택창일 때 ESC나 우클릭 감지
        if (currentScreen == GameScreenState.CharSelect || currentScreen == GameScreenState.SubCharSelect)
        {
            // ESC 키(GetKeyDown) 또는 마우스 우클릭(GetMouseButtonDown 1번)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                HandleBackButton();
                return; // 뒤로가기 처리 완료 시 다른 입력 패스
            }
        }

        // [인트로 전용 수신기]
        if (currentScreen == GameScreenState.Intro)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TriggerButton(introTouchButton, "스페이스바 ➡️ 인트로 터치");

                // 🎯 [정석 해결]: 버튼을 누름과 동시에 단축키 컨텍스트를 다음 화면(로비 메뉴)으로 즉시 전환합니다!
                ChangeScreenState(GameScreenState.LobbyMenu);
                return;
            }
        }


        // 🎯 [추가]: 서브 캐릭터 선택창일 때 엔터키를 누르면 출발하기 버튼 발동
        if (currentScreen == GameScreenState.SubCharSelect)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                TriggerButton(startAdventureButton, "엔터키 ➡️ 서브 캐릭터 선택창 [출발하기]");
                return;
            }
        }

        // [숫자 패드 통합 감지 엔진 (6번까지 확장)]
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) HandleNumberInput(1);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) HandleNumberInput(2);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) HandleNumberInput(3);
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) HandleNumberInput(4);
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) HandleNumberInput(5); // 추가 🎯
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) HandleNumberInput(6); // 추가 🎯
    }

    // 플레이어가 머물고 있는 화면에 따라 숫자 입력을 분기 처리
    private void HandleNumberInput(int number)
    {
        switch (currentScreen)
        {
            case GameScreenState.LobbyMenu:
                if (number == 1) TriggerButton(newGameButton, "숫자 1 ➡️ 새로운 모험 버튼 클릭");
                else if (number == 2) TriggerButton(continueButton, "숫자 2 ➡️ 이어가기 버튼 클릭");
                else if (number == 3) TriggerButton(settingButton, "숫자 3 ➡️ 설정 버튼 클릭");
                else if (number == 4) TriggerButton(restButton, "숫자 4 ➡️ 휴식하기 버튼 클릭");
                break;

            case GameScreenState.CharSelect:
                if (number == 1) TriggerButton(warriorButton, "숫자 1 ➡️ 전사 선택 클릭");
                else if (number == 2) TriggerButton(mageButton, "숫자 2 ➡️ 마법사 선택 클릭");
                else if (number == 3) TriggerButton(archerButton, "숫자 3 ➡️ 궁수 선택 클릭");
                break;

            case GameScreenState.SubCharSelect:
                // 🎯 [서브 캐릭터 1~6번 동적 처리]: 배열 인덱스는 0부터 시작하므로 (선택숫자 - 1) 적용
                int arrayIndex = number - 1;
                if (arrayIndex >= 0 && arrayIndex < subCharacterButtons.Length)
                {
                    TriggerButton(subCharacterButtons[arrayIndex], $"숫자 {number} ➡️ 서브 캐릭터 {number}번 선택 클릭");
                }
                break;

            case GameScreenState.ConfirmPopup:
                if (number == 1) TriggerButton(yesButton, "숫자 1 ➡️ 팝업 [예] 클릭");
                else if (number == 2) TriggerButton(noButton, "숫자 2 ➡️ 팝업 [아니오] 클릭");
                break;
        }
    }

    // ESC, 우클릭 시 현재 화면에 맞는 뒤로가기 버튼을 분기 연동하는 함수
    private void HandleBackButton()
    {
        if (currentScreen == GameScreenState.CharSelect)
        {
            TriggerButton(mainCharBackButton, "ESC/우클릭 ➡️ 메인 캐릭터 선택창 [뒤로가기]");
        }
        else if (currentScreen == GameScreenState.SubCharSelect)
        {
            TriggerButton(subCharBackButton, "ESC/우클릭 ➡️ 서브 캐릭터 선택창 [뒤로가기]");
        }
    }

    // UI 버튼을 안전하게 강제 발동시키는 통제 장치
    private void TriggerButton(Button targetButton, string debugMessage)
    {
        if (targetButton != null && targetButton.interactable)
        {
            Debug.Log($"<color=cyan>[단축키 발동]</color> {debugMessage}");
            targetButton.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning($"[단축키 경고] 연결된 버튼이 없거나 잠겨있습니다: {debugMessage}");
        }
    }

    // 화면 상태 변경 함수
    public void ChangeScreenState(GameScreenState newState)
    {
        currentScreen = newState;
        Debug.Log($"<color=lime>[화면 상태 변경]</color> 현재 입력 컨텍스트: {newState}");
    }
    // ====================================================================
    // 💡 [실제 기능 주머니]: 버튼이나 단축키가 눌렸을 때 진짜 실행될 게임 로직들
    // ====================================================================
    private void OnIntroScreenClicked()
    {
        Debug.Log("🎬 인트로 화면이 꺼지고 메인 메뉴가 열립니다.");
        ChangeScreenState(GameScreenState.LobbyMenu);
    }

    private void StartNewAdventure()
    {
        Debug.Log("⚔️ [새로운 모험] 시작! 캐릭터 선택창으로 장부를 이동합니다.");
        ChangeScreenState(GameScreenState.CharSelect);
    }

    private void GoBackToLobby()
    {
        Debug.Log("↩️ 캐릭터 선택 취소! 로비 메뉴로 돌아갑니다.");
        ChangeScreenState(GameScreenState.LobbyMenu);
    }

    private void SelectClass(string className)
    {
        Debug.Log($"🎭 메인 캐릭터로 [{className}]를 선택했습니다! 서브 캐릭터 선택창으로 이동합니다.");
        ChangeScreenState(GameScreenState.SubCharSelect);
    }

    private void GoBackToMainChar()
    {
        Debug.Log("↩️ 서브 캐릭터 선택 취소! 메인 캐릭터 선택창으로 돌아갑니다.");
        ChangeScreenState(GameScreenState.CharSelect);
    }

    private void StartBattleStage()
    {
        Debug.Log("🚀 모든 파티 구성 완료! 3매치 퍼즐 던전으로 진입합니다!");
    }

    private void ContinueGame() { Debug.Log("💾 이어하기 버튼 클릭됨"); }
    private void OpenSettings() { Debug.Log("⚙️ 설정 버튼 클릭됨"); }
    private void RestInVillage() { Debug.Log("💤 휴식하기 버튼 클릭됨"); }

}
