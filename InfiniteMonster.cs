using UnityEngine;
using UnityEngine.UI;
using TMPro; // 누적 대미지 표시용

public class InfiniteMonster : BaseMonster
{
    public static InfiniteMonster Instance { get; private set; }

    [Header("--- 점수 측정 ---")]
    public float totalDamageDealt = 0f; // 유저가 지금까지 입힌 총 누적 대미지

    [Header("--- UI 연동 ---")]
    public TextMeshProUGUI scoreText; // 화면에 "누적 대미지: XXX"를 보여줄 텍스트

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        monsterName = "심연의 허수아비 (무한모드)";
        maxHp = 9999999f; // 절대 안 죽도록 체력을 엄청 크게 설정
        currentHp = maxHp;
        UpdateMonsterUI();
    }

    // 퍼즐이 터질 때마다 이 함수가 호출되어 대미지를 누적합니다.
    // 💡 [수정] 부모의 TakeDamage를 정상적으로 이어받도록 override를 붙여줍니다.
    public override void TakeDamage(float damage)
    {
        totalDamageDealt += damage; // 대미지를 계속 차곡차곡 쌓음

        // 부모(BaseMonster)가 가진 기본 체력 깎기 및 HP바 갱신 기능을 실행합니다.
        base.TakeDamage(damage);

        if (currentHp <= 0) currentHp = maxHp; // 만약 피가 다 달면 몰래 풀피로 리필

        UpdateMonsterUI();
        Debug.Log($"💥 타격! 이번 대미지: {damage} | 총 누적 대미지: {totalDamageDealt}");
    }

    // 화면의 HP바와 누적 대미지 텍스트를 실시간 새로고침합니다.
    void UpdateMonsterUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHp / maxHp;
        }

        if (scoreText != null)
        {
            // 천 단위 쉼표(,)가 찍히도록 세련되게 표현합니다. (예: 누적 대미지: 12,500)
            scoreText.text = $"누적 대미지: {totalDamageDealt:N0}";
        }
    }
}
