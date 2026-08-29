using SQLite;
using MusicScoreManager.Models;

namespace MusicScoreManager.Services
{
    public class DatabaseService
    {
        private static SQLiteAsyncConnection? _database;
        private static Task? _initTask;
        private static readonly object _initLock = new();
        private readonly string _databasePath;
        private readonly SettingsService _settingsService;

        public DatabaseService()
        {
            _settingsService = new SettingsService();
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "scores.db3");
        }

        private Task Init()
        {
            lock (_initLock)
            {
                if (_initTask != null) return _initTask;

                _initTask = Task.Run(async () =>
                {
                    var db = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                    
                    try
                    {
                        await db.ExecuteAsync("PRAGMA journal_mode = WAL;");
                        await db.ExecuteAsync("PRAGMA synchronous = NORMAL;");
                    }
                    catch { }

                    await db.CreateTableAsync<Score>();
                    await db.CreateTableAsync<Setlist>();
                    await db.CreateTableAsync<SetlistScore>();
                    await db.CreateTableAsync<Tag>();
                    await db.CreateTableAsync<ScoreTag>();
                    await db.CreateTableAsync<ScoreAudioFile>();
                    await db.CreateTableAsync<Annotation>();
                    await db.CreateTableAsync<FavoriteSticker>();
                    await db.CreateTableAsync<ScorePageRotation>();
                    
                    _database = db;
                });
                return _initTask;
            }
        }
 
        public async Task<List<Score>> GetScoresAsync()
        {
            await Init();
            var scores = await _database!.Table<Score>().ToListAsync();
            await LoadDetailsForScoresAsync(scores);
            return scores;
        }

        public async Task<(int total, int pdfCount, int imageCount)> GetScoreCountsByTypeAsync()
        {
            await Init();
            var allScores = await _database!.Table<Score>().ToListAsync();
            int pdf = 0;
            int image = 0;
            foreach (var score in allScores)
            {
                string ext = Path.GetExtension(score.FilePath)?.ToLowerInvariant() ?? "";
                if (score.Type == ScoreType.PDF || ext == ".pdf")
                {
                    pdf++;
                }
                else
                {
                    image++;
                }
            }
            return (allScores.Count, pdf, image);
        }
 
        private async Task LoadDetailsForScoresAsync(List<Score> scores)
        {
            if (scores == null || scores.Count == 0 || _database == null) return;
            
            // Si on a beaucoup de scores, on charge TOUTES les tables de jointure une fois
            // et on fait le mapping en mémoire (BEAUCOUP plus rapide que Contains sur Android)
            List<ScoreTag> allScoreTags;
            List<Tag> allTags;
            List<ScoreAudioFile> allAudioFiles;

            if (scores.Count > 10)
            {
                allScoreTags = await _database.Table<ScoreTag>().ToListAsync();
                allTags = await _database.Table<Tag>().ToListAsync();
                allAudioFiles = await _database.Table<ScoreAudioFile>().ToListAsync();
            }
            else
            {
                var ids = scores.Select(s => s.Id).ToList();
                allScoreTags = await _database.Table<ScoreTag>().Where(st => ids.Contains(st.ScoreId)).ToListAsync();
                var tagIds = allScoreTags.Select(st => st.TagId).Distinct().ToList();
                allTags = await _database.Table<Tag>().Where(t => tagIds.Contains(t.Id)).ToListAsync();
                allAudioFiles = await _database.Table<ScoreAudioFile>().Where(af => ids.Contains(af.ScoreId)).ToListAsync();
            }

            var tagsLookup = allTags.ToDictionary(t => t.Id);
            var scoreTagsGrouped = allScoreTags.GroupBy(st => st.ScoreId).ToDictionary(g => g.Key, g => g.ToList());
            var audioFilesGrouped = allAudioFiles.GroupBy(af => af.ScoreId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var score in scores)
            {
                if (scoreTagsGrouped.TryGetValue(score.Id, out var sTags))
                {
                    score.AppliedTags = sTags.Select(st => tagsLookup.TryGetValue(st.TagId, out var t) ? t : null)
                                            .Where(t => t != null).Cast<Tag>().ToList();
                }
                else score.AppliedTags = new();

                score.AudioFiles = audioFilesGrouped.TryGetValue(score.Id, out var afs) ? afs : new();
                
                // Optimisation : On ne vérifie l'existence physique QUE si on a peu de scores (ex: ouverture partition)
                // ou si on est en train de rendre un item spécifique (mais ici on le fait pour tous)
                // Pour la liste principale, on accepte un léger décalage ou on le fera en tâche de fond.
                if (scores.Count <= 5)
                {
                    foreach (var af in score.AudioFiles)
                    {
                        var afPath = _settingsService.GetAbsolutePath(af.FilePath, isAudio: true);
                        af.IsFileMissing = !File.Exists(afPath);
                        af.IsExternal = Path.IsPathRooted(af.FilePath);
                    }
                    var sPath = _settingsService.GetAbsolutePath(score.FilePath);
                    score.IsFileMissing = !File.Exists(sPath);
                    score.IsExternal = Path.IsPathRooted(score.FilePath);
                }
                else
                {
                    // Valeurs par défaut rapides pour la liste
                    score.IsExternal = Path.IsPathRooted(score.FilePath);
                    // On ne check pas File.Exists ici pour la grosse liste
                }
            }
        }

