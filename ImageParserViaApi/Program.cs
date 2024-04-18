

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

DirectoryInfo d = new DirectoryInfo("./Input");
FileInfo[] files = d.GetFiles("*.*");

var client = new HttpClient();

if (!Directory.Exists("./Handled"))
    Directory.CreateDirectory("./Handled");
if (!Directory.Exists("./Failed"))
    Directory.CreateDirectory("./Failed");
if (!Directory.Exists("./Output"))
    Directory.CreateDirectory("./Output");

JObject appsettings = JObject.Parse(await File.ReadAllTextAsync("./AppSettings.json"));

string token = appsettings["X-WORKER-TOKEN"]?.Value<string>() ?? "";
string extractorId = appsettings["X-WORKER-EXTRACTOR-ID"]?.Value<string>() ?? "";


foreach (var file in files)
{
    Console.WriteLine("Sending " + file.Name);
    var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://worker.formextractorai.com/v2/extract"));
    request.Headers.TryAddWithoutValidation("X-WORKER-TOKEN", token);
    request.Headers.TryAddWithoutValidation("X-WORKER-EXTRACTOR-ID", extractorId);
    using var filestream = File.OpenRead(file.FullName);
    filestream.Seek(0, SeekOrigin.Begin);
    request.Content = new StreamContent(filestream);

    var res = await client.SendAsync(request);
    await filestream.DisposeAsync();
    if (res.IsSuccessStatusCode)
    {
        Console.WriteLine("Success");
        
        var cont = await res.Content.ReadAsStringAsync();
        dynamic parsedJson = JsonConvert.DeserializeObject(cont);
        await File.WriteAllTextAsync("./Output/" + file.Name + ".json", JsonConvert.SerializeObject(parsedJson, Formatting.Indented));

        if (File.Exists("./Handled/" + file.Name))
            File.Delete("./Handled/" + file.Name);

        File.Move(file.FullName, "./Handled/" + file.Name);
    }
    else
    {
        Console.WriteLine("Failed: " + res.StatusCode);
        Console.WriteLine("Message: " + await res.Content.ReadAsStringAsync());

        if (File.Exists("./Failed/" + file.Name))
            File.Delete("./Failed/" + file.Name);
        File.Move(file.FullName, "./Failed/" + file.Name);
    }
}