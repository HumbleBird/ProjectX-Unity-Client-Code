using Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
    public string GetKeyFieldName();
}

/// <summary>
/// 저장 방식의 추상화 (나중에 Easy Save, Firebase 등으로 교체 가능)
/// </summary>
public interface IDataStorage
{
    Task SaveAsync(string path, string json);
    Task<string> LoadAsync(string path);
    bool Exists(string path);
}


#region 기본 로컬 파일 저장 방식
public class LocalFileStorage : IDataStorage
{
    public async Task SaveAsync(string path, string json)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(path, json);
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    public async Task<string> LoadAsync(string path)
    {
        if (!File.Exists(path))
            return null;
        return await File.ReadAllTextAsync(path);
    }

    public bool Exists(string path) => File.Exists(path);
}
#endregion

// TODO 나중에 easy save 에셋을 이용할 것
public partial class DataManager
{
    // 🔹 캐시: 모든 로드된 데이터 저장
    private readonly Dictionary<Type, object> _dataCache = new Dictionary<Type, object>();
    private IDataStorage _storage;

    // 🔹 공통 데이터 (런타임 단일)
    public SettingData settingData => Get<SettingData>();
    public AchievementData achievementData => Get<AchievementData>();
    public PlayStatistics playStatistics => Get<PlayStatistics>();

    // 🔹 세이브 슬롯 (ID 기반)
    public Dictionary<int, SaveSlotData> SaveDic => GetDic<SaveSlotLoader, int, SaveSlotData>();

    public bool IsReady { get; private set; }
    public event Action OnDataReady;

    public DataManager()
    {
        _storage = new LocalFileStorage(); // 기본 로컬 파일 저장
    }

    JsonSerializerSettings settings = new JsonSerializerSettings
    {
        // 💥 핵심 수정: 기본값 5에서 32 (또는 그 이상)로 증가
        MaxDepth = 32,

        // 이외에 저장에 필요한 설정들을 추가합니다.
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto, // List<object> 등 타입을 보존하기 위해 필요할 수 있습니다
        Formatting = Formatting.Indented,                   // ✅ 보기 좋은 줄바꿈 JSON
    };

    #region 📂 경로
    public string GetFilePath()
    {
        string path;

#if UNITY_EDITOR
        // 1. Unity Editor 경로를 가져와서
        path = Application.dataPath + "/Resources/Data/Save";
#else
    // 2. 빌드 환경 경로를 가져와서
    path = Application.persistentDataPath + "/Save";
#endif

        // 3. 💡 최종 반환 전에 모든 백슬래시(\)를 슬래시(/)로 변경하여 경로 통일
        // Unity 환경에서 슬래시(/)를 사용하는 것이 일반적으로 안전합니다.
        return path.Replace('\\', '/');
    }
    #endregion

    #region 🧩 초기화
    public async Task InitAsync()
    {
        Directory.CreateDirectory(GetFilePath());

        // 고정 데이터 (딕셔너리형)
        await LoadLoaderAsync<SaveSlotLoader, int, SaveSlotData>();

        // 단일형
        await LoadSingleAsync<SettingData>();
        await LoadSingleAsync<AchievementData>();
        await LoadSingleAsync<PlayStatistics>();

        IsReady = true;
        OnDataReady?.Invoke();
    }
    #endregion

    #region 🔄 비동기 로드
    // DataManager.cs 파일 내 #region 🔄 비동기 로드 섹션에 추가

    /// <summary>
    /// 🔄 단일 데이터 타입 (T)을 저장 경로에서 로드합니다.
    /// (ILoader를 상속받지 않는 객체용)
    /// </summary>
    public async Task<T> LoadSingleAsync<T>(string fileName = null)
        where T : new() // New() 제약 조건을 추가하여 Activator.CreateInstance 대신 T new() 사용 가능하게 함
    {
        Type t = typeof(T);
        string name = fileName ?? t.Name;
        string path = $"{GetFilePath()}/{name}.json";
        string json = null;

        // File.ReadAllText는 동기식이지만, Task.Run을 사용하여 비동기 환경처럼 작동하도록 포장
        if (File.Exists(path))
        {
            // 💡 동기 파일 시스템 접근을 Task로 포장
            json = await File.ReadAllTextAsync(path);
        }

        if (string.IsNullOrEmpty(json))
        {
            // 파일이 없으면 새 인스턴스 생성 (default 대신 new T() 사용)
            T newObj = new T();
            _dataCache[t] = newObj;
            return newObj;
        }


        T obj = JsonConvert.DeserializeObject<T>(json);
        _dataCache[t] = obj;
        return obj;
    }

    // DataManager.cs 파일 내 #region 🔄 비동기 로드 섹션에 추가

