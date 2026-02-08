using UnityEngine;
using System.Collections.Generic;
using RustlikeClient.Network;
using System.IO;

namespace RustlikeClient.World
{
    public class LootManager : MonoBehaviour
    {
        public static LootManager Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject lootBagPrefab;

        [Header("UI")]
        public UI.LootUI lootUI;

        private Dictionary<int, LootBag> _lootBags = new Dictionary<int, LootBag>();
        private int _currentOpenLootId = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void HandleLootSpawn(byte[] data)
        {
            using (var reader = new BinaryReader(new MemoryStream(data)))
            {
                int id = reader.ReadInt32();
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                string owner = reader.ReadString();

                if (_lootBags.ContainsKey(id)) return;

                Vector3 pos = new Vector3(x, y, z);
                GameObject go = lootBagPrefab != null 
                    ? Instantiate(lootBagPrefab, pos, Quaternion.identity)
                    : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                
                if (lootBagPrefab == null)
                {
                    go.transform.position = pos;
                    go.transform.localScale = Vector3.one * 0.5f;
                }
                LootBag bag = go.GetComponent<LootBag>();
                if (bag != null)
                {
                    bag.Initialize(id, owner);
                    _lootBags.Add(id, bag);
                }
            }
        }

        public void HandleLootContent(byte[] data)
        {
            if (lootUI == null) return;

            using (var reader = new BinaryReader(new MemoryStream(data)))
            {
                int id = reader.ReadInt32();
                int itemCount = reader.ReadInt32();

                var items = new List<LootItemData>();
                for (int i = 0; i < itemCount; i++)
                {
                    int itemId = reader.ReadInt32();
                    int quantity = reader.ReadInt32();
                    items.Add(new LootItemData { ItemId = itemId, Quantity = quantity });
                }

                _currentOpenLootId = id;
                lootUI.ShowLoot(id, items);
                
                // Abre UI (precisa habilitar cursor)
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void InteractWithLoot(int id)
        {
            // Envia pacote de interação
            var packet = new Packet(PacketType.LootContainerInteract, System.BitConverter.GetBytes(id));
            NetworkManager.Instance.SendPacket(packet.Type, packet.Data);
        }

        public void TakeItem(int slotIndex, int quantity)
        {
            if (_currentOpenLootId == -1) return;

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(_currentOpenLootId);
                writer.Write(slotIndex);
                writer.Write(quantity);
                
                var packet = new Packet(PacketType.LootItemTake, ms.ToArray());
                NetworkManager.Instance.SendPacket(packet.Type, packet.Data);
            }
        }

        public void CloseLoot()
        {
            _currentOpenLootId = -1;
            lootUI?.Hide();
            
            // Retorna controle
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public class LootItemData
    {
        public int ItemId;
        public int Quantity;
    }
}
