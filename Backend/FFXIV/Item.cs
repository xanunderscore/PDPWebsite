using System.Runtime.InteropServices;
using Lumina.Data.Files;
using SkiaSharp;

namespace PDPWebsite.FFXIV;

public class Item
{
    public uint Id;
    public string Singular;
    public string Plural;
    public string Name;
    public ushort Icon;

    private GameClient _gameData;

    public Item(GameClient gameClient)
    {
        _gameData = gameClient;
    }

    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
    private const string IconHDFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";
    
    public unsafe SKBitmap? GetIconTexture()
    {
        var texFile = _gameData.GetTexFile(string.Format(IconHDFileFormat, Icon / 1000, string.Empty, Icon));
        if (texFile == default(TexFile))
        {
            texFile = _gameData.GetTexFile(string.Format(IconFileFormat, Icon / 1000, string.Empty, Icon));
        }
        if (texFile == null) return null;
        fixed (byte* p = texFile.ImageData)
        {
            var ptr = (nint)p;
            var bmp = new SKBitmap();
            bmp.InstallPixels(new SKImageInfo(texFile.Header.Width, texFile.Header.Height, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SKColorType.Bgra8888 : SKColorType.Rgba8888), ptr, texFile.Header.Width * 4);
            return bmp;
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