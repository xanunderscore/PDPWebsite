using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;
using PDPWebsite.FFXIV;

namespace PDPWebsite.Services;

public class GameClient : IDisposable
{
    private readonly UniversalisClient _client;
    private readonly GameData _gameData;

    private readonly List<Item> _marketItems = new();

    public GameClient(UniversalisClient client)
    {
#if DEBUG
        var gameDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "SquareEnix", "FINAL FANTASY XIV - A Realm Reborn", "game", "sqpack");
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
        _client = client;
        LoadMarket().GetAwaiter().GetResult();
    }

    public IReadOnlyList<Item> MarketItems => _marketItems;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private async Task LoadMarket()
    {
        var ids = await _client.GetMarketItems();

        var items = _gameData.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Item>(Language.English);
        if (items != null)
            foreach (var item in items)
                if (ids.Contains(item.RowId))
                    _marketItems.Add(new Item(this)
                    {
                        Id = item.RowId,
                        Name = item.Name,
                        Singular = item.Singular,
                        Plural = item.Plural,
                        Icon = item.Icon
                    });
    }

    public TexFile? GetTexFile(string path)
    {
        return _gameData.GetFile<TexFile>(path);
    }

    public ExcelSheet<T>? GetSheet<T>() where T : ExcelRow
    {
        return _gameData.GetExcelSheet<T>();
    }
}
