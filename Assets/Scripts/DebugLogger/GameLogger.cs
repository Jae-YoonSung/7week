using System;
using System.IO;
using UnityEngine;

public class GameLogger : MonoBehaviour
{
public static GameLogger Instance { get; private set; }

private string logFilePath;
public string LogFilePath => logFilePath;

private void Awake()
{
    if (Instance != null)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    string exeDir = Path.GetDirectoryName(Application.dataPath);
    string logDir = Path.Combine(exeDir, "log");
    Directory.CreateDirectory(logDir);

    string fileName = $"GameLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
    logFilePath = Path.Combine(logDir, fileName);

    // 유니티 로그만 처리
    Application.logMessageReceived += HandleUnityLog;

    Log("=== Game Session Started ===");
}

public void Log(string message)
{
    string formatted = $"[{DateTime.Now:HH:mm:ss}] {message}";
    Debug.Log(formatted); // 콘솔에는 출력
    File.AppendAllText(logFilePath, formatted + Environment.NewLine); // 파일에도 출력
}

private void HandleUnityLog(string logString, string stackTrace, LogType type)
{
    // 직접 출력한 로그는 이미 파일에 썼으므로, 중복 방지
    if (logString.StartsWith("["))
        return;

    string formatted = $"[{DateTime.Now:HH:mm:ss}] [{type}] {logString}";
    if (type == LogType.Error || type == LogType.Exception)
        formatted += $"\\n{stackTrace}";

    File.AppendAllText(logFilePath, formatted + Environment.NewLine);
}

private void OnDestroy()
{
    Application.logMessageReceived -= HandleUnityLog;
}

}