    /// <summary>
    /// 🔄 딕셔너리 기반 데이터 (TLoader)를 로드합니다.
    /// 1️ Application.persistentDataPath 에서 먼저 로드
    /// 2️ 없으면 Resources 폴더에서 기본 데이터(TextAsset) 로드
    /// 3️ 둘 다 없으면 새 객체 생성
    /// </summary>
    public async Task<TLoader> LoadLoaderAsync<TLoader, TKey, TValue>(string fileName = null)
        where TLoader : ILoader<TKey, TValue>, new()
    {
        Type t = typeof(TLoader);
        string name = fileName ?? t.Name;

        // 🔹 실제 JSON 파일 경로
        string jsonPath = string.Format("{0}/{1}.json", GetFilePath(), name);
        TLoader loaderObj = default;
        try
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"⚠️ 파일이 존재하지 않습니다: {jsonPath}");
                loaderObj = new TLoader();
                _dataCache[t] = loaderObj;
                return loaderObj;
            }

            // 🔹 JSON 파일 읽기
            string json = await File.ReadAllTextAsync(jsonPath);
            loaderObj = JsonConvert.DeserializeObject<TLoader>(json, settings);

            if (loaderObj == null)
            {
                Debug.LogWarning($"⚠️ 게임 슬롯 데이터 JsonUtility 변환 실패: {jsonPath}");
                loaderObj = new TLoader();
            }

            _dataCache[t] = loaderObj;
            Debug.Log($"✅ 게임 슬롯 데이터 로드 완료: {jsonPath}");
            return loaderObj;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 게임 슬롯 데이터 로드 실패: {e.Message}");
            loaderObj = new TLoader();
            _dataCache[t] = loaderObj;
            return loaderObj;
        }
    }

    #endregion

    #region 💾 비동기 저장
    public async Task SaveAsync<T>(string fileName = null)
    {
        Type t = typeof(T);
        if (!_dataCache.TryGetValue(t, out object value))
        {
            Debug.LogWarning($"⚠️ {t.Name} 캐시에 없음. 저장 불가");
            return;
        }

        //string json = JsonUtility.ToJson(value, true);
        string json = JsonConvert.SerializeObject(value, settings);
        string path = $"{GetFilePath()}/{(fileName ?? t.Name)}.json";

        await _storage.SaveAsync(path, json);

        Debug.Log($"✅ [{t.Name}] 저장 완료: {path}");
    }
    #endregion

    #region 📤 Get / Set
    public T Get<T>() where T : new()
    {
        Type t = typeof(T);

        // 1. 캐시에서 데이터 조회
        if (_dataCache.TryGetValue(t, out object value))
        {
            // 데이터가 로드되었지만 null인 경우도 포함 (LoadAsync에서 빈 객체가 생성되었을 경우)
            return (T)value;
        }

        return default;

        // 2. 캐시에 데이터가 없는 경우
        //{
        //    Debug.LogWarning($"⚠️ {t.Name} 데이터가 로드되지 않았습니다. 빈 객체를 생성하여 반환합니다.");

        //    // 3. 새로운 인스턴스 생성 및 캐시에 추가
        //    // T : new() 제약 조건이 있으므로 안전하게 new T() 사용
        //    T newObj = new T();
        //    _dataCache.Add(t, newObj);

        //    return newObj;
        //}
    }

    public void Set<T>(T data)
    {
        _dataCache[typeof(T)] = data;
    }

    // DataManager.cs 파일 내에 추가


    /// <summary>
    /// 딕셔너리 기반 데이터(ILoader<TKey, TValue>)의 캐시를 갱신
    /// (갱신된 Dict를 기반으로 TLoader 객체를 재구성하여 캐시에 반영)
    /// </summary>
    public void SetDic<TLoader, TKey, TValue>(Dictionary<TKey, TValue> dict)
        where TLoader : ILoader<TKey, TValue>
    {
        Type type = typeof(TLoader);

        // 1. 로더 객체 생성
        var loader = Activator.CreateInstance<TLoader>();

        // 2. TLoader 객체 내 List<TValue> 필드를 찾음 (ILoader 구현체의 전제)
        //    (CopyDicValueAsync 로직과 동일)
        var field = type.GetFields()
            .FirstOrDefault(f => f.FieldType == typeof(List<TValue>));

        if (field != null)
        {
            // 3. 갱신된 Dict의 Value들을 List로 변환하여 로더 필드에 할당
            var newList = dict.Values.ToList();
            field.SetValue(loader, newList);

            // 4. 재구성된 로더 객체를 DataManager 캐시에 반영
            _dataCache[type] = loader;
        }
        else
        {
            Debug.LogError($"❌ {type.Name}에서 List<{typeof(TValue).Name}> 타입 필드를 찾을 수 없어 캐시 갱신 실패.");
        }
    }

    #endregion

    #region 🔍 딕셔너리 접근
    public TValue GetDicValue<TLoader, TKey, TValue>(TKey key)
        where TLoader : ILoader<TKey, TValue>
    {
        if (_dataCache.TryGetValue(typeof(TLoader), out object loaderObj))
        {
            var loader = loaderObj as ILoader<TKey, TValue>;
            var dict = loader.MakeDict();
            if (dict.TryGetValue(key, out TValue value))
                return value;
        }

        Debug.LogWarning($"⚠️ {typeof(TLoader).Name}에 key({key}) 없음");
        return default;
    }

    public Dictionary<TKey, TValue> GetDic<TLoader, TKey, TValue>()
        where TLoader : ILoader<TKey, TValue>
    {
        if (_dataCache.TryGetValue(typeof(TLoader), out object loaderObj))
        {
            var loader = loaderObj as ILoader<TKey, TValue>;
            return loader.MakeDict();
        }

        Debug.LogWarning($"⚠️ {typeof(TLoader).Name} 데이터 캐시에 없음");
        return new Dictionary<TKey, TValue>();
    }
    #endregion

    #region ⚙️ 스토리지 교체 (Easy Save 전환 지원)
    public void SetStorage(IDataStorage storage)
    {
        _storage = storage;
    }
    #endregion

    /// <summary>
    /// 딕셔너리 기반 데이터(ILoader<TKey, TValue>)의 특정 키 데이터를 복제.
    /// 예: CopyDicValue<SaveSlotLoader, int, SaveSlot>(0, 2)
    /// </summary>
    public async Task CopyDicValueAsync<TLoader, TKey, TValue>(TKey fromKey, TKey toKey)
        where TLoader : ILoader<TKey, TValue>
    {
        // 전체 딕셔너리 로드 (ILoader 기반)
        var dict = GetDic<TLoader, TKey, TValue>();

        if (!dict.ContainsKey(fromKey))
        {
            Debug.LogError($"⚠️ 복사 실패: {typeof(TLoader).Name}에 Key({fromKey}) 없음");
            return;
        }

        // 원본 데이터 깊은 복사 (Json 직렬화 기반)
        var original = dict[fromKey];
        string json = JsonUtility.ToJson(original);
        var clone = JsonUtility.FromJson<TValue>(json);

        // 1️⃣ TLoader 인스턴스를 생성하고 GetKeyFieldName()을 호출하여 필드명을 동적으로 가져옵니다.
        var loaderInstance = Activator.CreateInstance<TLoader>();
        string keyFieldName = loaderInstance.GetKeyFieldName(); // 💡 동적으로 필드명 획득 (예: "slotId")
        var keyField = typeof(TValue).GetField(keyFieldName); 
        if (keyField != null)
        {
            // TKey를 키 필드(keyField)의 실제 타입(예: int)으로 변환
            // Convert.ToInt32(toKey)보다 훨씬 유연하고 안전합니다.
            object convertedValue = Convert.ChangeType(toKey, keyField.FieldType);

            keyField.SetValue(clone, convertedValue); // 변환된 값을 필드에 설정
        }

        // 덮어쓰기 또는 신규 추가
        dict[toKey] = clone;

        // 데이터 저장
        SetDic<TLoader, TKey, TValue>(dict);

        // 파일로 저장
        await SaveAsync<TLoader>();

        Debug.Log($"✅ {typeof(TLoader).Name} - Key({fromKey}) → Key({toKey}) 복사 완료");
    }

    // 🧹 단일 데이터 삭제
    public async Task DeleteAsync<T>()
    {
        Type t = typeof(T);
        string filePath = $"{GetFilePath()}/{t.Name}.json";

        BackupFile(filePath);

        if (_dataCache.ContainsKey(t))
            _dataCache.Remove(t);

        if (File.Exists(filePath))
            File.Delete(filePath);

        Debug.Log($"🗑️ {t.Name} 데이터 삭제 완료 (백업됨)");
        await Task.CompletedTask;
    }

    /// <summary>
    /// 🧹 딕셔너리 기반 데이터(ILoader)의 모든 항목을 삭제 (파일 및 캐시)
    /// </summary>
    public async Task DeleteDicAllAsync<TLoader, TKey, TValue>()
        where TLoader : ILoader<TKey, TValue>
    {
        Type type = typeof(TLoader);
        string filePath = $"{GetFilePath()}/{type.Name}.json";

        // 백업 후 캐시 및 파일 삭제
        BackupFile(filePath);
        _dataCache.Remove(type);

        if (File.Exists(filePath))
            File.Delete(filePath);

        Debug.Log($"🗑️ {type.Name} 전체 삭제 완료 (백업됨)");
        await Task.CompletedTask;
    }


    /// <summary>
    /// 🧹 딕셔너리 기반 데이터(ILoader)에서 특정 키를 삭제
    /// </summary>
    public async Task DeleteDicKeyAsync<TLoader, TKey, TValue>(TKey key)
        where TLoader : ILoader<TKey, TValue>
    {
        Type type = typeof(TLoader);
        string filePath = $"{GetFilePath()}/{type.Name}.json";
        var dict = GetDic<TLoader, TKey, TValue>();

        // 특정 키 삭제
        if (!dict.ContainsKey(key))
        {
            Debug.LogWarning($"⚠️ {type.Name}에 Key({key}) 없음. 삭제 불가");
            return;
        }

        // 💡 변경 전 상태를 백업
        BackupFile(filePath); // 여기에 백업을 추가하는 것을 고려할 수 있습니다.

        dict.Remove(key);

        // 💡 SetDic을 사용하여 로더 재구성 및 캐시 반영 (SetDic이 구현되었다고 가정)
        SetDic<TLoader, TKey, TValue>(dict);

        await SaveAsync<TLoader>();

        Debug.Log($"🗑️ {type.Name} - Key({key}) 삭제 완료");
    }
}
