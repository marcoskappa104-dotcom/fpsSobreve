using System;
using System.Collections.Generic;
using RustlikeServer.Items;

namespace RustlikeServer.World
{
    public class LootContainer
    {
        public int Id { get; set; }
        public Vector3 Position { get; set; }
        public List<ItemStack> Items { get; set; } = new List<ItemStack>();
        public DateTime CreatedAt { get; set; }
        public string OwnerName { get; set; } // "Mochila de [Player]"
        
        // Tempo de vida (ex: 5 minutos)
        public bool IsExpired => (DateTime.Now - CreatedAt).TotalMinutes > 5;

        public LootContainer(int id, Vector3 pos, string owner)
        {
            Id = id;
            Position = pos;
            OwnerName = owner;
            CreatedAt = DateTime.Now;
        }

        public void AddItem(ItemStack item)
        {
            Items.Add(item);
        }
    }
}