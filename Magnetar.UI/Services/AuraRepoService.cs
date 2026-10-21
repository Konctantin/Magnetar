using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Magnetar.UI.Services;

public class AauraBuffer
{
    public byte[] Buffer { get; set; } = [];
    public string Name { get; set; } = "";
    public string Hash { get; set; } = "";
}

public class AuraRepoService
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IndentSize = 2,
    };

    public Dictionary<string, AauraBuffer> Data { get; } = [];

    private string FileName => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "auras.json");

    public AuraRepoService()
    {
        if (File.Exists(FileName))
        {
            var text = File.ReadAllText(FileName);
            var auras = JsonSerializer.Deserialize<AauraBuffer[]>(text, jsonOptions) ?? [];
            foreach (var aura in auras)
            {
                if (!Data.ContainsKey(aura.Hash))
                {
                    Data.Add(aura.Hash, aura);
                }
            }
        }
    }

    public void Save()
    {
        var list = Data.Values.ToArray();
        var json = JsonSerializer.Serialize(list, jsonOptions);
        File.WriteAllText(FileName, json);
    }

    public string GetName(string hash)
    {
        if (Data.TryGetValue(hash, out var aura))
            return aura.Name;
        return "";
    }

    public void AddIfNotExists(string hash, Bitmap buffer)
    {
        if (!Data.ContainsKey(hash))
        {
            using var stream = new MemoryStream(1000);
            buffer.Save(stream, ImageFormat.Bmp);
            stream.Position = 0;
            var bytes = new byte[stream.Length];
            Array.Copy(stream.GetBuffer(), bytes, stream.Length);

            var item = new AauraBuffer { Hash = hash, Buffer = bytes, Name = "" };
            Data.Add(hash, item);
            Updated?.Invoke(this, item);
            Save();
        }
    }

    public event EventHandler<AauraBuffer>? Updated;
}
