using UnityEngine;

/// <summary>
/// ESC 키를 누르면 지정된 UI 패널을 열고 닫는 범용 매니저입니다.
/// </summary>
public class EscPanelManager : MonoBehaviour
{
    [Tooltip("ESC를 눌렀을 때 켜고 끌 UI 패널 (종료 확인창, 설정창 등)")]
    [SerializeField] private GameObject _targetPanel;

    private void Update()
    {
        // ESC 키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanel();
        }
    }

    /// <summary>
    /// 패널을 켜거나 끕니다. (버튼의 OnClick에 연결해서 수동으로 닫을 때도 사용할 수 있습니다)
    /// </summary>
    public void TogglePanel()
    {
        if (_targetPanel != null)
        {
            bool isActive = _targetPanel.activeSelf;
            _targetPanel.SetActive(!isActive);
        }
    }

    /// <summary>
    /// 명시적으로 패널을 닫습니다. (예: '취소' 버튼 클릭 시)
    /// </summary>
    public void ClosePanel()
    {
        if (_targetPanel != null)
        {
            _targetPanel.SetActive(false);
        }
    }
}
