using UnityEngine;

namespace RustlikeClient.Network
{
    /// <summary>
    /// ⭐ ATUALIZADO: Adicionado PlayerId para sistema de combate
    /// Sincronização suave de movimento de outros jogadores (interpolação + extrapolação)
    /// </summary>
    public class NetworkPlayerSync : MonoBehaviour
    {
        [Header("Player Info")]
        [Tooltip("ID do player no servidor")]
        public int playerId = -1;
        public string playerName = "Unknown";

        [Header("Interpolation Settings")]
        [Tooltip("Velocidade de interpolação de posição")]
        public float positionLerpSpeed = 15f;
        
        [Tooltip("Velocidade de interpolação de rotação")]
        public float rotationLerpSpeed = 20f;
        
        [Tooltip("Distância mínima para teleportar ao invés de interpolar")]
        public float teleportDistance = 10f;

        [Header("Extrapolation (Dead Reckoning)")]
        [Tooltip("Ativa extrapolação para prever movimento")]
        public bool useExtrapolation = true;
        
        [Tooltip("Tempo máximo de extrapolação sem receber pacotes")]
        public float maxExtrapolationTime = 0.5f;

        [Header("Visual")]
        public GameObject nametagPrefab;
        public Transform nametagAnchor;

        // Targets (recebidos da rede)
        private Vector3 _targetPosition;
        private float _targetYaw;

        // Extrapolação
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private float _lastUpdateTime;

        // Estado
        private bool _hasReceivedFirstUpdate = false;

        // Nametag
        private GameObject _nametag;

        private void Awake()
        {
            _targetPosition = transform.position;
            _lastPosition = transform.position;
            _targetYaw = transform.eulerAngles.y;
            _lastUpdateTime = Time.time;
        }

        private void Start()
        {
            CreateNametag();
        }

        private void Update()
        {
            if (!_hasReceivedFirstUpdate) return;

            float timeSinceLastUpdate = Time.time - _lastUpdateTime;

            // Se passou muito tempo sem update, usa extrapolação
            if (useExtrapolation && timeSinceLastUpdate < maxExtrapolationTime)
            {
                // Extrapola baseado na velocidade
                Vector3 extrapolatedPos = _targetPosition + (_velocity * timeSinceLastUpdate);
                SmoothMoveTo(extrapolatedPos);
            }
            else
            {
                // Interpolação normal
                SmoothMoveTo(_targetPosition);
            }

            // Rotação sempre interpola suavemente
            SmoothRotateTo(_targetYaw);

            // Atualiza nametag
            UpdateNametag();
        }

        /// <summary>
        /// Atualiza posição e rotação alvo (chamado quando recebe pacote da rede)
        /// </summary>
        public void UpdateTargetTransform(Vector3 position, float yaw)
        {
            // Calcula velocidade para extrapolação
            if (_hasReceivedFirstUpdate)
            {
                float deltaTime = Time.time - _lastUpdateTime;
                if (deltaTime > 0.001f) // Evita divisão por zero
                {
                    _velocity = (position - _targetPosition) / deltaTime;
                }
            }

            // Atualiza targets
            _lastPosition = _targetPosition;
            _targetPosition = position;
            _targetYaw = yaw;
            _lastUpdateTime = Time.time;
            _hasReceivedFirstUpdate = true;

            // Se a distância for muito grande, teleporta ao invés de interpolar
            float distance = Vector3.Distance(transform.position, _targetPosition);
            if (distance > teleportDistance)
            {
                transform.position = _targetPosition;
                _velocity = Vector3.zero;
            }
        }

        private void SmoothMoveTo(Vector3 target)
        {
            // Interpolação suave usando Lerp
            transform.position = Vector3.Lerp(
                transform.position,
                target,
                Time.deltaTime * positionLerpSpeed
            );
        }

        private void SmoothRotateTo(float targetYaw)
        {
            // Interpolação suave de rotação
            float currentYaw = transform.eulerAngles.y;
            float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * rotationLerpSpeed);
            transform.rotation = Quaternion.Euler(0, newYaw, 0);
        }

        /// <summary>
        /// ⭐ NOVO: Pega ID do player (usado pelo WeaponController)
        /// </summary>
        public int GetPlayerId()
        {
            return playerId;
        }

        /// <summary>
        /// ⭐ NOVO: Define ID e nome do player
        /// </summary>
        public void SetPlayerInfo(int id, string name)
        {
            playerId = id;
            playerName = name;
            gameObject.name = $"Player_{id}_{name}";

            Debug.Log($"[NetworkPlayerSync] Player info setado: ID={id}, Name={name}");

            // Atualiza nametag
            UpdateNametag();
        }

        /// <summary>
        /// Cria nametag sobre o player
        /// </summary>
        private void CreateNametag()
        {
            if (nametagPrefab == null) return;

            // Cria anchor se não existir
            if (nametagAnchor == null)
            {
                GameObject anchorObj = new GameObject("NametagAnchor");
                anchorObj.transform.SetParent(transform);
                anchorObj.transform.localPosition = new Vector3(0, 2.5f, 0);
                nametagAnchor = anchorObj.transform;
            }

            // Instancia nametag
            _nametag = Instantiate(nametagPrefab, nametagAnchor);
            UpdateNametag();
        }

        /// <summary>
        /// Atualiza nametag
        /// </summary>
        private void UpdateNametag()
        {
            if (_nametag == null) return;

            // Atualiza texto
            var text = _nametag.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                text.text = playerName;
            }

            // Faz nametag sempre olhar para câmera
            Camera cam = Camera.main;
            if (cam != null && nametagAnchor != null)
            {
                nametagAnchor.LookAt(cam.transform);
                nametagAnchor.Rotate(0, 180, 0);
            }
        }

        /// <summary>
        /// Para debug
        /// </summary>
private void OnDrawGizmos()
{
    if (!_hasReceivedFirstUpdate) return;

    // Desenha posição alvo
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(_targetPosition, 0.2f);

    // Desenha linha de velocidade
    if (useExtrapolation)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + _velocity);
    }

#if UNITY_EDITOR
    // Desenha ID do player (somente no Editor)
    UnityEditor.Handles.Label(
        transform.position + Vector3.up * 3f,
        $"Player {playerId}"
    );
#endif
}
    }
}