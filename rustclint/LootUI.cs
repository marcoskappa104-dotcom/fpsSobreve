using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RustlikeClient.World;

namespace RustlikeClient.UI
{
    public class LootUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panel;
        public Transform itemsContainer;
        public GameObject itemSlotPrefab;
        public Text titleText;

        private List<GameObject> _currentSlots = new List<GameObject>();

        public void ShowLoot(int lootId, List<LootItemData> items)
        {
            panel.SetActive(true);
            if (titleText != null) titleText.text = $"Loot Container #{lootId}";

            // Limpa slots antigos
            foreach (var slot in _currentSlots)
            {
                Destroy(slot);
            }
            _currentSlots.Clear();

            // Cria novos slots
            for (int i = 0; i < items.Count; i++)
            {
                var itemData = items[i];
                var slotObj = Instantiate(itemSlotPrefab, itemsContainer);
                var slotUI = slotObj.GetComponent<InventorySlotUI>(); // Reusa InventorySlotUI

                if (slotUI != null)
                {
                    slotUI.SetItem(itemData.ItemId, itemData.Quantity);
                    
                    // Adiciona botão para pegar
                    int slotIndex = i; // Captura para lambda
                    var btn = slotObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnSlotClick(slotIndex, itemData.Quantity));
                    }
                }
                
                _currentSlots.Add(slotObj);
            }
        }

        private void OnSlotClick(int slotIndex, int quantity)
        {
            // Tenta pegar o item
            LootManager.Instance.TakeItem(slotIndex, quantity);
        }

        public void Hide()
        {
            panel.SetActive(false);
        }
    }
}
