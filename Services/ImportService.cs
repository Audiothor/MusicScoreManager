using MusicScoreManager.Models;
using System.Text.RegularExpressions;

namespace MusicScoreManager.Services
{
    public class ImportService
    {
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;
        private readonly PdfService _pdfService;

        public ImportService(DatabaseService databaseService, PdfService? pdfService = null)
        {
            _databaseService = databaseService;
            _settingsService = new SettingsService();
            _pdfService = pdfService ?? new PdfService();
        }

        public async Task<Score?> ImportScoreAsync()
        {
            var results = await ImportScoresAsync();
            return results.FirstOrDefault();
        }

        public async Task<List<Score>> ImportScoresAsync()
        {
            var importedScores = new List<Score>();
            try
            {
                // Sur Android, FilePicker utilise le sélecteur système SAF qui n'exige pas de permission préalable.
                try
                {
                    await CheckAndRequestStoragePermissionAsync();
                }
                catch { /* Ne pas bloquer si les permissions optionnelles échouent */ }

                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.image", "com.adobe.pdf" } },
                        { DevicePlatform.Android, new[] { "image/*", "application/pdf" } },
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".pdf" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.image", "com.adobe.pdf" } },
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Sélectionnez une ou plusieurs partitions (PDF ou Image)",
                    FileTypes = customFileType,
                };

                var results = await FilePicker.Default.PickMultipleAsync(options);
                if (results == null || !results.Any())
                {
                    return importedScores;
                }

                var rootDir = _settingsService.ScoresRootDirectory;
                if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

                var cacheDir = FileSystem.CacheDirectory;
                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

                var validResults = results.Where(r => r != null && (!string.IsNullOrEmpty(r.FullPath) || !string.IsNullOrEmpty(r.FileName))).Cast<FileResult>().ToList();
                if (!validResults.Any()) return importedScores;

                var imageFiles = validResults.Where(r => IsImageFile(r.FileName ?? r.FullPath)).ToList();
                var pdfFiles = validResults.Where(r => IsPdfFile(r.FileName ?? r.FullPath)).ToList();

                // Information conviviale pour les fichiers de type image
                if (imageFiles.Any())
                {
                    var fileNames = imageFiles
                        .Select(f => Path.GetFileName(f.FileName ?? f.FullPath))
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToList();

                    string fileListStr = string.Join("\n• ", fileNames.Take(8));
                    if (fileNames.Count > 8)
                    {
                        fileListStr += $"\n... et {fileNames.Count - 8} autre(s)";
                    }

                    string alertTitle = "Importation d'images";
                    string alertMessage = (imageFiles.Count == 1)
                        ? $"Le fichier sélectionné est une image :\n• {fileListStr}\n\nIl va être converti au format PDF pour être intégré à votre bibliothèque.\n\nSouhaitez-vous continuer ?"
                        : $"Les {imageFiles.Count} fichiers sélectionnés sont des images :\n• {fileListStr}\n\nIls vont être convertis au format PDF pour être intégrés à votre bibliothèque.\n\nSouhaitez-vous continuer ?";

                    bool acceptConversion = await Shell.Current.DisplayAlertAsync(
                        alertTitle,
                        alertMessage,
                        "Continuer",
                        "Annuler");

                    if (!acceptConversion)
                    {
                        // L'utilisateur refuse : on ignore les images
                        imageFiles.Clear();
                    }
                }

                // Cas 1 : Plusieurs images acceptées -> Proposer de fusionner en un unique PDF ou partitions individuelles
                if (imageFiles.Count > 1)
                {
                    string action = await Shell.Current.DisplayActionSheetAsync(
                        $"Sélection de {imageFiles.Count} images",
                        "Annuler",
                        null,
                        "📑 Fusionner en 1 seule partition PDF multi-pages (Conseillé)",
                        "📄 Convertir en partitions individuelles (PDF)");

                    if (action == "Annuler" || string.IsNullOrEmpty(action))
                    {
                        // L'utilisateur annule le traitement des images
                        imageFiles.Clear();
                    }
                    else if (action.StartsWith("📑"))
                    {
                        // Fusionner en un unique PDF
                        var firstFileName = Path.GetFileNameWithoutExtension(imageFiles[0].FileName ?? imageFiles[0].FullPath);
                        var suggestedTitle = Regex.Replace(firstFileName, @"[_-]?\d+$", "").Trim();
                        if (string.IsNullOrEmpty(suggestedTitle)) suggestedTitle = firstFileName;
                        if (string.IsNullOrWhiteSpace(suggestedTitle)) suggestedTitle = "Partition";

                        string? inputTitle = await Shell.Current.DisplayPromptAsync(
                            "Titre de la partition",
                            "Entrez le titre de la nouvelle partition PDF :",
                            initialValue: suggestedTitle,
                            accept: "Créer",
                            cancel: "Annuler");

                        if (inputTitle != null) // non annulé
                        {
                            var finalTitle = string.IsNullOrWhiteSpace(inputTitle) ? suggestedTitle : inputTitle.Trim();
                            var sortedImages = imageFiles.OrderBy(f => f.FileName ?? f.FullPath, new NaturalComparer()).ToList();
                            var tempPaths = new List<string>();

                            try
                            {
                                foreach (var img in sortedImages)
                                {
                                    var ext = Path.GetExtension(img.FileName ?? img.FullPath);
                                    if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                                    var cleanExt = new string(ext.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());
                                    if (string.IsNullOrWhiteSpace(cleanExt)) cleanExt = ".jpg";

                                    var tempPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}{cleanExt}");
                                    using (var srcStream = await img.OpenReadAsync())
                                    using (var dstStream = File.Create(tempPath))
                                    {
                                        await srcStream.CopyToAsync(dstStream);
                                    }
                                    tempPaths.Add(tempPath);
                                }

                                var sanitizedTitle = SanitizeFileName(finalTitle);
                                var localPdfPath = GetUniqueFilePath(rootDir, sanitizedTitle, ".pdf");

                                await _pdfService.ConvertImagesToPdfAsync(tempPaths, localPdfPath);

                                var score = new Score
                                {
                                    Title = finalTitle,
                                    FilePath = _settingsService.GetRelativePath(localPdfPath),
                                    Type = ScoreType.PDF,
                                    DateAdded = DateTime.Now
                                };

                                await _databaseService.SaveScoreAsync(score);
                                importedScores.Add(score);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[ImportService] Erreur fusion images : {ex.Message}");
                                await Shell.Current.DisplayAlertAsync("Erreur de conversion", $"Impossible de créer la partition PDF : {ex.Message}", "OK");
                            }
                            finally
                            {
                                foreach (var t in tempPaths)
                                {
                                    try { if (File.Exists(t)) File.Delete(t); } catch { }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Convertir individuellement chaque image en un PDF
                        var converted = await ConvertAndImportIndividualImagesAsync(imageFiles, rootDir);
                        importedScores.AddRange(converted);
                    }
                }
                else if (imageFiles.Count == 1)
                {
                    // 1 seule image acceptée -> conversion en PDF dans la bibliothèque
                    var converted = await ConvertAndImportIndividualImagesAsync(imageFiles, rootDir);
                    importedScores.AddRange(converted);
                }

                // Traiter les éventuels fichiers PDF (qu'ils soient seuls ou accompagnés d'images)
                if (pdfFiles.Any())
                {
                    var pdfImported = await ProcessPdfFilesAsync(pdfFiles, rootDir);
                    importedScores.AddRange(pdfImported);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImportService] Erreur lors de l'import : {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Erreur d'import", $"Une erreur est survenue lors de l'import : {ex.Message}", "OK");
            }

            return importedScores;
        }

        private async Task<List<Score>> ConvertAndImportIndividualImagesAsync(List<FileResult> imageFiles, string rootDir)
        {
            var results = new List<Score>();
            var cacheDir = FileSystem.CacheDirectory;
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            foreach (var img in imageFiles)
            {
                var ext = Path.GetExtension(img.FileName ?? img.FullPath);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                var cleanExt = new string(ext.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());
                if (string.IsNullOrWhiteSpace(cleanExt)) cleanExt = ".jpg";

                var tempPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}{cleanExt}");
                try
                {
                    using (var src = await img.OpenReadAsync())
                    using (var dst = File.Create(tempPath))
                    {
                        await src.CopyToAsync(dst);
                    }

                    var rawName = Path.GetFileNameWithoutExtension(img.FileName ?? img.FullPath);
                    if (string.IsNullOrWhiteSpace(rawName)) rawName = "Partition";

                    var sanitized = SanitizeFileName(rawName);
                    var localPdfPath = GetUniqueFilePath(rootDir, sanitized, ".pdf");

                    await _pdfService.ConvertImagesToPdfAsync(new[] { tempPath }, localPdfPath);

                    var score = new Score
                    {
                        Title = rawName,
                        FilePath = _settingsService.GetRelativePath(localPdfPath),
                        Type = ScoreType.PDF,
                        DateAdded = DateTime.Now
                    };

                    await _databaseService.SaveScoreAsync(score);
                    results.Add(score);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ImportService] Erreur conversion image {img.FileName} : {ex.Message}");
                    await Shell.Current.DisplayAlertAsync("Erreur de conversion", $"Impossible de convertir {Path.GetFileName(img.FileName ?? "l'image")} en PDF : {ex.Message}", "OK");
                }
                finally
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                }
            }

