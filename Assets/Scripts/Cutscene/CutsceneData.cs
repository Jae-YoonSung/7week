using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCutsceneData", menuName = "Cutscene/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    public List<CutsceneEntry> entries = new List<CutsceneEntry>();
}

[System.Serializable]
public class CutsceneEntry
{
    [Header("대사")]
    [TextArea(2, 5)]
    public string dialogueText;

    [Tooltip("글자 하나가 출력되는 간격 (초)")]
    public float typewriterInterval = 0.05f;

    [Tooltip("텍스트 완성 후 다음으로 자동 넘어가기까지 대기 시간 (초)")]
    public float autoAdvanceDelay = 2f;

    [Header("캐릭터 1 (비워두면 이전 엔트리 유지)")]
    public Sprite characterSprite;
    [Tooltip("UI 캔버스 기준 앵커 포지션 (픽셀)")]
    public Vector2 characterPosition;

    [Header("캐릭터 2 (비워두면 숨김 / 이전 엔트리 유지 원하면 keepChar2 체크)")]
    public Sprite characterSprite2;
    [Tooltip("UI 캔버스 기준 앵커 포지션 (픽셀)")]
    public Vector2 characterPosition2;
    [Tooltip("체크하면 캐릭터 2를 이전 엔트리 그대로 유지 (스프라이트가 비어도 숨기지 않음)")]
    public bool keepCharacter2;

    [Header("페이드 전환")]
    [Tooltip("이 대사 시작 전에 페이드 아웃 → 캐릭터 교체 → 페이드 인 수행")]
    public bool doFadeTransition = false;

    [Tooltip("페이드 아웃/인 각각의 지속 시간 (초)")]
    public float fadeDuration = 0.8f;
}
