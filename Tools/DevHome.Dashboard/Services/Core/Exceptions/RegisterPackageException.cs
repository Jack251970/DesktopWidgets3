// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Dashboard.Services.Core.Exceptions;

/// <summary>
/// Exception thrown if a package registration failed
/// </summary>
public class RegisterPackageException(string message, Exception innerException) : Exception(message, innerException)
{
}
