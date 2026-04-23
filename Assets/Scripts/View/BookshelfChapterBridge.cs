using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 책장 플로우에서 선택된 BookshelfBook을 챕터 UI 버튼들에 연결합니다.
///
/// 기존 LobbyPresetSeedButton들을 그대로 사용하되,
/// 책장에서 클릭한 책이 이미 시드/씬/스테이지 정보를 가지고 있으므로
/// 챕터 UI의 "시작" 버튼들을 해당 책의 StartGame()으로 자동 리디렉션합니다.
///
/// 사용 방법:
///   1. LobbyUIManager 오브젝트 또는 부모에 이 컴포넌트를 추가합니다.
///   2. Inspector에서 StartButtons 배열에 챕터 내 시작 버튼들을 등록합니다.
///   3. BookshelfController가 BookSelectedBook을 설정하면 버튼들이 자동 재연결됩니다.
/// </summary>
[DisallowMultipleComponent]
public class BookshelfChapterBridge : MonoBehaviour
{
    [Tooltip("챕터 UI 안의 '시작하기' 버튼 목록. 책 선택 시 StartGame()으로 자동 연결됩니다.")]
    [SerializeField] private Button[] _startButtons;

    private BookshelfBook _currentBook;

    /// <summary>BookshelfController가 책 선택 시 이 메서드를 호출합니다.</summary>
    public void SetSelectedBook(BookshelfBook book)
    {
        _currentBook = book;
        RebindButtons();
    }

    private void RebindButtons()
    {
        if (_startButtons == null) return;

        foreach (var btn in _startButtons)
        {
            if (btn == null) continue;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnStartClicked);
        }
    }

    private void OnStartClicked()
    {
        if (_currentBook == null)
        {
            Debug.LogWarning("[BookshelfChapterBridge] 선택된 책이 없습니다.");
            return;
        }
        _currentBook.StartGame();
    }
}
