using System;
using System.Text;
using System.Collections.Generic;

namespace RustlikeServer.Network
{
    // Tipos de pacotes
    public enum PacketType : byte
    {
        ConnectionRequest = 0,
        ConnectionAccept = 1,
        PlayerSpawn = 2,
        PlayerMovement = 3,
        PlayerDisconnect = 4,
        WorldState = 5,
        Heartbeat = 6,
        ClientReady = 7,
        
        // Sistema de Stats
        StatsUpdate = 8,
        PlayerDeath = 9,
        PlayerRespawn = 10,
        TakeDamage = 11,
        ConsumeItem = 12,
        
        // Sistema de Inventário
        InventoryUpdate = 13,
        ItemUse = 14,
        ItemMove = 15,
        ItemDrop = 16,
        HotbarSelect = 17,
        
        // Sistema de Gathering/Recursos
        ResourcesSync = 18,
        ResourceHit = 19,
        ResourceUpdate = 20,
        ResourceDestroyed = 21,
        ResourceRespawn = 22,
        GatherResult = 23,
        
        // ⭐ NOVO: Sistema de Crafting
        RecipesSync = 24,          // Servidor -> Cliente (envia todas as receitas)
        CraftRequest = 25,         // Cliente -> Servidor (solicita crafting)
        CraftStarted = 26,         // Servidor -> Cliente (crafting iniciado)
        CraftComplete = 27,        // Servidor -> Cliente (crafting completo)
        CraftCancel = 28,          // Cliente -> Servidor (cancela crafting)
		CraftQueueUpdate = 29,
		
		// ⭐ NOVO: Sistema de Combate
		AttackRequest = 30,      // Cliente -> Servidor (solicita ataque)
		AttackConfirm = 31,      // Servidor -> Cliente (confirma ataque)
		PlayerHit = 32,          // Servidor -> Todos (broadcast de hit)
		PlayerDeathDetailed = 33,// Servidor -> Todos (morte com detalhes)
		RespawnRequest = 34,     // Cliente -> Servidor (solicita respawn)
		RespawnConfirm = 35,     // Servidor -> Cliente (confirma respawn)
		ReloadRequest = 36,      // Cliente -> Servidor (reload de arma)
		ReloadConfirm = 37,       // Servidor -> Cliente (confirma reload)

        // ⭐ NOVO: Loot System
        LootContainerSpawn = 40,
        LootContainerInteract = 41,
        LootContainerContent = 42,
        LootContainerClose = 43,
        LootItemTake = 44
	
    }

    // Classe base para serialização de pacotes
    public class Packet
    {
        public PacketType Type { get; set; }
        public byte[] Data { get; set; }

        public Packet(PacketType type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        public byte[] Serialize()
        {
            byte[] result = new byte[1 + 4 + Data.Length];
            result[0] = (byte)Type;
            BitConverter.GetBytes(Data.Length).CopyTo(result, 1);
            Data.CopyTo(result, 5);
            return result;
        }

        public static Packet? Deserialize(byte[] data)
        {
            if (data.Length < 5) return null;
            
            PacketType type = (PacketType)data[0];
            int dataLength = BitConverter.ToInt32(data, 1);
            byte[] packetData = new byte[dataLength];
            Array.Copy(data, 5, packetData, 0, dataLength);
            
            return new Packet(type, packetData);
        }
    }

    // ==================== PACOTES EXISTENTES ====================

    public class ConnectionRequestPacket
    {
        public string PlayerName { get; set; }

        public byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(PlayerName);
        }

        public static ConnectionRequestPacket Deserialize(byte[] data)
        {
            return new ConnectionRequestPacket
            {
                PlayerName = Encoding.UTF8.GetString(data)
            };
        }
    }

    public class ConnectionAcceptPacket
    {
        public int PlayerId { get; set; }
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public float SpawnZ { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[16];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(SpawnX).CopyTo(data, 4);
            BitConverter.GetBytes(SpawnY).CopyTo(data, 8);
            BitConverter.GetBytes(SpawnZ).CopyTo(data, 12);
            return data;
        }

