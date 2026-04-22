using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼에 연결하면 클릭 시 씬 내 모든 SequentialImageToggle의 Sprite를 인덱스 0으로 초기화합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class AllToggleResetButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(ResetAllToggles);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(ResetAllToggles);
    }

    private void ResetAllToggles()
    {
        var toggles = FindObjectsByType<SequentialImageToggle>(FindObjectsSortMode.None);
        foreach (var toggle in toggles)
            toggle.ResetImage();
    }
}
