using Microsoft.Extensions.DependencyInjection;

namespace MusicScoreManager;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

    protected override async void OnStart()
    {
        base.OnStart();
        _ = WarmupAssetsAsync();
        await CheckAutoBackupAsync();
    }

    private static async Task WarmupAssetsAsync()
    {
        try
        {
            string cacheDir = FileSystem.CacheDirectory;
            string pdfjsDir = Path.Combine(cacheDir, "pdfjs");
            if (!Directory.Exists(pdfjsDir)) Directory.CreateDirectory(pdfjsDir);

            string[] files = { "pdf.min.js", "pdf.worker.min.js", "viewer.html" };
            foreach (var file in files)
            {
                string dest = Path.Combine(pdfjsDir, file);
                if (!File.Exists(dest))
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync($"pdfjs/{file}");
                    using var fileStream = File.Create(dest);
                    await stream.CopyToAsync(fileStream);
                }
            }
        }
        catch { }
    }

    private async Task CheckAutoBackupAsync()
    {
        try
        {
            var lastBackup = Preferences.Default.Get("LastBackupDate", DateTime.MinValue);
            var interval = Preferences.Default.Get("BackupIntervalDays", 30);

            if ((DateTime.Now - lastBackup).TotalDays >= interval)
            {
                var dbService = new Services.DatabaseService();
                await dbService.BackupDatabaseAsync();
                
                var maxKeep = Preferences.Default.Get("MaxBackupFiles", 6);
                dbService.PurgeBackups(maxKeep);
                
                Preferences.Default.Set("LastBackupDate", DateTime.Now);
            }
        }
        catch
        {
            // On ne bloque pas le lancement de l'app si la backup échoue
        }
    }
}