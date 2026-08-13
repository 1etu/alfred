using System.Text.Json;
using Alfred.Core.Items;
using Alfred.Core.Ledger;

namespace Alfred.Core.Storage;

public sealed class VaultData
{
    public List<LedgerEntry> Entries { get; set; } = [];

    public List<Todo> Todos { get; set; } = [];

    public List<Reminder> Reminders { get; set; } = [];

    public List<Plan> Plans { get; set; } = [];

    public List<Meal> Meals { get; set; } = [];

    public List<WishItem> Wishes { get; set; } = [];

    public List<BoardCard> Cards { get; set; } = [];

    public List<TrashEntry> Trash { get; set; } = [];
}

public sealed class Vault
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _path;

    public Vault(string path)
    {
        _path = path;
        Data = Read(path);
    }

    public VaultData Data { get; }

    public event EventHandler? Changed;

    public void Save()
    {
        string? directory = Path.GetDirectoryName(_path);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        string staging = _path + ".tmp";
        File.WriteAllText(staging, JsonSerializer.Serialize(Data, SerializerOptions));
        File.Move(staging, _path, overwrite: true);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static VaultData Read(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new VaultData();
            }

            return JsonSerializer.Deserialize<VaultData>(File.ReadAllText(path)) ?? new VaultData();
        }
        catch (Exception failure) when (failure is IOException or JsonException or UnauthorizedAccessException)
        {
            return new VaultData();
        }
    }
}
