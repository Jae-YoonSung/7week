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
    [Header("이미지 (비워두면 이전 이미지 유지)")]
    public Sprite image;

    [Header("대사")]
    [TextArea(2, 5)]
    public string dialogueText;

    [Tooltip("글자 하나가 출력되는 간격 (초)")]
    public float typewriterInterval = 0.05f;

    [Tooltip("텍스트 완성 후 다음으로 자동 넘어가기까지 대기 시간 (초)")]
    public float autoAdvanceDelay = 2f;

    [Header("페이드 전환")]
    [Tooltip("이 대사 시작 전에 페이드 아웃 → 이미지 교체 → 페이드 인 수행")]
    public bool doFadeTransition = false;

    [Tooltip("페이드 아웃/인 각각의 지속 시간 (초)")]
    public float fadeDuration = 0.8f;
}
