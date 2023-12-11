#nullable enable

using System.Collections.Generic;
using System.Data;
using DevToys.Api.Core.Navigation;
using DevToys.Shared.Core;
using DevToys.ViewModels.Tools.JsonTable;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DevToys.Views.Tools.JsonTable
{
    public sealed partial class JsonTableToolPage : Page
    {
        public static readonly DependencyProperty ViewModelProperty
            = DependencyProperty.Register(
                nameof(ViewModel),
                typeof(JsonTableToolViewModel),
                typeof(JsonTableToolPage),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        public JsonTableToolViewModel ViewModel
        {
            get => (JsonTableToolViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public JsonTableToolPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var parameters = (NavigationParameter)e.Parameter;

            if (ViewModel is null)
            {
                // Set the view model once per app run.
                Assumes.NotNull(parameters.ViewModel, nameof(parameters.ViewModel));
                ViewModel = (JsonTableToolViewModel)parameters.ViewModel!;
                DataContext = ViewModel;
                ViewModel.OnOutputDataUpdated += ViewModel_OnOutputDataUpdated;
            }

            if (!string.IsNullOrWhiteSpace(parameters.ClipBoardContent))
            {
                ViewModel.InputText = parameters.ClipBoardContent ?? string.Empty;
            }

            base.OnNavigatedTo(e);
        }

        private void ViewModel_OnOutputDataUpdated(object sender, DataTable table)
        {
            // The UWP DataGrid isn't as easy to bind with as other DataGrid implementations (eg WPF);
            // Dynamic columns must be generated explicitly.
            // https://xamlbrewer.wordpress.com/2018/05/29/displaying-dynamic-sql-results-in-a-uwp-datagrid/;
            OutputDataGrid.Columns.Clear();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                OutputDataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = table.Columns[i].ColumnName,
                    Binding = new() { Path = new PropertyPath($"[{i}]") }
                });
            }

            var collection = new List<object>();
            foreach (DataRow row in table.Rows)
            {
                collection.Add(row.ItemArray);
            }
            OutputDataGrid.ItemsSource = collection;
        }

        private void ExpandButton_Click(object _, RoutedEventArgs e)
        {
            if (ViewModel.IsOutputExpanded)
            {
                ExpandedGrid.Children.Remove(OutputSection);
                InputOutputGrid.Children.Add(OutputSection);
                MainGrid.Visibility = Visibility.Visible;
                ViewModel.IsOutputExpanded = false;
            }
            else
            {
                InputOutputGrid.Children.Remove(OutputSection);
                MainGrid.Visibility = Visibility.Collapsed;
                ExpandedGrid.Children.Add(OutputSection);
                ViewModel.IsOutputExpanded = true;
            }
        }

        private void CopyButton_Click(object _, RoutedEventArgs e) => ViewModel.CopyToClipboard();
    }
}
