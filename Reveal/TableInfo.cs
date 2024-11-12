using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RevealSdk.Server.Reveal
{
    public class TableInfo
    {
        public string? TABLE_SCHEMA { get; set; }
        public string? TABLE_NAME { get; set; }
        public string? COLUMN_NAME { get; set; }

        public static List<TableInfo> GetAllowedTables()
        {
            var json = File.ReadAllText("allowedTables.json");
            return JsonSerializer.Deserialize<List<TableInfo>>(json);
        }
    }

}
