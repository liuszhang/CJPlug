using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StationSettingUI.Helpers;

/// <summary>
/// Windows Shell COM 互操作 — 文件夹选择对话框
/// </summary>
public static class FolderBrowserHelper
{
    /// <summary>
    /// 显示 Windows 原生文件夹选择对话框
    /// </summary>
    public static string? ShowDialog(Window? owner, string title, string initialPath = "")
    {
        IntPtr ownerHandle = IntPtr.Zero;
        if (owner != null)
        {
            ownerHandle = new WindowInteropHelper(owner).Handle;
        }

        var dialog = (IFileOpenDialog)new FileOpenDialog();
        try
        {
            dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
            dialog.SetTitle(title);

            if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
            {
                var shellItem = SHCreateItemFromParsingName(initialPath, null, typeof(IShellItem).GUID);
                if (shellItem != null)
                    dialog.SetFolder(shellItem);
            }

            if (dialog.Show(ownerHandle) == 0) // S_OK
            {
                dialog.GetResult(out var item);
                if (item != null)
                {
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                    return path;
                }
            }
        }
        catch
        {
            // 对话框失败时静默处理
        }

        return null;
    }

    // ========== COM 互操作定义 ==========

    [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    private class FileOpenDialog { }

    [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes(int cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(int iFileType);
        void GetFileTypeIndex(out int piFileType);
        void Advise(IntPtr pfde, out int pdwCookie);
        void Unadvise(int dwCookie);
        void SetOptions(FOS fos);
        void GetOptions(out FOS pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem? ppsi);
        void GetCurrentSelection(out IShellItem? ppsi);
        void SetFileName(string pszName);
        void GetFileName(out string? pszName);
        void SetTitle(string pszTitle);
        void SetOkButtonLabel(string pszText);
        void SetFileNameLabel(string pszLabel);
        void GetResult(out IShellItem? ppsi);
        void AddPlace(IShellItem psi, int alignment);
        void SetDefaultExtension(string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
        void GetResults(out IShellItemArray? ppenum);
        void GetSelectedItems(out IShellItemArray? ppsai);
    }

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem? ppsi);
        void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string? ppszName);
        void GetAttributes(int sfgaoMask, out int psfgaoAttribs);
        void Compare(IShellItem psi, int hint, out int piOrder);
    }

    [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppvOut);
        void GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
        void GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);
        void GetAttributes(int AttribFlags, int sfgaoMask, out int psfgaoAttribs);
        void GetCount(out int pdwNumItems);
        void GetItemAt(int dwIndex, out IShellItem? ppsi);
        void EnumItems(out IntPtr ppenumShellItems);
    }

    [Flags]
    private enum FOS : uint
    {
        FOS_PICKFOLDERS = 0x00000020,
        FOS_FORCEFILESYSTEM = 0x00000040,
    }

    private enum SIGDN : uint
    {
        SIGDN_FILESYSPATH = 0x80058000,
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IShellItem? SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr? pbc,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid);
}
