using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ProPixelizer.Tools;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEngine.EventSystems;
using System;
using Unity.VisualScripting;
using static ProPixelizer.Tools.SteppedAnimation;

public class SettingManager
{
    [Tooltip("Stepped 애니메이션이 저장될 최상위 폴더 경로 (예: Assets/SteppedClips)")]
    public string baseSaveFolder = "Assets/Resources/Data/Animation/SteppedClips";
    private const string CACHE_FILE_NAME = "SteppedCache.json";

    private int fps;
    private StepMode mode;

    public void Init()
    {
        fps = GameConfig.AnimationStepFps;
        mode = GameConfig.AnimationStepMode;
    }


    public void ReplaceAnimationClipsInAttackPattern(string ownerName, AttackPattern pattern)
    {
        if (pattern == null) return;

        bool modified = false;

        var type = pattern.GetType();
        while (type != null && type != typeof(object))
        {
            var fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (var field in fields)
            {
                // 단일 AttackPatternInfoClip
                if (typeof(AttackPatternInfoClip).IsAssignableFrom(field.FieldType))
                {
                    var info = field.GetValue(pattern) as AttackPatternInfoClip;
                    if (info != null)
                        modified |= ReplaceAnimationClipsInInfoClip(info);
                }
                // 배열 / 리스트 AttackPatternInfoClip
                else if (typeof(IEnumerable<AttackPatternInfoClip>).IsAssignableFrom(field.FieldType))
                {
                    var infos = field.GetValue(pattern) as IEnumerable<AttackPatternInfoClip>;
                    if (infos == null) continue;

                    foreach (var info in infos)
                    {
                        if (info != null)
                            modified |= ReplaceAnimationClipsInInfoClip(info);
                    }
                }
            }

            type = type.BaseType;
        }

        if (modified)
            EditorUtility.SetDirty(pattern);

        bool ReplaceAnimationClipsInInfoClip(AttackPatternInfoClip info)
        {
            bool modified = false;

            var type = info.GetType();
            var fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(AnimationClip))
                {
                    var clip = field.GetValue(info) as AnimationClip;
                    var replaced = ReplaceOrLoadSteppedClip(ownerName, clip);

                    if (replaced != null && replaced != clip)
                    {
                        field.SetValue(info, replaced);
                        modified = true;
                    }
                }
            }

