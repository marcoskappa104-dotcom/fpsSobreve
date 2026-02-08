using UnityEngine;

namespace RustlikeClient.Combat
{
    /// <summary>
    /// Dados de uma arma no cliente
    /// </summary>
    [System.Serializable]
    public class WeaponData
    {
        public int itemId;
        public string weaponName;
        public WeaponType weaponType;

        [Header("Damage")]
        public float damage;
        public float headshotMultiplier = 2f;

        [Header("Range & Fire Rate")]
        public float range;
        public float fireRate; // Segundos entre tiros
        public bool isAutomatic;

        [Header("Ammo")]
        public bool requiresAmmo;
        public int ammoItemId;
        public int magazineSize;
        public float reloadTime;

        [Header("Visual")]
        public GameObject modelPrefab;
        public Sprite icon;

        [Header("Audio")]
        public AudioClip shootSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;

        public override string ToString()
        {
            return $"{weaponName} ({weaponType}) - Damage: {damage}";
        }
    }

    /// <summary>
    /// Tipos de armas
    /// </summary>
    public enum WeaponType
    {
        Melee,
        Ranged,
        Explosive
    }


}