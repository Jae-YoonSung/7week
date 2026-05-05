using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼에 붙이면 클릭 시 무작위 시드로 게임을 시작합니다.
/// 해금 조건 관리는 LobbyUnlockManager가 담당합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class LobbyRandomSeedButton : MonoBehaviour
{
    [SerializeField] private string _stageId;
    [SerializeField] private string _gameSceneName = "Stage_1";
    [Tooltip("이 버튼이 에필로그 진입용이면 체크하세요.")]
    [SerializeField] private bool   _isEpilogue;

    [Header("컷씬")]
    [Tooltip("체크 해제 시 스테이지 진입 컷씬을 건너뜁니다.")]
    [SerializeField] private bool _playCutscene = true;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        TurnHistoryRepository.Instance.ClearAll();
        NewGameConfig.SetRandom(_stageId, _isEpilogue);
        NewGameConfig.SetPlayCutscene(_playCutscene);
        UnityEngine.SceneManagement.SceneManager.LoadScene(_gameSceneName);
    }
}
