using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MusicScoreManager.Models;

namespace MusicScoreManager.Services
{
    public class ExportOptions
    {
        public bool IncludeAnnotations { get; set; } = true;
        public bool IncludeAudio { get; set; } = true;
    }

    public class SetlistExportPackage
    {
        public string Version { get; set; } = "1.9.0";
        public SetlistTransferMetadata Setlist { get; set; } = new();
    }

    public class ScoresExportPackage
    {
        public string Version { get; set; } = "1.9.0";
        public List<ScoreTransferMetadata> Scores { get; set; } = new();
    }

    public class ExportImportService
    {
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;

        public ExportImportService(DatabaseService databaseService, SettingsService settingsService)
        {
            _databaseService = databaseService;
            _settingsService = settingsService;
        }

        #region Préparation des métadonnées

        public async Task<SetlistTransferMetadata> BuildSetlistMetadataAsync(Setlist setlist, ExportOptions options)
        {
            var meta = new SetlistTransferMetadata
            {
                Name = setlist.Name,
                DateCreated = setlist.DateCreated,
                ConcertDate = setlist.ConcertDate,
                ConcertTime = setlist.ConcertTime,
                Status = setlist.Status,
                IsLocked = setlist.IsLocked,
                IsContinuousReading = setlist.IsContinuousReading
            };

            var setlistScores = await _databaseService.GetScoresForSetlistAsync(setlist.Id);
            foreach (var score in setlistScores)
            {
                var scoreMeta = await BuildScoreMetadataAsync(score, options);
                meta.Scores.Add(scoreMeta);
            }

            return meta;
        }

        public async Task<ScoreTransferMetadata> BuildScoreMetadataAsync(Score score, ExportOptions options)
        {
            var tags = await _databaseService.GetTagsForScoreAsync(score.Id);
            var annotations = options.IncludeAnnotations 
                ? await _databaseService.GetAnnotationsForScoreAsync(score.Id) 
                : new List<Annotation>();

            var scoreMeta = new ScoreTransferMetadata
            {
                Title = score.Title,
                FileName = Path.GetFileName(score.FilePath),
                Type = score.Type,
                Rotation = score.Rotation,
                IsRotationSaved = score.IsRotationSaved,
                ShowMetronome = score.ShowMetronome,
                BPM = score.BPM,
                HasMetronomeSound = score.HasMetronomeSound,
                ShowAudioPlayer = score.ShowAudioPlayer,
                PreCountMeasures = score.PreCountMeasures,
                Tags = tags.Select(t => t.Name).ToList(),
                TagColors = tags.Select(t => t.ColorHex).ToList(),
                Annotations = annotations.Select(a => new AnnotationMetadata
                {
                    Type = a.Type,
                    Category = a.Category,
                    Content = a.Content,
                    X = a.X,
                    Y = a.Y,
                    Scale = a.Scale,
                    Color = a.Color,
                    BackgroundColor = a.BackgroundColor,
                    PageNumber = a.PageNumber
                }).ToList()
            };

            if (options.IncludeAudio && score.AudioFiles != null && score.AudioFiles.Any())
            {
                foreach (var af in score.AudioFiles)
                {
                    string audioPath = _settingsService.GetAbsolutePath(af.FilePath, isAudio: true);
                    if (File.Exists(audioPath))
                    {
                        string audioName = Path.GetFileName(audioPath);
                        if (!scoreMeta.AudioFiles.Contains(audioName))
                        {
                            scoreMeta.AudioFiles.Add(audioName);
                        }
                    }
                }
            }

            return scoreMeta;
        }

        #endregion

        #region Génération de paquets Bluetooth

