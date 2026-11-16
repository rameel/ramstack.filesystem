using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Scope = "namespace", Target = "~N:Ramstack.FileSystem.Sub",
    Justification = "The namespace matches the package name, and renaming it would break the public API.")]
