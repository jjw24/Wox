using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
using Wox.Infrastructure.Logger;
using System.IO;

namespace Wox.Core
{
    public class Updater
    {
        public string GitHubRepository { get; }
        private bool IsPortableMode => Directory.Exists(Constant.PortableDataPath);

        public Updater(string gitHubRepository)
        {
            GitHubRepository = gitHubRepository;
        }

        public async Task UpdateApp(bool silentIfLatestVersion = true)
        {
            UpdateManager m;
            UpdateInfo u;

            try
            {
                m = await GitHubUpdateManager(GitHubRepository);
            }
            catch (Exception e) when (e is HttpRequestException || e is WebException || e is SocketException)
            {
                Log.Exception($"|Updater.UpdateApp|Please check your connection and proxy settings to api.github.com.", e);
                return;
            }

            try
            {
                // UpdateApp CheckForUpdate will return value only if the app is squirrel installed
                u = await m.CheckForUpdate().NonNull();
            }
            catch (Exception e) when (e is HttpRequestException || e is WebException || e is SocketException)
            {
                Log.Exception($"|Updater.UpdateApp|Check your connection and proxy settings to api.github.com.", e);
                m.Dispose();
                return;
            }

            var newReleaseVersion = Version.Parse(u.FutureReleaseEntry.Version.ToString());
            var currentVersion = Version.Parse(Constant.Version);

            Log.Info($"|Updater.UpdateApp|Future Release <{u.FutureReleaseEntry.Formatted()}>");

            if (newReleaseVersion <= currentVersion)
            {
                if (!silentIfLatestVersion)
                    MessageBox.Show("You already have the latest Wox version");
                m.Dispose();
                return;
            }
            
            try
            {
                await m.DownloadReleases(u.ReleasesToApply);
            }
            catch (Exception e) when (e is HttpRequestException || e is WebException || e is SocketException)
            {
                Log.Exception($"|Updater.UpdateApp|Check your connection and proxy settings to github-cloud.s3.amazonaws.com.", e);
                m.Dispose();
                return;
            }
            
            await m.ApplyReleases(u);
            
            // Not needed when in portable mode
            if(!IsPortableMode)
                await m.CreateUninstallerRegistryEntry();
            
            if (IsPortableMode)
            {
                var targetDestination = m.RootAppDirectory + $"\\app-{newReleaseVersion.ToString()}\\{Constant.PortableFolderName}";
                FilesFolders.Copy(Constant.PortableDataPath, targetDestination);
            }
            
            var newVersionTips = NewVersinoTips(newReleaseVersion.ToString());
            
            MessageBox.Show(newVersionTips);
            Log.Info($"|Updater.UpdateApp|Update success:{newVersionTips}");

            // always dispose UpdateManager
            m.Dispose();
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