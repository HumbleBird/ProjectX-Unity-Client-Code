using ProPixelizer.Tools;
using UnityEngine;


public static class GameConfig
{
    private const string baseFolder = "Data/GameConfig";

    private static LayerData _layer;
    public static LayerData Layer
        => _layer ??= Resources.Load<LayerData>($"{baseFolder}/LayerData"); // Resources/LayerData.asset

    private static RuntimeSettingsData _runtimeSettings;
    public static RuntimeSettingsData RuntimeSettings
    {
        get
        {
            if (_runtimeSettings == null)
                _runtimeSettings = Resources.Load<RuntimeSettingsData>($"{baseFolder}/RuntimeSettingsData");

            return _runtimeSettings;
        }
    }

    /// <summary>
    /// 씬 시작 시점에 한번 호출해서 Resources 로드를 앞당김(예압).
    /// </summary>
    public static void PreloadRuntimeSettings()
    {
        _ = RuntimeSettings;
        if (_runtimeSettings == null)
            Debug.LogError($"[GameConfig] RuntimeSettingsData not found at Resources/{baseFolder}/RuntimeSettingsData.asset");
    }

    // 편의 프로퍼티
    public static int AnimationStepFps => RuntimeSettings != null ? RuntimeSettings.animationStepFps : 30;
    public static AudioClip UIButtonClickClip => RuntimeSettings != null ? RuntimeSettings.m_UIButtonClickAudioClip : null;
    public static SteppedAnimation.StepMode AnimationStepMode
    => RuntimeSettings != null
        ? RuntimeSettings.mode
        : SteppedAnimation.StepMode.FixedRate;

}
