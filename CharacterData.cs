using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character Data")]
public class CharacterData : ScriptableObject
{
    public int id;
    public string characterName;

    [TextArea]
    public string description;

    public int hp;
    public int attackPower;
    public int defense;

    [Header("캐릭터 일러스트 (외형 이미지)")]
    public Sprite characterSprite;

    // 🌟 [새로 추가한 작업]: 유니티 인스펙터 창에서 "이 캐릭터가 가질 고유 색상"을 마우스로 클릭해서 고를 수 있게 칸을 파주는 코드입니다.
    [Header("캐릭터 테마 색상 (사각형 컬러)")]
    public Color characterColor = Color.white; // 기본값은 흰색으로 설정해둡니다.

    [Header("시너지 시스템")]
    public string synergyName;        // 예: "기사단", "화염마법", "도적단" 
    public string synergyDescription; // 예: "기사단 2명 이상 조합 시 방어력 +10% 증가"

    
}
