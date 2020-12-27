using System.Collections.Generic;
using Newtonsoft.Json;
using Wox.Infrastructure.Storage;

namespace Wox.Plugin.Folder
{
    public class Settings
    {
        [JsonProperty]
        public List<FolderLink> FolderLinks { get; set; } = new List<FolderLink>();

        [JsonProperty]
        public int MaxResult { get; set; } = 100;

        [JsonProperty]
        public List<FolderLink> QuickFolderAccessLinks { get; set; } = new List<FolderLink>();

        [JsonProperty]
        public bool UseWindowsIndexForDirectorySearch { get; set; } = true;

        [JsonProperty]
        public List<FolderLink> IndexSearchExcludedSubdirectoryPaths { get; set; } = new List<FolderLink>();

        [JsonProperty]
        public string SearchActionKeyword { get; set; } = Query.GlobalPluginWildcardSign;

        [JsonProperty]
        public string FileContentSearchActionKeyword { get; set; } = "doc:";
    }
}
