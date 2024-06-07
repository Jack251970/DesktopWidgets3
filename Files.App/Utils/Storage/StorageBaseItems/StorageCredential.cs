// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Security;

namespace Files.App.Utils.Storage;

// Code from System.Net.NetworkCredential
public sealed class StorageCredential
{
    private string _userName = string.Empty;
    public string UserName
    {
        get => _userName;
        set => _userName = value ?? string.Empty;
    }

    private object? _password;
    public string Password
    {
        get => _password is SecureString sstr ? MarshalToString(sstr) : (string?)_password ?? string.Empty;
        set
        {
            var old = _password as SecureString;
            _password = value;

            old?.Dispose();
        }
    }

    public SecureString SecurePassword
    {
        get => _password is string str ? MarshalToSecureString(str) : _password is SecureString sstr ? sstr.Copy() : new SecureString();
        set
        {
            var old = _password as SecureString;
            _password = value?.Copy();

            old?.Dispose();
        }
    }

    public StorageCredential()
        : this(string.Empty, string.Empty)
    {
    }

    public StorageCredential(string? userName, string? password)
    {
        UserName = userName!;
        Password = password!;
    }

    public StorageCredential(string? userName, SecureString? password)
    {
        UserName = userName!;
        SecurePassword = password!;
    }

    private static string MarshalToString(SecureString sstr)
    {
        if (sstr == null || sstr.Length == 0)
        {
            return string.Empty;
        }

        var ptr = IntPtr.Zero;
        var result = string.Empty;

        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(sstr);
            result = Marshal.PtrToStringUni(ptr)!;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }

        return result;
    }

    private unsafe SecureString MarshalToSecureString(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return new SecureString();
        }

        fixed (char* ptr = str)
        {
            return new SecureString(ptr, str.Length);
        }
    }
}
