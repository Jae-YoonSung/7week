using UnityEngine;

/// <summary>
/// 스테이지 클리어 컷씬 데이터입니다.
/// 하나의 배경 이미지와 순서대로 표시될 대사 목록을 정의합니다.
///
/// 사용법:
///   Assets/Create/Cutscene/Stage Clear Cutscene 으로 에셋 생성 후
///   StageClearCutsceneController의 _cutscene 필드에 연결하세요.
/// </summary>
[CreateAssetMenu(fileName = "New StageClearCutscene", menuName = "Cutscene/Stage Clear Cutscene")]
public class StageClearCutsceneSO : ScriptableObject
{
    [Header("배경 이미지")]
    [Tooltip("컷씬 전체에서 표시될 단일 배경 스프라이트입니다.")]
    public Sprite backgroundImage;

    [Header("대사 목록")]
    [Tooltip("위에서부터 순서대로 표시됩니다. 클릭으로 다음 대사로 넘어갑니다.")]
    [TextArea(2, 5)]
    public string[] lines;

    [Header("배경 변경 목록")]
    [Tooltip("특정 대사 인덱스(0부터 시작)에서 배경을 변경하고 싶을 때 추가하세요.")]
    public CutsceneBackgroundChange[] backgroundChanges;
}

[System.Serializable]
public struct CutsceneBackgroundChange
{
    [Tooltip("배경이 변경될 대사 인덱스 (0부터 시작)")]
    public int lineIndex;
    [Tooltip("변경할 배경 이미지")]
    public Sprite backgroundImage;
}
