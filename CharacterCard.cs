using UnityEngine;
using UnityEngine.UI; // 유니티 UI 엔진 제어
using TMPro;

public class CharacterCard : MonoBehaviour
{
    [Header("연동할 실제 캐릭터 데이터")]
    public CharacterData myData; // 이 카드가 바라볼 캐릭터 데이터 에셋

    [Header("내부 UI 컴포넌트 연결")]
    public TextMeshProUGUI txt_Name; // 카드의 이름 텍스트 (TMP)

    // 게임 시작 시 GameManager가 이 함수를 원격으로 깨웁니다.
    public void RefreshUI()
    {
        // 1. 캐릭터 데이터가 정상적으로 연결되어 있는지 확인
        if (myData == null) return;

        // 2. [이름 연동]: 카드의 텍스트를 데이터에 적힌 진짜 이름("프로스트" 등)으로 바꿉니다.
        if (txt_Name != null)
        {
            txt_Name.text = myData.characterName;
        }

        // 3. 🌟 [개발자님 맞춤형 - 자식 사각형 없이 본체에 직접 색상 창조 주입]
        // 인스펙터에서 사각형 이미지를 드래그할 필요도 없고, 미리 만들어둘 필요도 없습니다!

        // 이 버튼 본체 오브젝트에 색상을 칠할 '도화지(Image)'가 컴포넌트로 붙어있는지 확인합니다.
        Image bodyImage = GetComponent<Image>();

        // 🔥 만약 본체에 도화지(Image)가 없다면? 코드가 실시간으로 도화지를 냅다 생성해서 붙여버립니다!
        if (bodyImage == null)
        {
            bodyImage = gameObject.AddComponent<Image>();
        }

        // 4. 주입받은 도화지에 데이터에 입력된 진짜 테마 컬러(하늘색, 노란색 등)를 다이렉트로 칠해버립니다.
        if (bodyImage != null)
        {
            // 유니티 기본 민무늬 도화지(UISprite)를 코드로 강제 세팅합니다.
            bodyImage.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);

            Color targetColor = myData.characterColor;
            targetColor.a = 1.0f; // 100% 선명하게 설정

            bodyImage.color = targetColor; // 본체 색상 변경 완료!

            // 5. 버튼 컴포넌트가 있다면 타겟 그래픽을 방금 코드로 만든 이 본체 도화지로 자동 세팅해줍니다.
            Button bodyButton = GetComponent<Button>();
            if (bodyButton != null)
            {
                bodyButton.targetGraphic = bodyImage;
            }
        }
    }
}
