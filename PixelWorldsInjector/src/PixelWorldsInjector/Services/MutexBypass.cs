using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PixelWorldsInjector.Native;

namespace PixelWorldsInjector.Services;

/// <summary>
/// Closes named-mutex handles inside a target process so that subsequent launches of
/// the same single-instance application can succeed. This is the core "injector"
/// technique used by Sandboxie-style multi-instance tools and is non-invasive:
/// we do not modify the game's memory or executable, we only close one kernel
/// handle from outside.
///
/// Strategy:
/// 1. Snapshot all kernel handles in the system via NtQuerySystemInformation(64).
/// 2. Filter to handles owned by our target PID.
/// 3. For each handle, duplicate it into our process so we can query its type/name.
/// 4. If the type is "Mutant" (NT name for mutex), duplicate the handle again with
///    DUPLICATE_CLOSE_SOURCE to close it inside the target process.
///
/// Notes on permissions:
///   - Requires PROCESS_DUP_HANDLE on the target. The app manifest requests
///     administrator elevation so this works against any process owned by the user.
///   - Closing a non-mutex handle (e.g. a file) is safe in principle but we
///     restrict by type to avoid surprising side effects.
/// </summary>
[SupportedOSPlatform("windows")]
public static class MutexBypass
{
    /// <summary>
    /// Closes mutex handles in <paramref name="targetPid"/>. Returns number of mutexes closed.
    /// </summary>
    public static int CloseMutexesInProcess(int targetPid, Func<string, bool>? nameFilter = null)
    {
        var hTarget = NtApi.OpenProcess(NtApi.PROCESS_DUP_HANDLE, false, (uint)targetPid);
        if (hTarget == IntPtr.Zero)
        {
            var err = Marshal.GetLastWin32Error();
            Logger.Warn($"OpenProcess({targetPid}) failed with error {err}. Are you running as admin?");
            return 0;
        }

        try
        {
            var handles = QuerySystemHandles();
            var closed = 0;

            foreach (var handle in handles)
            {
                if ((int)handle.UniqueProcessId != targetPid)
                {
                    continue;
                }

                if (TryCloseIfMutex(hTarget, handle, nameFilter))
                {
                    closed++;
                }
            }

            return closed;
        }
        finally
        {
            NtApi.CloseHandle(hTarget);
        }
    }

    private static bool TryCloseIfMutex(IntPtr hTarget, NtApi.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handle, Func<string, bool>? nameFilter)
    {
        // Duplicate into our process so we can query the type.
        var status = NtApi.NtDuplicateObject(
            hTarget,
            handle.HandleValue,
            NtApi.GetCurrentProcess(),
            out var hLocal,
            0,
            0,
            NtApi.DUPLICATE_SAME_ACCESS);

        if (status != NtApi.STATUS_SUCCESS || hLocal == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var typeName = QueryObjectType(hLocal);
            if (!string.Equals(typeName, "Mutant", StringComparison.Ordinal))
            {
                return false;
            }

            if (nameFilter is not null)
            {
                var objectName = QueryObjectName(hLocal) ?? string.Empty;
                if (!nameFilter(objectName))
                {
                    return false;
                }
            }
        }
        finally
        {
            NtApi.CloseHandle(hLocal);
        }

        // Now actually close the handle inside the target by re-duplicating with DUPLICATE_CLOSE_SOURCE.
        var closeStatus = NtApi.NtDuplicateObject(
            hTarget,
            handle.HandleValue,
            NtApi.GetCurrentProcess(),
            out var hScratch,
            0,
            0,
            NtApi.DUPLICATE_CLOSE_SOURCE);

        if (closeStatus == NtApi.STATUS_SUCCESS && hScratch != IntPtr.Zero)
        {
            NtApi.CloseHandle(hScratch);
        }

        return closeStatus == NtApi.STATUS_SUCCESS;
    }

    private static IReadOnlyList<NtApi.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> QuerySystemHandles()
    {
        uint bufferSize = 0x10000;
        IntPtr buffer = IntPtr.Zero;
        try
        {
            while (true)
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }

                buffer = Marshal.AllocHGlobal((int)bufferSize);
                var status = NtApi.NtQuerySystemInformation(NtApi.SystemExtendedHandleInformation, buffer, bufferSize, out var needed);
                if (status == NtApi.STATUS_INFO_LENGTH_MISMATCH)
                {
                    bufferSize = Math.Max(bufferSize * 2, needed + 0x1000);
                    continue;
                }

                if (status != NtApi.STATUS_SUCCESS)
                {
                    Logger.Warn($"NtQuerySystemInformation failed with status 0x{status:X8}");
                    return Array.Empty<NtApi.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
                }

                // The extended buffer starts with: ULONG_PTR NumberOfHandles, ULONG_PTR Reserved
                // then an array of SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX entries.
                var nptr = IntPtr.Size;
                var handleCount = Marshal.ReadIntPtr(buffer).ToInt64();
                var entrySize = Marshal.SizeOf<NtApi.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
                var firstEntry = IntPtr.Add(buffer, nptr * 2);

                var result = new List<NtApi.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>((int)Math.Min(handleCount, 200_000));
                for (long i = 0; i < handleCount; i++)
                {
                    var entryPtr = IntPtr.Add(firstEntry, (int)(i * entrySize));
                    result.Add(Marshal.PtrToStructure<NtApi.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(entryPtr));
                }

                return result;
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static string? QueryObjectType(IntPtr handle)
    {
        uint size = 0x400;
        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            while (true)
            {
                var status = NtApi.NtQueryObject(handle, NtApi.ObjectTypeInformation, buffer, size, out var needed);
                if (status == NtApi.STATUS_INFO_LENGTH_MISMATCH || needed > size)
                {
                    size = Math.Max(needed, size * 2);
                    Marshal.FreeHGlobal(buffer);
                    buffer = Marshal.AllocHGlobal((int)size);
                    continue;
                }

                if (status != NtApi.STATUS_SUCCESS)
                {
                    return null;
                }

                var info = Marshal.PtrToStructure<NtApi.OBJECT_TYPE_INFORMATION>(buffer);
                if (info.TypeName.Buffer == IntPtr.Zero || info.TypeName.Length == 0)
                {
                    return null;
                }

                return Marshal.PtrToStringUni(info.TypeName.Buffer, info.TypeName.Length / 2);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? QueryObjectName(IntPtr handle)
    {
        uint size = 0x1000;
        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            while (true)
            {
                var status = NtApi.NtQueryObject(handle, NtApi.ObjectNameInformation, buffer, size, out var needed);
                if (status == NtApi.STATUS_INFO_LENGTH_MISMATCH || needed > size)
                {
                    size = Math.Max(needed, size * 2);
                    Marshal.FreeHGlobal(buffer);
                    buffer = Marshal.AllocHGlobal((int)size);
                    continue;
                }

                if (status != NtApi.STATUS_SUCCESS)
                {
                    return null;
                }

                var name = Marshal.PtrToStructure<NtApi.UNICODE_STRING>(buffer);
                if (name.Buffer == IntPtr.Zero || name.Length == 0)
                {
                    return string.Empty;
                }

                return Marshal.PtrToStringUni(name.Buffer, name.Length / 2);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