            return results;
        }

        private async Task<List<Score>> ProcessPdfFilesAsync(List<FileResult> pdfFiles, string rootDir)
        {
            var imported = new List<Score>();
            var filesToProcess = new List<(FileResult File, bool AlreadyInRoot)>();

            foreach (var result in pdfFiles)
            {
                var fullPath = result.FullPath;
                bool isAlreadyInRoot = !string.IsNullOrEmpty(rootDir) && fullPath.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase);

                if (!isAlreadyInRoot)
                {
                    try
                    {
                        var fileInfoSource = new FileInfo(fullPath);
                        long sourceLength = fileInfoSource.Length;
                        string targetFileName = result.FileName ?? Path.GetFileName(fullPath);

                        string directPath = Path.Combine(rootDir, targetFileName);
                        if (File.Exists(directPath) && new FileInfo(directPath).Length == sourceLength)
                        {
                            isAlreadyInRoot = true;
                            filesToProcess.Add((new FileResult(directPath), true));
                            continue;
                        }

                        string? foundPath = FindFileRecursively(rootDir, targetFileName, sourceLength);
                        if (foundPath != null)
                        {
                            isAlreadyInRoot = true;
                            filesToProcess.Add((new FileResult(foundPath), true));
                            continue;
                        }
                    }
                    catch { /* Ignore */ }
                }

                filesToProcess.Add((result, isAlreadyInRoot));
            }

