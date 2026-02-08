using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RustlikeClient.UI
{
    /// <summary>
    /// UI de combate (ammo counter, crosshair, killstreak)
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        public static CombatUI Instance { get; private set; }

        [Header("Ammo Counter")]
        public TextMeshProUGUI ammoText;
        public TextMeshProUGUI weaponNameText;

        [Header("Crosshair")]
        public Image crosshairImage;
        public Color normalCrosshairColor = Color.white;
        public Color enemyCrosshairColor = Color.red;

        [Header("Hit Marker")]
        public GameObject hitMarker;
        public float hitMarkerDuration = 0.2f;

        [Header("Killstreak")]
        public TextMeshProUGUI killstreakText;
        public GameObject killstreakPanel;

        [Header("Damage Indicators")]
        public GameObject damageIndicatorPrefab;
        public Transform damageIndicatorContainer;

        private int _currentKillstreak;
        private float _hitMarkerTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (hitMarker != null)
            {
                hitMarker.SetActive(false);
            }

            if (killstreakPanel != null)
            {
                killstreakPanel.SetActive(false);
            }

            _currentKillstreak = 0;
        }

        private void Update()
        {
            // Hit marker timer
            if (_hitMarkerTimer > 0)
            {
                _hitMarkerTimer -= Time.deltaTime;
                
                if (_hitMarkerTimer <= 0 && hitMarker != null)
                {
                    hitMarker.SetActive(false);
                }
            }

            // Atualiza ammo counter
            UpdateAmmoCounter();
        }

        /// <summary>
        /// Atualiza contador de munição
        /// </summary>
        private void UpdateAmmoCounter()
        {
            if (ammoText == null) return;

            // Pega arma atual do WeaponController
            var weaponController = FindObjectOfType<Combat.WeaponController>();
            if (weaponController == null || weaponController.currentWeaponData == null)
            {
                ammoText.text = "";
                if (weaponNameText != null)
                {
                    weaponNameText.text = "";
                }
                return;
            }

            var weapon = weaponController.currentWeaponData;

            // Nome da arma
            if (weaponNameText != null)
            {
                weaponNameText.text = weapon.weaponName;
            }

            // Contador de munição
            if (weapon.requiresAmmo)
            {
                int ammoCount = UI.InventoryManager.Instance?.CountItem(weapon.ammoItemId) ?? 0;
                ammoText.text = $"{ammoCount}";

                // Muda cor se munição baixa
                if (ammoCount <= 10)
                {
                    ammoText.color = Color.red;
                }
                else if (ammoCount <= 30)
                {
                    ammoText.color = Color.yellow;
                }
                else
                {
                    ammoText.color = Color.white;
                }
            }
            else
            {
                ammoText.text = "∞";
                ammoText.color = Color.white;
            }
        }

        /// <summary>
        /// Mostra hit marker
        /// </summary>
        public void ShowHitMarker(bool isHeadshot = false)
        {
            if (hitMarker != null)
            {
                hitMarker.SetActive(true);
                _hitMarkerTimer = hitMarkerDuration;

                // Se for headshot, aumenta tamanho
                if (isHeadshot)
                {
                    hitMarker.transform.localScale = Vector3.one * 1.5f;
                }
                else
                {
                    hitMarker.transform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// Adiciona kill ao killstreak
        /// </summary>
        public void AddKill()
        {
            _currentKillstreak++;

            if (killstreakText != null)
            {
                killstreakText.text = $"Killstreak: {_currentKillstreak}";
            }

            if (killstreakPanel != null)
            {
                killstreakPanel.SetActive(true);
            }

            // Notificações especiais
            if (_currentKillstreak == 3)
            {
                ShowKillstreakNotification("Triple Kill!");
            }
            else if (_currentKillstreak == 5)
            {
                ShowKillstreakNotification("🔥 ON FIRE! 🔥");
            }
            else if (_currentKillstreak == 10)
            {
                ShowKillstreakNotification("⚡ UNSTOPPABLE! ⚡");
            }
        }

        /// <summary>
        /// Reseta killstreak (quando morre)
        /// </summary>
        public void ResetKillstreak()
        {
            _currentKillstreak = 0;

            if (killstreakPanel != null)
            {
                killstreakPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Mostra notificação de killstreak
        /// </summary>
        private void ShowKillstreakNotification(string message)
        {
            if (UI.NotificationManager.Instance != null)
            {
                UI.NotificationManager.Instance.ShowSuccess(message);
            }

            Debug.Log($"[CombatUI] 🔥 {message}");
        }

        /// <summary>
        /// Muda cor do crosshair
        /// </summary>
        public void SetCrosshairColor(bool isOverEnemy)
        {
            if (crosshairImage != null)
            {
                crosshairImage.color = isOverEnemy ? enemyCrosshairColor : normalCrosshairColor;
            }
        }

        /// <summary>
        /// Mostra indicador de direção do dano
        /// </summary>
        public void ShowDamageIndicator(Vector3 damageSourcePosition)
        {
            if (damageIndicatorPrefab == null || damageIndicatorContainer == null)
                return;

            // Calcula direção
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 direction = (damageSourcePosition - cam.transform.position).normalized;
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            // Spawna indicador
            GameObject indicator = Instantiate(damageIndicatorPrefab, damageIndicatorContainer);
            indicator.transform.rotation = Quaternion.Euler(0, 0, -angle);

            // Destrói após 1s
            Destroy(indicator, 1f);
        }

        /// <summary>
        /// Para debug
        /// </summary>
        private void OnGUI()
        {
            if (Input.GetKey(KeyCode.F11))
            {
                GUI.Box(new Rect(Screen.width - 310, 10, 300, 100), "Combat UI (F11)");
                GUI.Label(new Rect(Screen.width - 300, 35, 280, 20), $"Killstreak: {_currentKillstreak}");
                GUI.Label(new Rect(Screen.width - 300, 55, 280, 20), $"Hit Marker: {(_hitMarkerTimer > 0 ? "Active" : "Inactive")}");
                
                var weaponController = FindObjectOfType<Combat.WeaponController>();
                if (weaponController != null && weaponController.currentWeaponData != null)
                {
                    GUI.Label(new Rect(Screen.width - 300, 75, 280, 20), $"Weapon: {weaponController.currentWeaponData.weaponName}");
                }
            }
        }
    }
}