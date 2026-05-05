using UnityEngine;

/// <summary>
/// Canvas 안에서 배경 프리팹을 1→2→3→4 순으로 왼쪽→오른쪽 방향으로 순환 스크롤합니다.
///
/// [BackgroundLayer 셋업]
/// - Canvas 자식으로 빈 오브젝트 생성 → 이름 BackgroundLayer
/// - Anchor : 전체 스트레치 (min=0,0 / max=1,1), Pivot=(0.5, 0.5)
/// - 이 스크립트를 BackgroundLayer에 부착
///
/// [배경 프리팹]
/// - Image(또는 RawImage) 컴포넌트를 가진 UI 프리팹
/// - Pivot=(0.5, 0.5), Width = backgroundWidth 와 동일하게 설정
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("배경 프리팹 (순서대로: 1→2→3→4)")]
    public GameObject[] backgroundPrefabs;

    [Header("스크롤 설정")]
    [Tooltip("초당 이동 속도 (캔버스 픽셀 기준)")]
    public float scrollSpeed = 200f;

    [Tooltip("각 배경 프리팹의 UI 너비 (캔버스 픽셀). 프리팹 RectTransform Width와 동일하게 설정.")]
    public float backgroundWidth = 1920f;

    private RectTransform[] instances;
    private int nextPrefabIndex;
    private float rightExitX; // 이 X를 넘으면 재활용

    void Start()
    {
        if (backgroundPrefabs == null || backgroundPrefabs.Length == 0)
        {
            Debug.LogWarning("[BackgroundScroller] backgroundPrefabs 가 비어 있습니다.");
            return;
        }

        // 캔버스 너비로 재활용 기준점 계산
        // BackgroundLayer pivot=(0.5,0.5) 기준: 화면 오른쪽 끝 = canvasWidth/2
        // 배경 중심이 오른쪽 끝 + 배경 반너비를 넘으면 완전히 화면 밖
        var canvas = GetComponentInParent<Canvas>();
        float canvasWidth = ((RectTransform)canvas.transform).rect.width;
        rightExitX = canvasWidth / 2f + backgroundWidth / 2f;

        int count = backgroundPrefabs.Length;
        instances = new RectTransform[count];

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(backgroundPrefabs[i], transform);
            instances[i] = go.GetComponent<RectTransform>();

            // bg[0] → x=0 (화면 중앙, 첫 번째로 보임)
            // bg[1] → x=-backgroundWidth (왼쪽 대기, 두 번째로 들어옴)
            // bg[2] → x=-backgroundWidth*2  ...
            instances[i].anchoredPosition = new Vector2(-backgroundWidth * i, 0f);
        }

        nextPrefabIndex = 0;
    }

    void Update()
    {
        if (instances == null) return;

        float move = scrollSpeed * Time.deltaTime;

        for (int i = 0; i < instances.Length; i++)
        {
            instances[i].anchoredPosition += Vector2.right * move;

            // 오른쪽 화면 밖으로 나가면 가장 왼쪽에 재배치 + 다음 프리팹으로 교체
            if (instances[i].anchoredPosition.x > rightExitX)
            {
                float leftmostX = GetLeftmostX(i);

                Destroy(instances[i].gameObject);
                var go = Instantiate(backgroundPrefabs[nextPrefabIndex], transform);
                instances[i] = go.GetComponent<RectTransform>();
                instances[i].anchoredPosition = new Vector2(leftmostX - backgroundWidth, 0f);

                nextPrefabIndex = (nextPrefabIndex + 1) % backgroundPrefabs.Length;
            }
        }
    }

    float GetLeftmostX(int excludeIndex)
    {
        float leftmost = float.MaxValue;
        for (int i = 0; i < instances.Length; i++)
        {
            if (i == excludeIndex) continue;
            float x = instances[i].anchoredPosition.x;
            if (x < leftmost) leftmost = x;
        }
        return leftmost == float.MaxValue ? 0f : leftmost;
    }
}
