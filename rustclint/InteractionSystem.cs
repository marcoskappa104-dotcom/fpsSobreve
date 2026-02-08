using UnityEngine;
using RustlikeClient.World;

namespace RustlikeClient.Player
{
    public class InteractionSystem : MonoBehaviour
    {
        public float interactionDistance = 5f;
        public KeyCode interactKey = KeyCode.E;
        
        // Camada para interagir (Loot, Portas, etc)
        public LayerMask interactionLayers;

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            if (_camera == null) return;

            Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            
            // Adicione a layer do LootBag no inspector do Unity
            int mask = interactionLayers.value != 0 ? interactionLayers.value : ~0;
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, mask))
            {
                // Verifica se é LootBag
                var lootBag = hit.collider.GetComponent<LootBag>();
                if (lootBag != null)
                {
                    Debug.Log($"[Interaction] Interagindo com LootBag {lootBag.Id}");
                    LootManager.Instance.InteractWithLoot(lootBag.Id);
                }
            }
        }
    }
}
