using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼에 부착하여 게임을 종료하는 기능을 제공합니다.
/// 유니티 에디터에서는 플레이 모드를 종료하고, 빌드된 게임에서는 프로그램을 종료합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class GameQuitButton : MonoBehaviour
{
    private void Start()
    {
        // 버튼 컴포넌트를 가져와서 클릭 이벤트를 연결합니다.
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(QuitGame);
    }

    /// <summary>
    /// 게임을 종료합니다. 버튼의 OnClick() 인스펙터에 직접 끌어다 써도 됩니다.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameQuitButton] 게임을 종료합니다.");

#if UNITY_EDITOR
        // 유니티 에디터 플레이 모드 정지
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 빌드된 앱 종료
        Application.Quit();
#endif
    }
}