        public async Task<List<BluetoothFilePayload>> BuildBluetoothPayloadForSetlistAsync(Setlist setlist, ExportOptions options)
        {
            var payloads = new List<BluetoothFilePayload>();
            var setlistMeta = await BuildSetlistMetadataAsync(setlist, options);

            // Manifest JSON
            string json = JsonSerializer.Serialize(setlistMeta, new JsonSerializerOptions { WriteIndented = true });
            payloads.Add(new BluetoothFilePayload
            {
                FileName = "setlist_manifest.json",
                Data = System.Text.Encoding.UTF8.GetBytes(json)
            });

            // Fichiers physiques de partitions et audio
            var setlistScores = await _databaseService.GetScoresForSetlistAsync(setlist.Id);
            foreach (var score in setlistScores)
            {
                string scorePath = _settingsService.GetAbsolutePath(score.FilePath);
                if (File.Exists(scorePath))
                {
                    string fname = Path.GetFileName(scorePath);
                    if (!payloads.Any(p => p.FileName == fname))
                    {
                        byte[] scoreBytes = await File.ReadAllBytesAsync(scorePath);
                        payloads.Add(new BluetoothFilePayload
                        {
                            FileName = fname,
                            Data = scoreBytes
                        });
                    }
                }

                if (options.IncludeAudio && score.AudioFiles != null && score.AudioFiles.Any())
                {
                    foreach (var af in score.AudioFiles)
                    {
                        string audioPath = _settingsService.GetAbsolutePath(af.FilePath, isAudio: true);
                        if (File.Exists(audioPath))
                        {
                            string fname = Path.GetFileName(audioPath);
                            if (!payloads.Any(p => p.FileName == fname))
                            {
                                byte[] audioBytes = await File.ReadAllBytesAsync(audioPath);
                                payloads.Add(new BluetoothFilePayload
                                {
                                    FileName = fname,
                                    Data = audioBytes
                                });
                            }
                        }
                    }
                }
            }

            return payloads;
        }

        public async Task<List<BluetoothFilePayload>> BuildBluetoothPayloadForScoresAsync(List<Score> scores, ExportOptions options)
        {
            var payloads = new List<BluetoothFilePayload>();
            var scoresMeta = new List<ScoreTransferMetadata>();

            foreach (var score in scores)
            {
                var scoreMeta = await BuildScoreMetadataAsync(score, options);
                scoresMeta.Add(scoreMeta);

                string scorePath = _settingsService.GetAbsolutePath(score.FilePath);
                if (File.Exists(scorePath))
                {
                    string fname = Path.GetFileName(scorePath);
                    if (!payloads.Any(p => p.FileName == fname))
                    {
                        byte[] scoreBytes = await File.ReadAllBytesAsync(scorePath);
                        payloads.Add(new BluetoothFilePayload
                        {
                            FileName = fname,
                            Data = scoreBytes
                        });
                    }
                }

                if (options.IncludeAudio && score.AudioFiles != null && score.AudioFiles.Any())
                {
                    foreach (var af in score.AudioFiles)
                    {
                        string audioPath = _settingsService.GetAbsolutePath(af.FilePath, isAudio: true);
                        if (File.Exists(audioPath))
                        {
                            string fname = Path.GetFileName(audioPath);
                            if (!payloads.Any(p => p.FileName == fname))
                            {
                                byte[] audioBytes = await File.ReadAllBytesAsync(audioPath);
                                payloads.Add(new BluetoothFilePayload
                                {
                                    FileName = fname,
                                    Data = audioBytes
                                });
                            }
                        }
                    }
                }
            }

            string json = JsonSerializer.Serialize(scoresMeta, new JsonSerializerOptions { WriteIndented = true });
            payloads.Insert(0, new BluetoothFilePayload
            {
                FileName = "scores_manifest.json",
                Data = System.Text.Encoding.UTF8.GetBytes(json)
            });

            return payloads;
        }

        #endregion

        #region Export Fichier Archive (.msmsetlist, .msmscores)

