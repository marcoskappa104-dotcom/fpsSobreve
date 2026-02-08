using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using RustlikeServer.Network;
using RustlikeServer.World;
using RustlikeServer.Crafting;
using RustlikeServer.Combat;

namespace RustlikeServer.Core
{
    /// <summary>
    /// ⭐ ATUALIZADO COM SISTEMA DE CRAFTING - Servidor autoritativo UDP
    /// </summary>
    public class GameServer : INetEventListener
    {
        private NetManager _netManager;
        private Dictionary<int, Player> _players;
        private Dictionary<NetPeer, ClientHandler> _clients;
        private Dictionary<int, NetPeer> _playerPeers;
        private int _nextPlayerId;
        private bool _isRunning;
        private readonly int _port;
        public ResourceManager ResourceManager => _resourceManager;
        public CraftingManager CraftingManager => _craftingManager;
        public LootManager LootManager => _lootManager;

        private readonly object _playersLock = new object();


        // Resource Manager
        private ResourceManager _resourceManager;

        // ⭐ NOVO: Crafting Manager
        private CraftingManager _craftingManager;

        // ⭐ NOVO: Persistence Manager
        private PersistenceManager _persistenceManager;

        // ⭐ NOVO: Loot Manager
        private LootManager _lootManager;

        // Stats update
        private const float STATS_UPDATE_RATE = 1f;
        private const float STATS_SYNC_RATE = 2f;
        
        // Resource update
        private const float RESOURCE_UPDATE_RATE = 10f;

        // ⭐ NOVO: Crafting update
        private const float CRAFTING_UPDATE_RATE = 0.5f; // Verifica craftings 2x por segundo
        
        // ⭐ NOVO: Auto-save interval
        private const float AUTO_SAVE_INTERVAL = 60f; // Salva a cada 60 segundos

		private CombatManager _combatManager;

        private NetDataWriter _reusableWriter;

        public GameServer(int port = 7777)
        {
            _port = port;
            _players = new Dictionary<int, Player>();
            _clients = new Dictionary<NetPeer, ClientHandler>();
            _playerPeers = new Dictionary<int, NetPeer>();
            _nextPlayerId = 1;
            _isRunning = false;
            _reusableWriter = new NetDataWriter();
            
            // Inicializa Resource Manager
            _resourceManager = new ResourceManager();

            // ⭐ NOVO: Inicializa Crafting Manager
            _craftingManager = new CraftingManager();

            // ⭐ NOVO: Inicializa Persistence Manager
            _persistenceManager = new PersistenceManager();

            // ⭐ NOVO: Inicializa Loot Manager
            _lootManager = new LootManager(this);
			
			_combatManager = new CombatManager();
        }

        public async Task StartAsync()
        {
            try
            {
                _netManager = new NetManager(this)
                {
                    AutoRecycle = true,
                    UpdateTime = 15,
                    DisconnectTimeout = 10000,
                    PingInterval = 1000,
                    UnconnectedMessagesEnabled = false
                };

                _netManager.Start(_port);
                _isRunning = true;

                Console.WriteLine($"╔════════════════════════════════════════════════╗");
                Console.WriteLine($"║  SERVIDOR RUST-LIKE (LiteNetLib/UDP)           ║");
                Console.WriteLine($"║  Porta: {_port}                                    ║");
                Console.WriteLine($"║  Sistema de Sobrevivência: ATIVO               ║");
                Console.WriteLine($"║  Sistema de Gathering: ATIVO 🪓🪨             ║");
                Console.WriteLine($"║  Sistema de Crafting: ATIVO 🔨                ║");
				Console.WriteLine($"║  Sistema de Combate: ATIVO ⚔️                 ║");
                Console.WriteLine($"║  Aguardando conexões...                        ║");
                Console.WriteLine($"╚════════════════════════════════════════════════╝");
                Console.WriteLine();

                // Inicializa recursos do mundo
                _resourceManager.Initialize();

                // ⭐ NOVO: Inicializa crafting
                _craftingManager.Initialize();

                // ⭐ NOVO: Carrega dados persistentes
                LoadData();

                Task updateTask = UpdateLoopAsync();
                Task statsTask = UpdateStatsLoopAsync();
                Task monitorTask = MonitorPlayersAsync();
                Task resourceTask = UpdateResourcesLoopAsync();
                Task craftingTask = UpdateCraftingLoopAsync();
                Task autoSaveTask = AutoSaveLoopAsync(); // ⭐ NOVO: Inicia AutoSave

                await Task.WhenAll(updateTask, statsTask, monitorTask, resourceTask, craftingTask, autoSaveTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameServer] Erro fatal: {ex.Message}");
            }
        }

