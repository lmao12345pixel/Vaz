using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vaz.WinUI.ViewModels;
using Vazer.Core.Versions;

namespace Vaz.WinUI.Views;

/// <summary>
/// Dialog tạo instance mới: Name + Loader + Minecraft version (+ loader version).
/// Validate qua CreateInstanceViewModel, tạo qua IInstanceManager.
/// </summary>
public sealed partial class CreateInstanceDialog : ContentDialog
{
    private readonly CreateInstanceViewModel _vm;

    /// <summary>Id instance vừa tạo (null nếu user hủy hoặc tạo thất bại).</summary>
    public string? CreatedInstanceId { get; private set; }

    public CreateInstanceDialog(CreateInstanceViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();

        // Nạp danh sách + giá trị mặc định từ VM.
        LoaderCombo.ItemsSource = CreateInstanceViewModel.Loaders;
        LoaderCombo.SelectedItem = _vm.SelectedLoader;
        VersionCombo.ItemsSource = _vm.VanillaVersions;
        VersionCombo.SelectedItem = _vm.VanillaVersions.FirstOrDefault(v => v.Id == _vm.SelectedMinecraftVersion);
        NameBox.Text = _vm.Name;

        SyncLoaderVersions();

        // Đồng bộ input control → VM (trigger load loader versions + validation).
        VersionCombo.SelectionChanged += (_, _) =>
        {
            if (VersionCombo.SelectedItem is MinecraftVersionInfo info)
            {
                _vm.SelectedMinecraftVersion = info.Id;
            }
        };

        LoaderCombo.SelectionChanged += (_, _) =>
        {
            if (LoaderCombo.SelectedItem is string loader)
            {
                _vm.SelectedLoader = loader;
            }
        };

        LoaderVersionBox.SelectionChanged += (_, _) =>
        {
            _vm.SelectedLoaderVersion = LoaderVersionBox.SelectedItem as string;
        };

        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CreateInstanceViewModel.VanillaVersions):
                VersionCombo.ItemsSource = _vm.VanillaVersions;
                VersionCombo.SelectedItem = _vm.VanillaVersions.FirstOrDefault(v => v.Id == _vm.SelectedMinecraftVersion);
                break;

            case nameof(CreateInstanceViewModel.SelectedMinecraftVersion):
                // LoadVersionsAsync chọn default sau khi nạp xong; sync selection lên control.
                VersionCombo.SelectedItem = _vm.VanillaVersions.FirstOrDefault(v => v.Id == _vm.SelectedMinecraftVersion);
                break;

            case nameof(CreateInstanceViewModel.LoaderVersions):
            case nameof(CreateInstanceViewModel.IsLoaderVersionsLoading):
                SyncLoaderVersions();
                break;

            case nameof(CreateInstanceViewModel.SelectedLoaderVersion):
                LoaderVersionBox.SelectedItem = _vm.SelectedLoaderVersion;
                break;
        }
    }

    /// <summary>
    /// Đồng bộ LoaderVersionBox với trạng thái VM (items, selection, empty state).
    /// </summary>
    private void SyncLoaderVersions()
    {
        LoaderVersionBox.ItemsSource = _vm.LoaderVersions;

        var hasItems = _vm.LoaderVersions.Count > 0;
        LoaderVersionBox.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
        LoaderVersionBox.SelectedItem = _vm.SelectedLoaderVersion;

        var showEmptyText = !hasItems && !_vm.IsLoaderVersionsLoading;
        NoLoaderVersionsText.Visibility = showEmptyText ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Mở log NeoForge installer mới nhất (hoặc thư mục downloads nếu chưa có log).</summary>
    private void ViewInstallLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Vazer", "Minecraft", "java", "downloads");

            string? log = null;
            if (Directory.Exists(dir))
            {
                log = Directory.GetFiles(dir, "neoforge-*-installer.jar.log")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            var target = log ?? (Directory.Exists(dir)
                ? dir
                : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
            // Không chặn flow khi không mở được log — thông điệp lỗi đã hiển thị trên InfoBar.
        }
    }

    /// <summary>
    /// WinUI 3 ContentDialog không có IsCloseButtonEnabled — chặn nút Hủy khi đang cài
    /// bằng cách cancel CloseButtonClick (dialog giữ nguyên, cài đặt tiếp tục).
    /// </summary>
    private void Dialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_vm.IsInstalling)
        {
            args.Cancel = true;
        }
    }

    private async void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Giữ dialog mở trong lúc await; nếu fail thì Cancel để user sửa input.
        var deferral = args.GetDeferral();
        try
        {
            CreateErrorBar.IsOpen = false;

            // Đồng bộ input từ controls vào VM (code-behind style như OptifineInstallDialog).
            _vm.Name = NameBox.Text;
            _vm.SelectedLoader = LoaderCombo.SelectedItem as string ?? string.Empty;
            _vm.SelectedMinecraftVersion = (VersionCombo.SelectedItem as MinecraftVersionInfo)?.Id
                ?? string.Empty;
            _vm.SelectedLoaderVersion = LoaderVersionBox.SelectedItem as string;

            // VM set IsInstalling=true bên trong CreateAsync — ProgressBar/IsEnabled bind tự cập nhật.
            var instance = await _vm.CreateAsync();

            if (instance == null)
            {
                CreateErrorBar.Message = _vm.ErrorMessage ?? "Không tạo được instance.";
                CreateErrorBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            CreatedInstanceId = instance.Id;
        }
        finally
        {
            deferral.Complete();
        }
    }
}
