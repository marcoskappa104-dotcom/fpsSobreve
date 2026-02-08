using System;
using System.Collections.Generic;
using RustlikeServer.Items;

namespace RustlikeServer.World
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector2 Rotation { get; set; } // X = Yaw, Y = Pitch
        public DateTime LastHeartbeat { get; set; }
        public bool IsConnected { get; set; }

        // ⭐ NOVO: Sistema de Stats
        public PlayerStats Stats { get; private set; }

        // ⭐ NOVO: Sistema de Inventário
        public PlayerInventory Inventory { get; private set; }

        // ⭐ NOVO: Controle de Combate
        public int SelectedHotbarSlot { get; set; }
        public DateTime LastAttackTime { get; set; }
        public bool IsDeathHandled { get; set; }

        public Player(int id, string name)
        {
            Id = id;
            Name = name;
            Position = new Vector3(0, 1, 0); // Spawn inicial
            Rotation = new Vector2(0, 0);
            LastHeartbeat = DateTime.Now;
            IsConnected = false;
            SelectedHotbarSlot = 0;
            LastAttackTime = DateTime.MinValue;
            IsDeathHandled = false;

            // ⭐ Inicializa stats
            Stats = new PlayerStats();

            // ⭐ Inicializa inventário
            Inventory = new PlayerInventory();
        }

        public void UpdatePosition(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
        }

        public void UpdateRotation(float yaw, float pitch)
        {
            Rotation = new Vector2(yaw, pitch);
        }

        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.Now;
        }

        public bool IsTimedOut()
        {
            return (DateTime.Now - LastHeartbeat).TotalSeconds > 10;
        }

        /// <summary>
        /// Atualiza stats do jogador (chamado pelo servidor periodicamente)
        /// </summary>
        public void UpdateStats()
        {
            Stats.Update();
        }

        /// <summary>
        /// Aplica dano ao jogador
        /// </summary>
        public void TakeDamage(float amount, DamageType type = DamageType.Generic)
        {
            Stats.TakeDamage(amount, type);
        }

        /// <summary>
        /// Verifica se o jogador está morto
        /// </summary>
        public bool IsDead()
        {
            return Stats.IsDead;
        }

        /// <summary>
        /// Respawn do jogador
        /// </summary>
        public void Respawn()
        {
            Stats.Respawn();
            Position = new Vector3(0, 1, 0); // Reset position
            IsDeathHandled = false;
        }

        public PlayerData ToData()
        {
            var data = new PlayerData
            {
                Id = Id,
                Name = Name,
                X = Position.X,
                Y = Position.Y,
                Z = Position.Z,
                RotX = Rotation.X,
                RotY = Rotation.Y,
                Health = Stats.Health,
                Hunger = Stats.Hunger,
                Thirst = Stats.Thirst,
                Temperature = Stats.Temperature,
                IsDead = Stats.IsDead,
                IsDeathHandled = IsDeathHandled,
                InventorySlots = new List<PlayerInventorySlotData>()
            };

            var slots = Inventory.GetAllSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && !slots[i].IsEmpty())
                {
                    data.InventorySlots.Add(new PlayerInventorySlotData
                    {
                        SlotIndex = i,
                        ItemId = slots[i].ItemId,
                        Quantity = slots[i].Quantity
                    });
                }
            }

            return data;
        }

        public static Player FromData(PlayerData data)
        {
            var player = new Player(data.Id, data.Name);
            player.UpdatePosition(data.X, data.Y, data.Z);
            player.UpdateRotation(data.RotX, data.RotY);
            player.Stats.SetStats(data.Health, data.Hunger, data.Thirst, data.Temperature, data.IsDead);
            player.IsDeathHandled = data.IsDeathHandled;

            var slots = new ItemStack?[24]; // Assuming 24 size
            foreach (var slotData in data.InventorySlots)
            {
                if (slotData.SlotIndex >= 0 && slotData.SlotIndex < slots.Length)
                {
                    var itemDef = ItemDatabase.GetItem(slotData.ItemId);
                    if (itemDef != null)
                    {
                        slots[slotData.SlotIndex] = new ItemStack(itemDef, slotData.Quantity);
                    }
                }
            }
            player.Inventory.SetSlots(slots);

            return player;
        }
    }

    public class PlayerData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }
        
        public float Health { get; set; }
        public float Hunger { get; set; }
        public float Thirst { get; set; }
        public float Temperature { get; set; }
        public bool IsDead { get; set; }
        public bool IsDeathHandled { get; set; }

        public List<PlayerInventorySlotData> InventorySlots { get; set; } = new List<PlayerInventorySlotData>();
    }

    public class PlayerInventorySlotData
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    // Estruturas auxiliares
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }

    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
