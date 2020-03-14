﻿using System;
using System.Globalization;
using System.Windows.Data;
using Wox.Infrastructure.Logger;
using Wox.ViewModel;

namespace Wox.Converters
{
    public class QuerySuggestionBoxConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
            {
                return string.Empty;
            }

            // first prop is the current query string
            var queryText = (string)values[0];

            if (string.IsNullOrEmpty(queryText))
                return "Type here to search";

            // second prop is the current selected item result
            var val = values[1];
            if (val == null)
            {
                return string.Empty;
            }
            if (!(val is ResultViewModel))
            {
                return System.Windows.Data.Binding.DoNothing;
            }

            try
            {
                var selectedItem = (ResultViewModel)val;

                var selectedResult = selectedItem.Result;
                var selectedResultActionKeyword = string.IsNullOrEmpty(selectedResult.ActionKeywordAssigned) ? "" : selectedResult.ActionKeywordAssigned + " ";
                var selectedResultPossibleSuggestion = selectedResultActionKeyword + selectedResult.Title;

                if (!selectedResultPossibleSuggestion.StartsWith(queryText, StringComparison.CurrentCultureIgnoreCase))
                    return string.Empty;

                // When user typed lower case and result title is uppercase, we still want to display suggestion
                return queryText + selectedResultPossibleSuggestion.Substring(queryText.Length);
            }
            catch (Exception e)
            {
                Log.Exception(nameof(QuerySuggestionBoxConverter), "fail to convert text for suggestion box", e);
                return string.Empty;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}