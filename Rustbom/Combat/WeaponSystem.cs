using System;
using System.Collections.Generic;

namespace RustlikeServer.Combat
{
    /// <summary>
    /// Sistema de armas no servidor
    /// </summary>
    public static class WeaponSystem
    {
        private static Dictionary<int, WeaponDefinition> _weapons = new Dictionary<int, WeaponDefinition>();

        static WeaponSystem()
        {
            InitializeWeapons();
        }

        private static void InitializeWeapons()
        {
            // === ARMAS CORPO A CORPO ===
            
            // Lança de Madeira
            AddWeapon(new WeaponDefinition
            {
                ItemId = 301,
                Name = "Wooden Spear",
                Type = WeaponType.Melee,
                Damage = 35f,
                Range = 3f,
                FireRate = 1.0f,
                RequiresAmmo = false
            });

            // Machado de Pedra (como arma)
            AddWeapon(new WeaponDefinition
            {
                ItemId = 201,
                Name = "Stone Hatchet",
                Type = WeaponType.Melee,
                Damage = 25f,
                Range = 2.5f,
                FireRate = 0.8f,
                RequiresAmmo = false
            });

            // === ARMAS DE LONGO ALCANCE ===

            // Arco
            AddWeapon(new WeaponDefinition
            {
                ItemId = 302,
                Name = "Hunting Bow",
                Type = WeaponType.Ranged,
                Damage = 50f,
                Range = 50f,
                FireRate = 1.5f,
                RequiresAmmo = true,
                AmmoItemId = 305, // Arrow
                HeadshotMultiplier = 2.5f,
                ProjectileSpeed = 40f
            });

            // Revólver
            AddWeapon(new WeaponDefinition
            {
                ItemId = 303,
                Name = "Revolver",
                Type = WeaponType.Ranged,
                Damage = 40f,
                Range = 100f,
                FireRate = 0.5f,
                RequiresAmmo = true,
                AmmoItemId = 304, // Pistol Ammo
                MagazineSize = 6,
                ReloadTime = 2f,
                HeadshotMultiplier = 3f,
                ProjectileSpeed = 200f
            });

            // Espingarda
            AddWeapon(new WeaponDefinition
            {
                ItemId = 306,
                Name = "Shotgun",
                Type = WeaponType.Ranged,
                Damage = 15f, // Por pellet (8 pellets = 120 damage total)
                Range = 20f,
                FireRate = 1.2f,
                RequiresAmmo = true,
                AmmoItemId = 307, // Shotgun Shells
                MagazineSize = 4,
                ReloadTime = 3f,
                PelletCount = 8,
                Spread = 5f,
                ProjectileSpeed = 150f
            });

            // Rifle
            AddWeapon(new WeaponDefinition
            {
                ItemId = 308,
                Name = "Assault Rifle",
                Type = WeaponType.Ranged,
                Damage = 30f,
                Range = 150f,
                FireRate = 0.15f,
                RequiresAmmo = true,
                AmmoItemId = 309, // Rifle Ammo
                MagazineSize = 30,
                ReloadTime = 2.5f,
                HeadshotMultiplier = 2f,
                ProjectileSpeed = 300f,
                IsAutomatic = true
            });

            // ======= NOVOS IDS ALINHADOS COM items.json =======
            AddWeapon(new WeaponDefinition
            {
                ItemId = 500, // Hunting Bow
                Name = "Hunting Bow",
                Type = WeaponType.Ranged,
                Damage = 50f,
                Range = 80f,
                FireRate = 1.5f,
                RequiresAmmo = true,
                AmmoItemId = 600, // Wooden Arrow
                HeadshotMultiplier = 3f,
                ProjectileSpeed = 40f,
                MagazineSize = 1,
                ReloadTime = 1f
            });

            AddWeapon(new WeaponDefinition
            {
                ItemId = 502, // Revolver
                Name = "Revolver",
                Type = WeaponType.Ranged,
                Damage = 40f,
                Range = 100f,
                FireRate = 0.4f,
                RequiresAmmo = true,
                AmmoItemId = 601, // Pistol Ammo
                MagazineSize = 8,
                ReloadTime = 2.5f,
                HeadshotMultiplier = 3f,
                ProjectileSpeed = 200f
            });

            AddWeapon(new WeaponDefinition
            {
                ItemId = 503, // Waterpipe Shotgun
                Name = "Waterpipe Shotgun",
                Type = WeaponType.Ranged,
                Damage = 120f, // dano total aproximado
                Range = 30f,
                FireRate = 1.2f,
                RequiresAmmo = true,
                AmmoItemId = 603, // Handmade Shell
                MagazineSize = 1,
                ReloadTime = 1.8f,
                PelletCount = 8,
                Spread = 5f,
                ProjectileSpeed = 150f
            });

            AddWeapon(new WeaponDefinition
            {
                ItemId = 509, // Assault Rifle
                Name = "Assault Rifle",
                Type = WeaponType.Ranged,
                Damage = 30f,
                Range = 150f,
                FireRate = 0.1f,
                RequiresAmmo = true,
                AmmoItemId = 602, // 5.56 Rifle Ammo
                MagazineSize = 30,
                ReloadTime = 2.5f,
                HeadshotMultiplier = 2.5f,
                ProjectileSpeed = 300f,
                IsAutomatic = true
            });

            Console.WriteLine($"[WeaponSystem] {_weapons.Count} armas inicializadas");
        }

        private static void AddWeapon(WeaponDefinition weapon)
        {
            _weapons[weapon.ItemId] = weapon;
        }

        public static WeaponDefinition GetWeapon(int itemId)
        {
            return _weapons.TryGetValue(itemId, out var weapon) ? weapon : null;
        }

        public static bool IsWeapon(int itemId)
        {
            return _weapons.ContainsKey(itemId);
        }

        public static List<WeaponDefinition> GetAllWeapons()
        {
            return new List<WeaponDefinition>(_weapons.Values);
        }
    }

    /// <summary>
    /// Tipos de armas
    /// </summary>
    public enum WeaponType
    {
        Melee,      // Corpo a corpo
        Ranged,     // Longo alcance
        Explosive   // Explosivos
    }

    /// <summary>
    /// Definição de uma arma
    /// </summary>
    public class WeaponDefinition
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public WeaponType Type { get; set; }
        
        // Dano
        public float Damage { get; set; }
        public float HeadshotMultiplier { get; set; } = 1.5f;
        
        // Alcance e taxa de fogo
        public float Range { get; set; }
        public float FireRate { get; set; } // Segundos entre tiros
        
        // Munição
        public bool RequiresAmmo { get; set; }
        public int AmmoItemId { get; set; }
        public int MagazineSize { get; set; }
        public float ReloadTime { get; set; }
        
        // Projéteis
        public float ProjectileSpeed { get; set; } = 100f;
        public int PelletCount { get; set; } = 1; // Para shotgun
        public float Spread { get; set; } = 0f; // Dispersão
        
        // Comportamento
        public bool IsAutomatic { get; set; } = false;
        public float DurabilityLossPerShot { get; set; } = 0.1f;

        public override string ToString()
        {
            return $"{Name} ({Type}) - Damage: {Damage}, Range: {Range}m";
        }
    }

    /// <summary>
    /// Resultado de um ataque
    /// </summary>
    public class AttackResult
    {
        public bool Hit { get; set; }
        public int VictimId { get; set; }
        public float Damage { get; set; }
        public bool WasHeadshot { get; set; }
        public bool WasKill { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Hitbox types para diferentes multiplicadores de dano
    /// </summary>
    public enum HitboxType
    {
        Head,
        Torso,
        Limb
    }
}