        public static ConnectionAcceptPacket Deserialize(byte[] data)
        {
            return new ConnectionAcceptPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                SpawnX = BitConverter.ToSingle(data, 4),
                SpawnY = BitConverter.ToSingle(data, 8),
                SpawnZ = BitConverter.ToSingle(data, 12)
            };
        }
    }

    public class PlayerMovementPacket
    {
        public int PlayerId { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[24];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(PosX).CopyTo(data, 4);
            BitConverter.GetBytes(PosY).CopyTo(data, 8);
            BitConverter.GetBytes(PosZ).CopyTo(data, 12);
            BitConverter.GetBytes(RotX).CopyTo(data, 16);
            BitConverter.GetBytes(RotY).CopyTo(data, 20);
            return data;
        }

        public static PlayerMovementPacket Deserialize(byte[] data)
        {
            return new PlayerMovementPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                PosX = BitConverter.ToSingle(data, 4),
                PosY = BitConverter.ToSingle(data, 8),
                PosZ = BitConverter.ToSingle(data, 12),
                RotX = BitConverter.ToSingle(data, 16),
                RotY = BitConverter.ToSingle(data, 20)
            };
        }
    }

    public class PlayerSpawnPacket
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }

        public byte[] Serialize()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(PlayerName);
            byte[] data = new byte[20 + nameBytes.Length];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(nameBytes.Length).CopyTo(data, 4);
            nameBytes.CopyTo(data, 8);
            BitConverter.GetBytes(PosX).CopyTo(data, 8 + nameBytes.Length);
            BitConverter.GetBytes(PosY).CopyTo(data, 12 + nameBytes.Length);
            BitConverter.GetBytes(PosZ).CopyTo(data, 16 + nameBytes.Length);
            return data;
        }

        public static PlayerSpawnPacket Deserialize(byte[] data)
        {
            int nameLength = BitConverter.ToInt32(data, 4);
            return new PlayerSpawnPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                PlayerName = Encoding.UTF8.GetString(data, 8, nameLength),
                PosX = BitConverter.ToSingle(data, 8 + nameLength),
                PosY = BitConverter.ToSingle(data, 12 + nameLength),
                PosZ = BitConverter.ToSingle(data, 16 + nameLength)
            };
        }
    }

    public class StatsUpdatePacket
    {
        public int PlayerId { get; set; }
        public float Health { get; set; }
        public float Hunger { get; set; }
        public float Thirst { get; set; }
        public float Temperature { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[20];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(Health).CopyTo(data, 4);
            BitConverter.GetBytes(Hunger).CopyTo(data, 8);
            BitConverter.GetBytes(Thirst).CopyTo(data, 12);
            BitConverter.GetBytes(Temperature).CopyTo(data, 16);
            return data;
        }

        public static StatsUpdatePacket Deserialize(byte[] data)
        {
            return new StatsUpdatePacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                Health = BitConverter.ToSingle(data, 4),
                Hunger = BitConverter.ToSingle(data, 8),
                Thirst = BitConverter.ToSingle(data, 12),
                Temperature = BitConverter.ToSingle(data, 16)
            };
        }
    }

    public class PlayerDeathPacket
    {
        public int PlayerId { get; set; }
        public string KillerName { get; set; }

        public byte[] Serialize()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(KillerName ?? "");
            byte[] data = new byte[8 + nameBytes.Length];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(nameBytes.Length).CopyTo(data, 4);
            nameBytes.CopyTo(data, 8);
            return data;
        }

        public static PlayerDeathPacket Deserialize(byte[] data)
        {
            int nameLength = BitConverter.ToInt32(data, 4);
            return new PlayerDeathPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                KillerName = nameLength > 0 ? Encoding.UTF8.GetString(data, 8, nameLength) : ""
            };
        }
    }

    public class InventoryUpdatePacket
    {
        public List<InventorySlotData> Slots { get; set; }

        public InventoryUpdatePacket()
        {
            Slots = new List<InventorySlotData>();
        }

        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Slots.Count));

            foreach (var slot in Slots)
            {
                data.AddRange(BitConverter.GetBytes(slot.SlotIndex));
                data.AddRange(BitConverter.GetBytes(slot.ItemId));
                data.AddRange(BitConverter.GetBytes(slot.Quantity));
            }

            return data.ToArray();
        }

        public static InventoryUpdatePacket Deserialize(byte[] data)
        {
            var packet = new InventoryUpdatePacket();
            int offset = 0;

            int slotCount = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < slotCount; i++)
            {
                int slotIndex = BitConverter.ToInt32(data, offset);
                offset += 4;
                int itemId = BitConverter.ToInt32(data, offset);
                offset += 4;
                int quantity = BitConverter.ToInt32(data, offset);
                offset += 4;

                packet.Slots.Add(new InventorySlotData
                {
                    SlotIndex = slotIndex,
                    ItemId = itemId,
                    Quantity = quantity
                });
            }

            return packet;
        }
    }

    public class InventorySlotData
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class ItemUsePacket
    {
        public int SlotIndex { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(SlotIndex);
        }

        public static ItemUsePacket Deserialize(byte[] data)
        {
            return new ItemUsePacket
            {
                SlotIndex = BitConverter.ToInt32(data, 0)
            };
        }
    }

    public class ItemMovePacket
    {
        public int FromSlot { get; set; }
        public int ToSlot { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[8];
            BitConverter.GetBytes(FromSlot).CopyTo(data, 0);
            BitConverter.GetBytes(ToSlot).CopyTo(data, 4);
            return data;
        }

        public static ItemMovePacket Deserialize(byte[] data)
        {
            return new ItemMovePacket
            {
                FromSlot = BitConverter.ToInt32(data, 0),
                ToSlot = BitConverter.ToInt32(data, 4)
            };
        }
    }

    public class HotbarSelectPacket
    {
        public int SlotIndex { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(SlotIndex);
        }

        public static HotbarSelectPacket Deserialize(byte[] data)
        {
            return new HotbarSelectPacket
            {
                SlotIndex = BitConverter.ToInt32(data, 0)
            };
        }
    }

    // ==================== PACOTES DE RECURSOS ====================

    public class ResourcesSyncPacket
    {
        public List<ResourceData> Resources { get; set; }

        public ResourcesSyncPacket()
        {
            Resources = new List<ResourceData>();
        }

        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Resources.Count));

            foreach (var res in Resources)
            {
                data.AddRange(BitConverter.GetBytes(res.Id));
                data.Add((byte)res.Type);
                data.AddRange(BitConverter.GetBytes(res.PosX));
                data.AddRange(BitConverter.GetBytes(res.PosY));
                data.AddRange(BitConverter.GetBytes(res.PosZ));
                data.AddRange(BitConverter.GetBytes(res.Health));
                data.AddRange(BitConverter.GetBytes(res.MaxHealth));
            }

            return data.ToArray();
        }

        public static ResourcesSyncPacket Deserialize(byte[] data)
        {
            var packet = new ResourcesSyncPacket();
            int offset = 0;

            int count = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < count; i++)
            {
                packet.Resources.Add(new ResourceData
                {
                    Id = BitConverter.ToInt32(data, offset),
                    Type = data[offset + 4],
                    PosX = BitConverter.ToSingle(data, offset + 5),
                    PosY = BitConverter.ToSingle(data, offset + 9),
                    PosZ = BitConverter.ToSingle(data, offset + 13),
                    Health = BitConverter.ToSingle(data, offset + 17),
                    MaxHealth = BitConverter.ToSingle(data, offset + 21)
                });
                offset += 25;
            }

            return packet;
        }
    }

    public class ResourceData
    {
        public int Id { get; set; }
        public byte Type { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
    }

    public class ResourceHitPacket
    {
        public int ResourceId { get; set; }
        public float Damage { get; set; }
        public int ToolType { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(ResourceId).CopyTo(data, 0);
            BitConverter.GetBytes(Damage).CopyTo(data, 4);
            BitConverter.GetBytes(ToolType).CopyTo(data, 8);
            return data;
        }

        public static ResourceHitPacket Deserialize(byte[] data)
        {
            return new ResourceHitPacket
            {
                ResourceId = BitConverter.ToInt32(data, 0),
                Damage = BitConverter.ToSingle(data, 4),
                ToolType = BitConverter.ToInt32(data, 8)
            };
        }
    }

    public class ResourceUpdatePacket
    {
        public int ResourceId { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(ResourceId).CopyTo(data, 0);
            BitConverter.GetBytes(Health).CopyTo(data, 4);
            BitConverter.GetBytes(MaxHealth).CopyTo(data, 8);
            return data;
        }

        public static ResourceUpdatePacket Deserialize(byte[] data)
        {
            return new ResourceUpdatePacket
            {
                ResourceId = BitConverter.ToInt32(data, 0),
                Health = BitConverter.ToSingle(data, 4),
                MaxHealth = BitConverter.ToSingle(data, 8)
            };
        }
    }

    public class ResourceDestroyedPacket
    {
        public int ResourceId { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(ResourceId);
        }

        public static ResourceDestroyedPacket Deserialize(byte[] data)
        {
            return new ResourceDestroyedPacket
            {
                ResourceId = BitConverter.ToInt32(data, 0)
            };
        }
    }

    public class ResourceRespawnPacket
    {
        public int ResourceId { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(ResourceId).CopyTo(data, 0);
            BitConverter.GetBytes(Health).CopyTo(data, 4);
            BitConverter.GetBytes(MaxHealth).CopyTo(data, 8);
            return data;
        }

        public static ResourceRespawnPacket Deserialize(byte[] data)
        {
            return new ResourceRespawnPacket
            {
                ResourceId = BitConverter.ToInt32(data, 0),
                Health = BitConverter.ToSingle(data, 4),
                MaxHealth = BitConverter.ToSingle(data, 8)
            };
        }
    }

    public class GatherResultPacket
    {
        public int WoodGained { get; set; }
        public int StoneGained { get; set; }
        public int MetalGained { get; set; }
        public int SulfurGained { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[16];
            BitConverter.GetBytes(WoodGained).CopyTo(data, 0);
            BitConverter.GetBytes(StoneGained).CopyTo(data, 4);
            BitConverter.GetBytes(MetalGained).CopyTo(data, 8);
            BitConverter.GetBytes(SulfurGained).CopyTo(data, 12);
            return data;
        }

        public static GatherResultPacket Deserialize(byte[] data)
        {
            return new GatherResultPacket
            {
                WoodGained = BitConverter.ToInt32(data, 0),
                StoneGained = BitConverter.ToInt32(data, 4),
                MetalGained = BitConverter.ToInt32(data, 8),
                SulfurGained = BitConverter.ToInt32(data, 12)
            };
        }
    }

    // ==================== ⭐ NOVOS PACOTES DE CRAFTING ====================

    /// <summary>
    /// Envia todas as receitas para o cliente
    /// </summary>
    public class RecipesSyncPacket
    {
        public List<RecipeData> Recipes { get; set; }

        public RecipesSyncPacket()
        {
            Recipes = new List<RecipeData>();
        }

        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Recipes.Count));

            foreach (var recipe in Recipes)
            {
                // Recipe ID
                data.AddRange(BitConverter.GetBytes(recipe.Id));
                
                // Recipe Name
                byte[] nameBytes = Encoding.UTF8.GetBytes(recipe.Name);
                data.AddRange(BitConverter.GetBytes(nameBytes.Length));
                data.AddRange(nameBytes);
                
                // Result Item
                data.AddRange(BitConverter.GetBytes(recipe.ResultItemId));
                data.AddRange(BitConverter.GetBytes(recipe.ResultQuantity));
                
                // Crafting Time
                data.AddRange(BitConverter.GetBytes(recipe.CraftingTime));
                
                // Required Workbench
                data.AddRange(BitConverter.GetBytes(recipe.RequiredWorkbench));
                
                // Ingredients
                data.AddRange(BitConverter.GetBytes(recipe.Ingredients.Count));
                foreach (var ingredient in recipe.Ingredients)
                {
                    data.AddRange(BitConverter.GetBytes(ingredient.ItemId));
                    data.AddRange(BitConverter.GetBytes(ingredient.Quantity));
                }
            }

            return data.ToArray();
        }

        public static RecipesSyncPacket Deserialize(byte[] data)
        {
            var packet = new RecipesSyncPacket();
            int offset = 0;

            int recipeCount = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < recipeCount; i++)
            {
                var recipe = new RecipeData();
                
                recipe.Id = BitConverter.ToInt32(data, offset);
                offset += 4;
                
                int nameLength = BitConverter.ToInt32(data, offset);
                offset += 4;
                recipe.Name = Encoding.UTF8.GetString(data, offset, nameLength);
                offset += nameLength;
                
                recipe.ResultItemId = BitConverter.ToInt32(data, offset);
                offset += 4;
                recipe.ResultQuantity = BitConverter.ToInt32(data, offset);
                offset += 4;
                
                recipe.CraftingTime = BitConverter.ToSingle(data, offset);
                offset += 4;
                
                recipe.RequiredWorkbench = BitConverter.ToInt32(data, offset);
                offset += 4;
                
                int ingredientCount = BitConverter.ToInt32(data, offset);
                offset += 4;
                
                for (int j = 0; j < ingredientCount; j++)
                {
                    int itemId = BitConverter.ToInt32(data, offset);
                    offset += 4;
                    int quantity = BitConverter.ToInt32(data, offset);
                    offset += 4;
                    
                    recipe.Ingredients.Add(new IngredientData
                    {
                        ItemId = itemId,
                        Quantity = quantity
                    });
                }
                
                packet.Recipes.Add(recipe);
            }

            return packet;
        }
    }

    public class RecipeData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ResultItemId { get; set; }
        public int ResultQuantity { get; set; }
        public float CraftingTime { get; set; }
        public int RequiredWorkbench { get; set; }
        public List<IngredientData> Ingredients { get; set; }

        public RecipeData()
        {
            Ingredients = new List<IngredientData>();
        }
    }

    public class IngredientData
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Cliente solicita crafting de uma receita
    /// </summary>
    public class CraftRequestPacket
    {
        public int RecipeId { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(RecipeId);
        }

        public static CraftRequestPacket Deserialize(byte[] data)
        {
            return new CraftRequestPacket
            {
                RecipeId = BitConverter.ToInt32(data, 0)
            };
        }
    }

    /// <summary>
    /// Servidor confirma início do crafting
    /// </summary>
    public class CraftStartedPacket
    {
        public int RecipeId { get; set; }
        public float Duration { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

        public byte[] Serialize()
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(Message ?? "");
            byte[] data = new byte[13 + messageBytes.Length];
            
            BitConverter.GetBytes(RecipeId).CopyTo(data, 0);
            BitConverter.GetBytes(Duration).CopyTo(data, 4);
            data[8] = Success ? (byte)1 : (byte)0;
            BitConverter.GetBytes(messageBytes.Length).CopyTo(data, 9);
            messageBytes.CopyTo(data, 13);
            
            return data;
        }

        public static CraftStartedPacket Deserialize(byte[] data)
        {
            int messageLength = BitConverter.ToInt32(data, 9);
            return new CraftStartedPacket
            {
                RecipeId = BitConverter.ToInt32(data, 0),
                Duration = BitConverter.ToSingle(data, 4),
                Success = data[8] == 1,
                Message = messageLength > 0 ? Encoding.UTF8.GetString(data, 13, messageLength) : ""
            };
        }
    }

    /// <summary>
    /// Servidor notifica que crafting foi completo
    /// </summary>
    public class CraftCompletePacket
    {
        public int RecipeId { get; set; }
        public int ResultItemId { get; set; }
        public int ResultQuantity { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(RecipeId).CopyTo(data, 0);
            BitConverter.GetBytes(ResultItemId).CopyTo(data, 4);
            BitConverter.GetBytes(ResultQuantity).CopyTo(data, 8);
            return data;
        }

        public static CraftCompletePacket Deserialize(byte[] data)
        {
            return new CraftCompletePacket
            {
                RecipeId = BitConverter.ToInt32(data, 0),
                ResultItemId = BitConverter.ToInt32(data, 4),
                ResultQuantity = BitConverter.ToInt32(data, 8)
            };
        }
    }

    /// <summary>
    /// Cliente solicita cancelamento de crafting
    /// </summary>
    public class CraftCancelPacket
    {
        public int QueueIndex { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(QueueIndex);
        }

        public static CraftCancelPacket Deserialize(byte[] data)
        {
            return new CraftCancelPacket
            {
                QueueIndex = BitConverter.ToInt32(data, 0)
            };
        }
    }

    /// <summary>
    /// Servidor envia atualização da fila de crafting
    /// </summary>
    public class CraftQueueUpdatePacket
    {
        public List<CraftQueueItem> QueueItems { get; set; }

        public CraftQueueUpdatePacket()
        {
            QueueItems = new List<CraftQueueItem>();
        }

        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(QueueItems.Count));

            foreach (var item in QueueItems)
            {
                data.AddRange(BitConverter.GetBytes(item.RecipeId));
                data.AddRange(BitConverter.GetBytes(item.Progress));
                data.AddRange(BitConverter.GetBytes(item.RemainingTime));
            }

            return data.ToArray();
        }

        public static CraftQueueUpdatePacket Deserialize(byte[] data)
        {
            var packet = new CraftQueueUpdatePacket();
            int offset = 0;

            int count = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < count; i++)
            {
                packet.QueueItems.Add(new CraftQueueItem
                {
                    RecipeId = BitConverter.ToInt32(data, offset),
                    Progress = BitConverter.ToSingle(data, offset + 4),
                    RemainingTime = BitConverter.ToSingle(data, offset + 8)
                });
                offset += 12;
            }

            return packet;
        }
    }

    public class CraftQueueItem
    {
        public int RecipeId { get; set; }
        public float Progress { get; set; }       // 0.0 a 1.0
        public float RemainingTime { get; set; }  // Em segundos
    }

    // ==================== ⭐ NOVOS PACOTES DE COMBATE (SINCRONIZADO COM CLIENTE) ====================

    public class AttackRequestPacket
    {
        public int VictimId { get; set; }
        public int WeaponItemId { get; set; }
        public float HitPosX { get; set; }
        public float HitPosY { get; set; }
        public float HitPosZ { get; set; }
        public bool IsHeadshot { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[22];
            BitConverter.GetBytes(VictimId).CopyTo(data, 0);
            BitConverter.GetBytes(WeaponItemId).CopyTo(data, 4);
            BitConverter.GetBytes(HitPosX).CopyTo(data, 8);
            BitConverter.GetBytes(HitPosY).CopyTo(data, 12);
            BitConverter.GetBytes(HitPosZ).CopyTo(data, 16);
            data[20] = IsHeadshot ? (byte)1 : (byte)0;
            return data;
        }

        public static AttackRequestPacket Deserialize(byte[] data)
        {
            return new AttackRequestPacket
            {
                VictimId = BitConverter.ToInt32(data, 0),
                WeaponItemId = BitConverter.ToInt32(data, 4),
                HitPosX = BitConverter.ToSingle(data, 8),
                HitPosY = BitConverter.ToSingle(data, 12),
                HitPosZ = BitConverter.ToSingle(data, 16),
                IsHeadshot = data[20] == 1
            };
        }
    }

    public class AttackConfirmPacket
    {
        public bool Success { get; set; }
        public int VictimId { get; set; }
        public float Damage { get; set; }
        public bool WasHeadshot { get; set; }
        public bool WasKill { get; set; }
        public string Message { get; set; }

        public byte[] Serialize()
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(Message ?? "");
            byte[] data = new byte[15 + messageBytes.Length];
            
            data[0] = Success ? (byte)1 : (byte)0;
            BitConverter.GetBytes(VictimId).CopyTo(data, 1);
            BitConverter.GetBytes(Damage).CopyTo(data, 5);
            data[9] = WasHeadshot ? (byte)1 : (byte)0;
            data[10] = WasKill ? (byte)1 : (byte)0;
            BitConverter.GetBytes(messageBytes.Length).CopyTo(data, 11);
            messageBytes.CopyTo(data, 15);
            
            return data;
        }

        public static AttackConfirmPacket Deserialize(byte[] data)
        {
            int messageLength = BitConverter.ToInt32(data, 11);
            return new AttackConfirmPacket
            {
                Success = data[0] == 1,
                VictimId = BitConverter.ToInt32(data, 1),
                Damage = BitConverter.ToSingle(data, 5),
                WasHeadshot = data[9] == 1,
                WasKill = data[10] == 1,
                Message = messageLength > 0 ? Encoding.UTF8.GetString(data, 15, messageLength) : ""
            };
        }
    }

    public class PlayerHitPacket
    {
        public int VictimId { get; set; }
        public int AttackerId { get; set; }
        public float Damage { get; set; }
        public bool WasHeadshot { get; set; }
        public float HitPosX { get; set; }
        public float HitPosY { get; set; }
        public float HitPosZ { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[25];
            BitConverter.GetBytes(VictimId).CopyTo(data, 0);
            BitConverter.GetBytes(AttackerId).CopyTo(data, 4);
            BitConverter.GetBytes(Damage).CopyTo(data, 8);
            data[12] = WasHeadshot ? (byte)1 : (byte)0;
            BitConverter.GetBytes(HitPosX).CopyTo(data, 13);
            BitConverter.GetBytes(HitPosY).CopyTo(data, 17);
            BitConverter.GetBytes(HitPosZ).CopyTo(data, 21);
            return data;
        }

        public static PlayerHitPacket Deserialize(byte[] data)
        {
            return new PlayerHitPacket
            {
                VictimId = BitConverter.ToInt32(data, 0),
                AttackerId = BitConverter.ToInt32(data, 4),
                Damage = BitConverter.ToSingle(data, 8),
                WasHeadshot = data[12] == 1,
                HitPosX = BitConverter.ToSingle(data, 13),
                HitPosY = BitConverter.ToSingle(data, 17),
                HitPosZ = BitConverter.ToSingle(data, 21)
            };
        }
    }

    public class PlayerDeathDetailedPacket
    {
        public int VictimId { get; set; }
        public int KillerId { get; set; }
        public string KillerName { get; set; }
        public int WeaponItemId { get; set; }
        public bool WasHeadshot { get; set; }
        public float Distance { get; set; }

        public byte[] Serialize()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(KillerName ?? "Unknown");
            byte[] data = new byte[21 + nameBytes.Length];
            
            BitConverter.GetBytes(VictimId).CopyTo(data, 0);
            BitConverter.GetBytes(KillerId).CopyTo(data, 4);
            BitConverter.GetBytes(nameBytes.Length).CopyTo(data, 8);
            nameBytes.CopyTo(data, 12);
            int offset = 12 + nameBytes.Length;
            BitConverter.GetBytes(WeaponItemId).CopyTo(data, offset);
            data[offset + 4] = WasHeadshot ? (byte)1 : (byte)0;
            BitConverter.GetBytes(Distance).CopyTo(data, offset + 5);
            
            return data;
        }

        public static PlayerDeathDetailedPacket Deserialize(byte[] data)
        {
            int nameLength = BitConverter.ToInt32(data, 8);
            return new PlayerDeathDetailedPacket
            {
                VictimId = BitConverter.ToInt32(data, 0),
                KillerId = BitConverter.ToInt32(data, 4),
                KillerName = Encoding.UTF8.GetString(data, 12, nameLength),
                WeaponItemId = BitConverter.ToInt32(data, 12 + nameLength),
                WasHeadshot = data[12 + nameLength + 4] == 1,
                Distance = BitConverter.ToSingle(data, 12 + nameLength + 5)
            };
        }
    }

    public class RespawnRequestPacket
    {
        // Vazio, apenas sinaliza intenção
        public byte[] Serialize() => new byte[0];
        public static RespawnRequestPacket Deserialize(byte[] data) => new RespawnRequestPacket();
    }

    /// <summary>
    /// Respawn após morte (Confirmação)
    /// </summary>
    [Serializable]
    public class RespawnPacket
    {
        public int PlayerId { get; set; }
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public float SpawnZ { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[16];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(SpawnX).CopyTo(data, 4);
            BitConverter.GetBytes(SpawnY).CopyTo(data, 8);
            BitConverter.GetBytes(SpawnZ).CopyTo(data, 12);
            return data;
        }

        public static RespawnPacket Deserialize(byte[] data)
        {
            return new RespawnPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                SpawnX = BitConverter.ToSingle(data, 4),
                SpawnY = BitConverter.ToSingle(data, 8),
                SpawnZ = BitConverter.ToSingle(data, 12)
            };
        }
    }

    /// <summary>
    /// Cliente solicita reload de arma
    /// </summary>
    [Serializable]
    public class ReloadRequestPacket
    {
        public int WeaponItemId { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(WeaponItemId);
        }

        public static ReloadRequestPacket Deserialize(byte[] data)
        {
            return new ReloadRequestPacket
            {
                WeaponItemId = BitConverter.ToInt32(data, 0)
            };
        }
    }

    /// <summary>
    /// Confirma reload
    /// </summary>
    [Serializable]
    public class ReloadConfirmPacket
    {
        public bool Success { get; set; }
        public int AmmoRemaining { get; set; }
        public string Message { get; set; }

        public byte[] Serialize()
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(Message ?? "");
            byte[] data = new byte[9 + messageBytes.Length];
            
            data[0] = Success ? (byte)1 : (byte)0;
            BitConverter.GetBytes(AmmoRemaining).CopyTo(data, 1);
            BitConverter.GetBytes(messageBytes.Length).CopyTo(data, 5);
            messageBytes.CopyTo(data, 9);
            
            return data;
        }

        public static ReloadConfirmPacket Deserialize(byte[] data)
        {
            int messageLength = BitConverter.ToInt32(data, 5);
            return new ReloadConfirmPacket
            {
                Success = data[0] == 1,
                AmmoRemaining = BitConverter.ToInt32(data, 1),
                Message = messageLength > 0 ? Encoding.UTF8.GetString(data, 9, messageLength) : ""
            };
        }
    }
}