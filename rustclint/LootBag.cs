using UnityEngine;

namespace RustlikeClient.World
{
    public class LootBag : MonoBehaviour
    {
        public int Id { get; private set; }
        public string OwnerName { get; private set; }

        [Header("UI")]
        public TextMesh nameTag; // Opcional: mostrar nome do dono

        public void Initialize(int id, string owner)
        {
            Id = id;
            OwnerName = owner;
            
            if (nameTag != null)
            {
                nameTag.text = $"Loot de {owner}";
            }
            
            gameObject.name = $"LootBag_{id}_{owner}";
            
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = false;
                sphere.radius = 0.6f;
            }
            
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }
}
