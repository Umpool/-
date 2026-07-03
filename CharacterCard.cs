using UnityEngine;
using UnityEngine.UI;

public class CharacterCard : MonoBehaviour
{
    [Header("실시간 화면 연동 UI 컴포넌트 목록")]
    public Image characterImageUI;     // 캐릭터 일러스트(characterSprite)를 입힐 곳
    public TMPro.TMP_Text nameTextUI;  // 캐릭터 이름을 적을 곳
    public TMPro.TMP_Text atkTextUI;   // 캐릭터 공격력을 적을 곳
    public Slider hpSlider;            // ✨ 스펙 주입 시 형성될 실시간 HP바 슬라이더

    // 카드 내부에서 관리할 실시간 체력 데이터 저장소
    private float maxHP;
    private float currentHP;

    // 🌟 [핵심] 외부에서 데이터(스펙)를 주입해 줄 때 호출하는 셋업 함수입니다.
    public void SetupCharacterCard(CharacterData data)
    {
        if (data == null) return;

        // 1. 받은 데이터 상자를 내 몸통에 기억합니다.
        // 유저님이 CharacterData.cs에 만들어두신 소중한 변수들을 100% 활용합니다.
        maxHP = data.hp;
        currentHP = maxHP;

        // 2. 일러스트, 이름, 공격력 스펙을 화면 UI에 실시간 주입합니다.
        if (characterImageUI != null && data.characterSprite != null)
        {
            characterImageUI.sprite = data.characterSprite;
        }

        if (nameTextUI != null)
        {
            nameTextUI.text = data.characterName;
        }

        if (atkTextUI != null)
        {
            atkTextUI.text = "ATK: " + data.attackPower;
        }

        // 3. ✨ [유저님 요구사항] 스펙 주입과 동시에 HP바 게이지를 가득 채워 형성합니다!
        if (hpSlider != null)
        {
            hpSlider.gameObject.SetActive(true); // 혹시 꺼져있다면 HP바를 활성화
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;

            // 4. 유저님이 데이터에 설정해둔 고유 테마 색상(characterColor)으로 HP바 컬러까지 자동 깔맞춤!
            if (hpSlider.fillRect != null)
            {
                Image fillImage = hpSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = data.characterColor;
                }
            }
        }

        Debug.Log($"🎨 배틀 카드 형성 완료: [{data.characterName}] (HP: {maxHP} / ATK: {data.attackPower})");
    }

    // 몬스터에게 맞았을 때 정직하게 내 피만 깎는 수신기 기능
    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;

        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        if (currentHP <= 0)
        {
            Debug.Log($"😭 파티원 카드가 전사(체력 0)했습니다.");
        }
    }
}
