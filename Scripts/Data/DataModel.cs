using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UIElements;
using static ControllableObject;
using static Define;

namespace Data
{
    #region Object Save Data (오브젝트가 가지고 있는 데이터)

    [Serializable]
    public abstract class BaseData
    {
        // 모든 저장 데이터가 공통으로 가질 필드를 여기에 정의합니다.
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string guid;
    }

    [Serializable]
    public class GameEntityData : BaseData
    {
        public AttributeSystemData attributeSystemData; // 스탯, 보상, 공격 패턴의 정보를 가짐.
        public E_ActionType CurrentActionType;
        public E_ActionType BeforeActionType;
        public E_ActionType NextActionType;
        public GameEntityAnimationData gameEntityAnimationData;
        public AttackPatternData thisAttackPattern;
    }

    [Serializable]
    public class ControllableObjectData : GameEntityData
    {
        public List<BaseData> attackReadyItemData;
        public HashSet<AttackPatternData> readyAttackPatternData;
        public OnChangeGradeEventArgs gradeArgs;
        public string targetGuid;
    }

    // 1. AttributeSystem의 "상태"만 담는 순수 데이터 클래스 (MonoBehaviour 상속 금지)
    [Serializable]
    public class AttributeSystemData : BaseData
    {
        public BaseStat stat;
        public List<AttackPatternData> attackPatterns;
        public RewardData rewardData;
    }

    [Serializable]
    public class BuildingCardData : BaseData
    {
        public GameEntityData gameEntitySaveData;
    }


    [Serializable]
    public class ItemData : BaseData
    {
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        public string onwerGuid;
    }

    [Serializable]
    public class ProjectileData : ItemData
    {
        public Vector3 velocity;   // Rigidbody.velocity
        public Vector3 angularVelocity; // Rigidbody.angularVelocity
        public string targetGuid;  // GameEntity._guid
    }

    [Serializable]
    public class AttackPatternData : BaseData
    {
        public int id; // 고유 ID
        public List<string> targetsGuid;

        [Header("Condition")]
        public StatValue coolTime = new StatValue(1, false);
        public StatValue lastCoolTime = new StatValue(1, false);
        public StatValue manaCost = new StatValue(0, false);

        [Header("Damage Info")]
        public StatValue physicalAttackDamage = new StatValue(0, false);      // 물리 공격 데미지
        public StatValue magicAttackDamage = new StatValue(0, false);         // 마법 공격 데미지
        public StatValue physicalFixedDamage = new StatValue(0, false);       // 물리 고정 데미지
        public StatValue magicFixedDamage = new StatValue(0, false);          // 마법 고정 데미지
        public StatValue physicalArmorPenetraion = new StatValue(0, false);   // 물리 방어구 관통력
        public StatValue magicalArmorPenetraion = new StatValue(0, false);    // 마법 방어구 관통력

        [Header("Battle Attack Chance")]
        public StatValue criticalChance = new StatValue(0, false);     // 치명타율
        public StatValue criticalDamageUp = new StatValue(1, false);   // 치명타 데미지 증가율
        public StatValue accuracy = new StatValue(0, false);           // 명중률
        public StatValue attackSpeed = new StatValue(1, false);        // 공격 속도
        public StatValue knockbackChance = new StatValue(0, false);    // 넉백 확률
        public StatValue lifeStealPercent = new StatValue(0, false);   // 흡혈 비율 - 피해량 대비
    }

    [Serializable]
    public class GameEntityAnimationData
    {
        // 애니메이션 진행 정도
        public float speed;
        public float normalizedTime;
        // 진행 중인 애니메이션 이름
        public int stateNameHash;
    }

    [Serializable]
    public class RewardData
    {
        [Header("Down Jam")]
        public int downJamMin;
        public int downJamMax;
        public float downJamProb;

        [Header("Card")]
        public List<string> rewardCardNames;
        public float CardProb;

        [Header("Buff")]
        public List<int> rewardBuffIds;
        public float BuffProb;

        [Header("Effect")]
        public List<int> rewardEffectIds;
        public float EffectProb;

        public bool m_ISCheckChange = false;
    }

    #endregion

    #region 🧱 Save Data (유동 데이터)

    [Serializable]
    public class DungeonSaveData
    {
        // 맵에 배치된 오브젝트들의 정보
        public List<BaseData> gameEntityDatas = new ();
        //public List<GameEntitySaveData> itemDatas = new ();

        // 카드
        public List<BaseData> buildingCardDatas = new();
        // 다운 잼 (재화)
        public int downJam;

        // 카메라 위치
        public Vector3 cameraPos;
        public Quaternion cameraRot;
    }

    [Serializable]
    public class CampSaveData
    {

    }

    [Serializable]
    public class SaveSlotData
    {
        public int slotId;
        public DungeonSaveData dungeondata;
        public CampSaveData campdata;
        public Define.Scene LastScene; // 마지막으로 플레이 했던 씬
        public string createTime;
        public string lastSaveTime;
        public double totalPlaySeconds; // 총 플레이 시간 (초)
    }

    [Serializable]
    public class SaveSlotLoader : ILoader<int, SaveSlotData>
    {
        public List<SaveSlotData> stats = new List<SaveSlotData>();

        public string GetKeyFieldName() => "slotId";

        public Dictionary<int, SaveSlotData> MakeDict()
        {
            Dictionary<int, SaveSlotData> dict = new Dictionary<int, SaveSlotData>();
            
            foreach (SaveSlotData stat in stats)
                dict.Add(stat.slotId, stat);
            return dict;
        }
    }
    #endregion


    #region ⚙️ Single Data
    [Serializable]
    public class SettingData
    {
        public float masterVolume = 1f;
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;
        public bool isFullscreen = true;
    }

    [Serializable]
    public class AchievementData
    {
        public List<string> achievedList = new();
    }

    [Serializable]
    public class PlayStatistics
    {
        public int lastSlotID;
    }
    #endregion

}