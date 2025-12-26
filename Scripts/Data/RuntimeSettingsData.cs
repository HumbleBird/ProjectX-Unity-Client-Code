// RuntimeSettingsData.cs
// Resources에 두고 GameConfig가 로드해서 쓰는 런타임 설정(SO)

using ProPixelizer.Tools;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Config/Runtime Settings", fileName = "RuntimeSettingsData")]
public class RuntimeSettingsData : ScriptableObject
{
    [Header("Animation / Timing")]
    [Min(1)] public int animationStepFps = 30;
    public SteppedAnimation.StepMode mode = SteppedAnimation.StepMode.FixedRate;


    [Header("UI Audio")]
    public AudioClip m_UIButtonClickAudioClip;
}
