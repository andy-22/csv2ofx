using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Csv2Ofx.Gui.Conversion;

namespace Csv2Ofx.Gui;

public partial class MainWindow : Window
{
    private readonly InvestmentConversionService _conversionService = new();
    private bool _outputFolderWasAutoSelected = true;

    public MainWindow()
    {
        InitializeComponent();
        KindComboBox.ItemsSource = ConversionProfileCatalog.All;
        KindComboBox.SelectedIndex = 0;
        UpdateFormState();
    }

    private async void BrowseCsvButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider is null)
        {
            SetStatus("File picker is not available on this platform.", isError: true);
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose investment CSV",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV files")
                {
                    Patterns = ["*.csv"],
                    MimeTypes = ["text/csv", "application/csv", "application/vnd.ms-excel"]
                },
                FilePickerFileTypes.All
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var path = file.Path.LocalPath;
        CsvPathTextBox.Text = path;

        var csvFolder = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(csvFolder) && ShouldApplyCsvFolderDefault())
        {
            OutputFolderTextBox.Text = csvFolder;
            _outputFolderWasAutoSelected = true;
        }

        if (string.IsNullOrWhiteSpace(AccountNameTextBox.Text))
        {
            AccountNameTextBox.Text = Path.GetFileNameWithoutExtension(path);
        }

        SetStatus("CSV selected. Review the account name and output folder.");
        UpdateFormState();
    }

    private async void BrowseOutputButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider is null)
        {
            SetStatus("Folder picker is not available on this platform.", isError: true);
            return;
        }

        var options = new FolderPickerOpenOptions
        {
            Title = "Choose OFX output folder",
            AllowMultiple = false
        };

        var folder = await TryGetFolderAsync(storageProvider, OutputFolderTextBox.Text);
        if (folder is not null)
        {
            options.SuggestedStartLocation = folder;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(options);
        var selected = folders.FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        OutputFolderTextBox.Text = selected.Path.LocalPath;
        _outputFolderWasAutoSelected = false;
        SetStatus("Output folder selected.");
        UpdateFormState();
    }

    private async void ConvertButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var request = TryBuildRequest();
        if (request is null)
        {
            UpdateFormState();
            return;
        }

        SetBusy(true);
        SetStatus("Converting CSV to OFX...");

        try
        {
            var result = await _conversionService.ConvertAsync(request);
            SetStatus($"Wrote {result.TransactionCount} transactions and {result.SecurityCount} securities to {result.OutputPath}");
            UpdateOutputPreview();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
        finally
        {
            SetBusy(false);
            UpdateFormState();
        }
    }

    private void Input_OnChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateFormState();
    }

    private void TextInput_OnChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender == OutputFolderTextBox)
        {
            _outputFolderWasAutoSelected = false;
        }

        UpdateFormState();
    }

    private ConversionRequest? TryBuildRequest()
    {
        if (KindComboBox.SelectedItem is not ConversionProfile profile)
        {
            SetStatus("Choose a statement kind.", isError: true);
            return null;
        }

        var csvPath = CsvPathTextBox.Text?.Trim() ?? string.Empty;
        var outputFolder = OutputFolderTextBox.Text?.Trim() ?? string.Empty;
        var accountName = AccountNameTextBox.Text?.Trim() ?? string.Empty;

        return new ConversionRequest(csvPath, outputFolder, accountName, profile);
    }

    private void UpdateFormState()
    {
        var request = TryBuildRequestWithoutStatus();
        ConvertButton.IsEnabled =
            request is not null
            && File.Exists(request.CsvPath)
            && Directory.Exists(request.OutputFolder)
            && !string.IsNullOrWhiteSpace(request.AccountName)
            && !ConversionProgressBar.IsVisible;

        UpdateOutputPreview();
    }

    private ConversionRequest? TryBuildRequestWithoutStatus()
    {
        if (KindComboBox?.SelectedItem is not ConversionProfile profile)
        {
            return null;
        }

        var csvPath = CsvPathTextBox?.Text?.Trim() ?? string.Empty;
        var outputFolder = OutputFolderTextBox?.Text?.Trim() ?? string.Empty;
        var accountName = AccountNameTextBox?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(csvPath) || string.IsNullOrWhiteSpace(outputFolder))
        {
            return null;
        }

        return new ConversionRequest(csvPath, outputFolder, accountName, profile);
    }

    private void UpdateOutputPreview()
    {
        var request = TryBuildRequestWithoutStatus();
        if (request is null || string.IsNullOrWhiteSpace(request.AccountName))
        {
            OutputPreviewTextBlock.Text = "The OFX path will appear here.";
            return;
        }

        OutputPreviewTextBlock.Text = _conversionService.ResolveOutputPath(request);
    }

    private bool ShouldApplyCsvFolderDefault()
    {
        return _outputFolderWasAutoSelected || string.IsNullOrWhiteSpace(OutputFolderTextBox.Text);
    }

    private void SetBusy(bool isBusy)
    {
        ConversionProgressBar.IsVisible = isBusy;
        BrowseCsvButton.IsEnabled = !isBusy;
        BrowseOutputButton.IsEnabled = !isBusy;
        KindComboBox.IsEnabled = !isBusy;
        AccountNameTextBox.IsEnabled = !isBusy;
        OutputFolderTextBox.IsEnabled = !isBusy;
        ConvertButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Classes.Set("error", isError);
    }

    private IStorageProvider? GetStorageProvider()
    {
        return TopLevel.GetTopLevel(this)?.StorageProvider;
    }

    private static async Task<IStorageFolder?> TryGetFolderAsync(IStorageProvider storageProvider, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return null;
        }

        return await storageProvider.TryGetFolderFromPathAsync(new Uri(path));
    }
}