        public async Task<Score?> GetScoreAsync(int id)
        {
            await Init();
            var score = await _database!.Table<Score>().Where(i => i.Id == id).FirstOrDefaultAsync();
            if (score != null)
                await LoadDetailsForScoresAsync(new List<Score> { score });
            return score;
        }

        public async Task<int> SaveScoreAsync(Score score)
        {
            await Init();
            
            // Si l'Id n'est pas 0, on met à jour. Sinon on l'insère (AutoIncrement).
            if (score.Id != 0)
                return await _database!.UpdateAsync(score);
            else
                return await _database!.InsertAsync(score);
        }

        public async Task<int> DeleteScoreAsync(Score score)
        {
            await Init();
            await _database!.Table<ScoreTag>().Where(st => st.ScoreId == score.Id).DeleteAsync();
            await _database!.Table<ScorePageRotation>().Where(pr => pr.ScoreId == score.Id).DeleteAsync();
            await _database!.Table<Annotation>().Where(a => a.ScoreId == score.Id).DeleteAsync();
            return await _database!.DeleteAsync(score);
        }

        public async Task UpdateScoreTagsAsync(int scoreId, List<int> tagIds)
        {
            await Init();
            // Supprimer les anciens liens
            await _database!.Table<ScoreTag>().Where(st => st.ScoreId == scoreId).DeleteAsync();

            // Ajouter les nouveaux liens
            var newMappings = tagIds.Select(id => new ScoreTag { ScoreId = scoreId, TagId = id });
            await _database!.InsertAllAsync(newMappings);
        }

        public async Task<List<Score>> SearchScoresAsync(string query, List<int>? tagIds)
        {
            await Init();
            
            var queryable = _database!.Table<Score>();
            
            if (!string.IsNullOrWhiteSpace(query))
                queryable = queryable.Where(s => s.Title.Contains(query));

            var scores = await queryable.ToListAsync();
            await LoadDetailsForScoresAsync(scores);

            if (tagIds != null && tagIds.Any())
            {
                // Un score doit contenir TOUS les tags sélectionnés
                scores = scores.Where(s => tagIds.All(id => s.AppliedTags.Any(t => t.Id == id))).ToList();
            }

            return scores;
        }

        public async Task<List<Score>> SearchScoresAsync(string query, int? tagId = null)
        {
            return await SearchScoresAsync(query, tagId.HasValue ? new List<int> { tagId.Value } : null);
        }

        // --- Audio Files ---
        public async Task<int> SaveAudioFileAsync(ScoreAudioFile audioFile)
        {
            await Init();
            if (audioFile.Id != 0)
                return await _database!.UpdateAsync(audioFile);
            else
                return await _database!.InsertAsync(audioFile);
        }

        public async Task<int> DeleteAudioFileAsync(ScoreAudioFile audioFile)
        {
            await Init();
            return await _database!.DeleteAsync(audioFile);
        }

        public async Task SetSelectedAudioFileAsync(int scoreId, int audioFileId)
        {
            await Init();
            var audioFiles = await _database!.Table<ScoreAudioFile>().Where(af => af.ScoreId == scoreId).ToListAsync();
            foreach (var af in audioFiles)
            {
                af.IsSelected = (af.Id == audioFileId);
                await _database.UpdateAsync(af);
            }
        }

        public async Task<List<Score>> GetScoresByTagAsync(int tagId)
        {
            return await SearchScoresAsync(string.Empty, tagId);
        }

        // --- Setlists ---
        public async Task<List<Setlist>> GetSetlistsAsync()
        {
            await Init();
            return await _database!.Table<Setlist>().ToListAsync();
        }

        public async Task<int> SaveSetlistAsync(Setlist setlist)
        {
            await Init();
            if (setlist.Id != 0)
                return await _database!.UpdateAsync(setlist);
            else
                return await _database!.InsertAsync(setlist);
        }

        public async Task<int> DeleteSetlistAsync(Setlist setlist)
        {
            await Init();
            // Delete associated SetlistScore entries
            await _database!.Table<SetlistScore>().Where(ss => ss.SetlistId == setlist.Id).DeleteAsync();
            return await _database!.DeleteAsync(setlist);
        }

