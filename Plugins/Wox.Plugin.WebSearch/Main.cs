using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wox.Infrastructure;
using Wox.Infrastructure.Storage;
using Wox.Plugin.SharedCommands;

namespace Wox.Plugin.WebSearch
{
    public class Main : IPlugin, ISettingProvider, IPluginI18n, ISavable, IResultUpdated
    {
        private PluginInitContext _context;

        private readonly Settings _settings;
        private readonly SettingsViewModel _viewModel;
        private CancellationTokenSource _updateSource;
        private CancellationToken _updateToken;

        public const string Images = "Images";
        public static string ImagesDirectory;

        private readonly string SearchSourceGlobalPluginWildCardSign = "*";

        public void Save()
        {
            _viewModel.Save();
        }

        public List<Result> Query(Query query)
        {
            var searchSourceList = new List<SearchSource>();
            var results = new List<Result>();

            _updateSource?.Cancel();
            _updateSource = new CancellationTokenSource();
            _updateToken = _updateSource.Token;
            
            _settings.SearchSources.Where(o => (o.ActionKeyword == query.ActionKeyword || o.ActionKeyword == SearchSourceGlobalPluginWildCardSign) 
                                               && o.Enabled)
                                    .ToList()
                                    .ForEach(x => searchSourceList.Add(x));

            if (searchSourceList.Any())
            {
                foreach (SearchSource searchSource in searchSourceList)
                {
                    string keyword = string.Empty;
                    keyword = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign ? query.ToString() : query.Search;
                    var title = keyword;
                    string subtitle = _context.API.GetTranslation("wox_plugin_websearch_search") + " " + searchSource.Title;

                    if (string.IsNullOrEmpty(keyword))
                    {
                        var result = new Result
                        {
                            Title = subtitle,
                            SubTitle = string.Empty,
                            IcoPath = searchSource.IconPath
                        };
                        results.Add(result);
                    }
                    else
                    {
                        var result = new Result
                        {
                            Title = title,
                            SubTitle = subtitle,
                            Score = 6,
                            IcoPath = searchSource.IconPath,
                            ActionKeywordAssigned = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign ? string.Empty : searchSource.ActionKeyword,
                            Action = c =>
                            {
                                if (_settings.OpenInNewBrowser)
                                {
                                    searchSource.Url.Replace("{q}", Uri.EscapeDataString(keyword)).NewBrowserWindow(_settings.BrowserPath);
                                }
                                else
                                {
                                    searchSource.Url.Replace("{q}", Uri.EscapeDataString(keyword)).NewTabInBrowser(_settings.BrowserPath);
                                }

                                return true;
                            }
                        };

                        results.Add(result);
                        ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
                        {
                            Results = results,
                            Query = query
                        });

                        UpdateResultsFromSuggestion(results, keyword, subtitle, searchSource, query);                        
                    }
                }
            }

            return results;
        }

        private void UpdateResultsFromSuggestion(List<Result> results, string keyword, string subtitle,
            SearchSource searchSource, Query query)
        {
            if (_settings.EnableSuggestion)
            {
                const int waittime = 300;
                var task = Task.Run(async () =>
                {
                    var suggestions = await Suggestions(keyword, subtitle, searchSource);
                    results.AddRange(suggestions);
                }, _updateToken);

                if (!task.Wait(waittime))
                {
                    task.ContinueWith(_ => ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
                    {
                        Results = results,
                        Query = query
                    }), _updateToken);
                }
            }
        }

        private async Task<IEnumerable<Result>> Suggestions(string keyword, string subtitle, SearchSource searchSource)
        {
            var source = _settings.SelectedSuggestion;
            if (source != null)
            {
                var suggestions = await source.Suggestions(keyword);
                var resultsFromSuggestion = suggestions.Select(o => new Result
                {
                    Title = o,
                    SubTitle = subtitle,
                    Score = 5,
                    IcoPath = searchSource.IconPath,
                    ActionKeywordAssigned = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign ? string.Empty : searchSource.ActionKeyword,
                    Action = c =>
                    {
                        if (_settings.OpenInNewBrowser)
                        {
                            searchSource.Url.Replace("{q}", Uri.EscapeDataString(o)).NewBrowserWindow(_settings.BrowserPath);
                        }
                        else
                        {
                            searchSource.Url.Replace("{q}", Uri.EscapeDataString(o)).NewTabInBrowser(_settings.BrowserPath);
                        }

                        return true;
                    }
                });
                return resultsFromSuggestion;
            }
            return new List<Result>();
        }

        public Main()
        {
            _viewModel = new SettingsViewModel();
            _settings = _viewModel.Settings;
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
            var pluginDirectory = _context.CurrentPluginMetadata.PluginDirectory;
            var bundledImagesDirectory = Path.Combine(pluginDirectory, Images);
            ImagesDirectory = Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, Images);
            Helper.ValidateDataDirectory(bundledImagesDirectory, ImagesDirectory);
        }

        #region ISettingProvider Members

        public Control CreateSettingPanel()
        {
            return new SettingsControl(_context, _viewModel);
        }

        #endregion

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_websearch_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_websearch_plugin_description");
        }

        public event ResultUpdatedEventHandler ResultsUpdated;
    }
}
