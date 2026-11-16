using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Scope = "namespace", Target = "~N:Ramstack.FileSystem.Readonly",
    Justification = "The namespace matches the package name, and renaming it would break the public API.")]
