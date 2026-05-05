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
}
