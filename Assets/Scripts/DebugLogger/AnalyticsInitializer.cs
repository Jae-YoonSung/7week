using System;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

// Analytics SDK 6.x의 비동기 초기화를 담당하는 컴포넌트.
// GameLogger와 동일한 GameObject에 추가한다.
// DontDestroyOnLoad는 GameLogger에서 이미 처리하므로 여기서는 생략한다.
public class AnalyticsInitializer : MonoBehaviour
{
    // Analytics 초기화 완료 여부. AnalyticsReporter가 이벤트 전송 전 반드시 확인한다.
    public static bool IsReady { get; private set; }

    // UnityServices 초기화 → 데이터 수집 시작 순서로 진행한다.
    // 실패 시 경고만 출력하고, 파일 로그 수집은 계속 동작한다.
    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            AnalyticsService.Instance.StartDataCollection();
            IsReady = true;
            Debug.Log("[AnalyticsInitializer] Analytics 초기화 완료.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AnalyticsInitializer] Analytics 초기화 실패 — 파일 로그는 계속 동작합니다. 사유: {e.Message}");
        }
    }
}
