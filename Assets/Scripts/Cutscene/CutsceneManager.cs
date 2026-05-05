using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 컷씬 재생 매니저.
///
/// [씬 셋업]
/// Canvas 구조 (권장):
///   Canvas
///   ├─ BackgroundLayer        ← BackgroundScroller 컴포넌트 부착
///   ├─ CharacterImage         ← Image 컴포넌트 (characterImage 슬롯)
///   ├─ CharacterImage2        ← Image 컴포넌트 (characterImage2 슬롯, 선택)
///   ├─ DialoguePanel (Top)
///   │   └─ DialogueText       ← TMP_Text 컴포넌트 (dialogueText 슬롯)
///   └─ FadeOverlay            ← Image 컴포넌트, Color=(0,0,0,0) (fadeOverlay 슬롯)
///
/// [사용법]
/// 1. CutsceneData ScriptableObject를 생성하고 엔트리를 채운다.
/// 2. 이 컴포넌트의 cutsceneData 슬롯에 연결한다.
/// 3. UI 슬롯(dialogueText, characterImage, fadeOverlay)을 연결한다.
/// 4. BackgroundScroller가 있는 오브젝트를 backgroundScroller 슬롯에 연결한다.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    [Header("컷씬 데이터")]
    public CutsceneData cutsceneData;

    [Header("UI 참조")]
    public TMP_Text dialogueText;
    public Image characterImage;
    [Tooltip("두 번째 캐릭터 Image. 사용 안 하면 비워두세요.")]
    public Image characterImage2;

    [Tooltip("전체 화면을 덮는 검은 Image. 시작 시 Alpha=0 이어야 합니다.")]
    public Image fadeOverlay;

    [Header("배경 스크롤러")]
    public BackgroundScroller backgroundScroller;

    [Header("컷씬 종료 시 비활성화할 루트 오브젝트 (Canvas 등)")]
    public GameObject cutsceneRoot;

    [Header("캐릭터 둥실 효과")]
    public float bobAmplitude = 8f;
    public float bobFrequency = 1f;

    // ── 내부 상태 ──────────────────────────────────────────
    private int entryIndex;
    private bool isTyping;
    private bool isWaiting;
    private bool skipTyping;    // 클릭 → 타이핑 즉시 완료
    private bool advanceNow;    // 클릭 → 대기 즉시 종료

    private Vector2 _char1BasePos;
    private Vector2 _char2BasePos;
    private bool _playCutscene;

    // ── Unity 생명주기 ──────────────────────────────────────

    void Awake()
    {
        // NewGameConfig.Clear()가 Start()보다 먼저 불릴 수 있으므로 Awake에서 읽어둠
        _playCutscene = NewGameConfig.PlayCutscene;
    }

    void Start()
    {
        if (cutsceneData == null || cutsceneData.entries == null || cutsceneData.entries.Count == 0)
        {
            Debug.LogWarning("[CutsceneManager] cutsceneData 가 비어 있습니다.");
            return;
        }

        if (!_playCutscene)
        {
            OnCutsceneComplete();
            return;
        }

        // FadeOverlay 초기화
        SetFadeAlpha(0f);
        characterImage.enabled = false;
        if (characterImage2 != null) characterImage2.enabled = false;

        StartCoroutine(PlayAllEntries());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleClick();

        float y = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        if (characterImage.enabled)
            characterImage.rectTransform.anchoredPosition = _char1BasePos + new Vector2(0f, y);
        if (characterImage2 != null && characterImage2.enabled)
            characterImage2.rectTransform.anchoredPosition = _char2BasePos + new Vector2(0f, -y);
    }

    // ── 클릭 처리 ───────────────────────────────────────────

    void HandleClick()
    {
        if (isTyping)
            skipTyping = true;
        else if (isWaiting)
            advanceNow = true;
    }

    // ── 메인 코루틴 ─────────────────────────────────────────

    IEnumerator PlayAllEntries()
    {
        for (entryIndex = 0; entryIndex < cutsceneData.entries.Count; entryIndex++)
        {
            var entry = cutsceneData.entries[entryIndex];

            if (entry.doFadeTransition)
            {
                // 페이드 아웃 → 캐릭터 교체 → 페이드 인
                yield return StartCoroutine(FadeOut(entry.fadeDuration));
                ApplyCharacter(entry);
                yield return StartCoroutine(FadeIn(entry.fadeDuration));
            }
            else
            {
                // 첫 엔트리이거나 캐릭터 스프라이트가 지정된 경우 즉시 적용
                if (entryIndex == 0 || entry.characterSprite != null)
                    ApplyCharacter(entry);
            }

            yield return StartCoroutine(TypewriterEffect(entry));
            yield return StartCoroutine(WaitOrSkip(entry.autoAdvanceDelay));
        }

        OnCutsceneComplete();
    }

    // ── 캐릭터 적용 ─────────────────────────────────────────

    void ApplyCharacter(CutsceneEntry entry)
    {
        // 캐릭터 1
        if (entry.characterSprite != null)
        {
            characterImage.sprite = entry.characterSprite;
            characterImage.enabled = true;
            _char1BasePos = entry.characterPosition;
            characterImage.rectTransform.anchoredPosition = _char1BasePos;
        }
        else if (entryIndex == 0)
        {
            characterImage.enabled = false;
        }

        // 캐릭터 2
        if (characterImage2 == null) return;

        if (entry.characterSprite2 != null)
        {
            characterImage2.sprite = entry.characterSprite2;
            characterImage2.enabled = true;
            _char2BasePos = entry.characterPosition2;
            characterImage2.rectTransform.anchoredPosition = _char2BasePos;
        }
        else if (entry.keepCharacter2)
        {
            // 이전 상태 그대로 유지
        }
        else
        {
            characterImage2.enabled = false;
        }
    }

    // ── 타이핑 효과 ─────────────────────────────────────────

    IEnumerator TypewriterEffect(CutsceneEntry entry)
    {
        isTyping = true;
        skipTyping = false;
        dialogueText.text = string.Empty;

        string full = entry.dialogueText ?? string.Empty;

        for (int i = 0; i < full.Length; i++)
        {
            if (skipTyping)
            {
                dialogueText.text = full;
                break;
            }
            dialogueText.text += full[i];
            yield return new WaitForSeconds(Mathf.Max(0f, entry.typewriterInterval));
        }

        isTyping = false;
    }

    // ── 자동 진행 대기 ──────────────────────────────────────

    IEnumerator WaitOrSkip(float delay)
    {
        isWaiting = true;
        advanceNow = false;
        float elapsed = 0f;

        while (elapsed < delay && !advanceNow)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isWaiting = false;
    }

    // ── 페이드 ──────────────────────────────────────────────

    IEnumerator FadeOut(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(elapsed / duration);
            yield return null;
        }
        SetFadeAlpha(1f);
    }

    IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(1f - elapsed / duration);
            yield return null;
        }
        SetFadeAlpha(0f);
    }

    void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null) return;
        Color c = fadeOverlay.color;
        c.a = Mathf.Clamp01(alpha);
        fadeOverlay.color = c;
    }

    // ── 컷씬 종료 ───────────────────────────────────────────

    // DialogueManager 등 다른 시스템이 구독해서 컷씬 종료를 감지할 수 있습니다.
    public static event System.Action OnCutsceneFinished;

    protected virtual void OnCutsceneComplete()
    {
        Debug.Log("[CutsceneManager] 컷씬 재생 완료.");
        if (cutsceneRoot != null) cutsceneRoot.SetActive(false);
        OnCutsceneFinished?.Invoke();
    }
}
