using UnityEngine;
using System.Collections;

namespace RustlikeClient.UI
{
    /// <summary>
    /// Gerencia efeitos visuais de combate (sangue, impactos, etc)
    /// </summary>
    public class CombatEffects : MonoBehaviour
    {
        public static CombatEffects Instance { get; private set; }

        [Header("Particle Effects")]
        public GameObject bloodEffectPrefab;
        public GameObject bloodHeadshotEffectPrefab;
        public GameObject bulletImpactPrefab;
        public GameObject muzzleFlashPrefab;

        [Header("Screen Effects")]
        public UnityEngine.UI.Image bloodSplatterOverlay;
        public float bloodSplatterFadeDuration = 2f;

        [Header("Camera Shake")]
        public bool enableCameraShake = true;
        public float shakeIntensity = 0.1f;
        public float shakeDuration = 0.2f;

        [Header("Sound Effects")]
        public AudioClip[] bulletFleshSounds;
        public AudioClip[] bulletImpactSounds;
        public AudioClip headshotSound;

        private AudioSource _audioSource;
        private Camera _mainCamera;
        private Vector3 _originalCameraPosition;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            _mainCamera = Camera.main;

            // Configura blood overlay
            if (bloodSplatterOverlay != null)
            {
                Color c = bloodSplatterOverlay.color;
                c.a = 0f;
                bloodSplatterOverlay.color = c;
            }

            Debug.Log("[CombatEffects] Inicializado");
        }

        /// <summary>
        /// Spawna efeito de sangue
        /// </summary>
        public void SpawnBloodEffect(Vector3 position, bool isHeadshot)
        {
            GameObject prefab = isHeadshot ? bloodHeadshotEffectPrefab : bloodEffectPrefab;

            if (prefab != null)
            {
                GameObject effect = Instantiate(prefab, position, Quaternion.identity);
                Destroy(effect, 3f);

                Debug.Log($"[CombatEffects] 🩸 Efeito de sangue spawned @ {position} (Headshot: {isHeadshot})");
            }

            // Som
            if (isHeadshot && headshotSound != null)
            {
                PlaySound(headshotSound);
            }
            else if (bulletFleshSounds != null && bulletFleshSounds.Length > 0)
            {
                PlayRandomSound(bulletFleshSounds);
            }

            // Se for headshot, efeito extra
            if (isHeadshot)
            {
                SpawnHeadshotEffect(position);
            }
        }

