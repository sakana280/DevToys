#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevToys.Api.Core;
using DevToys.Api.Core.Settings;
using DevToys.Api.Tools;
using DevToys.Core;
using DevToys.Core.Threading;
using DevToys.Shared.Core.Threading;
using DevToys.Views.Tools.JsonTable;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel.DataTransfer;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace DevToys.ViewModels.Tools.JsonTable
{
    [Export(typeof(JsonTableToolViewModel))]
    public sealed class JsonTableToolViewModel : ObservableRecipient, IToolViewModel
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IMarketingService _marketingService;
        private readonly Queue<string?> _conversionQueue = new();

        private readonly JsonSerializerSettings _defaultJsonSerializerSettings = new()
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };

        private bool _toolSuccessfullyWorked;
        private bool _conversionInProgress;
        private string _inputText = "";
        private string? _outputText = null;
        private string? _errorMessage = LanguageManager.Instance.JsonTable.JsonError;
        private bool _isOutputExpanded = false;

        [ImportingConstructor]
        public JsonTableToolViewModel(ISettingsProvider settingsProvider, IMarketingService marketingService)
        {
            _settingsProvider = settingsProvider;
            _marketingService = marketingService;
        }

        public Type View { get; } = typeof(JsonTableToolPage);

        internal JsonTableStrings Strings => LanguageManager.Instance.JsonTable;

        /// <summary>
        /// The format to serialize when copying to clipboard.
        /// </summary>
        private static readonly SettingDefinition<CopyFormatItem> CopyFormat
            = new(
                name: $"{nameof(JsonTableToolViewModel)}.{nameof(CopyFormat)}",
                isRoaming: true,
                defaultValue: CopyFormatItem.TSV);

        /// <summary>
        /// Gets or sets the desired copy format.
        /// Bind to a string version of the enum, since UWP ComboBox doesn't bind easily to enums.
        /// </summary>
        internal string CopyFormatMode
        {
            get => _settingsProvider.GetSetting(CopyFormat).ToString();
            set
            {
                if (CopyFormatMode != value && Enum.TryParse(value, out CopyFormatItem e))
                {
                    _settingsProvider.SetSetting(CopyFormat, e);
                    OnPropertyChanged();
                    QueueConversion();
                }
            }
        }

        /// <summary>
        /// Get a list of supported Indentation
        /// </summary>
        internal IReadOnlyList<CopyFormatDisplayPair> CopyFormatItems = new CopyFormatDisplayPair[] {
            new(CopyFormatItem.TSV.ToString(), LanguageManager.Instance.JsonTable.CopyFormatTSV),
            new(CopyFormatItem.CSV.ToString(), LanguageManager.Instance.JsonTable.CopyFormatCSV),
        };

        internal class CopyFormatDisplayPair
        {
            public CopyFormatDisplayPair(string tag, string displayName)
            {
                Tag = tag;
                DisplayName = displayName;
            }

            public string Tag { get; }
            public string DisplayName { get; }
        }

        internal enum CopyFormatItem
        {
            /// <summary>
            /// Tab separated values
            /// </summary>
            TSV,

            /// <summary>
            /// Comma separated values
            /// </summary>
            CSV,
        }

        /// <summary>
        /// Gets or sets the input text.
        /// </summary>
        internal string InputText
        {
            get => _inputText;
            set
            {
                SetProperty(ref _inputText, value);
                QueueConversion();
            }
        }

        /// <summary>
        /// Fires when the output data in table content changes.
        /// Using this janky mechanism since UWP doesn't bind easily to dynamic (variable column) data sources.
        /// </summary>
        public event EventHandler<DataTable>? OnOutputDataUpdated;

        /// <summary>
        /// Gets or sets the output text to copy to clipboard.
        /// </summary>
        internal string? OutputText
        {
            get => _outputText;
            set => SetProperty(ref _outputText, value);
        }

        /// <summary>
        /// Gets or sets the output code editor's language.
        /// </summary>
        internal string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        internal bool IsOutputExpanded
        {
            get => _isOutputExpanded;
            set => SetProperty(ref _isOutputExpanded, value);
        }

        internal void CopyToClipboard()
        {
            try
            {
                var data = new DataPackage
                {
                    RequestedOperation = DataPackageOperation.Copy
                };
                data.SetText(OutputText);

                Clipboard.SetContentWithOptions(data, new ClipboardContentOptions() { IsAllowedInHistory = true, IsRoamable = true });
                Clipboard.Flush();
            }
            catch (Exception ex)
            {
                Logger.LogFault("Failed to copy from table", ex);
            }
        }

        private void QueueConversion()
        {
            _conversionQueue.Enqueue(InputText);
            TreatQueueAsync().Forget();
        }

        private async Task TreatQueueAsync()
        {
            if (_conversionInProgress)
            {
                return;
            }

            _conversionInProgress = true;

            try
            {
                await TaskScheduler.Default;

                while (_conversionQueue.TryDequeue(out string? text))
                {
                    ConvertResult result = ConvertFromJson(text);

                    ThreadHelper.RunOnUIThreadAsync(ThreadPriority.Low, () =>
                    {
                        OutputText = result.Text;
                        ErrorMessage = result.Error;
                        OnOutputDataUpdated?.Invoke(this, result.Data);

                        if (result.Error != null && !_toolSuccessfullyWorked)
                        {
                            _toolSuccessfullyWorked = true;
                            _marketingService.NotifyToolSuccessfullyWorked();
                        }
                    }).ForgetSafely();
                }
            }
            finally
            {
                _conversionInProgress = false;
            }
        }

        private ConvertResult ConvertFromJson(string? text)
        {
            JsonArrayAsDicts? jsonArray = ParseJsonArray(text);
            if (jsonArray == null)
            {
                return new(new(), "", LanguageManager.Instance.JsonTable.JsonError);
            }

            //todo flatten nested objects (_ delimited), remove nested lists

            var properties = jsonArray.SelectMany(o => o.Keys).Distinct().ToList();

            char separator = CopyFormatMode == CopyFormatItem.TSV.ToString() ? '\t' : ',';

            var table = new DataTable();
            table.Columns.AddRange(properties.Select(p => new DataColumn(p)).ToArray());

            var clipboard = new StringBuilder();
            clipboard.AppendLine(string.Join(separator, properties));

            foreach (JsonAsDict jsonObject in jsonArray)
            {
                string?[] values = properties
                    .Select(p => jsonObject.FirstOrDefault(kv => kv.Key == p).Value?.ToString())
                    .ToArray();

                table.Rows.Add(values);
                clipboard.AppendLine(string.Join(separator, values));
            }

            return new(table, clipboard.ToString(), null);
        }

        private class ConvertResult
        {
            public ConvertResult(DataTable data, string text, string? error)
            {
                Data = data;
                Text = text;
                Error = error;
            }

            public DataTable Data { get; }
            public string Text { get; }
            public string? Error { get; }
        }

        private class JsonAsDict : Dictionary<string, object> { }
        private class JsonArrayAsDicts : List<JsonAsDict> { }

        private static JsonArrayAsDicts? ParseJsonArray(string? text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text) || JToken.Parse(text!) is not JArray)
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<JsonArrayAsDicts>(text!)!;
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }
    }
}
