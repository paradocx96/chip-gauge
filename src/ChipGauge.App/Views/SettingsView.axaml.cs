using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ChipGauge.App.ViewModels;

namespace ChipGauge.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private async void OnExportSnapshotClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        IStorageProvider? storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null)
        {
            return;
        }

        IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = SettingsViewModel.SuggestedSnapshotFileName,
            DefaultExtension = "txt",
            FileTypeChoices = [new FilePickerFileType("Text file") { Patterns = ["*.txt"] }],
        });

        if (file is null)
        {
            return;
        }

        string report = viewModel.BuildSnapshotReport();
        await using (Stream stream = await file.OpenWriteAsync())
        await using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(report);
        }

        viewModel.LastExportPath = file.TryGetLocalPath() ?? file.Name;
    }
}
