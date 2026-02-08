using UnityEngine;
using System.Collections.Generic;

namespace RustlikeClient.Combat
{
    /// <summary>
    /// Database de armas (Singleton MonoBehaviour)
    /// ESTE SIM PODE SER ADICIONADO COMO COMPONENTE!
    /// </summary>
    public class WeaponDatabase : MonoBehaviour
    {
        public static WeaponDatabase Instance { get; private set; }

        [Header("Weapon List")]
        [Tooltip("Lista de todas as armas disponíveis no jogo")]
        public WeaponData[] weapons;

        private Dictionary<int, WeaponData> _weaponDict;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Inicializa dicionário
            InitializeWeaponDictionary();

            Debug.Log($"[WeaponDatabase] Inicializado com {weapons.Length} armas");
        }

        /// <summary>
        /// Cria dicionário para acesso rápido por itemId
        /// </summary>
        private void InitializeWeaponDictionary()
        {
            _weaponDict = new Dictionary<int, WeaponData>();

            foreach (var weapon in weapons)
            {
                if (weapon != null && weapon.itemId > 0)
                {
                    if (!_weaponDict.ContainsKey(weapon.itemId))
                    {
                        _weaponDict.Add(weapon.itemId, weapon);
                        Debug.Log($"[WeaponDatabase] ✅ {weapon.weaponName} (ID: {weapon.itemId}) registrada");
                    }
                    else
                    {
                        Debug.LogWarning($"[WeaponDatabase] ⚠️ Arma duplicada: ID {weapon.itemId}");
                    }
                }
            }
        }

        /// <summary>
        /// Busca arma por itemId
        /// </summary>
        public WeaponData GetWeapon(int itemId)
        {
            if (_weaponDict != null && _weaponDict.TryGetValue(itemId, out WeaponData weapon))
            {
                return weapon;
            }

            Debug.LogWarning($"[WeaponDatabase] ⚠️ Arma não encontrada: ID {itemId}");
            return null;
        }

        /// <summary>
        /// Verifica se um itemId é uma arma
        /// </summary>
        public bool IsWeapon(int itemId)
        {
            return _weaponDict != null && _weaponDict.ContainsKey(itemId);
        }

        /// <summary>
        /// Retorna todas as armas
        /// </summary>
        public WeaponData[] GetAllWeapons()
        {
            return weapons;
        }

        /// <summary>
        /// Busca arma por nome (útil para debug)
        /// </summary>
        public WeaponData GetWeaponByName(string name)
        {
            foreach (var weapon in weapons)
            {
                if (weapon != null && weapon.weaponName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return weapon;
                }
            }

            Debug.LogWarning($"[WeaponDatabase] ⚠️ Arma não encontrada pelo nome: {name}");
            return null;
        }

        /// <summary>
        /// Instancia o modelo visual da arma no holder informado
        /// </summary>
        public GameObject InstantiateWeaponModel(WeaponData weapon, Transform holder)
        {
            if (weapon == null || weapon.modelPrefab == null || holder == null)
                return null;

            var model = Instantiate(weapon.modelPrefab, holder);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            return model;
        }

        /// <summary>
        /// Destroi o modelo atual da arma
        /// </summary>
        public void DestroyWeaponModel(GameObject currentModel)
        {
            if (currentModel != null)
            {
                Destroy(currentModel);
            }
        }

        /// <summary>
        /// Cria armas padrão (útil para testes)
        /// Clique com botão direito no componente e selecione "Create Default Weapons"
        /// </summary>
        [ContextMenu("Create Default Weapons")]
        public void CreateDefaultWeapons()
        {
            weapons = new WeaponData[]
            {
                // MELEE
                new WeaponData
                {
                    itemId = 400, // Wooden Spear (items.json)
                    weaponName = "Wooden Spear",
                    weaponType = WeaponType.Melee,
                    damage = 35f,
                    headshotMultiplier = 1.5f,
                    range = 3f,
                    fireRate = 1f,
                    requiresAmmo = false
                },
                new WeaponData
                {
                    itemId = 301, // Stone Hatchet (items.json)
                    weaponName = "Stone Hatchet",
                    weaponType = WeaponType.Melee,
                    damage = 30f,
                    headshotMultiplier = 1.2f,
                    range = 2.5f,
                    fireRate = 0.8f,
                    requiresAmmo = false
                },

                // RANGED
                new WeaponData
                {
                    itemId = 500, // Hunting Bow (items.json)
                    weaponName = "Hunting Bow",
                    weaponType = WeaponType.Ranged,
                    damage = 50f,
                    headshotMultiplier = 3f,
                    range = 80f,
                    fireRate = 1.5f,
                    requiresAmmo = true,
                    ammoItemId = 600, // Wooden Arrow (items.json)
                    magazineSize = 1,
                    reloadTime = 1f
                },
                new WeaponData
                {
                    itemId = 502, // Revolver (items.json)
                    weaponName = "Revolver",
                    weaponType = WeaponType.Ranged,
                    damage = 40f,
                    headshotMultiplier = 3f,
                    range = 100f,
                    fireRate = 0.4f,
                    isAutomatic = false,
                    requiresAmmo = true,
                    ammoItemId = 601, // Pistol Ammo (items.json)
                    magazineSize = 8,
                    reloadTime = 2.5f
                },
                new WeaponData
                {
                    itemId = 503, // Waterpipe Shotgun (items.json)
                    weaponName = "Waterpipe Shotgun",
                    weaponType = WeaponType.Ranged,
                    damage = 120f, // dano total aproximado em curto alcance
                    headshotMultiplier = 2f,
                    range = 30f,
                    fireRate = 1.2f,
                    requiresAmmo = true,
                    ammoItemId = 603, // Handmade Shell (items.json)
                    magazineSize = 1,
                    reloadTime = 1.8f
                },
                new WeaponData
                {
                    itemId = 509, // Assault Rifle (items.json)
                    weaponName = "Assault Rifle",
                    weaponType = WeaponType.Ranged,
                    damage = 30f,
                    headshotMultiplier = 2.5f,
                    range = 150f,
                    fireRate = 0.1f,
                    isAutomatic = true,
                    requiresAmmo = true,
                    ammoItemId = 602, // 5.56 Rifle Ammo (items.json)
                    magazineSize = 30,
                    reloadTime = 2.5f
                }
            };

            InitializeWeaponDictionary();
            Debug.Log($"[WeaponDatabase] ✅ {weapons.Length} armas padrão criadas!");

            // Marca como "dirty" para salvar no editor
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        /// <summary>
        /// Debug GUI
        /// </summary>
        private void OnGUI()
        {
            if (Input.GetKey(KeyCode.F8))
            {
                GUI.Box(new Rect(10, 120, 300, 200), "Weapon Database (F8)");

                int y = 145;
                GUI.Label(new Rect(20, y, 280, 20), $"Total Weapons: {weapons?.Length ?? 0}");
                y += 25;

                if (weapons != null)
                {
                    foreach (var weapon in weapons)
                    {
                        if (weapon != null)
                        {
                            string typeIcon = weapon.weaponType == WeaponType.Melee ? "🗡️" : "🔫";
                            GUI.Label(new Rect(20, y, 280, 20), 
                                $"{typeIcon} {weapon.weaponName} (ID: {weapon.itemId})");
                            y += 20;

                            if (y > 300) break;
                        }
                    }
                }
            }
        }
    }
}