        private async Task UpdateLoopAsync()
        {
            while (_isRunning)
            {
                _netManager.PollEvents();
                _lootManager.Update();
                await Task.Delay(15);
            }
        }

        // ==================== LITENETLIB CALLBACKS ====================

        public void OnPeerConnected(NetPeer peer)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[GameServer] 🔗 Cliente conectado: {peer.Address}:{peer.Port}");
            Console.WriteLine($"[GameServer] Peer ID: {peer.Id} | Ping: {peer.Ping}ms");
            Console.ResetColor();

            var handler = new ClientHandler(peer, this);
            _clients[peer] = handler;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[GameServer] ❌ Cliente desconectado: {peer.Address}:{peer.Port}");
            Console.WriteLine($"[GameServer] Razão: {disconnectInfo.Reason}");
            Console.ResetColor();

            if (_clients.TryGetValue(peer, out var handler))
            {
                handler.Disconnect();
                _clients.Remove(peer);
            }
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            if (_clients.TryGetValue(peer, out var handler))
            {
                byte[] data = reader.GetRemainingBytes();
                _ = handler.ProcessPacketAsync(data);
            }

            reader.Recycle();
        }

        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Console.WriteLine($"[GameServer] Erro de rede: {socketError} em {endPoint}");
        }

        public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }

        // ==================== MÉTODOS PÚBLICOS ====================

        public Player GetOrCreatePlayer(string name)
        {
            lock (_playersLock)
            {
                // Tenta encontrar jogador existente pelo nome (simples auth por enquanto)
                foreach (var p in _players.Values)
                {
                    if (p.Name == name)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"\n[GameServer] ♻️ PLAYER RECONECTADO: {name} (ID: {p.Id})");
                        Console.ResetColor();
                        
                        // Atualiza status
                        p.IsConnected = true;
                        p.LastHeartbeat = DateTime.Now;
                        return p;
                    }
                }

                // Cria novo se não existir
                int id = _nextPlayerId++;
                Player player = new Player(id, name);

                player.IsConnected = true;
                player.LastHeartbeat = DateTime.Now;

                // ⭐ NOVO: Dá itens iniciais (apenas para novos players)
                StarterConfig.GiveStarterItems(player);

                _players[id] = player;
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✅ [GameServer] NOVO PLAYER CRIADO:");
                Console.WriteLine($"   → Nome: {name}");
                Console.WriteLine($"   → ID: {id}");
                Console.WriteLine($"   → Stats iniciais: {player.Stats}");
                Console.WriteLine($"   → Total de jogadores: {_players.Count}");
                Console.ResetColor();

                return player;
            }
        }

        // Removido CreatePlayer antigo para evitar confusão, usar GetOrCreatePlayer
        /*public Player CreatePlayer(string name) ... */

        private void LoadData()
        {
            Console.WriteLine("[GameServer] 📂 Carregando dados...");
            var loadedPlayers = _persistenceManager.LoadPlayers();
            
            lock (_playersLock)
            {
                _players = loadedPlayers;
                
                // Atualiza _nextPlayerId
                if (_players.Count > 0)
                {
                    _nextPlayerId = _players.Keys.Max() + 1;
                }
            }
            Console.WriteLine($"[GameServer] Dados carregados. Próximo ID: {_nextPlayerId}");
        }

        private async Task AutoSaveLoopAsync()
        {
            while (_isRunning)
            {
                await Task.Delay((int)(AUTO_SAVE_INTERVAL * 1000));
                
                Console.WriteLine("[GameServer] 💾 Auto-saving...");
                await _persistenceManager.SavePlayersAsync(_players);
            }
        }



        public void RegisterClient(int playerId, NetPeer peer, ClientHandler handler)
        {
            _playerPeers[playerId] = peer;
            _clients[peer] = handler;
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[GameServer] ClientHandler registrado: Player ID {playerId} | Total: {_clients.Count}");
            Console.ResetColor();
        }

        public void SendPacket(NetPeer peer, PacketType type, byte[] data, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            _reusableWriter.Reset();
            _reusableWriter.Put((byte)type);
            _reusableWriter.Put(data.Length);
            _reusableWriter.Put(data);
            
            peer.Send(_reusableWriter, method);
        }

        public void BroadcastToAll(PacketType type, byte[] data, int excludePlayerId = -1, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            _reusableWriter.Reset();
            _reusableWriter.Put((byte)type);
            _reusableWriter.Put(data.Length);
            _reusableWriter.Put(data);

            int sentCount = 0;

            foreach (var kvp in _playerPeers)
            {
                if (kvp.Key == excludePlayerId) continue;
                
                kvp.Value.Send(_reusableWriter, method);
                sentCount++;
            }

            if (sentCount > 0 && type != PacketType.PlayerMovement && type != PacketType.StatsUpdate && type != PacketType.ResourceUpdate)
            {
                Console.WriteLine($"[GameServer] Broadcast {type} enviado para {sentCount} jogadores");
            }
        }

        public void BroadcastPlayerSpawn(Player player)
        {
            var spawnPacket = new PlayerSpawnPacket
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                PosX = player.Position.X,
                PosY = player.Position.Y,
                PosZ = player.Position.Z
            };

            byte[] data = spawnPacket.Serialize();
            BroadcastToAll(PacketType.PlayerSpawn, data, player.Id, DeliveryMethod.ReliableOrdered);
        }

        public void BroadcastPlayerMovement(Player player, ClientHandler sender)
        {
            var movementPacket = new PlayerMovementPacket
            {
                PlayerId = player.Id,
                PosX = player.Position.X,
                PosY = player.Position.Y,
                PosZ = player.Position.Z,
                RotX = player.Rotation.X,
                RotY = player.Rotation.Y
            };

            byte[] data = movementPacket.Serialize();
            BroadcastToAll(PacketType.PlayerMovement, data, player.Id, DeliveryMethod.Sequenced);
        }

        public void BroadcastPlayerDisconnect(int playerId)
        {
            byte[] data = BitConverter.GetBytes(playerId);
            BroadcastToAll(PacketType.PlayerDisconnect, data, playerId, DeliveryMethod.ReliableOrdered);
        }

        public async Task SendExistingPlayersTo(ClientHandler newClient)
        {
            var newPlayerId = newClient.GetPlayer()?.Id ?? -1;
            var newPeer = newClient.GetPeer();
            
            int count = 0;

            List<Player> playersSnapshot;
            lock (_playersLock)
            {
                playersSnapshot = _players.Values.ToList();
            }

            foreach (var player in playersSnapshot)
            {
                if (player.Id == newPlayerId) continue;

                var spawnPacket = new PlayerSpawnPacket
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    PosX = player.Position.X,
                    PosY = player.Position.Y,
                    PosZ = player.Position.Z
                };

                byte[] data = spawnPacket.Serialize();

                try
                {
                    SendPacket(newPeer, PacketType.PlayerSpawn, data, DeliveryMethod.ReliableOrdered);
                    await Task.Delay(50);
                    count++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GameServer] Erro ao enviar player: {ex.Message}");
                }
            }
        }

        // ==================== RESOURCE METHODS ====================

        public async Task SendResourcesToClient(ClientHandler client)
        {
            var resources = _resourceManager.GetAllResources();

            var packet = new ResourcesSyncPacket();
            
            foreach (var resource in resources)
            {
                packet.Resources.Add(new ResourceData
                {
                    Id = resource.Id,
                    Type = (byte)resource.Type,
                    PosX = resource.Position.X,
                    PosY = resource.Position.Y,
                    PosZ = resource.Position.Z,
                    Health = resource.Health,
                    MaxHealth = resource.MaxHealth
                });
            }

            SendPacket(client.GetPeer(), PacketType.ResourcesSync, packet.Serialize(), DeliveryMethod.ReliableOrdered);
            
            await Task.CompletedTask;
        }

        public GatherResult GatherResource(int resourceId, float damage, int toolType, Player player)
        {
            return _resourceManager.GatherResource(resourceId, damage, toolType, player);
        }

        public void BroadcastResourceUpdate(int resourceId)
        {
            var resource = _resourceManager.GetResource(resourceId);
            if (resource == null || !resource.IsAlive) return;

            var packet = new ResourceUpdatePacket
            {
                ResourceId = resourceId,
                Health = resource.Health,
                MaxHealth = resource.MaxHealth
            };

            BroadcastToAll(PacketType.ResourceUpdate, packet.Serialize(), -1, DeliveryMethod.Unreliable);
        }

        public void BroadcastResourceDestroyed(int resourceId)
        {
            var packet = new ResourceDestroyedPacket
            {
                ResourceId = resourceId
            };

            BroadcastToAll(PacketType.ResourceDestroyed, packet.Serialize(), -1, DeliveryMethod.ReliableOrdered);
        }

        public void BroadcastResourceRespawn(int resourceId)
        {
            var resource = _resourceManager.GetResource(resourceId);
            if (resource == null || !resource.IsAlive) return;

            var packet = new ResourceRespawnPacket
            {
                ResourceId = resourceId,
                Health = resource.Health,
                MaxHealth = resource.MaxHealth
            };

            BroadcastToAll(PacketType.ResourceRespawn, packet.Serialize(), -1, DeliveryMethod.ReliableOrdered);
        }

        // ==================== ⭐ NOVO: CRAFTING METHODS ====================

        /// <summary>
        /// ⭐ NOVO: Envia receitas de crafting para um cliente
        /// </summary>
        public async Task SendRecipesToClient(ClientHandler client)
        {
            var recipes = _craftingManager.GetAllRecipes();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[GameServer] Enviando {recipes.Count} receitas para {client.GetPlayer()?.Name}");
            Console.ResetColor();

            var packet = new RecipesSyncPacket();
            
            foreach (var recipe in recipes)
            {
                var recipeData = new RecipeData
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    ResultItemId = recipe.ResultItemId,
                    ResultQuantity = recipe.ResultQuantity,
                    CraftingTime = recipe.CraftingTime,
                    RequiredWorkbench = recipe.RequiredWorkbench
                };

                foreach (var ingredient in recipe.Ingredients)
                {
                    recipeData.Ingredients.Add(new IngredientData
                    {
                        ItemId = ingredient.ItemId,
                        Quantity = ingredient.Quantity
                    });
                }

                packet.Recipes.Add(recipeData);
            }

            SendPacket(client.GetPeer(), PacketType.RecipesSync, packet.Serialize(), DeliveryMethod.ReliableOrdered);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// ⭐ NOVO: Inicia crafting para um player
        /// </summary>
        public CraftResult StartCrafting(int playerId, int recipeId)
        {
            var player = GetPlayer(playerId);
            if (player == null)
            {
                return new CraftResult
                {
                    Success = false,
                    Message = "Player não encontrado"
                };
            }

            return _craftingManager.StartCrafting(playerId, recipeId, player.Inventory);
        }

        /// <summary>
        /// ⭐ NOVO: Cancela crafting
        /// </summary>
        public bool CancelCrafting(int playerId, int queueIndex)
        {
            return _craftingManager.CancelCrafting(playerId, queueIndex);
        }

        /// <summary>
        /// ⭐ NOVO: Pega fila de crafting de um player
        /// </summary>
        public List<CraftingProgress> GetPlayerCraftQueue(int playerId)
        {
            return _craftingManager.GetPlayerQueue(playerId);
        }

        /// <summary>
        /// ⭐ NOVO: Loop de atualização de crafting
        /// </summary>
        private async Task UpdateCraftingLoopAsync()
        {
            DateTime lastUpdate = DateTime.Now;

            while (_isRunning)
            {
                await Task.Delay(500); // 2x por segundo

                DateTime now = DateTime.Now;

                if ((now - lastUpdate).TotalSeconds >= CRAFTING_UPDATE_RATE)
                {
                    lastUpdate = now;
                    
                    // Atualiza craftings e pega completados
                    var completedCrafts = _craftingManager.Update();

                    // Processa craftings completados
                    foreach (var completed in completedCrafts)
                    {
                        await HandleCraftComplete(completed);
                    }
                }
            }
        }

        /// <summary>
        /// ⭐ NOVO: Processa crafting completo
        /// </summary>
        private async Task HandleCraftComplete(CraftCompleteResult result)
        {
            var player = GetPlayer(result.PlayerId);
            if (player == null) return;

            // Adiciona item ao inventário
            bool success = player.Inventory.AddItem(result.ResultItemId, result.ResultQuantity);

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[GameServer] ✅ Crafting completo! Player {result.PlayerId} recebeu {result.ResultQuantity}x Item {result.ResultItemId}");
                Console.ResetColor();

                // Notifica o cliente
                if (_playerPeers.TryGetValue(result.PlayerId, out var peer) && 
                    _clients.TryGetValue(peer, out var handler))
                {
                    await handler.NotifyCraftComplete(
                        result.RecipeId,
                        result.ResultItemId,
                        result.ResultQuantity
                    );
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[GameServer] ❌ Inventário cheio! Item {result.ResultItemId} perdido");
                Console.ResetColor();
                
                // TODO: Dropar item no chão
            }
        }

        // ==================== STATS SYSTEM ====================

        private async Task UpdateStatsLoopAsync()
        {
            DateTime lastStatsUpdate = DateTime.Now;
            DateTime lastStatsSync = DateTime.Now;

            while (_isRunning)
            {
                await Task.Delay(100);

                DateTime now = DateTime.Now;

                if ((now - lastStatsUpdate).TotalSeconds >= STATS_UPDATE_RATE)
                {
                    lastStatsUpdate = now;
                    UpdateAllPlayersStats();
                }

                if ((now - lastStatsSync).TotalSeconds >= STATS_SYNC_RATE)
                {
                    lastStatsSync = now;
                    SyncAllPlayersStats();
                }
            }
        }

        private void UpdateAllPlayersStats()
        {
            List<Player> playersSnapshot;
            lock (_playersLock)
            {
                playersSnapshot = _players.Values.ToList();
            }

            foreach (var player in playersSnapshot)
            {
                if (!player.IsConnected) continue;

                player.UpdateStats();

                if (player.IsDead())
                {
                    HandlePlayerDeath(player);
                }
            }
        }

        private void SyncAllPlayersStats()
        {
            List<Player> playersSnapshot;
            lock (_playersLock)
            {
                playersSnapshot = _players.Values.ToList();
            }

            foreach (var player in playersSnapshot)
            {
                if (_playerPeers.TryGetValue(player.Id, out var peer))
                {
                    var statsPacket = new StatsUpdatePacket
                    {
                        PlayerId = player.Id,
                        Health = player.Stats.Health,
                        Hunger = player.Stats.Hunger,
                        Thirst = player.Stats.Thirst,
                        Temperature = player.Stats.Temperature
                    };

                    SendPacket(peer, PacketType.StatsUpdate, statsPacket.Serialize(), DeliveryMethod.Unreliable);
                }
            }
        }

        private void HandlePlayerDeath(Player player)
        {
            if (player.IsDeathHandled) return;
            player.IsDeathHandled = true;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[GameServer] ☠️  MORTE: {player.Name} (ID: {player.Id})");
            Console.ResetColor();

            var packet = new PlayerDeathDetailedPacket
            {
                VictimId = player.Id,
                KillerId = -1,
                KillerName = "Environment",
                WeaponItemId = -1,
                WasHeadshot = false,
                Distance = 0f
            };

            BroadcastToAll(PacketType.PlayerDeathDetailed, packet.Serialize(), -1, DeliveryMethod.ReliableOrdered);

            // Cria loot bag com inventário do jogador morto
            var inventorySnapshot = player.Inventory.GetAllSlots();
            _lootManager.CreateLootBag(player.Position, inventorySnapshot, player.Name);
            player.Inventory.Clear();
        }

        // ==================== RESOURCE UPDATE ====================

        private async Task UpdateResourcesLoopAsync()
        {
            DateTime lastUpdate = DateTime.Now;

            while (_isRunning)
            {
                await Task.Delay(1000);

                DateTime now = DateTime.Now;

                if ((now - lastUpdate).TotalSeconds >= RESOURCE_UPDATE_RATE)
                {
                    lastUpdate = now;
                    _resourceManager.Update();
                }
            }
        }

