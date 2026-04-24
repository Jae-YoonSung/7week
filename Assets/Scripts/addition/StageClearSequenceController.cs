using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class StageClearSequenceController : MonoBehaviour
{
    [Serializable]
    private struct Timings
    {
        public float initialDelay;
        public float hideDeskObjectsDelay;
        public float afterBookCloseDelay;
        public float afterStandUpDelay;
        public float afterBridgeCameraDelay;
        public float waitForShelfCameraTransition;
        public float afterInsertDelay;
        public float finalFadeDuration;
        public float lobbyLoadDelay;
    }

    [Serializable]
    private struct BookCloseEntry
    {
        [Tooltip("Empty means the spawned book root.")]
        public string childPath;
        public float rotationZ;
        public RotateMode rotateMode;
        public float duration;
        public float delay;
        public Ease ease;
    }

    [Serializable]
    private struct BookSpawnSettings
    {
        public Transform sourceTransform;
        public GameObject closingBookPrefab;
        public Transform parent;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        public Vector3 spawnScale;
        public BookCloseEntry[] closeEntries;
    }

    [Serializable]
    private struct BookInsertSettings
    {
        public Transform stagingTarget;
        public float stagingMoveDuration;
        public Ease stagingMoveEase;
        public bool matchStagingRotation;
        public bool matchStagingScale;
        [Header("Staging Swap")]
        public GameObject stagingSwapPrefab;
        public Transform shelfTarget;
        public float moveDuration;
        public Ease moveEase;
        public bool matchRotation;
        public bool matchScale;
    }

    [Serializable]
    private struct ColoredBookRevealSettings
    {
        [Header("Material Color Change")]
        public Color targetColor;
        public float colorChangeDuration;
        [Tooltip("If empty, changes all Renderers. Otherwise, specify child paths (e.g. 'Cover').")]
        public string[] targetRendererPaths;
    }

    [Serializable]
    private struct CameraRotationSettings
    {
        public bool enable;
        [Tooltip("회전할 각도 (현재 회전값 기준 상대값)")]
        public Vector3 rotationOffset;
        public float duration;
        public Ease ease;
        [Tooltip("회전이 끝날 때까지 다음 연출(책 꽂기 등)을 대기할지 여부")]
        public bool waitForCompletion;
    }

    [Serializable]
    private struct FigureRevealEntry
    {
        public GameObject existingObject;
        public GameObject prefab;
        public Transform spawnPoint;
        public float delay;
        public float appearDuration;
        public float startScaleMultiplier;
        public Ease appearEase;
    }

    [Serializable]
    private struct HeadBobSettings
    {
        public Transform target;
        public float stepDuration;
        public float verticalOffset;
        public Ease ease;
        public float resetDuration;
    }

    [Header("Start")]
    [SerializeField] private bool _autoPlayOnWin = true;
    [SerializeField] private CanvasGroup _fadeCanvasGroup;

    [Header("Hide On Sequence Start")]
    [Tooltip("Result panel, result texts, continue hint, and any object that must disappear immediately.")]
    [SerializeField] private GameObject[] _objectsToHideOnSequenceStart;
    [SerializeField] private bool _hideRuntimeCharacterObjects = true;

    [Header("Hide During Covered Fade")]
    [Tooltip("Opened book on the desk and objects on top of it.")]
    [SerializeField] private GameObject[] _deskObjectsToHide;

    [Header("Book")]
    [SerializeField] private BookSpawnSettings _bookSpawn;
    [SerializeField] private BookInsertSettings _bookInsert;
    [SerializeField] private ColoredBookRevealSettings _coloredBookReveal = new ColoredBookRevealSettings
    {
        targetColor = Color.red,
        colorChangeDuration = 1.0f
    };

    [Header("Cinemachine 3")]
    [SerializeField] private CinemachineCamera _standUpCamera;
    [SerializeField] private int _standUpCameraPriority = 90;
    [SerializeField] private CinemachineCamera _bridgeToShelfCamera;
    [SerializeField] private int _bridgeToShelfCameraPriority = 95;
    [SerializeField] private CinemachineCamera _lookAtShelfCamera;
    [SerializeField] private int _lookAtShelfCameraPriority = 100;
    [SerializeField] private CinemachineCamera[] _otherCameras;
    [SerializeField] private int _otherCameraPriority = 0;

    [Header("Stand Up Event")]
    [Tooltip("Use this for stand-up animation, chair movement, SFX, etc.")]
    [SerializeField] private UnityEvent _onStandUp;

    [Header("Bridge Camera Event")]
    [Tooltip("Use this for the camera step between stand-up and shelf look.")]
    [SerializeField] private UnityEvent _onBridgeToShelfCamera;

    [Header("Bridge To Shelf Head Bob")]
    [SerializeField] private HeadBobSettings _bridgeToShelfHeadBob = new HeadBobSettings
    {
        stepDuration = 0.24f,
        verticalOffset = 0.015f,
        ease = Ease.InOutSine,
        resetDuration = 0.12f
    };

    [Header("Shelf Look Event")]
    [Tooltip("Use this for player turn, timeline play, lights, SFX, etc.")]
    [SerializeField] private UnityEvent _onLookAtShelf;

    [Header("Shelf Camera Rotation")]
    [SerializeField] private CameraRotationSettings _shelfCameraRotation = new CameraRotationSettings
    {
        enable = false,
        rotationOffset = Vector3.zero,
        duration = 1.0f,
        ease = Ease.InOutSine,
        waitForCompletion = false
    };

    [Header("Figures")]
    [SerializeField] private bool _hideExistingFiguresOnAwake = true;
    [SerializeField] private FigureRevealEntry[] _figureRevealEntries;

    [Header("Scene")]
    [SerializeField] private string _lobbySceneName = "LobbyScene";
    [SerializeField] private bool _recordStageClear = true;
    [SerializeField] private bool _triggerLobbyEndingDialogue;
    [SerializeField] private string _stageIdOverride;

    [Header("Timings")]
    [SerializeField] private Timings _timings = new Timings
    {
        initialDelay = 0.25f,
        hideDeskObjectsDelay = 0.15f,
        afterBookCloseDelay = 0.2f,
        afterStandUpDelay = 0.6f,
        afterBridgeCameraDelay = 0.4f,
        waitForShelfCameraTransition = 0.35f,
        afterInsertDelay = 0.2f,
        finalFadeDuration = 1.5f,
        lobbyLoadDelay = 0.5f
    };

    private bool _isPlaying;
    private GameObject _spawnedClosingBook;
    private Sequence _bridgeHeadBobSequence;
    private Vector3 _bridgeHeadBobBaseLocalPosition;
    private bool _hasSavedHeadBobBasePosition;
    private int _cachedStandUpCameraPriority;
    private int _cachedBridgeToShelfCameraPriority;
    private int _cachedLookAtShelfPriority;
    private int[] _cachedOtherCameraPriorities;

    public bool CanHandleWinSequence => enabled && gameObject.activeInHierarchy;

    private void Awake()
    {
        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.interactable = false;
        }

        if (_hideExistingFiguresOnAwake)
            HideExistingFigureObjects();
    }

    private void Start()
    {
        RegisterGameFlowCallback();
    }

    private void OnEnable()
    {
        RegisterGameFlowCallback();
    }

    private void OnDisable()
    {
        UnregisterGameFlowCallback();
    }

    private void OnDestroy()
    {
        StopBridgeToShelfHeadBob(restoreImmediately: true);
        DOTween.Kill(this);
        RestoreCameraPriorities();
    }

    private void RegisterGameFlowCallback()
    {
        if (!_autoPlayOnWin) return;

        var gfc = GameFlowController.Instance;
        if (gfc == null) return;

        gfc.OnGameEndDialogueComplete -= HandleGameEndDialogueComplete;
        gfc.OnGameEndDialogueComplete += HandleGameEndDialogueComplete;
    }

    private void UnregisterGameFlowCallback()
    {
        var gfc = GameFlowController.Instance;
        if (gfc != null)
            gfc.OnGameEndDialogueComplete -= HandleGameEndDialogueComplete;
    }

    private void HandleGameEndDialogueComplete(bool isWin)
    {
        if (!isWin || _isPlaying) return;

        // 에필로그(What if) 스테이지인 경우 연출 없이 즉시 로비로 이동
        // (NewGameConfig는 게임 중 초기화되므로 GameFlowController의 백업값을 참조)
        if (GameFlowController.Instance != null && GameFlowController.Instance.IsEpilogue)
        {
            RecordStageClearIfNeeded();
            LobbyDialogueManager.PendingEndingDialogue = _triggerLobbyEndingDialogue;

            string lobbyScene = !string.IsNullOrEmpty(NewGameConfig.LobbySceneName)
                ? NewGameConfig.LobbySceneName
                : _lobbySceneName;
            SceneManager.LoadScene(lobbyScene);
            return;
        }

        PlaySequence();
    }

    [ContextMenu("Play Stage Clear Sequence")]
    public void PlaySequence()
    {
        if (_isPlaying) return;
        StartCoroutine(PlaySequenceCoroutine());
    }

    private IEnumerator PlaySequenceCoroutine()
    {
        _isPlaying = true;
        SetObjectsActive(_objectsToHideOnSequenceStart, false);
        HideRuntimeCharacterObjects();

        if (_timings.initialDelay > 0f)
            yield return new WaitForSeconds(_timings.initialDelay);

        if (_timings.hideDeskObjectsDelay > 0f)
            yield return new WaitForSeconds(_timings.hideDeskObjectsDelay);

        SetObjectsActive(_deskObjectsToHide, false);
        SpawnClosingBook();
        yield return CloseSpawnedBook();

        if (_timings.afterBookCloseDelay > 0f)
            yield return new WaitForSeconds(_timings.afterBookCloseDelay);

        ApplyStandUpCameraPriorities();
        _onStandUp?.Invoke();

        if (_timings.afterStandUpDelay > 0f)
            yield return new WaitForSeconds(_timings.afterStandUpDelay);

        ApplyBridgeToShelfCameraPriorities();
        _onBridgeToShelfCamera?.Invoke();

        if (_timings.afterBridgeCameraDelay > 0f)
            yield return new WaitForSeconds(_timings.afterBridgeCameraDelay);

        ApplyLookAtShelfCameraPriorities();
        StartBridgeToShelfHeadBob();
        _onLookAtShelf?.Invoke();

        if (_timings.waitForShelfCameraTransition > 0f)
            yield return new WaitForSeconds(_timings.waitForShelfCameraTransition);

        StopBridgeToShelfHeadBob(restoreImmediately: false);

        if (_shelfCameraRotation.enable)
        {
            if (_shelfCameraRotation.waitForCompletion)
                yield return RotateShelfCamera();
            else
                StartCoroutine(RotateShelfCamera());
        }

        yield return InsertBookToShelf();
        yield return RevealColoredBook();

        if (_timings.afterInsertDelay > 0f)
            yield return new WaitForSeconds(_timings.afterInsertDelay);

        yield return RevealFigures();
        yield return Fade(1f, _timings.finalFadeDuration);

        if (_timings.lobbyLoadDelay > 0f)
            yield return new WaitForSeconds(_timings.lobbyLoadDelay);

        RecordStageClearIfNeeded();
        LobbyDialogueManager.PendingEndingDialogue = _triggerLobbyEndingDialogue;

        // NewGameConfig에 전용 로비씬이 설정되어 있으면 우선 사용
        string lobbyScene = !string.IsNullOrEmpty(NewGameConfig.LobbySceneName)
            ? NewGameConfig.LobbySceneName
            : _lobbySceneName;
        SceneManager.LoadScene(lobbyScene);
    }

    private void SpawnClosingBook()
    {
        if (_bookSpawn.sourceTransform == null || _bookSpawn.closingBookPrefab == null)
            return;

        var parent = _bookSpawn.parent != null ? _bookSpawn.parent : _bookSpawn.sourceTransform.parent;
        Vector3 worldPosition = _bookSpawn.sourceTransform.position + _bookSpawn.positionOffset;
        Quaternion worldRotation = _bookSpawn.sourceTransform.rotation * Quaternion.Euler(_bookSpawn.rotationOffset);

        _spawnedClosingBook = Instantiate(_bookSpawn.closingBookPrefab, worldPosition, worldRotation, parent);

        if (_bookSpawn.spawnScale != Vector3.zero)
            _spawnedClosingBook.transform.localScale = _bookSpawn.spawnScale;
    }

    private IEnumerator CloseSpawnedBook()
    {
        if (_spawnedClosingBook == null || _bookSpawn.closeEntries == null || _bookSpawn.closeEntries.Length == 0)
            yield break;

        var sequence = DOTween.Sequence().SetLink(_spawnedClosingBook);

        foreach (var entry in _bookSpawn.closeEntries)
        {
            Transform target = ResolveCloseTarget(entry);
            if (target == null) continue;

            Vector3 targetEuler = new Vector3(
                target.localEulerAngles.x,
                target.localEulerAngles.y,
                entry.rotationZ);

            sequence.Insert(
                Mathf.Max(0f, entry.delay),
                target.DOLocalRotate(targetEuler, Mathf.Max(0.01f, entry.duration), entry.rotateMode)
                    .SetEase(entry.ease));
        }

        yield return sequence.WaitForCompletion();
    }

    private Transform ResolveCloseTarget(BookCloseEntry entry)
    {
        if (_spawnedClosingBook == null)
            return null;

        if (string.IsNullOrEmpty(entry.childPath))
            return _spawnedClosingBook.transform;

        Transform target = _spawnedClosingBook.transform.Find(entry.childPath);
        if (target == null)
            Debug.LogWarning($"[StageClearSequence] Missing child path: {entry.childPath}");
        return target;
    }

    private void ApplyStandUpCameraPriorities()
    {
        CacheAndLowerOtherCameraPriorities();

        if (_standUpCamera != null)
        {
            _cachedStandUpCameraPriority = _standUpCamera.Priority.Value;
            _standUpCamera.Priority = _standUpCameraPriority;
        }

        if (_lookAtShelfCamera != null)
        {
            _cachedLookAtShelfPriority = _lookAtShelfCamera.Priority.Value;
            _lookAtShelfCamera.Priority = _otherCameraPriority;
        }

        if (_bridgeToShelfCamera != null)
        {
            _cachedBridgeToShelfCameraPriority = _bridgeToShelfCamera.Priority.Value;
            _bridgeToShelfCamera.Priority = _otherCameraPriority;
        }
    }

    private void ApplyBridgeToShelfCameraPriorities()
    {
        CacheAndLowerOtherCameraPriorities();

        if (_standUpCamera != null)
            _standUpCamera.Priority = _otherCameraPriority;

        if (_bridgeToShelfCamera != null)
            _bridgeToShelfCamera.Priority = _bridgeToShelfCameraPriority;

        if (_lookAtShelfCamera != null)
            _lookAtShelfCamera.Priority = _otherCameraPriority;
    }

    private void ApplyLookAtShelfCameraPriorities()
    {
        CacheAndLowerOtherCameraPriorities();

        if (_standUpCamera != null)
            _standUpCamera.Priority = _otherCameraPriority;

        if (_bridgeToShelfCamera != null)
            _bridgeToShelfCamera.Priority = _otherCameraPriority;

        if (_lookAtShelfCamera != null)
        {
            _lookAtShelfCamera.Priority = _lookAtShelfCameraPriority;
        }
    }

    private void CacheAndLowerOtherCameraPriorities()
    {
        if (_otherCameras == null)
            return;

        if (_cachedOtherCameraPriorities != null && _cachedOtherCameraPriorities.Length == _otherCameras.Length)
        {
            for (int i = 0; i < _otherCameras.Length; i++)
            {
                var camera = _otherCameras[i];
                if (camera != null)
                    camera.Priority = _otherCameraPriority;
            }
            return;
        }

        _cachedOtherCameraPriorities = new int[_otherCameras.Length];
        for (int i = 0; i < _otherCameras.Length; i++)
        {
            var camera = _otherCameras[i];
            if (camera == null) continue;

            _cachedOtherCameraPriorities[i] = camera.Priority.Value;
            camera.Priority = _otherCameraPriority;
        }
    }

    private void RestoreCameraPriorities()
    {
        if (_standUpCamera != null)
            _standUpCamera.Priority = _cachedStandUpCameraPriority;

        if (_bridgeToShelfCamera != null)
            _bridgeToShelfCamera.Priority = _cachedBridgeToShelfCameraPriority;

        if (_lookAtShelfCamera != null)
            _lookAtShelfCamera.Priority = _cachedLookAtShelfPriority;

        if (_otherCameras == null || _cachedOtherCameraPriorities == null)
            return;

        int count = Mathf.Min(_otherCameras.Length, _cachedOtherCameraPriorities.Length);
        for (int i = 0; i < count; i++)
        {
            if (_otherCameras[i] != null)
                _otherCameras[i].Priority = _cachedOtherCameraPriorities[i];
        }
    }

    private void StartBridgeToShelfHeadBob()
    {
        Transform target = ResolveHeadBobTarget();
        if (target == null)
            return;

        StopBridgeToShelfHeadBob(restoreImmediately: true);

        if (!_hasSavedHeadBobBasePosition)
        {
            _bridgeHeadBobBaseLocalPosition = target.localPosition;
            _hasSavedHeadBobBasePosition = true;
        }

        float stepDuration = Mathf.Max(0.01f, _bridgeToShelfHeadBob.stepDuration);
        Vector3 upPos = _bridgeHeadBobBaseLocalPosition + new Vector3(
            0f,
            _bridgeToShelfHeadBob.verticalOffset,
            0f);
        Vector3 downPos = _bridgeHeadBobBaseLocalPosition;

        _bridgeHeadBobSequence = DOTween.Sequence().SetLink(target.gameObject);
        _bridgeHeadBobSequence.Append(target.DOLocalMove(upPos, stepDuration).SetEase(_bridgeToShelfHeadBob.ease));
        _bridgeHeadBobSequence.Append(target.DOLocalMove(downPos, stepDuration).SetEase(_bridgeToShelfHeadBob.ease));
        _bridgeHeadBobSequence.SetLoops(-1, LoopType.Restart);
    }

    private void StopBridgeToShelfHeadBob(bool restoreImmediately)
    {
        Transform target = ResolveHeadBobTarget();

        _bridgeHeadBobSequence?.Kill();
        _bridgeHeadBobSequence = null;

        if (target == null)
            return;

        DOTween.Kill(target);

        if (restoreImmediately)
        {
            if (_hasSavedHeadBobBasePosition)
                target.localPosition = _bridgeHeadBobBaseLocalPosition;
            return;
        }

        float resetDuration = Mathf.Max(0.01f, _bridgeToShelfHeadBob.resetDuration);
        target.DOLocalMove(_bridgeHeadBobBaseLocalPosition, resetDuration)
            .SetEase(Ease.OutSine)
            .SetLink(target.gameObject);
    }

    private Transform ResolveHeadBobTarget()
    {
        if (_bridgeToShelfHeadBob.target != null)
            return _bridgeToShelfHeadBob.target;

        return _bridgeToShelfCamera != null ? _bridgeToShelfCamera.transform : null;
    }

    private IEnumerator RotateShelfCamera()
    {
        if (_lookAtShelfCamera == null) yield break;

        Transform camTransform = _lookAtShelfCamera.transform;
        yield return camTransform.DOLocalRotate(
                _shelfCameraRotation.rotationOffset,
                Mathf.Max(0.01f, _shelfCameraRotation.duration),
                RotateMode.LocalAxisAdd)
            .SetEase(_shelfCameraRotation.ease)
            .SetLink(camTransform.gameObject)
            .WaitForCompletion();
    }

    private IEnumerator InsertBookToShelf()
    {
        if (_spawnedClosingBook == null)
            yield break;

        if (_bookInsert.stagingTarget != null)
        {
            yield return MoveBookToTarget(
                _bookInsert.stagingTarget,
                _bookInsert.stagingMoveDuration,
                _bookInsert.stagingMoveEase,
                _bookInsert.matchStagingRotation,
                _bookInsert.matchStagingScale);

            if (_bookInsert.stagingSwapPrefab != null)
            {
                var oldBook = _spawnedClosingBook;
                
                _spawnedClosingBook = Instantiate(
                    _bookInsert.stagingSwapPrefab,
                    oldBook.transform.position,
                    oldBook.transform.rotation,
                    oldBook.transform.parent);
                
                _spawnedClosingBook.transform.localScale = oldBook.transform.localScale;
                oldBook.SetActive(false);
            }
        }

        if (_bookInsert.shelfTarget != null)
            yield return MoveBookToTarget(
                _bookInsert.shelfTarget,
                _bookInsert.moveDuration,
                _bookInsert.moveEase,
                _bookInsert.matchRotation,
                _bookInsert.matchScale);
    }

    private IEnumerator RevealColoredBook()
    {
        if (_spawnedClosingBook == null)
            yield break;

        List<Renderer> targetRenderers = new List<Renderer>();

        if (_coloredBookReveal.targetRendererPaths != null && _coloredBookReveal.targetRendererPaths.Length > 0)
        {
            foreach (var path in _coloredBookReveal.targetRendererPaths)
            {
                Transform targetTransform = string.IsNullOrEmpty(path)
                    ? _spawnedClosingBook.transform
                    : _spawnedClosingBook.transform.Find(path);

                if (targetTransform != null)
                {
                    Renderer rnd = targetTransform.GetComponent<Renderer>();
                    if (rnd != null)
                        targetRenderers.Add(rnd);
                }
                else
                {
                    Debug.LogWarning($"[StageClearSequence] Missing renderer path: {path}");
                }
            }
        }
        else
        {
            targetRenderers.AddRange(_spawnedClosingBook.GetComponentsInChildren<Renderer>());
        }

        if (targetRenderers.Count > 0)
        {
            Sequence colorSeq = DOTween.Sequence().SetLink(_spawnedClosingBook);
            float duration = Mathf.Max(0.01f, _coloredBookReveal.colorChangeDuration);

            foreach (var rnd in targetRenderers)
                colorSeq.Join(rnd.material.DOColor(_coloredBookReveal.targetColor, duration));

            yield return colorSeq.WaitForCompletion();
        }
    }

    private IEnumerator MoveBookToTarget(
        Transform target,
        float duration,
        Ease moveEase,
        bool matchRotation,
        bool matchScale)
    {
        if (_spawnedClosingBook == null || target == null)
            yield break;

        var sequence = DOTween.Sequence().SetLink(_spawnedClosingBook);
        float tweenDuration = Mathf.Max(0.01f, duration);

        sequence.Join(_spawnedClosingBook.transform
            .DOMove(target.position, tweenDuration)
            .SetEase(moveEase));

        if (matchRotation)
        {
            sequence.Join(_spawnedClosingBook.transform
                .DORotateQuaternion(target.rotation, tweenDuration)
                .SetEase(moveEase));
        }

        if (matchScale)
        {
            sequence.Join(_spawnedClosingBook.transform
                .DOScale(target.lossyScale, tweenDuration)
                .SetEase(moveEase));
        }

        yield return sequence.WaitForCompletion();
    }

    private IEnumerator RevealFigures()
    {
        if (_figureRevealEntries == null) yield break;

        foreach (var entry in _figureRevealEntries)
        {
            if (entry.delay > 0f)
                yield return new WaitForSeconds(entry.delay);

            GameObject figure = ResolveFigureObject(entry);
            if (figure == null) continue;

            Transform target = figure.transform;
            Vector3 targetScale = target.localScale;
            float multiplier = entry.startScaleMultiplier <= 0f ? 0.01f : entry.startScaleMultiplier;

            target.localScale = targetScale * multiplier;
            figure.SetActive(true);

            yield return target
                .DOScale(targetScale, Mathf.Max(0.01f, entry.appearDuration))
                .SetEase(entry.appearEase)
                .SetLink(figure)
                .WaitForCompletion();
        }
    }

    private static GameObject ResolveFigureObject(FigureRevealEntry entry)
    {
        if (entry.existingObject != null)
            return entry.existingObject;

        if (entry.prefab == null || entry.spawnPoint == null)
            return null;

        var instance = Instantiate(entry.prefab, entry.spawnPoint.position, entry.spawnPoint.rotation, entry.spawnPoint.parent);
        instance.SetActive(false);
        return instance;
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (_fadeCanvasGroup == null)
            yield break;

        bool blocksRaycasts = targetAlpha > 0.999f;
        _fadeCanvasGroup.blocksRaycasts = blocksRaycasts;
        _fadeCanvasGroup.interactable = blocksRaycasts;

        yield return _fadeCanvasGroup
            .DOFade(targetAlpha, Mathf.Max(0.01f, duration))
            .SetEase(targetAlpha > _fadeCanvasGroup.alpha ? Ease.InQuad : Ease.OutQuad)
            .SetLink(gameObject)
            .WaitForCompletion();

        _fadeCanvasGroup.blocksRaycasts = blocksRaycasts;
        _fadeCanvasGroup.interactable = blocksRaycasts;
    }

    private void RecordStageClearIfNeeded()
    {
        if (!_recordStageClear) return;

        string stageId = !string.IsNullOrEmpty(_stageIdOverride)
            ? _stageIdOverride
            : NewGameConfig.StageId;

        if (!string.IsNullOrEmpty(stageId))
            StageClearRepository.Instance.RecordClear(stageId);
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (var go in objects)
        {
            if (go != null)
                go.SetActive(active);
        }
    }

    private void HideExistingFigureObjects()
    {
        if (_figureRevealEntries == null) return;

        foreach (var entry in _figureRevealEntries)
        {
            if (entry.existingObject != null)
                entry.existingObject.SetActive(false);
        }
    }

    private void HideRuntimeCharacterObjects()
    {
        if (!_hideRuntimeCharacterObjects)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        var roots = activeScene.GetRootGameObjects();
        var targets = new HashSet<GameObject>();

        foreach (var root in roots)
            CollectCharacterObjects(root.transform, targets);

        foreach (var target in targets)
        {
            if (target != null)
                target.SetActive(false);
        }
    }

    private static void CollectCharacterObjects(Transform current, HashSet<GameObject> targets)
    {
        if (current == null)
            return;

        if (current.name.StartsWith("Character", StringComparison.Ordinal))
        {
            targets.Add(current.gameObject);
            return;
        }

        for (int i = 0; i < current.childCount; i++)
            CollectCharacterObjects(current.GetChild(i), targets);
    }
}
