using System.Data;
using TShockAPI.DB;

namespace BetterTShock.Managers;

public class DbManager
{
    private readonly IDbConnection _db;
    private const string DbVersionKey = "Db_Version";
    
    public DbManager(IDbConnection db)
    {
        
        _db = db;
        _db.Query("CREATE TABLE IF NOT EXISTS MyPluginData (Key TEXT PRIMARY KEY, Value TEXT)");

        string? storedVersionStr = ReadData(DbVersionKey);
        int.TryParse(storedVersionStr, out int storedVersion);
        
        // 2. 比较版本号
        if (storedVersion != Plugin.CurrentDbVersion)
        {
            // 3. 版本不一致，删除旧的数据表
            TShock.Log.ConsoleInfo("插件数据版本不匹配，正在重建数据表...");
            _db.Query("DROP TABLE IF EXISTS MyPluginData");
            TShock.Log.ConsoleInfo("旧数据表已删除。");
        }
        
        
        // 4. (无论如何都)确保表结构存在
        var table = new SqlTable("MyPluginData",
            new SqlColumn("Key", MySql.Data.MySqlClient.MySqlDbType.Text) { Primary = true },
            new SqlColumn("Value", MySql.Data.MySqlClient.MySqlDbType.Text)
        );
        _db.Query("CREATE TABLE IF NOT EXISTS MyPluginData (Key TEXT PRIMARY KEY, Value TEXT)");

        // 5. 如果版本不一致，更新数据库中的版本号为当前版本
        if (storedVersion != Plugin.CurrentDbVersion)
        {
            SaveData(DbVersionKey, Plugin.CurrentDbVersion.ToString());
            TShock.Log.ConsoleInfo($"数据表已更新至版本: {Plugin.CurrentDbVersion}");
        }
    }
    
    /// <summary>
    /// 保存或更新一条数据
    /// </summary>
    public void SaveData(string key, string value)
    {
        // 使用 TShock 的 Query 方法，@0, @1 是安全的参数占位符
        _db.Query("INSERT OR REPLACE INTO MyPluginData (Key, Value) VALUES (@0, @1)", key, value);
    }

    /// <summary>
    /// 读取一条数据
    /// </summary>
    /// <returns>如果没找到，返回 null</returns>
    public string? ReadData(string key)
    {
        using (var reader = _db.QueryReader("SELECT Value FROM MyPluginData WHERE Key = @0", key))
        {
            if (reader.Read())
            {
                // .Get<string>("Value") 读取名为 "Value" 的列
                return reader.Get<string>("Value");
            }
        }
        return null; // 没找到
    }
    
}