// ==================== NOVOS MÉTODOS DE COMBATE ====================

/// <summary>
/// ⭐ NOVO: Processa ataque entre jogadores
/// </summary>
public Combat.AttackResult ProcessAttack(
    int attackerId, 
    int victimId, 
    int weaponItemId, 
    World.Vector3 hitPosition,
    bool isHeadshot)
{
    var attacker = GetPlayer(attackerId);
    if (attacker == null)
    {
        return new Combat.AttackResult
        {
            Hit = false,
            Message = "Atacante não encontrado"
        };
    }

    return _combatManager.ProcessAttack(
        attacker,
        victimId,
        weaponItemId,
        hitPosition,
        isHeadshot,
        _players
    );
}

/// <summary>
/// ⭐ NOVO: Broadcast de morte com detalhes
/// </summary>
public void BroadcastPlayerDeath(
            int victimId,
            int killerId,
            string killerName,
            int weaponItemId,
            bool wasHeadshot)
        {
            var victim = GetPlayer(victimId);
            var killer = GetPlayer(killerId);

            if (victim == null) return;
            if (victim.IsDeathHandled) return;
            victim.IsDeathHandled = true;

            float distance = 0f;
    if (killer != null)
    {
        float dx = victim.Position.X - killer.Position.X;
        float dy = victim.Position.Y - killer.Position.Y;
        float dz = victim.Position.Z - killer.Position.Z;
        distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    var packet = new PlayerDeathDetailedPacket
    {
        VictimId = victimId,
        KillerId = killerId,
        KillerName = killerName,
        WeaponItemId = weaponItemId,
        WasHeadshot = wasHeadshot,
        Distance = distance
    };

    BroadcastToAll(PacketType.PlayerDeathDetailed, packet.Serialize(), -1, DeliveryMethod.ReliableOrdered);

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[GameServer] ☠️ {victim.Name} foi morto por {killerName}");
    Console.WriteLine($"  → Distância: {distance:F1}m");
    Console.WriteLine($"  → Headshot: {wasHeadshot}");
    Console.ResetColor();

            // Atualiza stats de combate
            var attackerState = _combatManager.GetPlayerState(killerId);
            attackerState.TotalKills++;

            var victimState = _combatManager.GetPlayerState(victimId);
            victimState.TotalDeaths++;

            // ⭐ NOVO: Cria LootBag com inventário da vítima
            _lootManager.CreateLootBag(victim.Position, victim.Inventory.GetAllSlots(), victim.Name);
            
            // Limpa inventário da vítima
            victim.Inventory.Clear();
        }

/// <summary>
/// ⭐ NOVO: Broadcast de respawn
/// </summary>
public void BroadcastPlayerRespawn(Player player)
{
    var packet = new RespawnPacket
    {
        PlayerId = player.Id,
        SpawnX = player.Position.X,
        SpawnY = player.Position.Y,
        SpawnZ = player.Position.Z
    };

    BroadcastToAll(PacketType.RespawnConfirm, packet.Serialize(), player.Id, DeliveryMethod.ReliableOrdered);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[GameServer] ♻️ {player.Name} respawnou");
    Console.ResetColor();
}

// ==================== ATUALIZAR RemovePlayer PARA LIMPAR COMBAT STATE ====================

        public void RemovePlayer(int playerId)
        {
            string playerName = "";
            bool wasConnected = false;
            World.Vector3 lastPosition = new World.Vector3(0, 0, 0);
            Items.ItemStack?[]? inventorySnapshot = null;
            NetPeer? peerToRemove = null;

            lock (_playersLock)
            {
                if (_players.TryGetValue(playerId, out var player))
                {
                    playerName = player.Name;
                    lastPosition = player.Position;
                    inventorySnapshot = player.Inventory.GetAllSlots();
                    if (player.IsConnected)
                    {
                        player.IsConnected = false;
                        wasConnected = true;
                    }
                }

                if (_playerPeers.TryGetValue(playerId, out peerToRemove))
                {
                    _playerPeers.Remove(playerId);
                }
            }

            if (wasConnected || peerToRemove != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ [GameServer] PLAYER DESCONECTADO:");
                Console.WriteLine($"   → Nome: {playerName}");
                Console.WriteLine($"   → ID: {playerId}");
                Console.WriteLine($"   → Jogadores restantes (Online): {_playerPeers.Count}");
                Console.ResetColor();
                
                if (peerToRemove != null && _clients.ContainsKey(peerToRemove))
                {
                    _clients[peerToRemove].Disconnect();
                    _clients.Remove(peerToRemove);
                }

                // Remove estado de combate
                _combatManager.RemovePlayerState(playerId);

                // Cria LootBag com inventário do jogador desconectado
                if (inventorySnapshot != null)
                {
                    _lootManager.CreateLootBag(lastPosition, inventorySnapshot, playerName);
                }

                // Limpa inventário do jogador
                var p = GetPlayer(playerId);
                if (p != null)
                {
                    p.Inventory.Clear();
                }

                BroadcastPlayerDisconnect(playerId);
            }
        }
        // ==================== MONITORING ====================

        private async Task MonitorPlayersAsync()
        {
            while (_isRunning)
            {
                await Task.Delay(120000);

                List<Player> timedOutPlayers;
                lock (_playersLock)
                {
                    timedOutPlayers = _players.Values.Where(p => p.IsConnected && p.IsTimedOut()).ToList();
                }
                
                foreach (var player in timedOutPlayers)
                {
                    RemovePlayer(player.Id);
                }

                lock (_playersLock)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n╔════════════════════════════════════════════════╗");
                    Console.WriteLine($"║  JOGADORES ONLINE: {_players.Count,-2}                         ║");
                    Console.WriteLine($"║  CLIENTS CONECTADOS: {_clients.Count,-2}                      ║");
                    Console.WriteLine($"╚════════════════════════════════════════════════╝");
                    Console.ResetColor();
                }
            }
        }

        public Player? GetPlayer(int playerId)
        {
            lock (_playersLock)
            {
                return _players.TryGetValue(playerId, out var player) ? player : null;
            }
        }

        public void Stop()
        {
            _isRunning = false;
            
            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }

            _netManager?.Stop();
            Console.WriteLine("[GameServer] Servidor encerrado");
        }
    }
}
