using MusicScoreManager.Models;

namespace MusicScoreManager.Services
{
    public class ImportService
    {
        private readonly DatabaseService _databaseService;

        public ImportService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<Score?> ImportScoreAsync()
        {
            try
            {
                var status = await CheckAndRequestStoragePermissionAsync();
                if (status != PermissionStatus.Granted)
                {
                    // Permission refusée
                    return null;
                }

                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.image", "com.adobe.pdf" } },
                        { DevicePlatform.Android, new[] { "image/*", "application/pdf" } },
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".pdf" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.image", "com.adobe.pdf" } },
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Sélectionnez une partition (PDF ou Image)",
                    FileTypes = customFileType,
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    // Copie en local pour sécuriser l'accès (on remplace les espaces pour éviter des bugs de chemin)
                    var sanitizedFileName = result.FileName.Replace(" ", "_");
                    var newFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
                    var localFilePath = Path.Combine(FileSystem.AppDataDirectory, newFileName);

                    using var stream = await result.OpenReadAsync();
                    using var fileStream = File.Create(localFilePath);
                    await stream.CopyToAsync(fileStream);

                    var ext = Path.GetExtension(result.FileName).ToLowerInvariant();
                    var type = ext == ".pdf" ? ScoreType.PDF : ScoreType.Image;

                    var score = new Score
                    {
                        Title = Path.GetFileNameWithoutExtension(result.FileName),
                        FilePath = localFilePath,
                        Type = type,
                        DateAdded = DateTime.Now
                    };

                    await _databaseService.SaveScoreAsync(score);
                    return score;
                }
            }
            catch (Exception)
            {
                // Utilisateur a annulé ou erreur système
            }

            return null;
        }

        private async Task<PermissionStatus> CheckAndRequestStoragePermissionAsync()
        {
            if (DeviceInfo.Platform != DevicePlatform.Android)
                return PermissionStatus.Granted;

            if (DeviceInfo.Version.Major >= 13)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Photos>();
                if (status == PermissionStatus.Granted)
                    return status;
                return await Permissions.RequestAsync<Permissions.Photos>();
            }
            else
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                if (status == PermissionStatus.Granted)
                    return status;
                return await Permissions.RequestAsync<Permissions.StorageRead>();
            }
        }
    }
}
