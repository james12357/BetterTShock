using System.ComponentModel;
using Newtonsoft.Json;
namespace BetterTShock;

public class Config
{
    [JsonProperty("BondDamageSharingRatio", Order = 1)]
    [DefaultValue(0.2f)]
    public float BondDamageSharingRatio { get; set; } = 0.2f;
    
    [JsonProperty("TimeZoneString", Order = 2)]
    [DefaultValue("China Standard Time")]
    public string TimeZoneString { get; set; } = "China Standard Time";
    
    [JsonProperty("竞技场-场地半径(图格)", Order = 10)]
    [DefaultValue(50)]
    public int ArenaRadius { get; set; } = 50;
    
    /// <summary>
    /// 定义了竞技场中要刷新的一种怪物及其数量
    /// </summary>
    public class ArenaMonster
    {
        [JsonProperty("怪物ID")]
        public int NpcId { get; set; } = 1; // 默认是史莱姆

        [JsonProperty("刷新数量")]
        public int SpawnCount { get; set; } = 5;
    }
    
    [JsonProperty("竞技场-波数设置")]
    public List<ArenaWave> Waves { get; set; } = new()
    {
        // 在这里可以为服主预设几个默认的波数作为示例
        new ArenaWave
        {
            StartMessage = "第一波：热身运动！",
            Monsters = new List<ArenaMonster> { new() { NpcId = 1, SpawnCount = 10 } } // 10个史莱姆
        },
        new ArenaWave
        {
            StartMessage = "第二波：僵尸来袭！",
            Monsters = new List<ArenaMonster>
            {
                new() { NpcId = 3, SpawnCount = 15 }, // 15个僵尸
                new() { NpcId = 2, SpawnCount = 5 }  // 5个恶魔之眼
            }
        }
    };

    /// <summary>
    /// 定义了一个完整的波数，包含多种怪物和提示信息
    /// </summary>
    public class ArenaWave
    {
        [JsonProperty("本波怪物列表")]
        public List<ArenaMonster> Monsters { get; set; } = new();

        [JsonProperty("波数开始提示")]
        public string StartMessage { get; set; } = "战斗开始！";
    }
    
    

    public static Config Read(string path, out bool fileCreated)
    {
        fileCreated = false;

        if (!File.Exists(path))
        {
            fileCreated = true;
            var defaultConfig = new Config();
            defaultConfig.Waves = new()
            {
                new ArenaWave
                {
                    StartMessage = "第一波：热身运动！",
                    Monsters = new List<ArenaMonster> { new() { NpcId = 1, SpawnCount = 10 } }
                },
                new ArenaWave
                {
                    StartMessage = "第二波：僵尸来袭！",
                    Monsters = new List<ArenaMonster>
                    {
                        new() { NpcId = 3, SpawnCount = 15 },
                        new() { NpcId = 2, SpawnCount = 5 }
                    }
                }
            };
        
            File.WriteAllText(path, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            return defaultConfig;
        }

        var json = File.ReadAllText(path);
        var config = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
        return config;
    }
}