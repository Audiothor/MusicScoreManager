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
        await CheckAutoBackupAsync();
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