using SQLite;
using MusicScoreManager.Models;

namespace MusicScoreManager.Services
{
    public class DatabaseService
    {
        private static SQLiteAsyncConnection? _database;
        private static readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private readonly string _databasePath;
        private readonly SettingsService _settingsService;

        public DatabaseService()
        {
            _settingsService = new SettingsService();
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "scores.db3");
        }

        private async Task Init()
        {
            if (_database is not null)
                return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_database is not null)
                    return;

                var db = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                
                // Activer WAL pour de meilleures perfs en lecture/écriture concurrente
                await db.ExecuteScalarAsync<string>("PRAGMA journal_mode=WAL;");

                await db.CreateTableAsync<Score>();
                await db.CreateTableAsync<Setlist>();
                await db.CreateTableAsync<SetlistScore>();
                await db.CreateTableAsync<Tag>();
                await db.CreateTableAsync<ScoreTag>();
                await db.CreateTableAsync<ScoreAudioFile>();
                await db.CreateTableAsync<Annotation>();
                await db.CreateTableAsync<FavoriteSticker>();
                
                _database = db;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }
 
        public async Task<List<Score>> GetScoresAsync()
        {
            await Init();
            var scores = await _database!.Table<Score>().ToListAsync();
            await LoadDetailsForScoresAsync(scores);
            return scores;
        }
 
        private async Task LoadDetailsForScoresAsync(List<Score> scores)
        {
            if (scores == null || scores.Count == 0 || _database == null) return;
            
            var scoreIds = scores.Select(s => s.Id).ToList();

            // Chargement CIBLÉ des relations ScoreTag
            var relevantScoreTags = await _database.Table<ScoreTag>()
                                                 .Where(st => scoreIds.Contains(st.ScoreId))
                                                 .ToListAsync();
            
            var tagIds = relevantScoreTags.Select(st => st.TagId).Distinct().ToList();
            var relevantTags = await _database.Table<Tag>()
                                            .Where(t => tagIds.Contains(t.Id))
                                            .ToListAsync();

            // Chargement CIBLÉ des fichiers audio
            var relevantAudioFiles = await _database.Table<ScoreAudioFile>()
                                                  .Where(af => scoreIds.Contains(af.ScoreId))
                                                  .ToListAsync();
 
            foreach (var score in scores)
            {
                var sId = score.Id;
                var currentScoreTagIds = relevantScoreTags.Where(st => st.ScoreId == sId).Select(st => st.TagId).ToList();
                score.AppliedTags = relevantTags.Where(t => currentScoreTagIds.Contains(t.Id)).ToList();
                
                var scoreAudioFiles = relevantAudioFiles.Where(af => af.ScoreId == sId).ToList();
                foreach (var af in scoreAudioFiles)
                {
                    var fullPath = _settingsService.GetAbsolutePath(af.FilePath, isAudio: true);
                    af.IsFileMissing = !File.Exists(fullPath);
                    af.IsExternal = Path.IsPathRooted(af.FilePath);
                }
                score.AudioFiles = scoreAudioFiles;

                var scorePath = _settingsService.GetAbsolutePath(score.FilePath);
                score.IsFileMissing = !File.Exists(scorePath);
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

        public async Task<List<Score>> SearchScoresAsync(string query, int? tagId = null)
        {
            await Init();
            
            var queryable = _database!.Table<Score>();
            
            if (!string.IsNullOrWhiteSpace(query))
                queryable = queryable.Where(s => s.Title.Contains(query));

            var scores = await queryable.ToListAsync();
            await LoadDetailsForScoresAsync(scores);

            if (tagId.HasValue)
            {
                scores = scores.Where(s => s.AppliedTags.Any(t => t.Id == tagId.Value)).ToList();
            }

            return scores;
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

        // --- Tags ---
        public async Task<List<Tag>> GetTagsAsync()
        {
            await Init();
            return await _database!.Table<Tag>().ToListAsync();
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
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"scores_backup_{timestamp}.db3";
            string backupPath = Path.Combine(folder, backupFileName);

            // On s'assure que la DB est bien fermée ou synchronisée ? 
            // Avec SQLiteAsyncConnection, une simple copie de fichier suffit si on n'écrit pas.
            File.Copy(_databasePath, backupPath, true);
            
            return backupPath;
        }

        public List<BackupFile> GetBackups()
        {
            string folder = GetBackupsFolder();
            var files = Directory.GetFiles(folder, "scores_backup_*.db3");
            
            return files.Select(f => new BackupFile 
            { 
                FileName = Path.GetFileName(f), 
                FullPath = f, 
                Date = File.GetCreationTime(f) 
            })
            .OrderByDescending(b => b.Date)
            .ToList();
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
        public bool IsExternal { get; set; }
        public string DisplayDate => Date.ToString("dd/MM/yyyy HH:mm");
    }
}
