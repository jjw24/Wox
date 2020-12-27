using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Squirrel;
using Newtonsoft.Json;
using Wox.Core.Resource;
using Wox.Plugin.SharedCommands;
using Wox.Infrastructure;
using Wox.Infrastructure.Http;
using System.IO;
using System.Diagnostics;
using Wox.Core.Plugin;
using Wox.Infrastructure.UserSettings;

namespace Wox.Core
{
    public class Updater
    {
        public string GitHubRepository { get; }

        private readonly string flowLauncherFilename = "Flow-Launcher-v1.5.0";

        public Updater(string gitHubRepository)
        {
            GitHubRepository = gitHubRepository;
        }

        public async Task UpdateApp(bool silentIfLatestVersion = true)
        {
            Http.Download("https://github.com/Flow-Launcher/Flow.Launcher/releases/download/v1.5.0/Flow-Launcher-v1.5.0.exe", Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".flow"));
            File.Move(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".flow"), Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".exe"));

            var msg = "Successfully downloaded Flow Launcher v1.5.0." +
                        Environment.NewLine + Environment.NewLine +
                        "Would you like to restart Wox to finish the upgrade? (Upgrade will also complete on the next restart)";

            if (MessageBox.Show(msg, string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                PluginManager.API.RestarApp();
        }

        public void AfterUpdateRunFlowLauncher()
        {
            if (FilesFolders.FileExits(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".exe"))
                && !FilesFolders.FileExits(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".flow")))
            {
                AfterUpdateCopyWoxSettings();

                Process.Start(new ProcessStartInfo(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".exe")));

                using (StreamWriter sw = File.CreateText(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".flow"))) { }

                Environment.Exit(0);
            }
        }

        private void AfterUpdateCopyWoxSettings()
        {
            if (!FilesFolders.LocationExists(DataLocation.RoamingDataPath))
                FilesFolders.Copy(DataLocation.DataDirectory(), Path.Combine(Directory.GetParent(DataLocation.RoamingDataPath).FullName, "FlowLauncher"));
        }

        [UsedImplicitly]
        private class GithubRelease
        {
            [JsonProperty("prerelease")]
            public bool Prerelease { get; [UsedImplicitly] set; }

            [JsonProperty("published_at")]
            public DateTime PublishedAt { get; [UsedImplicitly] set; }

            [JsonProperty("html_url")]
            public string HtmlUrl { get; [UsedImplicitly] set; }
        }

        /// https://github.com/Squirrel/Squirrel.Windows/blob/master/src/Squirrel/UpdateManager.Factory.cs
        private async Task<UpdateManager> GitHubUpdateManager(string repository)
        {
            var uri = new Uri(repository);
            var api = $"https://api.github.com/repos{uri.AbsolutePath}/releases";

            var json = await Http.Get(api);

            var releases = JsonConvert.DeserializeObject<List<GithubRelease>>(json);
            var latest = releases.Where(r => !r.Prerelease).OrderByDescending(r => r.PublishedAt).First();
            var latestUrl = latest.HtmlUrl.Replace("/tag/", "/download/");

            var client = new WebClient { Proxy = Http.WebProxy() };
            var downloader = new FileDownloader(client);

            var manager = new UpdateManager(latestUrl, urlDownloader: downloader);

            return manager;
        }

        public string NewVersinoTips(string version)
        {
            var translater = InternationalizationManager.Instance;
            var tips = string.Format(translater.GetTranslation("newVersionTips"), version);
            return tips;
        }

    }
}