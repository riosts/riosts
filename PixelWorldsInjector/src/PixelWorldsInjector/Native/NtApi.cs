using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PixelWorldsInjector.Native;

/// <summary>
/// P/Invoke layer for the subset of NT and Win32 APIs needed to enumerate and
/// close kernel handles in other processes (used by MutexBypass).
/// All declarations target x64 Windows.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class NtApi
{
    public const uint STATUS_SUCCESS = 0x00000000;
    public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;

    public const uint PROCESS_DUP_HANDLE = 0x0040;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    public const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
    public const uint DUPLICATE_SAME_ACCESS = 0x00000002;

    public const int SystemHandleInformation = 16;
    public const int SystemExtendedHandleInformation = 64;

    public const int ObjectBasicInformation = 0;
    public const int ObjectNameInformation = 1;
    public const int ObjectTypeInformation = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
    {
        public IntPtr Object;
        public IntPtr UniqueProcessId;
        public IntPtr HandleValue;
        public uint GrantedAccess;
        public ushort CreatorBackTraceIndex;
        public ushort ObjectTypeIndex;
        public uint HandleAttributes;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_TYPE_INFORMATION
    {
        public UNICODE_STRING TypeName;
        // ... more fields exist, but we only need TypeName.
    }

    [DllImport("ntdll.dll")]
    public static extern uint NtQuerySystemInformation(
        int SystemInformationClass,
        IntPtr SystemInformation,
        uint SystemInformationLength,
        out uint ReturnLength);

    [DllImport("ntdll.dll")]
    public static extern uint NtQueryObject(
        IntPtr Handle,
        int ObjectInformationClass,
        IntPtr ObjectInformation,
        uint ObjectInformationLength,
        out uint ReturnLength);

    [DllImport("ntdll.dll")]
    public static extern uint NtDuplicateObject(
        IntPtr SourceProcessHandle,
        IntPtr SourceHandle,
        IntPtr TargetProcessHandle,
        out IntPtr TargetHandle,
        uint DesiredAccess,
        uint HandleAttributes,
        uint Options);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();
}