            return modified;
        }

    }

    /// <summary>
    /// 주어진 AnimationClip을 Stepped 방식으로 변환하거나, 캐시에 존재하면 로드하여 반환.
    /// </summary>
    public AnimationClip ReplaceOrLoadSteppedClip(string ownerName, AnimationClip clip)
    {
        if (clip == null) return null;
        if (string.IsNullOrEmpty(ownerName))
        {
            Debug.LogWarning("교체하고자 하는 오너의 이름이 없습니다. " + clip.name);
            return null;
        }

        string ownerFolder = GetOwnerFolder(ownerName);
        string cachePath = $"{ownerFolder}/{ownerName}_{CACHE_FILE_NAME}";

        var cacheData = LoadCache(cachePath);

        // Load
        if (TryGetCachedClip(cacheData, clip, out string cachedPath))
        {
            var cachedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(cachedPath);
            if (cachedClip != null)
            {
                //Debug.Log($"애니메이션 로드 성공 {clip.name}");
                return cachedClip;
            }

            Debug.LogWarning($"⚠️ 캐시 경로에 애셋이 없습니다. 재생성합니다: {cachedPath}");
        }

        // Create
        string outputPath = $"{ownerFolder}/{clip.name}_stepped_{fps}fps_{mode}.anim";

        AnimationClip stepped = CreateSteppedClip(clip, outputPath);
        if (stepped == null)
        {
            Debug.LogError($"❌ Stepped 애니메이션 생성 실패: {clip.name}");
            return clip;
        }

        AddCacheEntry(cacheData, clip, outputPath);
        SaveCache(cachePath, cacheData);
        return stepped;
    }

    /// <summary>
    /// 현재 애니메이션을 SteppAnimation으로 변경
    /// ownerName 폴더에 에셋이 있으면 가져오고, 없으면 새로 만든다.
    /// </summary>
    /// <param name="ownername"></param>
    /// <param name="gameEntity"></param>
    public void ReplaceAllAnimationClipArraysInObject(string ownername, UnityEngine.Object gameEntity)
    {
        Type type = gameEntity.GetType();

        while (type != null && type != typeof(object))
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                bool isModified = false;

                // 배열 처리
                if (field.FieldType == typeof(AnimationClip[]))
                {
                    var clips = field.GetValue(gameEntity) as AnimationClip[];
                    if (clips == null) continue;

                    for (int i = 0; i < clips.Length; i++)
                    {
                        // 애니메이션을 교체하거나 생성한 후 스탭 애니메이션을 반환한다.
                        var replaced = ReplaceOrLoadSteppedClip(ownername, clips[i]);
                        if (replaced != null && replaced != clips[i])
                        {
                            clips[i] = replaced;
                            isModified = true;
                        }
                    }

                    if (isModified)
                    {
                        field.SetValue(gameEntity, clips);
                    }
                }

                // 단일 AnimationClip 처리
                else if (field.FieldType == typeof(AnimationClip))
                {
                    var clip = field.GetValue(gameEntity) as AnimationClip;
                    var replaced = ReplaceOrLoadSteppedClip(ownername, clip);
                    if (replaced != null && replaced != clip)
                    {
                        field.SetValue(gameEntity, replaced);
                        isModified = true;
                    }
                }

                if (isModified)
                {
                    EditorUtility.SetDirty(gameEntity);
                }
            }

            type = type.BaseType;
        }
    }

    private string GetOwnerFolder(string ownerName)
    {
        string folder = $"{baseSaveFolder}/{ownerName}";
        EnsureFolderExists(folder);
        return folder;
    }

    private void EnsureFolderExists(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] split = folder.Split('/');
        string path = split[0];

        for (int i = 1; i < split.Length; i++)
        {
            if (!AssetDatabase.IsValidFolder($"{path}/{split[i]}"))
                AssetDatabase.CreateFolder(path, split[i]);

            path += "/" + split[i];
        }
    }


    private AnimationClip CreateSteppedClip(AnimationClip sourceClip, string outputPath)
    {
        AnimationClip steppedClip = new AnimationClip();
        EditorUtility.CopySerialized(sourceClip, steppedClip);

        var sampleTimes = GetKeyframeTimes(sourceClip);

        foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip))
        {
            var sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (sourceCurve == null || sourceCurve.length == 0)
                continue; // 빈 커브는 무시

            // Keyframe 배열을 한 번에 생성 (AddKey 반복 대신)
            Keyframe[] keys = new Keyframe[sampleTimes.Count];
            for (int i = 0; i < sampleTimes.Count; i++)
            {
                float t = Mathf.Clamp(sampleTimes[i], 0, sourceClip.length);
                float value = sourceCurve.Evaluate(t);

                keys[i] = new Keyframe(
                    t,
                    value,
                    float.NegativeInfinity,
                    float.PositiveInfinity,
                    0f,
                    0f
                );
            }

            var newCurve = new AnimationCurve(keys);
            AnimationUtility.SetEditorCurve(steppedClip, binding, newCurve);
        }

        // Asset 생성/갱신
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath) != null)
        {
            AssetDatabase.DeleteAsset(outputPath);
        }

        AssetDatabase.CreateAsset(steppedClip, outputPath);
        AssetDatabase.ImportAsset(outputPath);

        //Debug.Log($"애니메이션 생성 {sourceClip.name}");

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
    }

    private List<float> GetKeyframeTimes(AnimationClip clip)
    {
        List<float> times = new();

        switch (mode)
        {
            case SteppedAnimation.StepMode.FixedRate:
                int frameCount = Mathf.CeilToInt(clip.length * fps);
                for (int i = 0; i <= frameCount; i++)
                    times.Add(i / fps);
                break;

            case SteppedAnimation.StepMode.FixedTimeDelay:
                float delay = 1f / fps;
                int count = Mathf.CeilToInt(clip.length / delay);
                for (int i = 0; i <= count; i++)
                    times.Add(i * delay);
                break;

            case SteppedAnimation.StepMode.Manual:
                Debug.LogWarning("Manual 모드는 현재 지원되지 않습니다.");
                break;
        }

        return times;
    }

    private SteppedCacheData LoadCache(string path)
    {
        if (!File.Exists(path))
            return new SteppedCacheData();

        return JsonUtility.FromJson<SteppedCacheData>(
            File.ReadAllText(path)
        );
    }

    private void SaveCache(string path, SteppedCacheData data)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, JsonUtility.ToJson(data, true));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private bool TryGetCachedClip(
    SteppedCacheData cacheData,
    AnimationClip clip,
    out string cachedPath
)
    {
        string guid = AssetDatabase.AssetPathToGUID(
            AssetDatabase.GetAssetPath(clip)
        );

        foreach (var entry in cacheData.stepped_cache)
        {
            if (entry.clipGUID == guid &&
                Mathf.Approximately(entry.fps, fps) &&
                entry.mode == mode.ToString())
            {
                cachedPath = entry.path;
                return true;
            }
        }

        cachedPath = null;
        return false;
    }

    private void AddCacheEntry(
    SteppedCacheData cacheData,
    AnimationClip clip,
    string steppedPath
)
    {
        string guid = AssetDatabase.AssetPathToGUID(
            AssetDatabase.GetAssetPath(clip)
        );

        cacheData.stepped_cache.RemoveAll(e =>
            e.clipGUID == guid &&
            Mathf.Approximately(e.fps, fps) &&
            e.mode == mode.ToString()
        );

        cacheData.stepped_cache.Add(new SteppedCacheEntry
        {
            clipGUID = guid,
            fps = fps,
            mode = mode.ToString(),
            path = steppedPath
        });
    }

    public void ClearSteppedCache()
    {
        //steppedCache.Clear();
        //if (File.Exists(cachePath))
        //    File.Delete(cachePath);
    }
}

[System.Serializable]
public class SteppedCacheEntry
{
    public string clipGUID;
    public float fps;
    public string mode;
    public string path;
}

[System.Serializable]
public class SteppedCacheData
{
    public List<SteppedCacheEntry> stepped_cache = new();
}
