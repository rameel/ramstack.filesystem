namespace Ramstack.FileSystem.Physical;

/// <summary>
/// Defines filters for excluding specific types of files and directories.
/// </summary>
[Flags]
public enum ExclusionFilters
{
    /// <summary>
    /// No exclusion filter is applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Excludes files and directories prefixed with a dot "." (for example, ".git", ".gitignore", ".editorconfig").
    /// </summary>
    /// <remarks>
    /// Dot-prefixed files are often used for configuration or system files that are not intended for general use.
    /// Excluding them can help prevent accidental modifications or deletions.
    /// </remarks>
    DotPrefixed = 1,

    /// <summary>
    /// Excludes hidden files and directories when the <see cref="FileAttributes.Hidden"/> flag is set.
    /// </summary>
    /// <remarks>
    /// Do not modify. This value directly corresponds to the <see cref="FileAttributes.Hidden"/> value.
    /// </remarks>
    Hidden = 2,

    /// <summary>
    /// Excludes system files and directories when the <see cref="FileAttributes.System"/> flag is set.
    /// </summary>
    /// <remarks>
    /// Do not modify. This value directly corresponds to the <see cref="FileAttributes.System"/> value.
    /// </remarks>
    System = 4,

    /// <summary>
    /// Excludes dot-prefixed, hidden, and system files and directories.
    /// </summary>
    /// <remarks>
    /// Equivalent to <see cref="DotPrefixed"/> | <see cref="Hidden"/> | <see cref="System"/>.
    /// </remarks>
    Sensitive = DotPrefixed | Hidden | System
}
