using UnityEngine;
using UnityEngine.UI;
using TMPro; // [중요]: 글자(TextMeshPro)를 제어하기 위해 반드시 필요한 도구상자입니다.

public class PartyIcon : MonoBehaviour
{
    [Header("프리팹 내부 UI 연결")]
    // 프리팹 자식에 있는 글자 컴포넌트(NameText)를 조종하기 위해 찜해두는 칸
    public TextMeshProUGUI txt_Name;

    // 🌟 [새로 추가한 작업]: 프리팹 본체에 붙어있는 이미지 컴포넌트(하얀 네모창)를 직접 조종하기 위해 찜해두는 칸입니다.
    public Image img_CharacterVisual;

    // ✨ [HP바 시스템 추가]: 스펙 주입과 동시에 형성될 실시간 HP바 슬라이더 전선칸입니다.
    public Slider hpSlider;

    // 이 파티 아이콘이 현재 머릿속에 기억하고 있는 실제 캐릭터의 전체 데이터 주소
    public CharacterData myData;

    // 내부 실제 배틀 연산용 실시간 체력 저장 상자
    private float maxHP;
    private float currentHP;

    // // 두뇌(GameManager)가 "너 전사 정보로 변신해!" 하고 데이터를 던져주면 실행되는 배달 접수 함수입니다.
    // 참조 5개
    public void Setup(CharacterData data, bool isBattle = false)
    {
        // 1. 배달받은 전사/슬라임 데이터를 내 주머니(myData)에 쏙 저장합니다.
        myData = data;

        // 만약 배달받은 데이터가 텅 비어있지 않고 아주 정상적이라면, 화면을 바꾸기 시작합니다.
        if (myData != null)
        {
            // // 2. 글자 변경 작업: 글자 조종 칸이 연결되어 있다면, 캐릭터 카드에 적힌 실제 이름으로 화면 글자를 바꿉니다.
            if (txt_Name != null)
            {
                txt_Name.text = myData.characterName;
            }

            // // 2. 이미지 컴포넌트에 색상 반영 작업
            if (img_CharacterVisual != null)
            {
                // 🌟 [새로 추가한 작업]: 투명도를 무시하고 100% 선명하게 출력하기 위해 불투명도(A)를 최대치(1)로 고정합니다.
                Color finalColor = myData.characterColor;
                finalColor.a = 1.0f;

                // 🌟 [새로 추가한 작업]: '캐릭터용' 하얀 사각형 도화지 자체에 캐릭터 카드에 지정된 색상(빨강, 청록 등)을 통째로 칠해줍니다!
                img_CharacterVisual.color = finalColor;
            }

            // // 3. 만약 캐릭터 일러스트 그림(스프라이트)도 함께 등록되어 있다면 그림도 같이 얹어줍니다.
            if (myData.characterSprite != null && img_CharacterVisual != null)
            {
                img_CharacterVisual.sprite = myData.characterSprite;
            }

            // 💥 [유저님 기획 완벽 반영 연동]: 스펙 주입과 동시에 HP바 게이지를 가득 채워 형성합니다!
            maxHP = myData.hp;
            currentHP = maxHP;

            if (hpSlider != null)
            {
                // [수정 줄]: 무조건 true로 켜던 것을, 배틀 스위치 상태(isBattle)에 따라 켜지게 바꿉니다!
                hpSlider.gameObject.SetActive(isBattle);

                // 🌟 [여기서부터 격리 시작!]: 배틀 스위치가 활성화되었을 때만 작동하는 울타리를 칩니다.
                if (isBattle)
                {
                    hpSlider.minValue = 0f;
                    hpSlider.maxValue = maxHP;
                    hpSlider.value = currentHP;
                } // 🌟 울타리 마감선!
            }

            Debug.Log($"🎨 [{myData.characterName}] 배틀 카드 형성 성공! (데이터 색상 주입 및 HP바 동기화 완료)");
        }
    }

    // 몬스터에게 턴 반격을 맞았을 때 진짜 이 카드 한 장의 피만 깎아내리는 수신기 함수
    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;

        // 초록색 슬라이더 게이지를 실시간으로 줄여 뒤의 빨간 배경을 노출시킵니다!
        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        Debug.Log($"🩸 [{myData.characterName}] 카드가 공격받음! 남은 체력: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Debug.Log($"😭 파티원 [{myData.characterName}] 카드가 전사(체력 0)했습니다.");
        }
    }
}
