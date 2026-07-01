using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class PartySynergyManager : MonoBehaviour
{
    public static PartySynergyManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 🌟 [핵심 실시간 시너지 연산기]
    // 현재 파티원 명단을 주입받아, 중복과 조합을 계산한 뒤 최종 이쁜 텍스트를 만들어 반환합니다.
    public string CalculateSynergyText(List<CharacterData> currentParty)
    {
        if (currentParty == null || currentParty.Count == 0)
        {
            return "현재 활성화된 파티 시너지 효과가 없습니다.";
        }

        // 1. 현재 파티에 어떤 시너지들이 몇 명이나 모여있는지 바구니에 담아 체크합니다.
        Dictionary<string, int> synergyCounts = new Dictionary<string, int>();
        Dictionary<string, string> synergyDescs = new Dictionary<string, string>();

        foreach (var member in currentParty)
        {
            if (member == null || string.IsNullOrEmpty(member.synergyName)) continue;

            // 시너지 개수 카운팅
            if (synergyCounts.ContainsKey(member.synergyName))
            {
                synergyCounts[member.synergyName]++;
            }
            else
            {
                synergyCounts[member.synergyName] = 1;
                synergyDescs[member.synergyName] = member.synergyDescription; // 설명문 매칭 보관
            }
        }

        // 2. 글자를 이쁘고 가독성 좋게 줄바꿈하며 조립해나가는 유니티 정석 도구(StringBuilder) 활용
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("📜 <color=#FFE600>[현재 결성된 파티 시너지 조합]</color>\n");

        bool hasAnySynergy = false;

        foreach (var kvp in synergyCounts)
        {
            string sName = kvp.Key;
            int count = kvp.Value;

            // 💡 [기획 확장 규칙 기재 공간]: 
            // 현재는 캐릭터가 등록되면 시너지를 즉시 한 줄씩 표기해 주며, 나중에 "2명 이상 모일 때만 발동해라!" 같은 정교한 조건도 여기서 쉽게 추가 가능합니다.
            sb.AppendLine($"▶ <b>{sName}</b> (조합된 파티원 수: {count}명)");
            sb.AppendLine($"<size=15>{synergyDescs[sName]}</size>");
            sb.AppendLine();
            hasAnySynergy = true;
        }

        if (!hasAnySynergy)
        {
            return "현재 활성화된 파티 시너지 효과가 없습니다.";
        }

        return sb.ToString();
    }
}