        public async Task<string> ExportSetlistToFileAsync(Setlist setlist, ExportOptions options)
        {
            string exportsDir = _settingsService.ExportsRootDirectory;
            if (!Directory.Exists(exportsDir)) Directory.CreateDirectory(exportsDir);

            string safeName = string.Join("_", setlist.Name.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"Setlist_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.msmsetlist";
            string zipPath = Path.Combine(exportsDir, fileName);

            var payloads = await BuildBluetoothPayloadForSetlistAsync(setlist, options);

            using (var zipStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var item in payloads)
                {
                    var entry = archive.CreateEntry(item.FileName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(item.Data, 0, item.Data.Length);
                }
            }

            return zipPath;
        }

        public async Task<string> ExportScoresToFileAsync(List<Score> scores, ExportOptions options)
        {
            string exportsDir = _settingsService.ExportsRootDirectory;
            if (!Directory.Exists(exportsDir)) Directory.CreateDirectory(exportsDir);

            string fileName = scores.Count == 1
                ? $"Score_{string.Join("_", scores[0].Title.Split(Path.GetInvalidFileNameChars()))}_{DateTime.Now:yyyyMMdd_HHmmss}.msmscore"
                : $"Scores_Bundle_{scores.Count}_Items_{DateTime.Now:yyyyMMdd_HHmmss}.msmscores";

            string zipPath = Path.Combine(exportsDir, fileName);
            var payloads = await BuildBluetoothPayloadForScoresAsync(scores, options);

            using (var zipStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var item in payloads)
                {
                    var entry = archive.CreateEntry(item.FileName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(item.Data, 0, item.Data.Length);
                }
            }

            return zipPath;
        }

        #endregion

        #region Import depuis Payload ou Archive

        public async Task<bool> ImportPackageFromPayloadsAsync(List<BluetoothFilePayload> payloads)
        {
            try
            {
                var setlistManifestPayload = payloads.FirstOrDefault(p => p.FileName == "setlist_manifest.json");
                if (setlistManifestPayload != null)
                {
                    string json = System.Text.Encoding.UTF8.GetString(setlistManifestPayload.Data);
                    var setlistMeta = JsonSerializer.Deserialize<SetlistTransferMetadata>(json);
                    if (setlistMeta != null)
                    {
                        return await ImportSetlistWithScoresAsync(setlistMeta, payloads);
                    }
                }

                var scoresManifestPayload = payloads.FirstOrDefault(p => p.FileName == "scores_manifest.json");
                if (scoresManifestPayload != null)
                {
                    string json = System.Text.Encoding.UTF8.GetString(scoresManifestPayload.Data);
                    var scoresMetaList = JsonSerializer.Deserialize<List<ScoreTransferMetadata>>(json);
                    if (scoresMetaList != null)
                    {
                        return await ImportScoresAsync(scoresMetaList, payloads);
                    }
                }

                // Fallback sans manifest (fichiers bruts)
                return await ImportRawFilesAsync(payloads);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExportImportService] Import error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ImportPackageFromFileAsync(string filePath)
        {
            try
            {
                var payloads = new List<BluetoothFilePayload>();
                using (var zipStream = File.OpenRead(filePath))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        using var entryStream = entry.Open();
                        using var ms = new MemoryStream();
                        await entryStream.CopyToAsync(ms);
                        payloads.Add(new BluetoothFilePayload
                        {
                            FileName = entry.Name,
                            Data = ms.ToArray()
                        });
                    }
                }

                return await ImportPackageFromPayloadsAsync(payloads);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExportImportService] Error importing archive: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ImportSetlistWithScoresAsync(SetlistTransferMetadata setlistMeta, List<BluetoothFilePayload> payloads)
        {
            var savedScores = new List<Score>();
            string scoresRootDir = _settingsService.ScoresRootDirectory;
            string audioRootDir = _settingsService.AudioRootDirectory;

            if (!Directory.Exists(scoresRootDir)) Directory.CreateDirectory(scoresRootDir);
            if (!Directory.Exists(audioRootDir)) Directory.CreateDirectory(audioRootDir);

            foreach (var scoreMeta in setlistMeta.Scores)
            {
                var scorePayload = payloads.FirstOrDefault(p => p.FileName == scoreMeta.FileName);
                if (scorePayload != null)
                {
                    string targetFilePath = Path.Combine(scoresRootDir, scoreMeta.FileName);
                    if (!File.Exists(targetFilePath))
                    {
                        await File.WriteAllBytesAsync(targetFilePath, scorePayload.Data);
                    }

                    // Enregistrer ou récupérer la partition
                    var existingScore = (await _databaseService.GetScoresAsync()).FirstOrDefault(s => s.Title == scoreMeta.Title || s.FilePath == targetFilePath);
                    Score scoreToUse;

                    if (existingScore != null)
                    {
                        scoreToUse = existingScore;
                    }
                    else
                    {
                        scoreToUse = new Score
                        {
                            Title = scoreMeta.Title,
                            FilePath = _settingsService.GetRelativePath(targetFilePath),
                            Type = scoreMeta.Type,
                            Rotation = scoreMeta.Rotation,
                            IsRotationSaved = scoreMeta.IsRotationSaved,
                            ShowMetronome = scoreMeta.ShowMetronome,
                            BPM = scoreMeta.BPM,
                            HasMetronomeSound = scoreMeta.HasMetronomeSound,
                            ShowAudioPlayer = scoreMeta.ShowAudioPlayer,
                            PreCountMeasures = scoreMeta.PreCountMeasures,
                            DateAdded = DateTime.Now
                        };

                        await _databaseService.SaveScoreAsync(scoreToUse);

                        // Tags
                        for (int i = 0; i < scoreMeta.Tags.Count; i++)
                        {
                            string tagName = scoreMeta.Tags[i];
                            string tagColor = (i < scoreMeta.TagColors.Count) ? scoreMeta.TagColors[i] : "#007ACC";
                            var tag = await _databaseService.GetOrCreateTagAsync(tagName, tagColor);
                            await _databaseService.AddTagToScoreAsync(scoreToUse.Id, tag.Id);
                        }

                        // Annotations
                        foreach (var a in scoreMeta.Annotations)
                        {
                            var ann = new Annotation
                            {
                                ScoreId = scoreToUse.Id,
                                PageNumber = a.PageNumber,
                                Type = a.Type,
                                Category = a.Category,
                                Content = a.Content,
                                X = a.X,
                                Y = a.Y,
                                Scale = a.Scale,
                                Color = a.Color,
                                BackgroundColor = a.BackgroundColor
                            };
                            await _databaseService.SaveAnnotationAsync(ann);
                        }
                    }

                    // Audio
                    if (scoreMeta.AudioFiles.Any())
                    {
                        foreach (var audioFileName in scoreMeta.AudioFiles)
                        {
                            var audioPayload = payloads.FirstOrDefault(p => p.FileName == audioFileName);
                            if (audioPayload != null)
                            {
                                string targetAudioPath = Path.Combine(audioRootDir, audioFileName);
                                if (!File.Exists(targetAudioPath))
                                {
                                    await File.WriteAllBytesAsync(targetAudioPath, audioPayload.Data);
                                }

                                var audioFileModel = new ScoreAudioFile
                                {
                                    ScoreId = scoreToUse.Id,
                                    FileName = Path.GetFileName(audioFileName),
                                    FilePath = _settingsService.GetRelativePath(targetAudioPath, isAudio: true),
                                    IsSelected = true
                                };
                                await _databaseService.SaveAudioFileAsync(audioFileModel);
                            }
                        }
                    }

                    savedScores.Add(scoreToUse);
                }
            }

            // Créer la setlist avec son ordonnancement
            string setlistName = setlistMeta.Name;
            var existingSetlists = await _databaseService.GetSetlistsAsync();
            if (existingSetlists.Any(s => s.Name.Equals(setlistName, StringComparison.OrdinalIgnoreCase)))
            {
                setlistName = $"{setlistName} (Reçue {DateTime.Now:dd/MM})";
            }

            var newSetlist = new Setlist
            {
                Name = setlistName,
                DateCreated = DateTime.Now,
                ConcertDate = setlistMeta.ConcertDate,
                ConcertTime = setlistMeta.ConcertTime,
                Status = setlistMeta.Status,
                IsLocked = setlistMeta.IsLocked,
                IsContinuousReading = setlistMeta.IsContinuousReading
            };

            await _databaseService.SaveSetlistAsync(newSetlist);

            for (int order = 0; order < savedScores.Count; order++)
            {
                await _databaseService.AddScoreToSetlistAsync(newSetlist.Id, savedScores[order].Id, order + 1);
            }

            return true;
        }

        private async Task<bool> ImportScoresAsync(List<ScoreTransferMetadata> scoresMetaList, List<BluetoothFilePayload> payloads)
        {
            string scoresRootDir = _settingsService.ScoresRootDirectory;
            string audioRootDir = _settingsService.AudioRootDirectory;

            if (!Directory.Exists(scoresRootDir)) Directory.CreateDirectory(scoresRootDir);
            if (!Directory.Exists(audioRootDir)) Directory.CreateDirectory(audioRootDir);

            foreach (var scoreMeta in scoresMetaList)
            {
                var scorePayload = payloads.FirstOrDefault(p => p.FileName == scoreMeta.FileName);
                if (scorePayload != null)
                {
                    string targetFilePath = Path.Combine(scoresRootDir, scoreMeta.FileName);
                    if (!File.Exists(targetFilePath))
                    {
                        await File.WriteAllBytesAsync(targetFilePath, scorePayload.Data);
                    }

                    var newScore = new Score
                    {
                        Title = scoreMeta.Title,
                        FilePath = _settingsService.GetRelativePath(targetFilePath),
                        Type = scoreMeta.Type,
                        Rotation = scoreMeta.Rotation,
                        IsRotationSaved = scoreMeta.IsRotationSaved,
                        ShowMetronome = scoreMeta.ShowMetronome,
                        BPM = scoreMeta.BPM,
                        HasMetronomeSound = scoreMeta.HasMetronomeSound,
                        ShowAudioPlayer = scoreMeta.ShowAudioPlayer,
                        PreCountMeasures = scoreMeta.PreCountMeasures,
                        DateAdded = DateTime.Now
                    };

                    await _databaseService.SaveScoreAsync(newScore);

                    for (int i = 0; i < scoreMeta.Tags.Count; i++)
                    {
                        string tagName = scoreMeta.Tags[i];
                        string tagColor = (i < scoreMeta.TagColors.Count) ? scoreMeta.TagColors[i] : "#007ACC";
                        var tag = await _databaseService.GetOrCreateTagAsync(tagName, tagColor);
                        await _databaseService.AddTagToScoreAsync(newScore.Id, tag.Id);
                    }

                    foreach (var a in scoreMeta.Annotations)
                    {
                        var ann = new Annotation
                        {
                            ScoreId = newScore.Id,
                            PageNumber = a.PageNumber,
                            Type = a.Type,
                            Category = a.Category,
                            Content = a.Content,
                            X = a.X,
                            Y = a.Y,
                            Scale = a.Scale,
                            Color = a.Color,
                            BackgroundColor = a.BackgroundColor
                        };
                        await _databaseService.SaveAnnotationAsync(ann);
                    }

                    if (scoreMeta.AudioFiles.Any())
                    {
                        foreach (var audioFileName in scoreMeta.AudioFiles)
                        {
                            var audioPayload = payloads.FirstOrDefault(p => p.FileName == audioFileName);
                            if (audioPayload != null)
                            {
                                string targetAudioPath = Path.Combine(audioRootDir, audioFileName);
                                if (!File.Exists(targetAudioPath))
                                {
                                    await File.WriteAllBytesAsync(targetAudioPath, audioPayload.Data);
                                }

                                var audioFileModel = new ScoreAudioFile
                                {
                                    ScoreId = newScore.Id,
                                    FileName = Path.GetFileName(audioFileName),
                                    FilePath = _settingsService.GetRelativePath(targetAudioPath, isAudio: true),
                                    IsSelected = true
                                };
                                await _databaseService.SaveAudioFileAsync(audioFileModel);
                            }
                        }
                    }
                }
            }

            return true;
        }

        private async Task<bool> ImportRawFilesAsync(List<BluetoothFilePayload> payloads)
        {
            string scoresRootDir = _settingsService.ScoresRootDirectory;
            if (!Directory.Exists(scoresRootDir)) Directory.CreateDirectory(scoresRootDir);

            foreach (var filePayload in payloads)
            {
                if (filePayload.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

                string targetFilePath = Path.Combine(scoresRootDir, filePayload.FileName);
                if (!File.Exists(targetFilePath))
                {
                    await File.WriteAllBytesAsync(targetFilePath, filePayload.Data);
                }

                string ext = Path.GetExtension(filePayload.FileName).ToLowerInvariant();
                var type = (ext == ".pdf") ? ScoreType.PDF : ScoreType.Image;
                string title = Path.GetFileNameWithoutExtension(filePayload.FileName).Replace("_", " ");

                var newScore = new Score
                {
                    Title = title,
                    FilePath = _settingsService.GetRelativePath(targetFilePath),
                    Type = type,
                    DateAdded = DateTime.Now
                };

                await _databaseService.SaveScoreAsync(newScore);
            }

            return true;
        }

        #endregion
    }
}
