using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace JsonValidator;

using System.CommandLine;

internal class Program
{
    static void Main(string[] args)
    {
        var inputDirOption = new Option<DirectoryInfo>(name: "--input-dir", description: "The directory containing the JSON files to validate.")
        {
            IsRequired = true
        };

        var outputFileOption = new Option<FileInfo?>(name: "--output-file", description: "The file to write the validation results to.  Output will be written to the console even if this option isn't set.")
        {
            IsRequired = false
        };

        var rootCmd = new RootCommand("Validate JSON files");
        rootCmd.AddOption(inputDirOption);
        rootCmd.AddOption(outputFileOption);

        rootCmd.SetHandler<DirectoryInfo, FileInfo>(ValidateJsonFiles, inputDirOption, outputFileOption);
        Console.ReadLine();
    }

    private static void ValidateJsonFiles(DirectoryInfo inputDir, FileInfo? outputFile)
    {
        var jsonFiles = inputDir.EnumerateFiles("*.json", SearchOption.AllDirectories);

        foreach (var file in jsonFiles)
        {
            
        }
    }   
}
