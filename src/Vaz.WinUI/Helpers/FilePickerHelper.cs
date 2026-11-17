using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Vaz.WinUI.Helpers;

/// <summary>
/// Wrap FileOpenPicker cho WinUI 3 unpackaged: bắt buộc InitWithWindow (gắn HWND).
/// </summary>
public static class FilePickerHelper
{
    /// <summary>
    /// Tạo FileOpenPicker đã gắn vào window hiện tại.
    /// </summary>
    public static FileOpenPicker CreateOpenPicker(Window window, params string[] extensions)
    {
        var picker = new FileOpenPicker();
        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        if (extensions.Length == 0)
        {
            picker.FileTypeFilter.Add("*");
        }
        else
        {
            foreach (var ext in extensions)
            {
                string filter = (ext == "*" || ext.StartsWith('.')) ? ext : "." + ext;
                picker.FileTypeFilter.Add(filter);
            }
        }

        return picker;
    }

    /// <summary>
    /// Tạo FileSavePicker đã gắn vào window hiện tại.
    /// </summary>
    public static FileSavePicker CreateSavePicker(Window window, string defaultFileName, string ext, string description)
    {
        var picker = new FileSavePicker();
        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = defaultFileName;

        var cleanExt = ext.StartsWith('.') ? ext : "." + ext;
        picker.FileTypeChoices.Add(description, new List<string> { cleanExt });

        return picker;
    }
}
