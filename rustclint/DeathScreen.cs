using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace RustlikeClient.UI
{
    /// <summary>
    /// Tela de morte estilo Rust
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        public static DeathScreen Instance { get; private set; }

        [Header("UI References")]
        public GameObject deathPanel;
        public TextMeshProUGUI killedByText;
        public TextMeshProUGUI weaponText;
        public TextMeshProUGUI distanceText;
        public TextMeshProUGUI respawnTimerText;
        public Button respawnButton;
        public Image fadeImage;

        [Header("Settings")]
        public float respawnDelay = 5f;
        public Color fadeColor = new Color(1f, 0f, 0f, 0.3f);

        private float _respawnTimer;
        private bool _isDead;
        private Coroutine _respawnCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }

            if (respawnButton != null)
            {
                respawnButton.onClick.AddListener(OnRespawnClicked);
                respawnButton.interactable = false;
            }

            // Configura fade
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
            }
        }

        /// <summary>
        /// Mostra tela de morte
        /// </summary>
        public void Show(string killerName, int weaponItemId, bool wasHeadshot, float distance)
        {
            // Se já estiver morto, apenas atualiza as informações mas não reseta o timer
            if (_isDead)
            {
                Debug.Log($"[DeathScreen] Atualizando informações de morte (Timer não resetado)");
                UpdateDeathInfo(killerName, weaponItemId, wasHeadshot, distance);
                return;
            }

            Debug.Log($"[DeathScreen] Mostrando tela de morte");

            _isDead = true;

            if (deathPanel != null)
            {
                deathPanel.SetActive(true);
            }

            UpdateDeathInfo(killerName, weaponItemId, wasHeadshot, distance);

            // Libera cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Inicia timer de respawn
            _respawnTimer = respawnDelay;
            
            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
            }
            
            _respawnCoroutine = StartCoroutine(RespawnTimerCoroutine());

            // Fade vermelho
            StartCoroutine(FadeInCoroutine());
        }

        private void UpdateDeathInfo(string killerName, int weaponItemId, bool wasHeadshot, float distance)
        {
            // Texto "Killed by"
            if (killedByText != null)
            {
                string headshotText = wasHeadshot ? " 🎯 HEADSHOT!" : "";
                killedByText.text = $"Você foi morto por\n<color=#ff4444>{killerName}</color>{headshotText}";
            }

            // Arma usada
            if (weaponText != null)
            {
                string weaponName = "Desconhecido";
                
                if (weaponItemId == -1)
                {
                    weaponName = "Ambiente / Suicídio";
                }
                else
                {
                    var weaponData = Combat.WeaponDatabase.Instance?.GetWeapon(weaponItemId);
                    if (weaponData != null)
                    {
                        weaponName = weaponData.weaponName;
                    }
                    else
                    {
                        weaponName = $"Arma {weaponItemId}";
                    }
                }
                
                weaponText.text = $"Arma: {weaponName}";
            }

            // Distância
            if (distanceText != null)
            {
                distanceText.text = $"Distância: {distance:F1}m";
            }
        }

        /// <summary>
        /// Esconde tela de morte
        /// </summary>
        public void Hide()
        {
            Debug.Log($"[DeathScreen] Escondendo tela de morte");

            _isDead = false;

            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }

            // Trava cursor novamente
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Remove fade
            StartCoroutine(FadeOutCoroutine());

            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
            }
        }

        /// <summary>
        /// Timer de respawn
        /// </summary>
        private IEnumerator RespawnTimerCoroutine()
        {
            if (respawnButton != null)
            {
                respawnButton.interactable = false;
            }

            while (_respawnTimer > 0)
            {
                if (respawnTimerText != null)
                {
                    respawnTimerText.text = $"Respawn em {_respawnTimer:F0}s";
                }

                _respawnTimer -= Time.deltaTime;
                yield return null;
            }

            // Timer acabou - pode respawnar
            if (respawnTimerText != null)
            {
                respawnTimerText.text = "Você pode respawnar!";
            }

            if (respawnButton != null)
            {
                respawnButton.interactable = true;
            }
        }

        /// <summary>
        /// Callback do botão de respawn
        /// </summary>
        private void OnRespawnClicked()
        {
            Debug.Log("[DeathScreen] Botão de respawn clicado");

            // Solicita respawn ao servidor
            Network.NetworkManager.Instance?.RequestRespawn();
        }

        /// <summary>
        /// Fade in vermelho
        /// </summary>
        private IEnumerator FadeInCoroutine()
        {
            if (fadeImage == null) yield break;

            float elapsed = 0f;
            float duration = 1f;

            Color targetColor = fadeColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Color c = fadeImage.color;
                c.a = Mathf.Lerp(0f, targetColor.a, t);
                fadeImage.color = c;

                yield return null;
            }

            fadeImage.color = targetColor;
        }

        /// <summary>
        /// Fade out
        /// </summary>
        private IEnumerator FadeOutCoroutine()
        {
            if (fadeImage == null) yield break;

            float elapsed = 0f;
            float duration = 0.5f;

            Color startColor = fadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                fadeImage.color = c;

                yield return null;
            }

            Color finalColor = fadeImage.color;
            finalColor.a = 0f;
            fadeImage.color = finalColor;
        }

        /// <summary>
        /// Verifica se está morto
        /// </summary>
        public bool IsDead() => _isDead;

        private void OnDestroy()
        {
            if (respawnButton != null)
            {
                respawnButton.onClick.RemoveListener(OnRespawnClicked);
            }
        }
    }
}