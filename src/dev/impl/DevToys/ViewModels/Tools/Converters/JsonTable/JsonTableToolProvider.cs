#nullable enable

using System;
using System.Composition;
using DevToys.Api.Tools;
using DevToys.Shared.Api.Core;
using Newtonsoft.Json.Linq;

namespace DevToys.ViewModels.Tools.JsonTable
{
    [Export(typeof(IToolProvider))]
    [Name("Json > Table")]
    [Parent(ConvertersGroupToolProvider.InternalName)]
    [ProtocolName("jsontable")]
    [Order(1)]
    [NotScrollable]
    internal sealed class JsonTableToolProvider : IToolProvider
    {
        private readonly IMefProvider _mefProvider;

        public string MenuDisplayName => LanguageManager.Instance.JsonTable.MenuDisplayName;

        public string? SearchDisplayName => LanguageManager.Instance.JsonTable.SearchDisplayName;

        public string? Description => LanguageManager.Instance.JsonTable.Description;

        public string AccessibleName => LanguageManager.Instance.JsonTable.AccessibleName;

        public string? SearchKeywords => LanguageManager.Instance.JsonTable.SearchKeywords;

        public string IconGlyph => "\u0109"; // same as JSON<>YAML

        [ImportingConstructor]
        public JsonTableToolProvider(IMefProvider mefProvider)
        {
            _mefProvider = mefProvider;
        }

        public bool CanBeTreatedByTool(string data)
        {
            try
            {
                var jtoken = JToken.Parse(data ?? "");
                return jtoken is JArray;
            }
            catch (Exception)
            {
                // Exception in parsing json. It likely mean the text isn't a JSON.
                return false;
            }
        }

        public IToolViewModel CreateTool()
        {
            return _mefProvider.Import<JsonTableToolViewModel>();
        }
    }
}