        public async Task<List<Score>> GetScoresForSetlistAsync(int setlistId)
        {
            await Init();
            var associations = await _database!.Table<SetlistScore>()
                                             .Where(ss => ss.SetlistId == setlistId)
                                             .OrderBy(ss => ss.Order)
                                             .ToListAsync();
            
            var scoreIds = associations.Select(a => a.ScoreId).ToList();
            var allScores = await GetScoresAsync();
            
            // Return scores in the specified order
            return scoreIds.Select(id => allScores.FirstOrDefault(s => s.Id == id))
                          .Where(s => s != null)
                          .Cast<Score>()
                          .ToList();
        }

        public async Task UpdateSetlistScoresAsync(int setlistId, List<int> scoreIds)
        {
            await Init();
            
            // Supprimer les anciennes associations
            await _database!.Table<SetlistScore>().Where(ss => ss.SetlistId == setlistId).DeleteAsync();
            
            // Ajouter les nouvelles avec le bon ordre
            var newAssociations = new List<SetlistScore>();
            for (int i = 0; i < scoreIds.Count; i++)
            {
                newAssociations.Add(new SetlistScore 
                { 
                    SetlistId = setlistId, 
                    ScoreId = scoreIds[i], 
                    Order = i + 1 
                });
            }
            
            await _database!.InsertAllAsync(newAssociations);
        }

        public async Task AddScoreToSetlistAsync(int setlistId, int scoreId, int order)
        {
            await Init();
            await _database!.InsertAsync(new SetlistScore
            {
                SetlistId = setlistId,
                ScoreId = scoreId,
                Order = order
            });
        }

        // --- Tags ---
        public async Task<List<Tag>> GetTagsAsync()
        {
            await Init();
            return await _database!.Table<Tag>().ToListAsync();
        }

        public async Task<List<Tag>> GetTagsForScoreAsync(int scoreId)
        {
            await Init();
            var scoreTags = await _database!.Table<ScoreTag>().Where(st => st.ScoreId == scoreId).ToListAsync();
            var tagIds = scoreTags.Select(st => st.TagId).ToList();
            if (!tagIds.Any()) return new List<Tag>();
            return await _database!.Table<Tag>().Where(t => tagIds.Contains(t.Id)).ToListAsync();
        }

        public async Task<Tag> GetOrCreateTagAsync(string name, string colorHex)
        {
            await Init();
            var existing = await _database!.Table<Tag>().Where(t => t.Name == name).FirstOrDefaultAsync();
            if (existing != null) return existing;

            var newTag = new Tag { Name = name, ColorHex = colorHex };
            await _database.InsertAsync(newTag);
            return newTag;
        }

        public async Task AddTagToScoreAsync(int scoreId, int tagId)
        {
            await Init();
            var existing = await _database!.Table<ScoreTag>().Where(st => st.ScoreId == scoreId && st.TagId == tagId).FirstOrDefaultAsync();
            if (existing == null)
            {
                await _database.InsertAsync(new ScoreTag { ScoreId = scoreId, TagId = tagId });
            }
        }

        public async Task<int> SaveTagAsync(Tag tag)
        {
            await Init();
            if (tag.Id != 0)
                return await _database!.UpdateAsync(tag);
            else
                return await _database!.InsertAsync(tag);
        }

        public async Task<int> DeleteTagAsync(Tag tag)
        {
            await Init();
            await _database!.Table<ScoreTag>().Where(st => st.TagId == tag.Id).DeleteAsync();
            return await _database!.DeleteAsync(tag);
        }

