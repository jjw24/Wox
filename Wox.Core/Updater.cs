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

        private readonly string flowLauncherFilename = "Flow-Launcher-v1.6.0";

        public Updater(string gitHubRepository)
        {
            GitHubRepository = gitHubRepository;
        }

        public async Task UpdateApp(bool silentIfLatestVersion = true)
        {
            var upgradeMsg = "This update will upgrade Wox to Flow Launcher. " +
                Environment.NewLine + Environment.NewLine +
                "JJW24/Wox will no longer recieve updates or be maintained, " +
                "however if you still like to continue using it, " +
                "simply untick the auto update option to avoid this message from keep popping up. " +
                Environment.NewLine + Environment.NewLine +
                "Flow Launcher has a lot more improvements and is well maintained, visit flow-launcher.github.io for more details." +
                Environment.NewLine + Environment.NewLine +
                "The update will run in the background and take up to 5 minutes depending on your internet connection, you can still use the program in the meantime. " +
                "When it is done your settings for Wox will be transferred across to Flow Launcher. " +
                "Once you are familiar with Flow Launcher, simply uninstall Wox" +
                Environment.NewLine + Environment.NewLine +
                "Would you like to continue?";

            if (MessageBox.Show(upgradeMsg, "Upgrade to Flow Launcher", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            if (!FilesFolders.FileExits(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".exe")))
            {
                Http.Download("https://github.com/Flow-Launcher/Flow.Launcher/releases/download/v1.6.0/Flow-Launcher-v1.6.0.exe", Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".flow"));

                File.Move(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".flow"), Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".exe"));
            }

            var msg = "Successfully downloaded Flow Launcher v1.6.0." +
                        Environment.NewLine + Environment.NewLine +
                        "Would you like to restart Wox to finish the upgrade? (Upgrade will also complete on the next restart)";

            if (MessageBox.Show(msg, string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                PluginManager.API.RestarApp();
        }

        public void AfterUpdateRunFlowLauncher()
        {
            if (FilesFolders.FileExits(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".exe"))
                && !FilesFolders.FileExits(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".installed")))
            {
                AfterUpdateCopyWoxSettings();

                Process.Start(new ProcessStartInfo(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".exe")));

                using (StreamWriter sw = File.CreateText(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".installed"))) { }

                Environment.Exit(0);
            }

            if (FilesFolders.FileExits(Path.Combine(Constant.ApplicationDirectory, flowLauncherFilename + ".installed")))
            {
                MessageBox.Show("You have upgraded to Flow Launcher. Please run that instead.", "Upgraded to Flow Launcher");
                Environment.Exit(0);
            }
        }

        private void AfterUpdateCopyWoxSettings()
        {
            if (!FilesFolders.LocationExists(Path.Combine(Directory.GetParent(DataLocation.RoamingDataPath).FullName, "FlowLauncher")))
            {
                FilesFolders.Copy(DataLocation.DataDirectory(), Path.Combine(Directory.GetParent(DataLocation.RoamingDataPath).FullName, "FlowLauncher"));

                RenameWoxSettings();
            }
        }

        private void RenameWoxSettings()
        {
            var flowDataDirectory = Path.Combine(Directory.GetParent(DataLocation.RoamingDataPath).FullName, "FlowLauncher");
            var flowPluginSettingDirectory = Path.Combine(flowDataDirectory, "Settings","Plugins");

            Directory.Move(Path.Combine(flowPluginSettingDirectory, "Wox.Plugin.BrowserBookmark"), 
                Path.Combine(flowPluginSettingDirectory, "Flow.Launcher.Plugin.BrowserBookmark"));

            Directory.Move(Path.Combine(flowPluginSettingDirectory, "Wox.Plugin.Caculator"),
                Path.Combine(flowPluginSettingDirectory, "Flow.Launcher.Plugin.Caculator"));

            Directory.Move(Path.Combine(flowPluginSettingDirectory, "Wox.Plugin.Everything"),
                Path.Combine(flowPluginSettingDirectory, "Flow.Launcher.Plugin.Everything"));

            Directory.Move(Path.Combine(flowPluginSettingDirectory, "Wox.Plugin.Folder"),
                Path.Combine(flowPluginSettingDirectory, "Flow.Launcher.Plugin.Explorer"));

            Directory.Move(Path.Combine(flowPluginSettingDirectory, "Wox.Plugin.Program"),
                Path.Combine(flowPluginSettingDirectory, "Flow.Launcher.Plugin.Program"));

            Directory.Move(Path.Combine(flowPluginSettingDirectory, "Wox.Plugin.Shell"),
                Path.Combine(flowPluginSettingDirectory, "Flow.Launcher.Plugin.Shell"));

            Directory.Move(Path.Combine(flowPluginSettingDirectory, "Wox.Plugin.Url"),
                Path.Combine(flowPluginSettingDirectory, "Flow.Launcher.Plugin.Url"));

            Directory.Move(Path.Combine(flowPluginSettingDirectory, "Wox.Plugin.WebSearch"),
                Path.Combine(flowPluginSettingDirectory, "Flow.Launcher.Plugin.WebSearch"));
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