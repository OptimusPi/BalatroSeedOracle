// using DuckDB.NET.Data;

// namespace BalatroSeedOracle.Api;

// public class SeedSearchEngine
// {
//     private readonly string _databasePath;

//     public SeedSearchEngine(string databasePath)
//     {
//         _databasePath = databasePath;
//     }

//     public async Task<List<SeedResult>> SearchTopSeedsAsync(int topCount, CancellationToken cancellationToken)
//     {
//         var results = new List<SeedResult>();

//         await using var connection = new DuckDBConnection($"Data Source={_databasePath}");
//         await connection.OpenAsync(cancellationToken);

//         var command = connection.CreateCommand();
//         command.CommandText = "SELECT Seed, Tally FROM Seeds ORDER BY Tally DESC LIMIT @topCount";
//         command.Parameters.AddWithValue("@topCount", topCount);

//         await using var reader = await command.ExecuteReaderAsync(cancellationToken);
//         while (await reader.ReadAsync(cancellationToken))
//         {
//             var seed = reader.GetString(0);
//             var tally = reader.GetInt32(1);
//             results.Add(new SeedResult(seed, tally));
//         }

//         return results;
//     }
// }
