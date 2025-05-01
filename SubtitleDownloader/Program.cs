using OpenSubtitlesSharp;
using SubtitleDownloader;

var apiKey = AppSettings.ApiKey;
var userName = AppSettings.UserName;
var password = AppSettings.UserPassword;

var currentDirectory = AppContext.BaseDirectory;

Console.WriteLine($"Procurando arquivos em: {currentDirectory}");

var videoFiles = Directory.GetFiles(currentDirectory)
    .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
    .ToList();

if (videoFiles.Count <= 0)
{
    Console.WriteLine("Nenhum arquivo de vídeo encontrado na pasta do executável.");
    Console.ReadKey();

    return;
}

var path = videoFiles.First();
var fileName = Path.GetFileName(path);

using var client = new OpenSubtitlesClient(apiKey, null, BaseUrlType.Default);
//await client.LoginAsync(userName, password);

var searchParameters = new SearchParameters
{
    Query = fileName,
    Languages = ["pt-br"]
};

var searchResult = await client.SearchAsync(searchParameters);

if (searchResult == null)
{
    Console.WriteLine("API indisponível");
    return;
}

var subtitles = searchResult.Items.SelectMany(x => x.Information.Files).ToList();

if (!subtitles.Any())
{
    Console.WriteLine("legenda não encontrada");
    return;
}

var fileId = subtitles.First().FileId;

if (!fileId.HasValue)
{
    Console.WriteLine("legenda não encontrada");
    return;
}

var downloadParameters = new DownloadParameters
{
    FileId = fileId!.Value
};

var downloadInfo = await client.GetDownloadInfoAsync(downloadParameters);

if (downloadInfo == null)
{
    Console.WriteLine("link de download indisponível");
    return;
}

var httpClient = new HttpClient();

var response = await httpClient.GetAsync(downloadInfo.Link);

if (!response.IsSuccessStatusCode)
{
    Console.WriteLine("erro ao baixar legenda");
    return;
}

var subtitlePath = Path.Combine(currentDirectory, fileName + ".srt");

await using var fileStream = new FileStream(subtitlePath, FileMode.Create);
await response.Content.CopyToAsync(fileStream);

Console.WriteLine("arquivo criado com sucesso");

Console.ReadKey();