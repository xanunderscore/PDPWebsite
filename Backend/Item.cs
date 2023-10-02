using System.Runtime.InteropServices;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using PDPWebsite.Universalis;
using SkiaSharp;

namespace PDPWebsite;

public class Item
{
    public uint Id;
    public string Singular;
    public string Plural;
    public string Name;
    public ushort Icon;

    public static List<Item> Items = new();

    private static GameData _gameData;

    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
    private const string IconHDFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";

    private TexFile? GetTexFile()
    {
        var filePath = string.Format(IconHDFileFormat, Icon / 1000, string.Empty, Icon);
        var file = _gameData.GetFile<TexFile>(filePath);

        if (file != default(TexFile)) return file;

        filePath = string.Format(IconFileFormat, Icon / 1000, string.Empty, Icon);
        file = _gameData.GetFile<TexFile>(filePath);
        return file;
    }

    public unsafe SKBitmap? GetIconTexture()
    {
        var texFile = GetTexFile();
        if (texFile == null) return null;
        fixed (byte* p = texFile.ImageData)
        {
            var ptr = (IntPtr)p;
            var bmp = new SKBitmap();
            bmp.InstallPixels(new SKImageInfo(texFile.Header.Width, texFile.Header.Height,RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SKColorType.Bgra8888 : SKColorType.Rgba8888), ptr, texFile.Header.Width * 4);
            return bmp;
        }
    }

    public static async Task Load(UniversalisClient client)
    {
        var ids = await client.GetMarketItems();
#if DEBUG
        var gameDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SquareEnix", "FINAL FANTASY XIV - A Realm Reborn", "game", "sqpack");
#else
            var gameDataPath = Path.Combine(AppContext.BaseDirectory, "ffxiv", "sqpack");
#endif
        _gameData = new GameData(gameDataPath)
        {
            Options =
            {
                PanicOnSheetChecksumMismatch = false
            }
        };
        var items = _gameData.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Item>(Language.English);
        if (items != null)
            foreach (var item in items)
            {
                if (ids.Contains(item.RowId))
                    Items.Add(new Item
                    {
                        Id = item.RowId,
                        Name = item.Name,
                        Singular = item.Singular,
                        Plural = item.Plural,
                        Icon = item.Icon
                    });
            }
    }
}

public static partial class Extension
{
    public static IEnumerable<Item> SearchItem(this IEnumerable<Item> items, string name)
    {
        var segments = name.Split(' ');
        while (segments.Length > 0)
        {
            var segment = segments[0];
            items = items.Where(t => t.Name.Contains(segment, StringComparison.InvariantCultureIgnoreCase));
            segments = segments.Skip(1).ToArray();
        }

        return items;
    }
}