        /// <summary>
        /// Efeito especial de headshot
        /// </summary>
        private void SpawnHeadshotEffect(Vector3 position)
        {
            // Partículas extras
            if (bloodHeadshotEffectPrefab != null)
            {
                GameObject effect = Instantiate(bloodHeadshotEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            // TODO: Adicionar slowmotion momentâneo
            // StartCoroutine(SlowMotionEffect(0.3f, 0.2f));
        }

        /// <summary>
        /// Spawna efeito de impacto de bala
        /// </summary>
        public void SpawnBulletImpact(Vector3 position, Vector3 normal)
        {
            if (bulletImpactPrefab != null)
            {
                Quaternion rotation = Quaternion.LookRotation(normal);
                GameObject effect = Instantiate(bulletImpactPrefab, position, rotation);
                Destroy(effect, 2f);
            }

            // Som
            if (bulletImpactSounds != null && bulletImpactSounds.Length > 0)
            {
                PlayRandomSound(bulletImpactSounds);
            }
        }

        /// <summary>
        /// Spawna muzzle flash
        /// </summary>
        public void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
        {
            if (muzzleFlashPrefab != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, position, rotation);
                Destroy(flash, 0.1f);
            }
        }

        /// <summary>
        /// Mostra blood splatter na tela (quando leva dano)
        /// </summary>
        public void ShowBloodSplatter(float intensity = 0.5f)
        {
            if (bloodSplatterOverlay == null) return;

            StopAllCoroutines();
            StartCoroutine(BloodSplatterCoroutine(intensity));
        }

        private IEnumerator BloodSplatterCoroutine(float intensity)
        {
            // Fade in rápido
            float targetAlpha = Mathf.Clamp01(intensity);
            Color color = bloodSplatterOverlay.color;
            color.a = targetAlpha;
            bloodSplatterOverlay.color = color;

            // Aguarda um pouco
            yield return new WaitForSeconds(0.2f);

            // Fade out lento
            float elapsed = 0f;

            while (elapsed < bloodSplatterFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bloodSplatterFadeDuration;

                color = bloodSplatterOverlay.color;
                color.a = Mathf.Lerp(targetAlpha, 0f, t);
                bloodSplatterOverlay.color = color;

                yield return null;
            }

            color = bloodSplatterOverlay.color;
            color.a = 0f;
            bloodSplatterOverlay.color = color;
        }

        /// <summary>
        /// Camera shake
        /// </summary>
        public void ShakeCamera(float intensity = -1f, float duration = -1f)
        {
            if (!enableCameraShake || _mainCamera == null) return;

            if (intensity < 0) intensity = shakeIntensity;
            if (duration < 0) duration = shakeDuration;

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            _shakeCoroutine = StartCoroutine(CameraShakeCoroutine(intensity, duration));
        }

        private IEnumerator CameraShakeCoroutine(float intensity, float duration)
        {
            _originalCameraPosition = _mainCamera.transform.localPosition;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;

                _mainCamera.transform.localPosition = _originalCameraPosition + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _mainCamera.transform.localPosition = _originalCameraPosition;
        }

        /// <summary>
        /// Efeito de slow motion
        /// </summary>
        public IEnumerator SlowMotionEffect(float timeScale, float duration)
        {
            Time.timeScale = timeScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Toca som
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Toca som aleatório de um array
        /// </summary>
        private void PlayRandomSound(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            PlaySound(randomClip);
        }

        /// <summary>
        /// Cria prefabs simples se não existirem
        /// </summary>
        [ContextMenu("Create Default Prefabs")]
        public void CreateDefaultPrefabs()
        {
            // Blood Effect
            if (bloodEffectPrefab == null)
            {
                bloodEffectPrefab = CreateSimpleParticleEffect("BloodEffect", Color.red);
                Debug.Log("[CombatEffects] Blood effect prefab criado");
            }

            // Headshot Blood Effect
            if (bloodHeadshotEffectPrefab == null)
            {
                bloodHeadshotEffectPrefab = CreateSimpleParticleEffect("BloodHeadshotEffect", new Color(0.8f, 0f, 0f));
                Debug.Log("[CombatEffects] Headshot blood effect prefab criado");
            }

            // Bullet Impact
            if (bulletImpactPrefab == null)
            {
                bulletImpactPrefab = CreateSimpleParticleEffect("BulletImpact", Color.gray);
                Debug.Log("[CombatEffects] Bullet impact prefab criado");
            }

            // Muzzle Flash
            if (muzzleFlashPrefab == null)
            {
                muzzleFlashPrefab = CreateSimpleParticleEffect("MuzzleFlash", Color.yellow);
                Debug.Log("[CombatEffects] Muzzle flash prefab criado");
            }
        }

        /// <summary>
        /// Cria efeito de partículas simples
        /// </summary>
        private GameObject CreateSimpleParticleEffect(string name, Color color)
        {
            GameObject prefab = new GameObject(name);

            ParticleSystem ps = prefab.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startLifetime = 0.5f;
            main.startSize = 0.2f;
            main.startSpeed = 2f;
            main.maxParticles = 50;
            main.duration = 0.5f;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 20)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            prefab.SetActive(false);
            return prefab;
        }

        /// <summary>
        /// Para debug
        /// </summary>
        private void OnGUI()
        {
            if (Input.GetKey(KeyCode.F12))
            {
                GUI.Box(new Rect(Screen.width - 310, 120, 300, 100), "Combat Effects (F12)");

                if (GUI.Button(new Rect(Screen.width - 300, 145, 280, 25), "Test Blood Effect"))
                {
                    if (_mainCamera != null)
                    {
                        Vector3 testPos = _mainCamera.transform.position + _mainCamera.transform.forward * 5f;
                        SpawnBloodEffect(testPos, false);
                    }
                }

                if (GUI.Button(new Rect(Screen.width - 300, 175, 280, 25), "Test Camera Shake"))
                {
                    ShakeCamera();
                }
            }
        }
    }
}