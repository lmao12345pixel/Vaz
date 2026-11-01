using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Instance Detail: header + Launch, tab Mods (list/update/mrpack/OptiFine/folder),
/// tab Settings (RAM/JVM disabled, path readonly, delete).
/// </summary>
public sealed partial class InstanceDetailPage : Page
{
    public InstanceDetailViewModel ViewModel { get; }
    private UIElement? _lastPackInspectorTrigger;
    private UIElement? _lastShaderInspectorTrigger;

    public InstanceDetailPage()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<InstanceDetailViewModel>();
        InitializeComponent();
        ViewModel.LogAppended += OnLogAppended;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += (s, e) => SyncGraphicsModeComboBox();
        Unloaded += (s, e) =>
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.LogAppended -= OnLogAppended;
            ViewModel.Dispose();
        };
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModBrowserHostGrid.Visibility == Visibility.Visible)
        {
            CloseModBrowser();
            return;
        }

        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.GameOptions))
        {
            SyncGraphicsModeComboBox();
            Bindings.Update();
        }
    }

    private void OnLogAppended()
    {
        if (ViewModel.AutoScroll && LogScrollViewer != null)
        {
            _ = App.Dispatcher?.TryEnqueue(() =>
            {
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null, false);
            });
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt");
            System.IO.File.AppendAllText(logPath, $"InstanceDetailPage OnNavigatedTo: parameter={e.Parameter}\n");
        }
        catch { }

        if (e.Parameter is string instanceId)
        {
            var active = ((App)Application.Current).Services
                .GetRequiredService<Vaz.WinUI.Services.ActiveInstanceService>();
            if (instanceId == active.CurrentInstanceId)
            {
                _ = ViewModel.LoadCommand.ExecuteAsync(null);
            }
            else
            {
                active.SetCurrent(instanceId);
            }
        }
        else
        {
            _ = ViewModel.LoadCommand.ExecuteAsync(null);
        }

        DetailTabView.SelectionChanged -= DetailTabView_SelectionChanged;
        DetailTabView.SelectionChanged += DetailTabView_SelectionChanged;

        // Auto focus tab view for keyboard shortcuts
        DetailTabView.Focus(FocusState.Programmatic);

        // Đảm bảo click vào tab hoặc nút switcher đổi được SelectedItem
        DetailTabView.TabItemsChanged += (s, e) =>
        {
            if (DetailTabView.SelectedIndex < 0 && DetailTabView.TabItems.Count > 0)
                DetailTabView.SelectedIndex = 0;
        };

        var startupTab = Environment.GetEnvironmentVariable("VAZER_STARTUP_TAB");
        if (startupTab == "resourcepacks" && DetailTabView.TabItems.Count > 1)
        {
            DetailTabView.SelectedIndex = 1;
        }
        else if (startupTab == "shaders" && DetailTabView.TabItems.Count > 2)
        {
            DetailTabView.SelectedIndex = 2;
        }
    }

    private void DetailTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt");
            System.IO.File.AppendAllText(logPath, $"DetailTabView_SelectionChanged: SelectedIndex={DetailTabView.SelectedIndex}\n");
        }
        catch { }

        // Khi chuyển tab, đóng các inspector tương ứng
        if (ViewModel != null)
        {
            if (DetailTabView.SelectedIndex != 0 && ViewModel.IsInspectorOpen)
                ViewModel.CloseInspector();
            if (DetailTabView.SelectedIndex != 1 && ViewModel.IsPackInspectorOpen)
                ViewModel.ClosePackInspector();
            if (DetailTabView.SelectedIndex != 2 && ViewModel.IsShaderInspectorOpen)
                ViewModel.CloseShaderInspector();
        }
    }

    private void ModsTabButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt");
            System.IO.File.AppendAllText(logPath, "ModsTabButton_Click\n");
        }
        catch { }

        if (DetailTabView.TabItems.Count > 0)
        {
            DetailTabView.SelectedItem = DetailTabView.TabItems[0];
            DetailTabView.SelectedIndex = 0;
        }
    }

    private void ResourcePacksTabButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt");
            System.IO.File.AppendAllText(logPath, "ResourcePacksTabButton_Click\n");
        }
        catch { }

        if (ResourcePacksTab != null)
        {
            DetailTabView.SelectedItem = ResourcePacksTab;
        }
        else if (DetailTabView.TabItems.Count > 1)
        {
            DetailTabView.SelectedItem = DetailTabView.TabItems[1];
        }
        DetailTabView.SelectedIndex = 1;
    }

    private void ShadersTabButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt");
            System.IO.File.AppendAllText(logPath, "ShadersTabButton_Click\n");
        }
        catch { }

        if (ShadersTab != null)
        {
            DetailTabView.SelectedItem = ShadersTab;
        }
        else if (DetailTabView.TabItems.Count > 2)
        {
            DetailTabView.SelectedItem = DetailTabView.TabItems[2];
        }
        DetailTabView.SelectedIndex = 2;
    }

    private void ModEnabled_Toggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || ViewModel.IsGameRunning) return;
        if (sender is ToggleSwitch ts && ts.DataContext is InstalledModRow row && ts.IsOn != row.Enabled)
        {
            _ = ViewModel.SetModEnabledAsync(row, ts.IsOn);
        }
    }

    private async void InstallMrpack_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || ViewModel.IsGameRunning) return;
        var picker = FilePickerHelper.CreateOpenPicker(App.MainWindow, ".mrpack");
        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        await ShowMrpackInstallConfirmAsync(file.Path);
    }

    /// <summary>
    /// Luồng dùng chung (picker + drag-drop MC-SM-03): inspect .mrpack, confirm, import.
    /// </summary>
    private async Task ShowMrpackInstallConfirmAsync(string mrpackPath)
    {
        try
        {
            var preview = await ViewModel.InspectMrpackAsync(mrpackPath);

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Cài modpack?",
                PrimaryButtonText = "Cài",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = $"{preview.Name}\n" +
                           (string.IsNullOrEmpty(preview.Summary) ? "" : $"{preview.Summary}\n") +
                           $"Minecraft: {preview.MinecraftVersion}\n" +
                           $"Loader: {preview.LoaderType} {preview.LoaderVersion}\n" +
                           $"Files: {preview.FileCount}"
                }
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.ImportMrpackCommand.ExecuteAsync((mrpackPath, preview.Name));
            }
        }
        catch (Exception ex)
        {
            ViewModel.HasError = true;
            ViewModel.ErrorMessage = ex.Message;
        }
    }

    private async void InstallOptifine_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || ViewModel.IsGameRunning) return;
        var dialog = new OptifineInstallDialog(ViewModel);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }

    private async void DownloadMods_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || ViewModel.IsGameRunning) return;
        await OpenModBrowserAsync();
    }

    private ScrollViewer? _installedModsScrollViewer;
    private double _savedModsScrollOffset;

    private ScrollViewer? GetInstalledModsScrollViewer()
    {
        if (_installedModsScrollViewer != null) return _installedModsScrollViewer;
        if (InstalledModsListView == null) return null;
        _installedModsScrollViewer = FindVisualChild<ScrollViewer>(InstalledModsListView);
        return _installedModsScrollViewer;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) return typed;
            var sub = FindVisualChild<T>(child);
            if (sub != null) return sub;
        }
        return null;
    }

    private async Task OpenModBrowserAsync()
    {
        if (ViewModel?.Current == null) return;

        var sv = GetInstalledModsScrollViewer();
        if (sv != null)
        {
            _savedModsScrollOffset = sv.VerticalOffset;
        }

        ModBrowserHostGrid.Visibility = Visibility.Visible;
        ModBrowserHostGrid.Opacity = 0.0;
        ModBrowserTranslate.Y = 50;

        OpenModBrowserStoryboard.Begin();

        await EmbeddedModBrowser.InitializeForInstanceAsync(ViewModel.Current.Id);
        EmbeddedModBrowser.FocusSearchBox();
    }

    private bool _isClosingModBrowser;

    private void CloseModBrowser()
    {
        if (ModBrowserHostGrid.Visibility != Visibility.Visible || _isClosingModBrowser) return;
        _isClosingModBrowser = true;

        DownloadModsButton?.Focus(FocusState.Programmatic);
        CloseModBrowserStoryboard.Begin();
    }

    private void CloseModBrowserStoryboard_Completed(object? sender, object e)
    {
        ModBrowserHostGrid.Visibility = Visibility.Collapsed;
        _isClosingModBrowser = false;
        DownloadModsButton?.Focus(FocusState.Programmatic);

        if (ViewModel != null && EmbeddedModBrowser.HasChanges)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
            {
                await ViewModel.LoadAsync();
                if (_savedModsScrollOffset > 0)
                {
                    var sv = GetInstalledModsScrollViewer();
                    sv?.ChangeView(null, _savedModsScrollOffset, null, disableAnimation: true);
                }
            });
        }
    }

    private void EmbeddedModBrowser_CloseRequested(object? sender, EventArgs e)
    {
        CloseModBrowser();
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearLog();
    }

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(ViewModel.LogText ?? string.Empty);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
    }

    private async void DeleteInstance_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Xóa instance?",
            Content = "Toàn bộ thư mục instance sẽ bị xóa khỏi disk. Không thể hoàn tác.",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirm.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteCommand.ExecuteAsync(null);
        }
    }

    private void ViewMode_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            ViewModel.SwitchViewMode(ts.IsOn);
            if (!ts.IsOn)
            {
                SyncGraphicsModeComboBox();
                Bindings.Update();
            }
        }
    }

    private void PresetPerformance_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPreset("Performance");
        SyncGraphicsModeComboBox();
        Bindings.Update();
    }

    private void PresetBalanced_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPreset("Balanced");
        SyncGraphicsModeComboBox();
        Bindings.Update();
    }

    private void PresetHighQuality_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPreset("HighQuality");
        SyncGraphicsModeComboBox();
        Bindings.Update();
    }

    private void GraphicsMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GraphicsModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string mode)
        {
            if (ViewModel?.GameOptions != null)
            {
                ViewModel.GameOptions.GraphicsMode = mode;
            }
        }
    }

    private void SyncGraphicsModeComboBox()
    {
        if (GraphicsModeComboBox == null || ViewModel?.GameOptions == null) return;
        var mode = ViewModel.GameOptions.GraphicsMode?.ToLowerInvariant();
        var match = GraphicsModeComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(x => string.Equals((string)x.Tag, mode, StringComparison.OrdinalIgnoreCase));
        GraphicsModeComboBox.SelectedItem = match ?? GraphicsModeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => (string)x.Tag == "fancy");
    }

    private async void ExportOptions_Click(object sender, RoutedEventArgs e)
    {
        var picker = FilePickerHelper.CreateSavePicker(App.MainWindow, "options.txt", ".txt", "Minecraft Options");
        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            await ViewModel.ExportOptionsAsync(file.Path);
        }
    }

    private void Page_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && ModBrowserHostGrid.Visibility == Visibility.Visible)
        {
            if (EmbeddedModBrowser.HandleEscapeKey())
            {
                e.Handled = true;
                return;
            }

            CloseModBrowser();
            e.Handled = true;
        }
    }

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            if (ModBrowserHostGrid.Visibility == Visibility.Visible)
            {
                CloseModBrowser();
                e.Handled = true;
                return;
            }

            if (ViewModel.IsInspectorOpen)
            {
                ViewModel.CloseInspectorCommand.Execute(null);
                e.Handled = true;
            }
            else if (ViewModel.IsPackInspectorOpen)
            {
                ViewModel.ClosePackInspectorCommand.Execute(null);
                _lastPackInspectorTrigger?.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
            else if (ViewModel.IsShaderInspectorOpen)
            {
                ViewModel.CloseShaderInspectorCommand.Execute(null);
                _lastShaderInspectorTrigger?.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.F2)
        {
            DetailTabView.SelectedIndex = 1; // Tab Resource Packs
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.F3)
        {
            DetailTabView.SelectedIndex = 2; // Tab Shaders
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.F1)
        {
            DetailTabView.SelectedIndex = 0; // Tab Mods
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.F4)
        {
            var tempInvalidZip = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "corrupted_pack.zip");
            System.IO.File.WriteAllText(tempInvalidZip, "NOT_A_VALID_ZIP_CONTENT_CORRUPTED");
            _ = ViewModel.ImportResourcePackFilesAsync(new[] { tempInvalidZip });
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.F5)
        {
            foreach (var p in ViewModel.ResourcePacks) p.Enabled = false;
            ViewModel.RefreshResourcePackLists();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.F6)
        {
            ViewModel.ResourcePacks.Clear();
            ViewModel.RefreshResourcePackLists();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Up && Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            var target = ViewModel.SelectedResourcePack ?? ViewModel.ActiveResourcePacks.FirstOrDefault();
            if (target != null && ViewModel.ActiveResourcePacks.Contains(target))
            {
                ViewModel.MovePackUp(target);
                ViewModel.SelectedResourcePack = target;
                ActivePacksListView?.ScrollIntoView(target);
                if (ActivePacksListView?.ContainerFromItem(target) is Control c)
                {
                    c.Focus(FocusState.Keyboard);
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.Down && Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            var target = ViewModel.SelectedResourcePack ?? ViewModel.ActiveResourcePacks.FirstOrDefault();
            if (target != null && ViewModel.ActiveResourcePacks.Contains(target))
            {
                ViewModel.MovePackDown(target);
                ViewModel.SelectedResourcePack = target;
                ActivePacksListView?.ScrollIntoView(target);
                if (ActivePacksListView?.ContainerFromItem(target) is Control c)
                {
                    c.Focus(FocusState.Keyboard);
                }
                e.Handled = true;
            }
        }
    }

    private void ModInspector_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is InstalledModRow row)
        {
            ViewModel.OpenInspector(row);
        }
    }

    private void ModCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        // Bỏ qua sự kiện Tap nếu nguồn xuất phát từ một nút bấm hoặc ToggleSwitch
        // (MC-LD-03: gạt toggle không được mở Inspector).
        var cur = e.OriginalSource as Microsoft.UI.Xaml.DependencyObject;
        while (cur != null && !ReferenceEquals(cur, sender))
        {
            if (cur is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase ||
                cur is Microsoft.UI.Xaml.Controls.ToggleSwitch)
            {
                e.Handled = true;
                return;
            }
            cur = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(cur);
        }

        if (sender is FrameworkElement elem && elem.DataContext is InstalledModRow row)
        {
            ViewModel.OpenInspector(row);
        }
    }

    private void CategoryChip_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string category)
        {
            ViewModel?.SelectCategory(category);
        }
    }

    private void ModIcon_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is Image img)
        {
            img.Visibility = Visibility.Visible;
            if (img.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is FontIcon icon)
                    {
                        icon.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
    }

    private void ModIcon_Failed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image img)
        {
            img.Visibility = Visibility.Collapsed;
            if (img.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is FontIcon icon)
                    {
                        icon.Visibility = Visibility.Visible;
                    }
                }
            }
        }
    }

    private async void UninstallMod_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || ViewModel.IsGameRunning) return;
        if (sender is FrameworkElement elem && elem.DataContext is InstalledModRow row)
        {
            var confirm = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = row.IsMissingFile ? $"Gỡ {row.DisplayName} khỏi danh sách?" : $"Gỡ cài đặt {row.DisplayName}?",
                Content = row.IsMissingFile
                    ? $"Mục '{row.FileName}' không có file trên đĩa và sẽ được xóa khỏi cấu hình mods.json."
                    : $"Mod '{row.FileName}' sẽ bị xóa khỏi instance.",
                PrimaryButtonText = row.IsMissingFile ? "Gỡ bỏ" : "Gỡ cài đặt",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirm.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.UninstallAsync(row);
            }
        }
    }

    private async void ImportJar_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || ViewModel.IsGameRunning) return;
        var picker = FilePickerHelper.CreateOpenPicker(App.MainWindow, ".jar");
        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            await ViewModel.ImportJarFilesAsync(new[] { file.Path });
        }
    }

    private void ModsGrid_DragOver(object sender, DragEventArgs e)
    {
        if (ViewModel == null || ViewModel.IsGameRunning)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            return;
        }

        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Kéo thả .jar hoặc .mrpack để cài";
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
    }

    private async void ModsGrid_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel == null || ViewModel.IsGameRunning) return;
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var files = items
                .OfType<Windows.Storage.IStorageFile>()
                .ToList();

            // MC-SM-03: Ưu tiên .mrpack — gọi luồng import modpack thay vì import .jar.
            var mrpack = files.FirstOrDefault(f =>
                f.FileType.Equals(".mrpack", StringComparison.OrdinalIgnoreCase));
            if (mrpack != null)
            {
                await ShowMrpackInstallConfirmAsync(mrpack.Path);
                e.Handled = true;
                return;
            }

            var paths = files
                .Where(f => f.FileType.Equals(".jar", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .ToList();

            if (paths.Count > 0)
            {
                await ViewModel.ImportJarFilesAsync(paths);
                e.Handled = true;
            }
        }
    }

    // --- Resource Packs Handlers ---

    private async void ImportResourcePack_Click(object sender, RoutedEventArgs e)
    {
        var picker = FilePickerHelper.CreateOpenPicker(App.MainWindow, ".zip");
        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            await ViewModel.ImportResourcePackFilesAsync(new[] { file.Path });
        }
    }

    private void OpenResourcePacksFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel?.Current?.Path)) return;
        var folder = System.IO.Path.Combine(ViewModel.Current.Path, "resourcepacks");
        System.IO.Directory.CreateDirectory(folder);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private void RefreshResourcePacks_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.InitializeResourcePacksAndShaders();
    }

    private void PackInspector_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ResourcePackRow row)
        {
            _lastPackInspectorTrigger = elem;
            ViewModel.OpenPackInspector(row);
        }
    }

    private void PackCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        // Bỏ qua sự kiện Tap nếu nguồn xuất phát từ một nút bấm hoặc ToggleSwitch (Button, Toggle, ...)
        var cur = e.OriginalSource as Microsoft.UI.Xaml.DependencyObject;
        while (cur != null && !ReferenceEquals(cur, sender))
        {
            if (cur is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase ||
                cur is Microsoft.UI.Xaml.Controls.ToggleSwitch)
            {
                e.Handled = true;
                return;
            }
            cur = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(cur);
        }

        if (sender is FrameworkElement elem && elem.DataContext is ResourcePackRow row)
        {
            _lastPackInspectorTrigger = elem;
            ViewModel.OpenPackInspector(row);
        }
    }

    private void ClosePackInspector_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClosePackInspectorCommand.Execute(null);
        _lastPackInspectorTrigger?.Focus(FocusState.Programmatic);
    }

    private void PackEnable_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ResourcePackRow row)
        {
            ViewModel.EnablePack(row);
        }
    }

    private void PackDisable_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ResourcePackRow row)
        {
            ViewModel.DisablePack(row);
        }
    }

    private void PackMoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ResourcePackRow row)
        {
            ViewModel.MovePackUp(row);
        }
    }

    private void PackMoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ResourcePackRow row)
        {
            ViewModel.MovePackDown(row);
        }
    }

    private void ResourcePacksGrid_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Thêm gói tài nguyên (.zip)";
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
    }

    private async void ResourcePacksGrid_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var paths = items
                .OfType<Windows.Storage.IStorageFile>()
                .Where(f => f.FileType.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .ToList();

            if (paths.Count > 0)
            {
                await ViewModel.ImportResourcePackFilesAsync(paths);
            }
        }
    }

    // --- Shaders Handlers ---

    private async void ImportShader_Click(object sender, RoutedEventArgs e)
    {
        var picker = FilePickerHelper.CreateOpenPicker(App.MainWindow, ".zip");
        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            await ViewModel.ImportShaderFilesAsync(new[] { file.Path });
        }
    }

    private void OpenShadersFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel?.Current?.Path)) return;
        var folder = System.IO.Path.Combine(ViewModel.Current.Path, "shaderpacks");
        System.IO.Directory.CreateDirectory(folder);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private void DeactivateShaders_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeactivateShaders();
    }

    private void RefreshShaders_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.InitializeResourcePacksAndShaders();
    }

    private void ShaderChip_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string filter)
        {
            ViewModel?.SelectShaderFilter(filter);
        }
    }

    private void ShaderInspector_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ShaderPackRow row)
        {
            _lastShaderInspectorTrigger = elem;
            ViewModel.OpenShaderInspector(row);
        }
    }

    private void CloseShaderInspector_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CloseShaderInspectorCommand.Execute(null);
        _lastShaderInspectorTrigger?.Focus(FocusState.Programmatic);
    }

    private void ActivateShader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ShaderPackRow row)
        {
            ViewModel.ActivateShader(row);
        }
    }

    private void ActivateInspectorShader_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedShaderForInspector != null)
        {
            ViewModel.ActivateShader(ViewModel.SelectedShaderForInspector);
        }
    }

    private void ShadersGrid_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Thêm shader pack (.zip)";
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
    }

    private async void ShadersGrid_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var paths = items
                .OfType<Windows.Storage.IStorageFile>()
                .Where(f => f.FileType.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .ToList();

            if (paths.Count > 0)
            {
                await ViewModel.ImportShaderFilesAsync(paths);
            }
        }
    }
}
