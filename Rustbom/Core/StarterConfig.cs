using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RustlikeServer.Items;
using RustlikeServer.World;

namespace RustlikeServer.Core
{
    public class StarterConfig
    {
        private const string CONFIG_PATH = "Config/starter_items.json";

        public class StarterItemData
        {
            public int ItemId { get; set; }
            public int Quantity { get; set; }
        }
        
        private class StarterItemDataName
        {
            public string ItemId { get; set; }
            public int Quantity { get; set; }
        }

        public static void GiveStarterItems(Player player)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_PATH);
            
            // Garante que o diretório existe
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(path))
            {
                CreateDefaultConfig(path);
            }

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<StarterItemData>? items = null;
                
                try
                {
                    items = JsonSerializer.Deserialize<List<StarterItemData>>(json, options);
                }
                catch
                {
                    // Tenta migração: ItemId como string (nome do item)
                    var itemsByName = JsonSerializer.Deserialize<List<StarterItemDataName>>(json, options);
                    if (itemsByName != null)
                    {
                        items = new List<StarterItemData>();
                        foreach (var s in itemsByName)
                        {
                            var def = ItemDatabase.GetItemByName(s.ItemId);
                            if (def != null)
                            {
                                items.Add(new StarterItemData { ItemId = def.Id, Quantity = s.Quantity });
                            }
                            else
                            {
                                Console.WriteLine($"   ⚠️ Item desconhecido na config (nome): {s.ItemId}");
                            }
                        }
                        
                        // Migra arquivo para usar IDs (evita futuros erros)
                        try
                        {
                            string migratedJson = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(path, migratedJson);
                            Console.WriteLine("[StarterConfig] 🔄 Config migrada para usar IDs de item.");
                        }
                        catch (Exception mex)
                        {
                            Console.WriteLine($"[StarterConfig] ⚠️ Falha ao migrar config: {mex.Message}");
                        }
                    }
                }

                if (items != null)
                {
                    Console.WriteLine($"[StarterConfig] Dando itens iniciais para {player.Name}...");
                    foreach (var item in items)
                    {
                        var itemDef = ItemDatabase.GetItem(item.ItemId);
                        if (itemDef != null)
                        {
                            player.Inventory.AddItem(itemDef.Id, item.Quantity);
                            Console.WriteLine($"   + {item.Quantity}x {itemDef.Name}");
                        }
                        else
                        {
                            Console.WriteLine($"   ⚠️ Item ID desconhecido na config: {item.ItemId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StarterConfig] ❌ Erro ao dar itens iniciais: {ex.Message}");
            }
        }

        private static void CreateDefaultConfig(string path)
        {
            var defaults = new List<StarterItemData>
            {
                new StarterItemData { ItemId = 300, Quantity = 1 }, // Rock
                new StarterItemData { ItemId = 308, Quantity = 1 }  // Torch
            };

            string json = JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            Console.WriteLine("[StarterConfig] Arquivo de configuração padrão criado.");
        }
    }
}
