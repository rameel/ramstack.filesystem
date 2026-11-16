# Contributing

Bug reports, fixes, and documentation improvements are welcome, including small ones.

## Reporting bugs

Tell us what happened and what you expected.
Include the affected package and .NET versions, your operating system, and the file system configuration.
For cloud storage issues, include the service or emulator and its version.
A small example with the relevant paths, expected result, and actual result helps.

## Development setup

Use the .NET 10 SDK or later to build from source, and install the .NET 6 and .NET 8 runtimes to run the tests.

Run the commands below from the repository root.

## Building and testing

Build the solution and run the tests that do not require cloud storage emulators:

```console
dotnet test -c Debug --filter "TestCategory!~Cloud"
dotnet test -c Release --filter "TestCategory!~Cloud"
```

### Cloud storage tests

Azure tests use Azurite at `127.0.0.1:10000`; Amazon S3 tests use RustFS at `localhost:9000`.
The services are defined in [docker-compose.yml](docker-compose.yml). Start them locally before running these tests:

```console
docker compose up -d
dotnet test --filter "TestCategory=Cloud:Azure"  -maxcpucount:1
dotnet test --filter "TestCategory=Cloud:Amazon" -maxcpucount:1
```

Google Cloud Storage tests are currently ignored because they require a running emulator.
See [.github/workflows/tests.yml](.github/workflows/tests.yml) for the CI configuration.

## Code style

Follow [.editorconfig](.editorconfig) and the style of the surrounding code.
Keep formatting changes limited to the code you're working on.

## Pull requests

- Keep each pull request focused on one goal. Its commits can use different scopes when they serve that goal.
  Do not mix unrelated changes into it.
- Several cleanups that do not change behavior, such as formatting, comment fixes, and readability improvements,
  can share one pull request when each is a separate commit. Send behavior changes found along the way
  as separate pull requests.
- Explain what changed and why, and link to related issues if there are any.
- When fixing a reproducible bug, add a regression test. Add tests for new behavior where appropriate.
- Update the documentation if your change affects how people use the library.
- Keep changes compatible with the target frameworks declared in the project files.

For major public API changes, open an issue first so we can agree on the approach before you spend time on it.

Use the [scoped commit style](#commit-messages) for pull request titles. Choose the scope with the
[same rules](#choosing-a-scope) as for a commit, treating the whole pull request as one change.
The title does not need to summarize every supporting commit.

## Commit messages

Use scoped commit messages:

```text
<scope>: <concise description>
```

The scope identifies the part of the project that changed. Write the description in the imperative mood,
starting with a lowercase letter and without a trailing period. Describe what the change does rather than
just naming its type.

```text
fs: normalize virtual path separators
fs/amazon: complete multipart uploads on disposal
fs/composite: preserve the order of file systems
```

Do not prefix messages with conventional commit types such as `feat`, `fix`, `refactor`, or `chore`,
and do not use ticket numbers as scopes. The description should make the type of change clear when it matters.
Put ticket references and implementation details in the message body when needed.

### Scopes

Use the smallest stable area that describes the outcome. Use these root scopes,
with `fs/<area>` for a specific file system area:

| Scope   | Area                                                                                      |
|---------|-------------------------------------------------------------------------------------------|
| `fs`    | File system contracts, implementations, wrappers, and integrations                        |
| `spec`  | The file system specification and the rules that check implementations for conformance    |
| `docs`  | General project documentation                                                             |
| `tests` | Test infrastructure and helpers, including those in the specification package             |
| `build` | Build, CI, dependencies, development tooling, and NuGet packaging                         |
| `repo`  | Repository structure and metadata, contribution and code style rules, cross-area cleanups |

Use `fs` without a subscope for contracts, nodes, paths, and common extension methods,
and for a change shared by several file system areas without one primary owner.
For implementations, wrappers, and integrations, use `fs/` with the package suffix in lowercase, such as
`fs/physical`, `fs/azure`, `fs/composite`, or `fs/adapters`.
For other root scopes, add a subscope only when the history becomes difficult to navigate.

Use the product area's scope for its tests, documentation, and examples, even when a commit changes only
those files. Reserve `tests` and `docs` for their own infrastructure or for related changes across multiple product areas.

Use `spec` for the specification itself: the contract and the rules that check implementations for conformance.
Use `tests` for test infrastructure and helpers, such as `TempFileStorage`, even when they live in the specification package.
Use the implementation's `fs/<area>` scope for its specification test fixture or for a correction specific to that implementation.

### Choosing a scope

Most commits change one area; use its scope as described above. When a commit touches several scopes,
use the first rule that applies:

1. **It changes behavior, public API, performance, or debugging experience.** Use the scope of the area
   where the change is visible, even if the implementation spans other areas. Use `fs` when a file system change
   has no single primary owner, such as reducing allocations in several file systems.
2. **It introduces or updates one thing that the rest of the commit adopts.** Use the scope of that thing:
   `build: enable analyzers and treat warnings as errors`, `repo: define formatting rules`.
3. **It only changes comments or documentation.** Use `docs`.
4. **Anything else**, such as formatting, renames, or file moves. Use `repo` for a general cleanup, even when
   only file system code changes. Use the file system scope when the work targets file system code itself,
   such as simplifying traversal in several file systems.

Make each commit one logical change. Split logically separate changes into separate commits, even when they
share a scope: if the description needs "and" to join unrelated changes, the commit should be split.
Do not split a single change only to give each part its own scope, and do not list several scopes.

## Issue titles

Use a clear, descriptive issue title. Scope prefixes are optional; when used, choose from the same scopes
as commits and pull requests. Describe the problem or proposed capability; the imperative mood is not required.

## Test names

Prefer `Method_Condition_ExpectedResult` when adding or renaming tests. This is a recommendation,
not a strict requirement: choose a clearer name when the pattern does not fit naturally.
Use PascalCase within each part and describe the expected observable behavior.

```text
ExistsAsync_MissingFile_ReturnsFalse
ExistsAsync_ExistingFile_ReturnsTrue
```

For parameterized tests, describe the common scenario and let the arguments distinguish individual cases.
There is no need to rename existing tests solely to follow this convention.
