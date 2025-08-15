using System.ComponentModel;
using Newtonsoft.Json;
namespace BetterTShock;

public class Config
{
    [JsonProperty("BondDamageSharingRatio", Order = 1)]
    [DefaultValue(0.2)]
    public float BondDamageSharingRatio { get; set; }
    
    [JsonProperty("TimeZoneString", Order = 2)]
    [DefaultValue("China Standard Time")]
    public string TimeZoneString { get; set; } = "China Standard Time";

    public static Config GetDefault()
    {
        return new Config
        {
            BondDamageSharingRatio = 0.2f,
            TimeZoneString = "China Standard Time",
        };
    }
    

    public static Config Read(string path, out bool fileCreated)
    {
        fileCreated = false; // 默认为非新创建

        // 1. 检查文件是否存在
        if (!File.Exists(path))
        {
            fileCreated = true;
            // 2. 如果不存在，创建一个新的、包含默认值的Config对象
            var defaultConfig = new Config();
            // 3. 将这个默认对象写入到文件中
            File.WriteAllText(path, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            // 4. 返回这个新创建的默认配置
            return defaultConfig;
        }

        // 5. 如果文件已存在，读取文件中的所有文本
        var json = File.ReadAllText(path);
        // 6. 将文本内容解析（反序列化）成一个Config对象
        var config = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
        // 7. 返回从文件中加载的配置
        return config;
    }
}