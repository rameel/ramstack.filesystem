namespace Ramstack.FileSystem.Specification.Tests.Utilities;

public sealed class TempFileStorage : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public TempFileStorage()
    {
        var path = Root;
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

                "project/assets/fonts/Arial.ttf",
                "project/assets/fonts/Roboto.ttf",
                "project/assets/styles/main.css",
                "project/assets/styles/print.css",

                "project/packages/Ramstack.Globbing.2.1.0/lib/net60/Ramstack.Globbing.dll",
                "project/packages/Ramstack.Globbing.2.1.0/Ramstack.Globbing.2.1.0.zip",

                "project/.gitignore",
                "project/.editorconfig",
                "project/README.md",
                "project/global.json",
                "project/nuget.config",
            }
            .Select(p => Path.Combine(path, p))
            .ToArray();

        var directories = list
            .Select(p =>
                Path.GetDirectoryName(p)!)
            .Distinct()
            .ToArray();

        foreach (var d in directories)
            Directory.CreateDirectory(d);

        foreach (var f in list)
            File.WriteAllText(f, $"Automatically generated on {DateTime.Now:s}\n\nId:{Guid.NewGuid()}");
    }

    public TempFileStorage(TempFileStorage storage)
    {
        CopyDirectory(storage.Root, Root);

        static void CopyDirectory(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);

            foreach (var sourceFileName in Directory.GetFiles(sourcePath))
            {
                var name = Path.GetFileName(sourceFileName);
                var destFileName = Path.Join(destinationPath, name);
                File.Copy(sourceFileName, destFileName);
            }

            foreach (var directoryPath in Directory.GetDirectories(sourcePath))
            {
                var destination = Path.Join(destinationPath, Path.GetFileName(directoryPath));
                CopyDirectory(directoryPath, destination);
            }
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, true);
    }
}
