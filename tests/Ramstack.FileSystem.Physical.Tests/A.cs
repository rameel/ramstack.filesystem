namespace Ramstack.FileSystem.Physical;

[TestFixture]
public class A
{
    public static async Task TT(IVirtualFileSystem fs)
    {
        var file = fs.GetFile("/sample/hello.txt");

        if (await file.ExistsAsync())
        {
            Console.WriteLine($"Deleting file '{file.FullName}'");
            await file.DeleteAsync();
        }

        Console.WriteLine($"Writing 'Hello, World!' to '{file.FullName}'");

        await using (Stream stream = await file.OpenWriteAsync())
        await using (StreamWriter writer = new StreamWriter(stream))
        {
            await writer.WriteLineAsync("Hello, World!");
        }

        Console.WriteLine($"Reading contents of '{file.FullName}'");
        using (StreamReader reader = await file.OpenTextAsync())
        {
            Console.WriteLine(await reader.ReadToEndAsync());
        }

        // Replace content of the file with data from an existing file
        await using FileStream input = File.OpenRead(@"D:\sample\hello-world.txt");
        await file.WriteAsync(input, overwrite: true);

        Console.WriteLine($"The file '{file.Name}' has a length of {await file.GetLengthAsync()} bytes");
    }
}
