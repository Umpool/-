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
    // ⚔️ 무한모드 시작 시 대장 컴퓨터들이 호출하여 몬스터의 점수를 0점으로 완전 세탁합니다!
    public void ResetMonsterForNewGame()
    {
        totalDamageDealt = 0f;
        currentHp = maxHp;
        UpdateMonsterUI();
        Debug.Log("🧼 [몬스터 세탁 완료] 새 판을 위해 누적 대미지가 0으로 완벽 초기화되었습니다!");
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
    // 🧼 [왕초보 특제] 마을 복귀 버튼 클릭 시점에 원격 가동되는 0점 세탁기입니다!
    // 🦖 [대완공 통합형 스위치] 마을 복귀 및 무한모드 재입장 시 몬스터를 완전히 초기화하고 부활시킵니다!
    public void ResetAndRespawnMonster()
    {
        // 1. 점수 및 데이터를 태초의 상태로 세탁
        totalDamageDealt = 0f;
        monsterName = "심연의 허수아비 (무한모드)";

        // 2. 무한 체력 풀피로 강제 주입
        maxHp = 9999999f;
        currentHp = maxHp;

        // 3. 눈에 보이는 슬라이더와 전광판 글씨 정밀 새로고침
        UpdateMonsterUI();

        if (scoreText != null)
        {
            scoreText.text = "누적 대미지: 0";
        }

        if (hpSlider != null)
        {
            hpSlider.value = 1f;
        }

        Debug.Log("🎯 [합체 완공] 몬스터의 누적 대미지가 0으로 세탁되었으며 풀피 상태로 완벽히 리부팅(부활)되었습니다!");
    }


}
