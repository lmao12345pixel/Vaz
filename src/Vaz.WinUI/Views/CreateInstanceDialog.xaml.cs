using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Dialog tạo instance mới: Name + Loader + Minecraft version (+ loader version optional).
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
        InitializeComponent();

        // Nạp danh sách + giá trị mặc định từ VM.
        LoaderCombo.ItemsSource = CreateInstanceViewModel.Loaders;
        VersionCombo.ItemsSource = CreateInstanceViewModel.MinecraftVersions;
        NameBox.Text = _vm.Name;
        LoaderCombo.SelectedItem = _vm.SelectedLoader;
        VersionCombo.SelectedItem = _vm.SelectedMinecraftVersion;
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
            _vm.SelectedMinecraftVersion = VersionCombo.SelectedItem as string ?? string.Empty;
            _vm.LoaderVersion = LoaderVersionBox.Text;

            SetBusy(true, "Đang tạo instance...");
            var instance = await _vm.CreateAsync();
            SetBusy(false, string.Empty);

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

    private void SetBusy(bool busy, string message)
    {
        BusyRing.IsActive = busy;
        BusyText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        BusyText.Text = message;
        IsPrimaryButtonEnabled = !busy;
    }
}
