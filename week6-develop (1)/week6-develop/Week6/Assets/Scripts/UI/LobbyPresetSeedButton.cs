using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼에 붙여서 Inspector에서 시드값을 지정합니다.
/// 해금 조건 관리는 LobbyUnlockManager가 담당합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class LobbyPresetSeedButton : MonoBehaviour
{
    [SerializeField] private int    _seed;
    [SerializeField] private string _stageId;
    [SerializeField] private string _gameSceneName = "Stage_1";

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        TurnHistoryRepository.Instance.ClearAll();
        NewGameConfig.SetSeed(_seed, _stageId);
        UnityEngine.SceneManagement.SceneManager.LoadScene(_gameSceneName);
    }
}
