namespace Ramstack.FileSystem.Physical;

/// <summary>
/// Defines filters for excluding specific types of files and directories.
/// </summary>
public enum ExclusionFilters
{
    /// <summary>
    /// No exclusion filter is applied.
    /// </summary>
    None,

    /// <summary>
    /// Excludes dot-prefixed, hidden, and system files and directories.
    /// </summary>
    Sensitive
}
