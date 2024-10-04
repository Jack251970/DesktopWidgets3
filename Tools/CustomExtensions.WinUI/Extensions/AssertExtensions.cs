using System.Diagnostics.CodeAnalysis;

namespace CustomExtensions.WinUI.Extensions;

internal static class AssertExtensions
{
    [return: NotNull]
    public static T AssertDefined<T>([NotNull] this T? value) => value ?? throw new ArgumentNullException(nameof(value));
}
