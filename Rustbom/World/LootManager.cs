using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using RustlikeServer.Core;
using RustlikeServer.Items;
using RustlikeServer.Network;

namespace RustlikeServer.World
{
    public class LootManager
    {
        private GameServer _server;
        private Dictionary<int, LootContainer> _containers = new Dictionary<int, LootContainer>();
        private int _nextId = 1;

        public LootManager(GameServer server)
        {
            _server = server;
        }

        public void CreateLootBag(Vector3 position, ItemStack?[] inventoryItems, string ownerName)
        {
            var itemsToDrop = new List<ItemStack>();
            foreach(var item in inventoryItems)
            {
                if (item != null && !item.IsEmpty())
                {
                    itemsToDrop.Add(item);
                }
            }
            
            if (itemsToDrop.Count == 0) return; // Nada para dropar

            int id = _nextId++;
            var container = new LootContainer(id, position, ownerName);
            container.Items.AddRange(itemsToDrop);

            _containers[id] = container;

            Console.WriteLine($"[LootManager] 🎒 LootBag criada (ID: {id}) em {position} com {itemsToDrop.Count} itens.");

            // Broadcast spawn
            BroadcastLootSpawn(container);
        }

        public LootContainer? GetContainer(int id)
        {
            if (_containers.TryGetValue(id, out var container)) return container;
            return null;
        }

        public void RemoveItem(int containerId, int slotIndex, int quantity)
        {
            if (_containers.TryGetValue(containerId, out var container))
            {
                if (slotIndex >= 0 && slotIndex < container.Items.Count)
                {
                    var item = container.Items[slotIndex];
                    if (item.Quantity >= quantity)
                    {
                        item.Quantity -= quantity;
                        if (item.Quantity <= 0)
                        {
                            container.Items.RemoveAt(slotIndex);
                        }
                    }
                }

                if (container.Items.Count == 0)
                {
                    _containers.Remove(containerId);
                }
            }
        }

        private void BroadcastLootSpawn(LootContainer container)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(container.Id);
                writer.Write(container.Position.X);
                writer.Write(container.Position.Y);
                writer.Write(container.Position.Z);
                writer.Write(container.OwnerName);

                byte[] data = ms.ToArray();
                _server.BroadcastToAll(PacketType.LootContainerSpawn, data);
            }
        }
        
        public void SendContainerContent(int containerId, ClientHandler client)
        {
            if (_containers.TryGetValue(containerId, out var container))
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(container.Id);
                    writer.Write(container.Items.Count);
                    
                    foreach (var item in container.Items)
                    {
                        writer.Write(item.ItemId);
                        writer.Write(item.Quantity);
                    }

                    byte[] data = ms.ToArray();
                    client.SendPacket(PacketType.LootContainerContent, data);
                }
            }
        }

        public void Update()
        {
            var expired = _containers.Values.Where(c => c.IsExpired).ToList();
            foreach (var c in expired)
            {
                _containers.Remove(c.Id);
                Console.WriteLine($"[LootManager] LootBag {c.Id} expirou.");
            }
        }
    }
}