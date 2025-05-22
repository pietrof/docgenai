using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string ApiUrl = "http://192.168.2.252:11434/api/generate";
    private const string Prompt = "You are part of a source code documentation system which generates " +
        "documentation in the doxygen .dox format. " +
        " For each of the functions, classes, methods, enums, types in the supplied code create .dox documentation."+
        "the response should be contents of a file in .dox format only";

    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: dotnet run <directory_path>");
            return;
        }

        string directoryPath = args[0];

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine("Directory does not exist.");
            return;
        }
        // List of popular programming language file extensions9
        string[] allowedExtensions = { ".cs", ".java", ".py", ".js",".sql", ".cpp", ".c", ".ts", ".rb", ".php", ".swift" };

        foreach (string filePath in Directory.GetFiles(directoryPath))
        {
            string extension = Path.GetExtension(filePath);
            if (Array.Exists(allowedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                await ProcessFile(filePath);
            }
        }
    }

    private static async Task ProcessFile(string filePath)
    {
        string fileContent = await File.ReadAllTextAsync(filePath);

        var payload = new
        {
            model = "qwen3:latest", // Replace with your actual model name
            prompt = $"{Prompt}\n\n{fileContent}",
            stream = false
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            Console.WriteLine($"Processing: {filePath}");

            using var response = await httpClient.PostAsync(ApiUrl, requestContent);
            response.EnsureSuccessStatusCode();

            // Parse the response JSON directly
            var responseContent = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseContent);

            if (doc.RootElement.TryGetProperty("response", out var message))
            {
                string outputPath = filePath + ".dox";
                string rawResponse = message.GetString();

                // Strip out the <think>...</think> part
                string processedResponse = StripThinkTags(rawResponse);

                await File.WriteAllTextAsync(outputPath, processedResponse);
                Console.WriteLine($"Processed and saved: {outputPath}");
            }
            else
            {
                Console.WriteLine($"No 'response' property found in the API response for {filePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing {filePath}: {ex.Message}");
        }
    }

    private static string StripThinkTags(string input)
    {
        int startIndex = input.IndexOf("<think>");
        int endIndex = input.IndexOf("</think>");

        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
        {
            // Remove the <think>...</think> part
            return input.Remove(startIndex, (endIndex + "</think>".Length) - startIndex).Trim();
        }

        // If no <think> tags are found, return the input as is
        return input;
    }
}

