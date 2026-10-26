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

        public string DatabasePath => _databasePath;

        public long GetDatabaseSizeInBytes()
        {
            try
            {
                long total = 0;
                if (File.Exists(_databasePath))
                {
                    total += new FileInfo(_databasePath).Length;
                }
                // Inclure également les fichiers WAL et SHM de SQLite s'ils existent
                string walPath = _databasePath + "-wal";
                if (File.Exists(walPath))
                {
                    total += new FileInfo(walPath).Length;
                }
                string shmPath = _databasePath + "-shm";
                if (File.Exists(shmPath))
                {
                    total += new FileInfo(shmPath).Length;
                }
                return total;
            }
            catch
            {
                return 0;
            }
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
                    try
                    {
                        await db.ExecuteAsync("ALTER TABLE Score ADD COLUMN ShowAnnotations INTEGER NOT NULL DEFAULT 1;");
                    }
                    catch { }
                    try
                    {
                        await db.ExecuteAsync("ALTER TABLE Score ADD COLUMN ShowPageNumber INTEGER NOT NULL DEFAULT 1;");
                    }
                    catch { }
                    try
                    {
                        await db.ExecuteAsync("ALTER TABLE Score ADD COLUMN ShowToolsButton INTEGER NOT NULL DEFAULT 0;");
                    }
                    catch { }
                    try
                    {
                        await db.ExecuteAsync("ALTER TABLE Score ADD COLUMN ShowScoreTitle INTEGER NOT NULL DEFAULT 0;");
                    }
                    catch { }
                    try
                    {
                        await db.ExecuteAsync("ALTER TABLE Score ADD COLUMN Category TEXT NOT NULL DEFAULT 'Général';");
                    }
                    catch { }
                    await db.CreateTableAsync<Setlist>();
                    try
                    {
                        await db.ExecuteAsync("ALTER TABLE Setlists ADD COLUMN PedalProfileId TEXT;");
                    }
                    catch { }
                    await db.CreateTableAsync<SetlistScore>();
                    await db.CreateTableAsync<Tag>();
                    await db.CreateTableAsync<ScoreTag>();
                    await db.CreateTableAsync<ScoreAudioFile>();
                    await db.CreateTableAsync<Annotation>();
                    await db.CreateTableAsync<FavoriteSticker>();
                    await db.CreateTableAsync<ScorePageRotation>();
                    await db.CreateTableAsync<ScoreBookmark>();
                    
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

        public async Task<(int totalCount, long totalBytes)> GetScoreStatsAsync(SettingsService settingsService)
        {
            await Init();
            var allScores = await _database!.Table<Score>().ToListAsync();
            long totalBytes = 0;
            int count = 0;
            foreach (var score in allScores)
            {
                count++;
                try
                {
                    string fullPath = settingsService.GetAbsolutePath(score.FilePath, false);
                    if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                    {
                        var fi = new FileInfo(fullPath);
                        totalBytes += fi.Length;
                    }
                }
                catch { }
            }
            return (count, totalBytes);
        }
 
        private async Task LoadDetailsForScoresAsync(List<Score> scores)
        {
            if (scores == null || scores.Count == 0 || _database == null) return;
            
            // Si on a un très grand nombre de scores (> 150), on charge les tables de jointure complètes
            // Sinon on requête de manière ciblée uniquement les IDs concernés
            List<ScoreTag> allScoreTags;
            List<Tag> allTags;
            List<ScoreAudioFile> allAudioFiles;

            if (scores.Count > 150)
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
                
                foreach (var af in score.AudioFiles)
                {
                    var afPath = _settingsService.GetAbsolutePath(af.FilePath, isAudio: true);
                    af.IsFileMissing = string.IsNullOrWhiteSpace(af.FilePath) || !File.Exists(afPath);
                    af.IsExternal = Path.IsPathRooted(af.FilePath);
                }
                var sPath = _settingsService.GetAbsolutePath(score.FilePath);
                score.IsFileMissing = string.IsNullOrWhiteSpace(score.FilePath) || !File.Exists(sPath);
                score.IsExternal = Path.IsPathRooted(score.FilePath);
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
            await _database!.Table<ScoreBookmark>().Where(b => b.ScoreId == score.Id).DeleteAsync();
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
                queryable = queryable.Where(s => s.Title.Contains(query) || s.Composer.Contains(query));

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

        public async Task<List<ScoreAudioFile>> GetAudioFilesForScoreAsync(int scoreId)
        {
            await Init();
            return await _database!.Table<ScoreAudioFile>().Where(af => af.ScoreId == scoreId).ToListAsync();
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

        public async Task<Setlist?> DuplicateSetlistAsync(int setlistId, string newName)
        {
            await Init();
            var original = await _database!.Table<Setlist>().FirstOrDefaultAsync(s => s.Id == setlistId);
            if (original == null) return null;

            var newSetlist = new Setlist
            {
                Name = newName,
                DateCreated = DateTime.Now,
                ConcertDate = original.ConcertDate,
                ConcertTime = original.ConcertTime,
                Status = original.Status,
                IsLocked = original.IsLocked,
                IsContinuousReading = original.IsContinuousReading,
                PedalProfileId = original.PedalProfileId
            };
            await _database!.InsertAsync(newSetlist);

            var associations = await _database!.Table<SetlistScore>()
                                              .Where(ss => ss.SetlistId == setlistId)
                                              .OrderBy(ss => ss.Order)
                                              .ToListAsync();

            var newAssociations = associations.Select(a => new SetlistScore
            {
                SetlistId = newSetlist.Id,
                ScoreId = a.ScoreId,
                Order = a.Order
            }).ToList();

            if (newAssociations.Any())
            {
                await _database!.InsertAllAsync(newAssociations);
            }

            return newSetlist;
        }

        public async Task<List<Score>> GetScoresForSetlistAsync(int setlistId)
        {
            await Init();
            var associations = await _database!.Table<SetlistScore>()
                                             .Where(ss => ss.SetlistId == setlistId)
                                             .OrderBy(ss => ss.Order)
                                             .ToListAsync();
            
            if (associations == null || associations.Count == 0)
                return new List<Score>();

            var scoreIds = associations.Select(a => a.ScoreId).Distinct().ToList();
            
            // Optimisation majeure : charger UNIQUEMENT les partitions de la setlist ciblée (évite de charger toute la bibliothèque)
            var targetScores = await _database.Table<Score>()
                                             .Where(s => scoreIds.Contains(s.Id))
                                             .ToListAsync();
            await LoadDetailsForScoresAsync(targetScores);
            
            var scoreDict = targetScores.ToDictionary(s => s.Id);
            
            // Conserver l'ordre exact et les éventuelles répétitions définies dans la setlist
            var result = new List<Score>();
            foreach (var assoc in associations)
            {
                if (scoreDict.TryGetValue(assoc.ScoreId, out var score))
                {
                    result.Add(score);
                }
            }
            return result;
        }

        public async Task<Score?> GetFirstScoreForSetlistAsync(int setlistId)
        {
            await Init();
            var firstAssoc = await _database!.Table<SetlistScore>()
                                            .Where(ss => ss.SetlistId == setlistId)
                                            .OrderBy(ss => ss.Order)
                                            .FirstOrDefaultAsync();
            if (firstAssoc == null) return null;
            return await GetScoreAsync(firstAssoc.ScoreId);
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

        public async Task AddScoreToSetlistAtBeginningAsync(int setlistId, int scoreId)
        {
            await Init();
            var existingAssociations = await _database!.Table<SetlistScore>()
                                                      .Where(ss => ss.SetlistId == setlistId)
                                                      .OrderBy(ss => ss.Order)
                                                      .ToListAsync();

            var scoreIds = existingAssociations.Select(ss => ss.ScoreId).ToList();

            // On insère en première position (index 0), en autorisant les doublons
            scoreIds.Insert(0, scoreId);

            await UpdateSetlistScoresAsync(setlistId, scoreIds);
        }

        public async Task<List<string>> GetSetlistNamesForScoreAsync(int scoreId)
        {
            await Init();
            var setlistScores = await _database!.Table<SetlistScore>().Where(ss => ss.ScoreId == scoreId).ToListAsync();
            if (!setlistScores.Any()) return new List<string>();
            var setlistIds = setlistScores.Select(ss => ss.SetlistId).Distinct().ToList();
            var setlists = await _database.Table<Setlist>().Where(s => setlistIds.Contains(s.Id)).ToListAsync();
            return setlists.Select(s => s.Name).ToList();
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
            await Init();
            if (!File.Exists(_databasePath))
                return string.Empty;

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

        public async Task CheckpointAsync()
        {
            await Init();
            if (_database != null)
            {
                try
                {
                    await _database.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE);");
                }
                catch { }
            }
        }

        public async Task CloseConnectionAsync()
        {
            lock (_initLock)
            {
                _initTask = null;
            }

            if (_database != null)
            {
                try
                {
                    await _database.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE);");
                }
                catch { }

                try
                {
                    await _database.CloseAsync();
                }
                catch { }

                _database = null;
            }
        }

        public async Task<int> VerifyDatabaseAsync()
        {
            await Init();
            if (_database == null)
                throw new InvalidOperationException("Connexion à la base de données impossible.");

            var scores = await _database.Table<Score>().ToListAsync();
            return scores.Count;
        }

        public async Task RestoreBackupAsync(string backupPath)
        {
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Fichier de sauvegarde introuvable.");

            await CloseConnectionAsync();

            File.Copy(backupPath, _databasePath, true);

            // Vérifier et réinitialiser la base
            await VerifyDatabaseAsync();
        }

        #region Favorite Stickers

        public async Task<List<FavoriteSticker>> GetFavoriteStickersAsync()
        {
            await Init();
            return await _database!.Table<FavoriteSticker>()
                                   .OrderBy(f => f.SortOrder)
                                   .ThenBy(f => f.Id)
                                   .ToListAsync();
        }

        public async Task<int> SaveFavoriteStickerAsync(FavoriteSticker sticker)
        {
            await Init();
            if (sticker.Id != 0)
                return await _database!.UpdateAsync(sticker);
            else
                return await _database!.InsertAsync(sticker);
        }

        public async Task SaveFavoriteStickersOrderAsync(IEnumerable<FavoriteSticker> stickers)
        {
            await Init();
            await _database!.UpdateAllAsync(stickers);
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

        #region Bookmarks / Marqueurs

        public async Task<List<ScoreBookmark>> GetBookmarksForScoreAsync(int scoreId)
        {
            await Init();
            return await _database!.Table<ScoreBookmark>()
                                  .Where(b => b.ScoreId == scoreId)
                                  .OrderBy(b => b.PageNumber)
                                  .ThenBy(b => b.Name)
                                  .ToListAsync();
        }

        public async Task<int> SaveBookmarkAsync(ScoreBookmark bookmark)
        {
            await Init();
            if (bookmark.Id != 0)
            {
                return await _database!.UpdateAsync(bookmark);
            }
            else
            {
                return await _database!.InsertAsync(bookmark);
            }
        }

        public async Task<int> DeleteBookmarkAsync(ScoreBookmark bookmark)
        {
            await Init();
            return await _database!.DeleteAsync(bookmark);
        }

        public async Task<int> DeleteAllBookmarksForScoreAsync(int scoreId)
        {
            await Init();
            return await _database!.Table<ScoreBookmark>().Where(b => b.ScoreId == scoreId).DeleteAsync();
        }

        #region Categories (Catégories / Répertoires)

        public async Task<List<string>> GetCategoriesAsync()
        {
            await Init();
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Général" };

            try
            {
                var dbCategories = await _database!.QueryAsync<Score>("SELECT DISTINCT Category FROM Score WHERE Category IS NOT NULL AND TRIM(Category) != ''");
                foreach (var s in dbCategories)
                {
                    if (!string.IsNullOrWhiteSpace(s.Category))
                    {
                        categories.Add(s.Category.Trim());
                    }
                }
            }
            catch { }

            try
            {
                string scoresRoot = _settingsService.ScoresRootDirectory;
                if (Directory.Exists(scoresRoot))
                {
                    var dirs = Directory.GetDirectories(scoresRoot);
                    foreach (var d in dirs)
                    {
                        var dirName = Path.GetFileName(d);
                        if (!string.IsNullOrWhiteSpace(dirName) && !dirName.StartsWith("."))
                        {
                            categories.Add(dirName.Trim());
                        }
                    }
                }
            }
            catch { }

            // Ordonner avec "Général" toujours en première position
            var list = categories.OrderBy(c => c.Equals("Général", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                                 .ThenBy(c => c, StringComparer.CurrentCultureIgnoreCase)
                                 .ToList();
            return list;
        }

        public async Task<bool> CreateCategoryAsync(string categoryName)
        {
            await Init();
            string clean = SanitizeCategoryName(categoryName);
            if (string.IsNullOrWhiteSpace(clean)) return false;

            try
            {
                string path = Path.Combine(_settingsService.ScoresRootDirectory, clean);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RenameCategoryAsync(string oldName, string newName)
        {
            await Init();
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;
            string cleanOld = oldName.Trim();
            string cleanNew = SanitizeCategoryName(newName);
            if (string.Equals(cleanOld, cleanNew, StringComparison.OrdinalIgnoreCase)) return true;

            try
            {
                string scoresRoot = _settingsService.ScoresRootDirectory;
                string oldDir = Path.Combine(scoresRoot, cleanOld);
                string newDir = Path.Combine(scoresRoot, cleanNew);

                if (!Directory.Exists(newDir))
                {
                    if (Directory.Exists(oldDir))
                    {
                        Directory.Move(oldDir, newDir);
                    }
                    else
                    {
                        Directory.CreateDirectory(newDir);
                    }
                }
                else if (Directory.Exists(oldDir))
                {
                    // Déplacer les fichiers de oldDir vers newDir
                    foreach (var file in Directory.GetFiles(oldDir))
                    {
                        string dest = Path.Combine(newDir, Path.GetFileName(file));
                        if (!File.Exists(dest)) File.Move(file, dest);
                    }
                    try { Directory.Delete(oldDir, true); } catch { }
                }

                // Mettre à jour en base de données
                var allScores = await _database!.Table<Score>().ToListAsync();
                foreach (var score in allScores)
                {
                    if (string.Equals(score.Category, cleanOld, StringComparison.OrdinalIgnoreCase))
                    {
                        score.Category = cleanNew;

                        // Mettre à jour FilePath si relatif et préfixé par l'ancien dossier
                        if (!string.IsNullOrWhiteSpace(score.FilePath) && !Path.IsPathRooted(score.FilePath))
                        {
                            string norm = score.FilePath.Replace('\\', '/');
                            if (norm.StartsWith(cleanOld + "/", StringComparison.OrdinalIgnoreCase))
                            {
                                score.FilePath = cleanNew + norm.Substring(cleanOld.Length);
                            }
                        }

                        await _database.UpdateAsync(score);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] RenameCategoryAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(string categoryName, string fallbackCategory = "Général")
        {
            await Init();
            if (string.IsNullOrWhiteSpace(categoryName)) return false;
            string cleanCat = categoryName.Trim();
            string cleanFallback = string.IsNullOrWhiteSpace(fallbackCategory) ? "Général" : fallbackCategory.Trim();

            if (string.Equals(cleanCat, "Général", StringComparison.OrdinalIgnoreCase))
            {
                // Ne pas supprimer le dossier racine / par défaut
                return false;
            }

            try
            {
                string scoresRoot = _settingsService.ScoresRootDirectory;
                string oldDir = Path.Combine(scoresRoot, cleanCat);
                string targetDir = Path.Combine(scoresRoot, cleanFallback);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                // Déplacer les partitions en base de données vers le dossier de secours
                var allScores = await _database!.Table<Score>().ToListAsync();
                foreach (var score in allScores)
                {
                    if (string.Equals(score.Category, cleanCat, StringComparison.OrdinalIgnoreCase))
                    {
                        score.Category = cleanFallback;

                        if (!string.IsNullOrWhiteSpace(score.FilePath) && !Path.IsPathRooted(score.FilePath))
                        {
                            string oldFullPath = _settingsService.GetAbsolutePath(score.FilePath);
                            if (File.Exists(oldFullPath))
                            {
                                string fileName = Path.GetFileName(oldFullPath);
                                string newFullPath = Path.Combine(targetDir, fileName);
                                if (File.Exists(newFullPath) && !string.Equals(oldFullPath, newFullPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                                    string ext = Path.GetExtension(fileName);
                                    newFullPath = Path.Combine(targetDir, $"{nameOnly}_{DateTime.Now:yyyyMMddHHmmss}{ext}");
                                }
                                if (!File.Exists(newFullPath))
                                {
                                    File.Move(oldFullPath, newFullPath);
                                }
                                score.FilePath = _settingsService.GetRelativePath(newFullPath);
                            }
                        }

                        await _database.UpdateAsync(score);
                    }
                }

                // Supprimer le répertoire physique s'il existe
                if (Directory.Exists(oldDir))
                {
                    try { Directory.Delete(oldDir, true); } catch { }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] DeleteCategoryAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MoveScoreToCategoryAsync(Score score, string newCategory)
        {
            await Init();
            if (score == null) return false;
            string cleanCat = string.IsNullOrWhiteSpace(newCategory) ? "Général" : SanitizeCategoryName(newCategory);

            try
            {
                if (!score.IsExternal && !string.IsNullOrWhiteSpace(score.FilePath))
                {
                    string oldFullPath = _settingsService.GetAbsolutePath(score.FilePath);
                    if (File.Exists(oldFullPath))
                    {
                        string scoresRoot = _settingsService.ScoresRootDirectory;
                        string targetDir = Path.Combine(scoresRoot, cleanCat);
                        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                        string fileName = Path.GetFileName(oldFullPath);
                        string newFullPath = Path.Combine(targetDir, fileName);

                        if (!string.Equals(oldFullPath, newFullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            if (File.Exists(newFullPath))
                            {
                                string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                                string ext = Path.GetExtension(fileName);
                                newFullPath = Path.Combine(targetDir, $"{nameOnly}_{DateTime.Now:yyyyMMddHHmmss}{ext}");
                            }

                            File.Move(oldFullPath, newFullPath);
                            score.FilePath = _settingsService.GetRelativePath(newFullPath);
                        }
                    }
                }

                score.Category = cleanCat;
                await _database!.UpdateAsync(score);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] MoveScoreToCategoryAsync error: {ex.Message}");
                return false;
            }
        }

        private static string SanitizeCategoryName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Général";
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Trim().Where(c => !invalidChars.Contains(c) && c != '/' && c != '\\').ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "Général" : cleaned;
        }

        #endregion

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
