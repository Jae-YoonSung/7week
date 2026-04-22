using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterVisualEntry
{
    public int        characterId;
    public GameObject aliveModelPrefab;
    public GameObject deadModelPrefab;
}

/// <summary>
/// 스테이지별 캐릭터 외형 설정입니다.
/// 캐릭터 ID마다 생존 모델과 사망 모델 프리팹을 지정합니다.
/// GameFlowController의 _stageVisualConfig에 연결하세요.
/// </summary>
[CreateAssetMenu(fileName = "StageCharacterVisualConfig_", menuName = "MafiaGame/Stage/CharacterVisualConfig")]
public class StageCharacterVisualConfig : ScriptableObject
{
    [SerializeField] private List<CharacterVisualEntry> _entries = new();

    public CharacterVisualEntry GetEntry(int characterId)
        => _entries.Find(e => e.characterId == characterId);
}
