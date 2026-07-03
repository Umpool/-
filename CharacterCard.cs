using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCard : MonoBehaviour
{
    [Header("Data Link")]
    // 이 카드가 표시할 캐릭터 데이터 (인스펙터에서 할당하거나 매니저가 세팅)
    public CharacterData characterData;

    [Header("UI References")]
    public Image characterImageUI;
    public TextMeshProUGUI nameTextUI;
    public TextMeshProUGUI atkTextUI;
    public Slider hpSlider;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            // 버튼 클릭 시 GameManager의 파티 추가 메서드 호출 (자기 데이터 전달)
            button.onClick.AddListener(OnCardClicked);
        }
    }

    private void Start()
    {
        // 시작할 때 데이터가 들어있다면 자동으로 UI를 업데이트합니다.
        UpdateCardUI();
    }

    // 데이터를 외부에서 주입해 줄 때 사용할 메서드
    public void SetupCard(CharacterData data)
    {
        characterData = data;
        UpdateCardUI();
    }

    // 데이터의 내용대로 UI 텍스트와 이미지를 출력하는 함수
    public void UpdateCardUI()
    {
        if (characterData == null) return;

        // 1. 이름 연동
        if (nameTextUI != null) nameTextUI.text = characterData.characterName;

        // 2. 🌟 [코드적 해결] 버튼 컴포넌트의 컬러 블록을 직접 수정합니다.
        if (button == null) button = GetComponent<Button>();

        if (button != null)
        {
            // 버튼의 현재 컬러 상태를 복사해옵니다.
            ColorBlock cb = button.colors;

            // 기본 상태(Normal)의 색상을 캐릭터 고유 색상으로 강제 지정합니다.
            cb.normalColor = characterData.characterColor;

            // 마우스를 올리거나 눌렀을 때 캐릭터 색상이 유지되면서 살짝 어두워지게 연쇄 처리 (선택사항)
            cb.highlightedColor = characterData.characterColor * 0.9f;
            cb.pressedColor = characterData.characterColor * 0.7f;
            cb.selectedColor = characterData.characterColor;

            // 수정된 컬러 블록을 버튼에 다시 주입합니다.
            button.colors = cb;
        }
    }


    private void OnCardClicked()
    {
        if (characterData != null && GameManager.Instance != null)
        {
            // AddCharacterToParty 대신, GameManager에 이미 구현되어 있는 캐릭터 클릭 함수 호출
            GameManager.Instance.OnClickCharacter(characterData.id);
        }
    }
    // 🌟 캐릭터가 데미지를 입었을 때 체력을 깎고 UI 슬라이더를 갱신하는 함수 (새로 추가)
    public void TakeDamage(float damage)
    {
        if (characterData == null) return;

        // 1. 데이터의 현재 체력(hp)을 데미지만큼 깎습니다. (정수로 변환)
        characterData.hp -= Mathf.RoundToInt(damage);

        // 2. 체력이 0 이하로 떨어지지 않도록 방지합니다.
        if (characterData.hp < 0) characterData.hp = 0;

        // 3. 만약 화면에 체력바(Slider)가 연결되어 있다면 실시간으로 갱신합니다.
        if (hpSlider != null)
        {
            // 슬라이더의 최대값을 캐릭터의 maxHp로, 현재 값을 남은 hp로 매칭합니다.
            hpSlider.maxValue = characterData.maxHp;
            hpSlider.value = characterData.hp;
        }

        Debug.Log($"{characterData.characterName}이(가) {damage}의 데미지를 입었습니다. 남은 HP: {characterData.hp}");
    }


}
