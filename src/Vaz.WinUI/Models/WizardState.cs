using CommunityToolkit.Mvvm.ComponentModel;

namespace Vaz.WinUI.Models;

/// <summary>Bước hiện tại của wizard tạo instance.</summary>
public enum WizardStep
{
    /// <summary>Bước 1: chọn Minecraft version.</summary>
    Step1Version = 1,

    /// <summary>Bước 2: chọn loader (+ loader version).</summary>
    Step2Loader = 2,

    /// <summary>Bước 3: xác nhận & tạo instance.</summary>
    Step3Confirm = 3
}

/// <summary>
/// Trạng thái dùng chung giữa các step của wizard "Tạo instance".
/// Dialog giữ một instance, truyền cho cả 2 step; step đọc/ghi qua property này.
/// </summary>
public sealed partial class WizardState : ObservableObject
{
    /// <summary>Step đang hiển thị.</summary>
    [ObservableProperty]
    private WizardStep currentStep = WizardStep.Step1Version;

    /// <summary>MC version user đã chọn (vd "1.21.1"); null khi chưa chọn.</summary>
    [ObservableProperty]
    private string? selectedVersion;

    /// <summary>Loader user đã chọn ("Vanilla" | "Fabric" | "Quilt" | "NeoForge"); null khi chưa chọn.</summary>
    [ObservableProperty]
    private string? selectedLoader;

    /// <summary>Loader version user chọn; null cho Vanilla.</summary>
    [ObservableProperty]
    private string? selectedLoaderVersion;
}
