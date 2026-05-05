using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 컷씬 재생 매니저.
///
/// [Canvas 구조]
///   Canvas
///   ├─ CutsceneImage   ← Image 컴포넌트 (cutsceneImage 슬롯)
///   ├─ DialogueText    ← TMP_Text 컴포넌트 (dialogueText 슬롯)
///   └─ FadeOverlay     ← Image 컴포넌트, Color=(0,0,0,0) (fadeOverlay 슬롯)
///
/// [이미지 교체 타이밍]
///   CutsceneEntry.image 에 스프라이트를 지정한 엔트리에서만 이미지가 바뀝니다.
///   비워두면 이전 이미지를 그대로 유지합니다.
///   doFadeTransition 체크 시 페이드 아웃 → 이미지 교체 → 페이드 인 순으로 전환됩니다.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    [Header("컷씬 데이터")]
    public CutsceneData cutsceneData;

    [Header("UI 참조")]
    public Image    cutsceneImage;
    public TMP_Text dialogueText;

    [Tooltip("전체 화면을 덮는 검은 Image. 시작 시 Alpha=0 이어야 합니다.")]
    public Image fadeOverlay;

    [Header("컷씬 종료 시 비활성화할 루트 오브젝트 (Canvas 등)")]
    public GameObject cutsceneRoot;

    // ── 내부 상태 ──────────────────────────────────────────
    private int  entryIndex;
    private bool isTyping;
    private bool isWaiting;
    private bool skipTyping;
    private bool advanceNow;
    private bool _playCutscene;

    // ── Unity 생명주기 ──────────────────────────────────────

    void Awake()
    {
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
            StartCoroutine(CompleteNextFrame());
            return;
        }

        SetFadeAlpha(0f);
        dialogueText.text = string.Empty;

        StartCoroutine(PlayAllEntries());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    IEnumerator CompleteNextFrame()
    {
        yield return null;
        OnCutsceneComplete();
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

            if (entry.image != null)
            {
                if (entry.doFadeTransition)
                {
                    yield return StartCoroutine(FadeOut(entry.fadeDuration));
                    ApplyImage(entry.image);
                    yield return StartCoroutine(FadeIn(entry.fadeDuration));
                }
                else
                {
                    ApplyImage(entry.image);
                }
            }

            yield return StartCoroutine(TypewriterEffect(entry));
            yield return StartCoroutine(WaitOrSkip(entry.autoAdvanceDelay));
        }

        OnCutsceneComplete();
    }

    // ── 이미지 적용 ─────────────────────────────────────────

    void ApplyImage(Sprite sprite)
    {
        if (cutsceneImage == null) return;
        cutsceneImage.sprite  = sprite;
        cutsceneImage.enabled = true;
        cutsceneImage.SetNativeSize();
    }

    // ── 타이핑 효과 ─────────────────────────────────────────

    IEnumerator TypewriterEffect(CutsceneEntry entry)
    {
        isTyping   = true;
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
        isWaiting  = true;
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

    public static event System.Action OnCutsceneFinished;

    protected virtual void OnCutsceneComplete()
    {
        Debug.Log("[CutsceneManager] 컷씬 재생 완료.");
        if (cutsceneRoot != null) cutsceneRoot.SetActive(false);
        OnCutsceneFinished?.Invoke();
    }
}