        // --- Backups ---
        public string GetBackupsFolder()
        {
            string folder = Path.Combine(FileSystem.AppDataDirectory, "backups");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        public async Task<string> BackupDatabaseAsync()
        {
            string folder = GetBackupsFolder();
            var now = DateTime.Now;
            string timestamp = now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"scores_backup_{timestamp}.db3";
            string backupPath = Path.Combine(folder, backupFileName);

            File.Copy(_databasePath, backupPath, true);

            try
            {
                File.SetCreationTime(backupPath, now);
                File.SetLastWriteTime(backupPath, now);
            }
            catch { }
            
            return backupPath;
        }

        public List<BackupFile> GetBackups()
        {
            string folder = GetBackupsFolder();
            var files = Directory.GetFiles(folder, "scores_backup_*.db3");
            
            var list = new List<BackupFile>();
            foreach (var f in files)
            {
                string fileName = Path.GetFileName(f);
                DateTime backupDate = DateTime.MinValue;

                // Extraction précise de l'horodatage depuis le nom : scores_backup_yyyyMMdd_HHmmss.db3
                string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                string prefix = "scores_backup_";
                if (nameWithoutExt.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string timePart = nameWithoutExt.Substring(prefix.Length);
                    if (DateTime.TryParseExact(timePart, "yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        backupDate = parsedDate;
                    }
                }

                if (backupDate == DateTime.MinValue)
                {
                    try
                    {
                        backupDate = File.GetLastWriteTime(f);
                    }
                    catch
                    {
                        backupDate = DateTime.Now;
                    }
                }

                long size = 0;
                try
                {
                    size = new FileInfo(f).Length;
                }
                catch { }

                list.Add(new BackupFile 
                { 
                    FileName = fileName, 
                    FullPath = f, 
                    Date = backupDate,
                    SizeInBytes = size
                });
            }

            return list.OrderByDescending(b => b.Date).ToList();
        }

        public void PurgeBackups(int maxKeep)
        {
            var backups = GetBackups();
            if (backups.Count > maxKeep)
            {
                var toDelete = backups.Skip(maxKeep).ToList();
                foreach (var b in toDelete)
                {
                    if (File.Exists(b.FullPath))
                        File.Delete(b.FullPath);
                }
            }
        }

        public async Task RestoreBackupAsync(string backupPath)
        {
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Fichier de sauvegarde introuvable.");

            // Pour restaurer, on doit idéalement couper la connexion actuelle
            if (_database != null)
            {
                await _database.CloseAsync();
                _database = null;
            }

            File.Copy(backupPath, _databasePath, true);
            
            // La base sera réinitialisée au prochain appel via Init()
        }

        #region Favorite Stickers

        public async Task<List<FavoriteSticker>> GetFavoriteStickersAsync()
        {
            await Init();
            return await _database!.Table<FavoriteSticker>().ToListAsync();
        }

        public async Task<int> SaveFavoriteStickerAsync(FavoriteSticker sticker)
        {
            await Init();
            if (sticker.Id != 0)
                return await _database!.UpdateAsync(sticker);
            else
                return await _database!.InsertAsync(sticker);
        }

        public async Task<int> DeleteFavoriteStickerAsync(FavoriteSticker sticker)
        {
            await Init();
            return await _database!.DeleteAsync(sticker);
        }

        #endregion

        #region Page Rotations

        public async Task<List<ScorePageRotation>> GetPageRotationsForScoreAsync(int scoreId)
        {
            await Init();
            return await _database!.Table<ScorePageRotation>()
                                   .Where(pr => pr.ScoreId == scoreId)
                                   .ToListAsync();
        }

        public async Task SavePageRotationAsync(int scoreId, int pageNumber, int rotation)
        {
            await Init();
            var existing = await _database!.Table<ScorePageRotation>()
                                            .Where(pr => pr.ScoreId == scoreId && pr.PageNumber == pageNumber)
                                            .FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.Rotation = rotation;
                await _database.UpdateAsync(existing);
            }
            else
            {
                await _database.InsertAsync(new ScorePageRotation
                {
                    ScoreId = scoreId,
                    PageNumber = pageNumber,
                    Rotation = rotation
                });
            }
        }

        public async Task DeletePageRotationsForScoreAsync(int scoreId)
        {
            await Init();
            await _database!.Table<ScorePageRotation>().Where(pr => pr.ScoreId == scoreId).DeleteAsync();
        }

        #endregion

        #region Annotations

        public async Task<List<Annotation>> GetAnnotationsForScoreAsync(int scoreId)
        {
            await Init();
            return await _database!.Table<Annotation>()
                                  .Where(a => a.ScoreId == scoreId)
                                  .ToListAsync();
        }

        public async Task<int> SaveAnnotationAsync(Annotation annotation)
        {
            await Init();
            if (annotation.Id != 0)
            {
                return await _database!.UpdateAsync(annotation);
            }
            else
            {
                return await _database!.InsertAsync(annotation);
            }
        }

        public async Task<int> DeleteAnnotationAsync(Annotation annotation)
        {
            await Init();
            return await _database!.DeleteAsync(annotation);
        }

        public async Task<int> DeleteAllAnnotationsForScoreAsync(int scoreId)
        {
            await Init();
            return await _database!.Table<Annotation>().Where(a => a.ScoreId == scoreId).DeleteAsync();
        }

        #endregion
    }

    public class BackupFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public long SizeInBytes { get; set; }
        public bool IsExternal { get; set; }
        public string DisplayDate => Date.ToString("dd/MM/yyyy HH:mm:ss");
        public string DisplaySize => SizeInBytes < 1024 * 1024 ? $"{SizeInBytes / 1024.0:F1} Ko" : $"{SizeInBytes / (1024.0 * 1024.0):F1} Mo";
    }
}