            var externalFiles = filesToProcess.Where(f => !f.AlreadyInRoot).ToList();
            string? globalAction = null;

            if (externalFiles.Count > 0)
            {
                if (externalFiles.Count == 1)
                {
                    globalAction = await Shell.Current.DisplayActionSheetAsync(
                        "Organisation de la bibliothèque",
                        "Annuler",
                        null,
                        "Copier vers la bibliothèque (Conseillé)",
                        "Lier le fichier original (Externe)");
                }
                else
                {
                    globalAction = await Shell.Current.DisplayActionSheetAsync(
                        $"Organisation de la bibliothèque ({externalFiles.Count} fichiers)",
                        "Annuler",
                        null,
                        "Copier tous les fichiers vers la bibliothèque (Conseillé)",
                        "Lier tous les fichiers originaux (Externe)",
                        "Choisir au cas par cas");
                }

                if (globalAction == "Annuler" || globalAction == null)
                {
                    return imported;
                }
            }

            for (int i = 0; i < filesToProcess.Count; i++)
            {
                var (fileResult, isAlreadyInRoot) = filesToProcess[i];
                string finalStoredPath;

                if (isAlreadyInRoot)
                {
                    finalStoredPath = _settingsService.GetRelativePath(fileResult.FullPath);
                }
                else
                {
                    string? fileAction = globalAction;

                    if (globalAction == "Choisir au cas par cas")
                    {
                        fileAction = await Shell.Current.DisplayActionSheetAsync(
                            $"Fichier : {fileResult.FileName}",
                            "Passer ce fichier",
                            null,
                            "Copier vers la bibliothèque (Conseillé)",
                            "Lier le fichier original (Externe)");
                    }

                    if (fileAction == "Copier vers la bibliothèque (Conseillé)" || fileAction == "Copier tous les fichiers vers la bibliothèque (Conseillé)")
                    {
                        var sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(fileResult.FileName)) + Path.GetExtension(fileResult.FileName);
                        var localFilePath = GetUniqueFilePath(rootDir, Path.GetFileNameWithoutExtension(sanitizedFileName), Path.GetExtension(sanitizedFileName));

                        using var stream = await fileResult.OpenReadAsync();
                        using var fileStream = File.Create(localFilePath);
                        await stream.CopyToAsync(fileStream);

                        finalStoredPath = _settingsService.GetRelativePath(localFilePath);
                    }
                    else if (fileAction == "Lier le fichier original (Externe)" || fileAction == "Lier tous les fichiers originaux (Externe)")
                    {
                        finalStoredPath = fileResult.FullPath;
                    }
                    else
                    {
                        continue;
                    }
                }

                var score = new Score
                {
                    Title = Path.GetFileNameWithoutExtension(fileResult.FileName ?? fileResult.FullPath),
                    FilePath = finalStoredPath,
                    Type = ScoreType.PDF,
                    DateAdded = DateTime.Now
                };

                await _databaseService.SaveScoreAsync(score);
                imported.Add(score);
            }

            return imported;
        }

        private static bool IsImageFile(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" || ext == ".bmp" || ext == ".gif";
        }

        private static bool IsPdfFile(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
            return ext == ".pdf";
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            return clean.Replace(" ", "_");
        }

        private static string GetUniqueFilePath(string directory, string baseName, string extension)
        {
            var localPath = Path.Combine(directory, $"{baseName}{extension}");
            int counter = 1;
            while (File.Exists(localPath))
            {
                localPath = Path.Combine(directory, $"{baseName}_{counter}{extension}");
                counter++;
            }
            return localPath;
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

        private string? FindFileRecursively(string dir, string fileName, long size)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    if (Path.GetFileName(file).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (new FileInfo(file).Length == size) return file;
                        }
                        catch { }
                    }
                }

                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    var found = FindFileRecursively(subDir, fileName, size);
                    if (found != null) return found;
                }
            }
            catch { }
            return null;
        }

        private class NaturalComparer : IComparer<string>
        {
            public int Compare(string? x, string? y) => NaturalStringComparer.Compare(x, y);
        }
    }
}
