#nullable enable

using System.Composition;
using DevToys.Api.Tools;
using DevToys.Helpers;
using DevToys.Shared.Api.Core;

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
            return JsonTableHelper.IsValid(data);
        }

        public IToolViewModel CreateTool()
        {
            return _mefProvider.Import<JsonTableToolViewModel>();
        }
    }
}
