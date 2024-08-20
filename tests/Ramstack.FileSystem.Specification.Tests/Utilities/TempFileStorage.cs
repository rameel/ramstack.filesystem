namespace Ramstack.FileSystem.Specification.Tests.Utilities;

public sealed class TempFileStorage : IDisposable
{
    public string Root { get; }
    public IReadOnlyList<string> FileList { get; }

    public TempFileStorage()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var list = new[]
        {
            "project/docs/user_manual.pdf",
            "project/docs/api_reference.md",
            "project/docs/troubleshooting/common_issues.txt",
            "project/docs/troubleshooting/faq.docx",

            "project/docs_generated/html/index.html",
            "project/docs_generated/html/api.html",
            "project/docs_generated/pdf/documentation.pdf",

            "project/src/App/App.csproj",
            "project/src/App/Program.cs",
            "project/src/App/Utils.cs",
            "project/src/Modules/Module1/Module1.cs",
            "project/src/Modules/Module1/Module1.csproj",
            "project/src/Modules/Module1/Submodule/Submodule1.cs",
            "project/src/Modules/Module1/Submodule/Submodule2.cs",
            "project/src/Modules/Module1/Submodule/Submodule.csproj",
            "project/src/Modules/Module2/Module2.cs",
            "project/src/Modules/Module2/Module2.csproj",
            "project/src/App.sln",

            "project/tests/TestMain.cs",
            "project/tests/TestUtils.cs",
            "project/tests/Tests.csproj",
            "project/tests/Fixtures/SampleData.json",
            "project/tests/Fixtures/MockResponses.xml",

            "project/data/raw/dataset1.csv",
            "project/data/raw/dataset2.csv",
            "project/data/raw/dataset-[data]-{1}.csv",
            "project/data/processed/cleaned_data.csv",
            "project/data/processed/aggregated_results.json",

            "project/data/temp/temp_file1.tmp",
            "project/data/temp/temp_file2.tmp",
            "project/data/temp/ac/b2/34/2d/7e/temp_file2.tmp",
            "project/data/temp/hidden-folder/temp_1.tmp",
            "project/data/temp/hidden-folder/temp_2.tmp",
            "project/data/temp/hidden/temp_hidden3.dat",
            "project/data/temp/hidden/temp_hidden4.dat",

            "project/scripts/setup.p1",
            "project/scripts/deploy.ps1",
            "project/scripts/build.bat",
            "project/scripts/build.sh",

            "project/logs/app.log",
            "project/logs/error.log",
            "project/logs/archive/2019/01/app_2019-01.log",
            "project/logs/archive/2019/02/app_2019-02.log",
            "project/logs/archive/2019/03/app_2019-03.log",

            "project/config/production.json",
            "project/config/development.json",
            "project/config/test.json",

            "project/assets/images/logo.png",
            "project/assets/images/icon.svg",
            "project/assets/images/backgrounds/light.jpg",
            "project/assets/images/backgrounds/dark.jpeg",

            "project/assets/fonts/opensans.ttf",
            "project/assets/fonts/roboto.ttf",
            "project/assets/styles/main.css",
            "project/assets/styles/print.css",

            "project/packages/Ramstack.Globbing.2.1.0/lib/net60/Ramstack.Globbing.dll",
            "project/packages/Ramstack.Globbing.2.1.0/Ramstack.Globbing.2.1.0.nupkg",

            "project/.gitignore",
            "project/.editorconfig",
            "project/README.md",
            "project/global.json",
            "project/nuget.config",
        }
            .Select(p => Path.Combine(root, p))
            .ToArray();

        var directories = list
            .Select(p => Path.GetDirectoryName(p)!)
            .Distinct()
            .ToArray();

        foreach (var path in directories)
            Directory.CreateDirectory(path);

        foreach (var path in list)
            File.WriteAllText(path, $"Automatically generated on {DateTime.Now:s}\n\nId:{Guid.NewGuid()}");

        var hiddenFolder = directories.First(p => p.Contains("hidden-folder"));
        File.SetAttributes(hiddenFolder, FileAttributes.Hidden);

        foreach (var hiddenFile in list.Where(p => p.Contains("temp_hidden")))
            File.SetAttributes(hiddenFile, FileAttributes.Hidden);

        Root = root;
        FileList = list;
    }

    public void Dispose() =>
        Directory.Delete(Root, true);
}
