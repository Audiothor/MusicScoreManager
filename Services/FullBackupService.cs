using System.IO.Compression;
using System.Text.Json;
using MusicScoreManager.Models;

namespace MusicScoreManager.Services;

public class FullBackupProgressInfo
{
    public double Progress { get; set; } // 0.0 to 1.0
    public string StatusMessage { get; set; } = string.Empty;
}

public class FullBackupResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCancelled { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int ScoresFilesCount { get; set; }
    public int AudioFilesCount { get; set; }
}

public class FullRestoreResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCancelled { get; set; }
    public int RestoredScoresCount { get; set; }
    public int RestoredAudioFilesCount { get; set; }
}

public class FullBackupManifest
{
    public string AppName { get; set; } = "MusicScoreManager";
    public string BackupVersion { get; set; } = "1.0";
    public string AppVersion { get; set; } = "3.1.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int ScoresFilesCount { get; set; }
    public int AudioFilesCount { get; set; }
}

public class FullBackupService
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;

    public FullBackupService(DatabaseService databaseService, SettingsService settingsService)
    {
        _databaseService = databaseService;
        _settingsService = settingsService;
    }

    public async Task<FullBackupResult> CreateFullBackupAsync(
        string destinationFolder,
        IProgress<FullBackupProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        var result = new FullBackupResult();
        string tempZipPath = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(destinationFolder))
            {
                destinationFolder = _settingsService.ExportsRootDirectory;
            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string zipFileName = $"MusicScoreManager_FullBackup_{timestamp}.zip";
            string finalZipPath = Path.Combine(destinationFolder, zipFileName);
            tempZipPath = finalZipPath + ".tmp";

            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            progress.Report(new FullBackupProgressInfo
            {
                Progress = 0.02,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Preparing", "Préparation de la sauvegarde...")
            });

            // Sauvegarder l'état actuel de la base de données (WAL Checkpoint)
            await _databaseService.CheckpointAsync();
            cancellationToken.ThrowIfCancellationRequested();

            string dbPath = _databaseService.DatabasePath;
            string scoresDir = _settingsService.ScoresRootDirectory;
            string audioDir = _settingsService.AudioRootDirectory;

            // Recensement des fichiers à sauvegarder
            var scoreFiles = Directory.Exists(scoresDir)
                ? Directory.GetFiles(scoresDir, "*.*", SearchOption.AllDirectories)
                : Array.Empty<string>();

            var audioFiles = Directory.Exists(audioDir)
                ? Directory.GetFiles(audioDir, "*.*", SearchOption.AllDirectories)
                : Array.Empty<string>();

            result.ScoresFilesCount = scoreFiles.Length;
            result.AudioFilesCount = audioFiles.Length;

            int totalItems = 1 + scoreFiles.Length + audioFiles.Length;
            int processedItems = 0;

            using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                // 1. Manifest
                var manifest = new FullBackupManifest
                {
                    ScoresFilesCount = scoreFiles.Length,
                    AudioFilesCount = audioFiles.Length,
                    CreatedAt = DateTime.UtcNow
                };
                var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
                using (var manifestStream = manifestEntry.Open())
                {
                    await JsonSerializer.SerializeAsync(manifestStream, manifest, cancellationToken: cancellationToken);
                }

                // 2. Base de données SQLite
                progress.Report(new FullBackupProgressInfo
                {
                    Progress = 0.05,
                    StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Db", "Sauvegarde de la base de données...")
                });

                if (File.Exists(dbPath))
                {
                    var dbEntry = archive.CreateEntry("database/scores.db3", CompressionLevel.Optimal);
                    using var dbEntryStream = dbEntry.Open();
                    using var dbFileStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    await dbFileStream.CopyToAsync(dbEntryStream, cancellationToken);
                }
                processedItems++;

                // Fichiers WAL / SHM éventuels
                string walPath = dbPath + "-wal";
                if (File.Exists(walPath))
                {
                    var walEntry = archive.CreateEntry("database/scores.db3-wal", CompressionLevel.Optimal);
                    using var walEntryStream = walEntry.Open();
                    using var walFileStream = new FileStream(walPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    await walFileStream.CopyToAsync(walEntryStream, cancellationToken);
                }

                // 3. Fichiers de partitions
                for (int i = 0; i < scoreFiles.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string sFile = scoreFiles[i];
                    string relativePath = Path.GetRelativePath(scoresDir, sFile).Replace('\\', '/');
                    string entryName = $"scores/{relativePath}";

                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var srcStream = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await srcStream.CopyToAsync(entryStream, cancellationToken);

                    processedItems++;
                    double progressPct = 0.05 + ((double)processedItems / totalItems) * 0.90;
                    progress.Report(new FullBackupProgressInfo
                    {
                        Progress = Math.Min(progressPct, 0.95),
                        StatusMessage = string.Format(
                            LocalizationService.Instance.GetString("Backups_Progress_Scores", "Compression des partitions ({0}/{1})..."),
                            i + 1, scoreFiles.Length)
                    });
                }

                // 4. Fichiers audio
                for (int i = 0; i < audioFiles.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string aFile = audioFiles[i];
                    string relativePath = Path.GetRelativePath(audioDir, aFile).Replace('\\', '/');
                    string entryName = $"audio/{relativePath}";

                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var srcStream = new FileStream(aFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await srcStream.CopyToAsync(entryStream, cancellationToken);

                    processedItems++;
                    double progressPct = 0.05 + ((double)processedItems / totalItems) * 0.90;
                    progress.Report(new FullBackupProgressInfo
                    {
                        Progress = Math.Min(progressPct, 0.95),
                        StatusMessage = string.Format(
                            LocalizationService.Instance.GetString("Backups_Progress_Audio", "Compression des fichiers audio ({0}/{1})..."),
                            i + 1, audioFiles.Length)
                    });
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(new FullBackupProgressInfo
            {
                Progress = 0.98,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Finalizing", "Finalisation du fichier zip...")
            });

            if (File.Exists(finalZipPath))
            {
                File.Delete(finalZipPath);
            }
            File.Move(tempZipPath, finalZipPath);

            var fi = new FileInfo(finalZipPath);
            result.Success = true;
            result.FilePath = finalZipPath;
            result.FileSizeBytes = fi.Length;

            progress.Report(new FullBackupProgressInfo
            {
                Progress = 1.0,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Done", "Sauvegarde terminée !")
            });
        }
        catch (OperationCanceledException)
        {
            result.IsCancelled = true;
            result.Success = false;
            result.ErrorMessage = LocalizationService.Instance.GetString("Backups_Full_Cancelled", "Opération annulée.");
            if (!string.IsNullOrEmpty(tempZipPath) && File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch { }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            if (!string.IsNullOrEmpty(tempZipPath) && File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch { }
            }
        }

        return result;
    }

    public async Task<FullRestoreResult> RestoreFullBackupAsync(
        string zipFilePath,
        IProgress<FullBackupProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        var result = new FullRestoreResult();
        string stagingDir = Path.Combine(FileSystem.CacheDirectory, $"msm_restore_{Guid.NewGuid():N}");

        try
        {
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException("Le fichier d'archive spécifié est introuvable.", zipFilePath);
            }

            progress.Report(new FullBackupProgressInfo
            {
                Progress = 0.05,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Verifying", "Vérification de l'archive...")
            });

            // 1. Analyse préalable de l'archive ZIP
            using (var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false))
            {
                var dbEntry = archive.Entries.FirstOrDefault(e =>
                    e.FullName.Equals("database/scores.db3", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.Equals("scores.db3", StringComparison.OrdinalIgnoreCase));

                if (dbEntry == null)
                {
                    throw new InvalidOperationException("Cette archive ne contient aucune base de données Music Score Manager valide.");
                }

                int totalEntries = archive.Entries.Count;
                int extractedCount = 0;

                Directory.CreateDirectory(stagingDir);

                // Extraction dans le dossier temporaire
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        // Répertoire
                        continue;
                    }

                    string destinationPath = Path.GetFullPath(Path.Combine(stagingDir, entry.FullName));
                    string stagingDirFull = Path.GetFullPath(stagingDir);

                    // Sécurité anti Zip-Slip
                    if (!destinationPath.StartsWith(stagingDirFull, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string? parentDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                    extractedCount++;

                    double progressPct = 0.05 + ((double)extractedCount / Math.Max(1, totalEntries)) * 0.55;
                    progress.Report(new FullBackupProgressInfo
                    {
                        Progress = Math.Min(progressPct, 0.60),
                        StatusMessage = string.Format(
                            LocalizationService.Instance.GetString("Backups_Progress_Extracting", "Extraction de l'archive ({0}/{1})..."),
                            extractedCount, totalEntries)
                    });
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 2. Vérification des fichiers extraits
            string stagedDb = Path.Combine(stagingDir, "database", "scores.db3");
            if (!File.Exists(stagedDb))
            {
                stagedDb = Path.Combine(stagingDir, "scores.db3");
            }

            if (!File.Exists(stagedDb) || new FileInfo(stagedDb).Length == 0)
            {
                throw new InvalidOperationException("La base de données extraite est introuvable ou vide.");
            }

            // 3. Fermeture de la base de données active
            progress.Report(new FullBackupProgressInfo
            {
                Progress = 0.65,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Closing_Db", "Fermeture de la base de données en cours...")
            });

            await _databaseService.CloseConnectionAsync();

            // 4. Remplacement intégral de la base de données (Erase / Replace)
            progress.Report(new FullBackupProgressInfo
            {
                Progress = 0.72,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Replacing_Db", "Restauration de la base de données...")
            });

            string currentDbPath = _databaseService.DatabasePath;
            if (File.Exists(currentDbPath))
            {
                File.Delete(currentDbPath);
            }
            string currentWal = currentDbPath + "-wal";
            if (File.Exists(currentWal))
            {
                try { File.Delete(currentWal); } catch { }
            }
            string currentShm = currentDbPath + "-shm";
            if (File.Exists(currentShm))
            {
                try { File.Delete(currentShm); } catch { }
            }

            File.Copy(stagedDb, currentDbPath, true);

            string stagedWal = Path.Combine(stagingDir, "database", "scores.db3-wal");
            if (File.Exists(stagedWal))
            {
                File.Copy(stagedWal, currentWal, true);
            }

            // 5. Remplacement intégral des partitions (Erase / Replace)
            progress.Report(new FullBackupProgressInfo
            {
                Progress = 0.80,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Replacing_Scores", "Remplacement des partitions et dossiers...")
            });

            string targetScoresDir = _settingsService.ScoresRootDirectory;
            if (Directory.Exists(targetScoresDir))
            {
                ClearDirectoryContents(targetScoresDir);
            }
            else
            {
                Directory.CreateDirectory(targetScoresDir);
            }

            string stagedScoresDir = Path.Combine(stagingDir, "scores");
            int restoredScoresCount = 0;
            if (Directory.Exists(stagedScoresDir))
            {
                CopyDirectoryRecursive(stagedScoresDir, targetScoresDir);
                restoredScoresCount = Directory.GetFiles(targetScoresDir, "*.*", SearchOption.AllDirectories).Length;
            }

            // 6. Remplacement intégral des fichiers audio (Erase / Replace)
            progress.Report(new FullBackupProgressInfo
            {
                Progress = 0.90,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Replacing_Audio", "Remplacement des fichiers audio...")
            });

            string targetAudioDir = _settingsService.AudioRootDirectory;
            if (Directory.Exists(targetAudioDir))
            {
                ClearDirectoryContents(targetAudioDir);
            }
            else
            {
                Directory.CreateDirectory(targetAudioDir);
            }

            string stagedAudioDir = Path.Combine(stagingDir, "audio");
            int restoredAudioCount = 0;
            if (Directory.Exists(stagedAudioDir))
            {
                CopyDirectoryRecursive(stagedAudioDir, targetAudioDir);
                restoredAudioCount = Directory.GetFiles(targetAudioDir, "*.*", SearchOption.AllDirectories).Length;
            }

            // 7. Nettoyage du dossier temporaire de staging
            try
            {
                if (Directory.Exists(stagingDir))
                {
                    Directory.Delete(stagingDir, true);
                }
            }
            catch { }

            // 8. Reconnexion et vérification de la base de données
            progress.Report(new FullBackupProgressInfo
            {
                Progress = 0.96,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Verifying_Db", "Vérification de l'intégrité de la base restaurée...")
            });

            int dbScoresCount = await _databaseService.VerifyDatabaseAsync();

            result.Success = true;
            result.RestoredScoresCount = dbScoresCount;
            result.RestoredAudioFilesCount = restoredAudioCount;

            progress.Report(new FullBackupProgressInfo
            {
                Progress = 1.0,
                StatusMessage = LocalizationService.Instance.GetString("Backups_Progress_Done", "Restauration terminée !")
            });
        }
        catch (OperationCanceledException)
        {
            result.IsCancelled = true;
            result.Success = false;
            result.ErrorMessage = LocalizationService.Instance.GetString("Backups_Full_Cancelled", "Opération annulée.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            if (Directory.Exists(stagingDir))
            {
                try { Directory.Delete(stagingDir, true); } catch { }
            }
        }

        return result;
    }

    private static void ClearDirectoryContents(string dirPath)
    {
        var dirInfo = new DirectoryInfo(dirPath);
        foreach (var file in dirInfo.GetFiles())
        {
            try { file.Delete(); } catch { }
        }
        foreach (var subDir in dirInfo.GetDirectories())
        {
            try { subDir.Delete(true); } catch { }
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
            CopyDirectoryRecursive(subDir, destSubDir);
        }
    }
}
