using System;
using System.Collections.Generic;
using RustlikeServer.World;

namespace RustlikeServer.Combat
{
    /// <summary>
    /// Gerencia combate, ataques e mortes no servidor
    /// </summary>
    public class CombatManager
    {
        private Dictionary<int, PlayerCombatState> _playerStates;
        private readonly object _combatLock = new object();

        // Configurações de balanceamento
        private const float MAX_ATTACK_DISTANCE = 200f; // Distância máxima de ataque
        private const float HEADSHOT_HEIGHT_THRESHOLD = 1.6f; // Altura mínima para headshot
        
        public CombatManager()
        {
            _playerStates = new Dictionary<int, PlayerCombatState>();
        }

        /// <summary>
        /// Processa ataque de um jogador
        /// </summary>
        public AttackResult ProcessAttack(
            Player attacker, 
            int victimId, 
            int weaponItemId, 
            Vector3 hitPosition,
            bool isHeadshot,
            Dictionary<int, Player> allPlayers)
        {
            lock (_combatLock)
            {
                // Valida arma
                var weapon = WeaponSystem.GetWeapon(weaponItemId);
                if (weapon == null)
                {
                    return new AttackResult
                    {
                        Hit = false,
                        Message = "Arma inválida"
                    };
                }

                // ⭐ NOVO: Validação de FireRate
                double timeSinceLastAttack = (DateTime.Now - attacker.LastAttackTime).TotalSeconds;
                if (timeSinceLastAttack < weapon.FireRate)
                {
                    return new AttackResult
                    {
                        Hit = false,
                        Message = $"FireRate excedido ({timeSinceLastAttack:F2}s < {weapon.FireRate}s)"
                    };
                }

                // ⭐ NOVO: Validação de Item Equipado
                var equippedItem = attacker.Inventory.GetItem(attacker.SelectedHotbarSlot);
                
                // DEBUG: Log para diagnosticar problema de dano
                if (equippedItem == null || equippedItem.ItemId != weaponItemId)
                {
                    int equippedId = equippedItem?.ItemId ?? -1;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Combat] ❌ ERRO DE VALIDAÇÃO: Player {attacker.Name} tentou atacar com {weapon.Name} (ID {weaponItemId})");
                    Console.WriteLine($"  → Slot Selecionado: {attacker.SelectedHotbarSlot}");
                    Console.WriteLine($"  → Item no Slot: {equippedId} (Esperado: {weaponItemId})");
                    Console.ResetColor();

                    return new AttackResult
                    {
                        Hit = false,
                        Message = $"Arma incorreta (Slot {attacker.SelectedHotbarSlot} tem {equippedId})"
                    };
                }

                // Verifica se tem munição (se necessário)
                if (weapon.RequiresAmmo)
                {
                    if (!attacker.Inventory.HasItem(weapon.AmmoItemId, 1))
                    {
                        return new AttackResult
                        {
                            Hit = false,
                            Message = "Sem munição"
                        };
                    }

                    // Consome munição
                    attacker.Inventory.RemoveItem(weapon.AmmoItemId, 1);
                }

                // Valida vítima
                if (!allPlayers.TryGetValue(victimId, out var victim))
                {
                    return new AttackResult
                    {
                        Hit = false,
                        Message = "Vítima não encontrada"
                    };
                }

                if (victim.IsDead())
                {
                    return new AttackResult
                    {
                        Hit = false,
                        Message = "Vítima já está morta"
                    };
                }

                // Valida distância
                float distance = CalculateDistance(attacker.Position, victim.Position);
                if (distance > weapon.Range || distance > MAX_ATTACK_DISTANCE)
                {
                    return new AttackResult
                    {
                        Hit = false,
                        Message = $"Muito longe ({distance:F1}m > {weapon.Range}m)"
                    };
                }

                // Calcula dano
                float damage = CalculateDamage(weapon, distance, isHeadshot);

                // Aplica dano
                victim.TakeDamage(damage, DamageType.Bullet);

                // Registra estado de combate
                UpdateCombatState(attacker.Id, victim.Id);

                // Verifica morte
                bool wasKill = victim.IsDead();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Combat] {attacker.Name} atacou {victim.Name} com {weapon.Name}");
                Console.WriteLine($"  → Distância: {distance:F1}m");
                Console.WriteLine($"  → Headshot: {isHeadshot}");
                Console.WriteLine($"  → Dano: {damage:F1}");
                Console.WriteLine($"  → HP restante: {victim.Stats.Health:F1}");
                if (wasKill)
                {
                    Console.WriteLine($"  → ☠️ KILL!");
                }
                Console.ResetColor();

                return new AttackResult
                {
                    Hit = true,
                    VictimId = victimId,
                    Damage = damage,
                    WasHeadshot = isHeadshot,
                    WasKill = wasKill,
                    Message = wasKill ? "KILL!" : "Hit!"
                };
            }
        }

        /// <summary>
        /// Calcula dano baseado em arma, distância e headshot
        /// </summary>
        private float CalculateDamage(WeaponDefinition weapon, float distance, bool isHeadshot)
        {
            float damage = weapon.Damage;

            // Multiplicador de headshot
            if (isHeadshot)
            {
                damage *= weapon.HeadshotMultiplier;
            }

            // Falloff de distância (dano reduz com distância)
            if (weapon.Type == WeaponType.Ranged && weapon.Range > 0)
            {
                float distanceRatio = distance / weapon.Range;
                
                // Dano total até 50% da range, depois começa a cair
                if (distanceRatio > 0.5f)
                {
                    float falloff = 1f - ((distanceRatio - 0.5f) * 0.6f); // Até 30% de redução
                    damage *= Math.Max(0.4f, falloff);
                }
            }

            return damage;
        }

        /// <summary>
        /// Calcula distância entre dois pontos
        /// </summary>
        private float CalculateDistance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Atualiza estado de combate (para sistemas de karma, etc)
        /// </summary>
        private void UpdateCombatState(int attackerId, int victimId)
        {
            if (!_playerStates.ContainsKey(attackerId))
            {
                _playerStates[attackerId] = new PlayerCombatState();
            }

            var state = _playerStates[attackerId];
            state.LastAttackTime = DateTime.Now;
            state.TotalAttacks++;

            if (!state.AttackedPlayers.Contains(victimId))
            {
                state.AttackedPlayers.Add(victimId);
            }
        }

        /// <summary>
        /// Pega estado de combate de um player
        /// </summary>
        public PlayerCombatState GetPlayerState(int playerId)
        {
            lock (_combatLock)
            {
                if (!_playerStates.ContainsKey(playerId))
                {
                    _playerStates[playerId] = new PlayerCombatState();
                }
                return _playerStates[playerId];
            }
        }

        /// <summary>
        /// Remove estado ao desconectar
        /// </summary>
        public void RemovePlayerState(int playerId)
        {
            lock (_combatLock)
            {
                _playerStates.Remove(playerId);
            }
        }
    }

    /// <summary>
    /// Estado de combate de um jogador
    /// </summary>
    public class PlayerCombatState
    {
        public DateTime LastAttackTime { get; set; }
        public int TotalAttacks { get; set; }
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public List<int> AttackedPlayers { get; set; }

        public PlayerCombatState()
        {
            LastAttackTime = DateTime.MinValue;
            AttackedPlayers = new List<int>();
        }

        public bool IsInCombat()
        {
            return (DateTime.Now - LastAttackTime).TotalSeconds < 30; // 30s sem atacar = fora de combate
        }

        public float GetKDRatio()
        {
            if (TotalDeaths == 0) return TotalKills;
            return (float)TotalKills / TotalDeaths;
        }
    }
}