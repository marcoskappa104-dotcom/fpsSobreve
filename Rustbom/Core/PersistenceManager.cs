using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RustlikeServer.World;

namespace RustlikeServer.Core
{
    /// <summary>
    /// Gerencia o salvamento e carregamento de dados do servidor (Persistência)
    /// </summary>
    public class PersistenceManager
    {
        private const string DATA_FOLDER = "ServerData";
        private const string PLAYERS_FILE = "players.json";
        private const string WORLD_FILE = "world.json";

        private string _playersPath;
        private string _worldPath;

        public PersistenceManager()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string dataDir = Path.Combine(root, DATA_FOLDER);

            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            _playersPath = Path.Combine(dataDir, PLAYERS_FILE);
            _worldPath = Path.Combine(dataDir, WORLD_FILE);
        }

        public async Task SavePlayersAsync(Dictionary<int, Player> players)
        {
            try
            {
                // Converte para uma lista de DTOs (Data Transfer Objects) para serialização limpa
                var playerList = new List<PlayerData>();
                
                lock (players)
                {
                    foreach (var player in players.Values)
                    {
                        playerList.Add(player.ToData());
                    }
                }

                string json = JsonSerializer.Serialize(playerList, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_playersPath, json);
                
                Console.WriteLine($"[Persistence] ✅ Jogadores salvos: {playerList.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Persistence] ❌ Erro ao salvar jogadores: {ex.Message}");
            }
        }

        public Dictionary<int, Player> LoadPlayers()
        {
            var players = new Dictionary<int, Player>();

            if (!File.Exists(_playersPath))
            {
                Console.WriteLine("[Persistence] Nenhum arquivo de save encontrado. Iniciando novo mundo.");
                return players;
            }

            try
            {
                string json = File.ReadAllText(_playersPath);
                var playerList = JsonSerializer.Deserialize<List<PlayerData>>(json);

                if (playerList != null)
                {
                    foreach (var data in playerList)
                    {
                        var player = Player.FromData(data);
                        players[player.Id] = player;
                    }
                }

                Console.WriteLine($"[Persistence] ✅ Jogadores carregados: {players.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Persistence] ❌ Erro ao carregar jogadores: {ex.Message}");
            }

            return players;
        }
    }
}
