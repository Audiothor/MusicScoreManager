using SQLite;
using MusicScoreManager.Models;

namespace MusicScoreManager.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _databasePath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public DatabaseService()
        {
            // La base de données scores.db3 selon les spécifications.
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "scores.db3");
        }

        private async Task Init()
        {
            if (_database is not null)
                return;

            await _semaphore.WaitAsync();
            try
            {
                if (_database is not null)
                    return;

                var db = new SQLiteAsyncConnection(_databasePath);
                await db.CreateTableAsync<Score>();
                await db.CreateTableAsync<Setlist>();
                await db.CreateTableAsync<SetlistScore>();
                await db.CreateTableAsync<Tag>();
                await db.CreateTableAsync<ScoreTag>();
                
                _database = db; // On ne l'assigne qu'à la toute fin
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<Score>> GetScoresAsync()
        {
            await Init();
            var scores = await _database!.Table<Score>().ToListAsync();
            await LoadTagsForScoresAsync(scores);
            return scores;
        }

        private async Task LoadTagsForScoresAsync(List<Score> scores)
        {
            if (scores == null || scores.Count == 0 || _database == null) return;
            
            var allTags = await _database.Table<Tag>().ToListAsync();
            var allScoreTags = await _database.Table<ScoreTag>().ToListAsync();

            foreach (var score in scores)
            {
                var tagIds = allScoreTags.Where(st => st.ScoreId == score.Id).Select(st => st.TagId).ToList();
                score.AppliedTags = allTags.Where(t => tagIds.Contains(t.Id)).ToList();
            }
        }

        public async Task<Score?> GetScoreAsync(int id)
        {
            await Init();
            var score = await _database!.Table<Score>().Where(i => i.Id == id).FirstOrDefaultAsync();
            if (score != null)
                await LoadTagsForScoresAsync(new List<Score> { score });
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
            await LoadTagsForScoresAsync(scores);

            if (tagId.HasValue)
            {
                scores = scores.Where(s => s.AppliedTags.Any(t => t.Id == tagId.Value)).ToList();
            }

            return scores;
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
    }
}
