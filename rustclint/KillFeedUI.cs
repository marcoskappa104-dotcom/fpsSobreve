using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace RustlikeClient.UI
{
    /// <summary>
    /// Killfeed estilo Rust (canto superior direito)
    /// </summary>
    public class KillFeedUI : MonoBehaviour
    {
        public static KillFeedUI Instance { get; private set; }

        [Header("UI References")]
        public Transform killFeedContainer;
        public GameObject killFeedItemPrefab;

        [Header("Settings")]
        public int maxKillFeedItems = 5;
        public float killFeedDuration = 5f;
        public Color headshotColor = Color.yellow;
        public Color normalColor = Color.white;

        private Queue<GameObject> _killFeedItems = new Queue<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Se não tiver container, cria um
            if (killFeedContainer == null)
            {
                CreateKillFeedContainer();
            }

            // Se não tiver prefab, cria um simples
            if (killFeedItemPrefab == null)
            {
                CreateKillFeedItemPrefab();
            }

            Debug.Log("[KillFeedUI] Inicializado");
        }

        /// <summary>
        /// Adiciona kill no feed
        /// </summary>
        public void AddKill(string killerName, string victimName, int weaponItemId, bool wasHeadshot)
        {
            Debug.Log($"[KillFeedUI] {killerName} matou {victimName} com arma {weaponItemId} (Headshot: {wasHeadshot})");

            // Remove item mais antigo se atingiu o limite
            if (_killFeedItems.Count >= maxKillFeedItems)
            {
                RemoveOldestKill();
            }

            // Cria novo item
            GameObject itemObj = Instantiate(killFeedItemPrefab, killFeedContainer);
            
            // Configura texto
            var text = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                // Pega nome da arma
                var weaponData = Combat.WeaponDatabase.Instance?.GetWeapon(weaponItemId);
                string weaponName = weaponData != null ? weaponData.weaponName : "Unknown";

                // Monta texto
                string headshotIcon = wasHeadshot ? " 🎯" : "";
                string killText = $"<color=#ffaa00>{killerName}</color> [{weaponName}] <color=#ff4444>{victimName}</color>{headshotIcon}";
                
                text.text = killText;
                text.color = wasHeadshot ? headshotColor : normalColor;
            }

            _killFeedItems.Enqueue(itemObj);

            // Inicia fade out
            StartCoroutine(FadeOutKillItem(itemObj, killFeedDuration));
        }

        /// <summary>
        /// Remove item mais antigo
        /// </summary>
        private void RemoveOldestKill()
        {
            if (_killFeedItems.Count > 0)
            {
                GameObject oldest = _killFeedItems.Dequeue();
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }
        }

        /// <summary>
        /// Fade out e destroy
        /// </summary>
        private IEnumerator FadeOutKillItem(GameObject item, float delay)
        {
            // Aguarda
            yield return new WaitForSeconds(delay);

            // Fade out
            var canvasGroup = item.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = item.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            float fadeDuration = 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            // Remove da fila e destrói
            if (_killFeedItems.Contains(item))
            {
                var tempQueue = new Queue<GameObject>();
                while (_killFeedItems.Count > 0)
                {
                    var current = _killFeedItems.Dequeue();
                    if (current != item)
                    {
                        tempQueue.Enqueue(current);
                    }
                }
                _killFeedItems = tempQueue;
            }

            Destroy(item);
        }

        /// <summary>
        /// Cria container se não existir
        /// </summary>
        private void CreateKillFeedContainer()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[KillFeedUI] Nenhum Canvas encontrado!");
                return;
            }

            GameObject container = new GameObject("KillFeedContainer");
            container.transform.SetParent(canvas.transform);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(400, 300);

            // Vertical Layout
            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            killFeedContainer = container.transform;
        }

        /// <summary>
        /// Cria prefab simples
        /// </summary>
        private void CreateKillFeedItemPrefab()
        {
            GameObject prefab = new GameObject("KillFeedItem");

            // Background
            var bg = prefab.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);

            RectTransform rect = prefab.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 30);

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(prefab.transform);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            // Canvas Group
            prefab.AddComponent<CanvasGroup>();

            killFeedItemPrefab = prefab;
        }
    }
}