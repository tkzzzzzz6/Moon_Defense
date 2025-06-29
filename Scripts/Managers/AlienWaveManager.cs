using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class AlienWaveManager : MonoBehaviour
    {
        [Header("Wave Settings")]
        public float m_WaveInterval = 30f;
        public int m_InitialAlienCount = 2;
        public float m_DifficultyMultiplier = 1.3f;
        public int m_MaxAliensPerWave = 8;
        
        [Header("Alien Spawn Settings")]
        [Tooltip("外星人预制体 (推荐使用Spaceship预制件)")]
        public GameObject m_AlienPrefab;
        [Tooltip("生成距离范围 (相对于场景中心)")]
        public float m_SpawnRadius = 50f;
        [Tooltip("外星人生命值")]
        public float m_AlienHealth = 100f;
        [Tooltip("外星人移动速度")]
        public float m_AlienSpeed = 5f;
        
        [Header("Debug")]
        public bool m_EnableDebugLog = true;
        public bool m_EnableAlienSystem = true;
        
        private GameManager m_GameManager;
        private int m_CurrentWave = 0;
        private int m_CurrentAlienCount;
        private bool m_IsSystemActive = false;
        private System.Collections.Generic.List<GameObject> m_CurrentAliens = new System.Collections.Generic.List<GameObject>();
        
        private void Awake()
        {
            m_CurrentAlienCount = m_InitialAlienCount;
            
            if (m_EnableDebugLog)
                Debug.Log("[AlienWaveManager] 外星飞船波次管理器已创建");
        }
        
        public void Initialize(GameManager gameManager)
        {
            m_GameManager = gameManager;
            
            if (m_EnableDebugLog)
                Debug.Log("[AlienWaveManager] 外星飞船系统初始化完成");
        }
        
        public void StartAlienWaves()
        {
            if (!m_EnableAlienSystem)
            {
                if (m_EnableDebugLog)
                    Debug.Log("[AlienWaveManager] 外星飞船系统已禁用");
                return;
            }
            
            if (m_GameManager == null)
            {
                Debug.LogError("[AlienWaveManager] GameManager引用为空！");
                return;
            }
            
            m_IsSystemActive = true;
            
            if (m_EnableDebugLog)
                Debug.Log("[AlienWaveManager] 外星飞船波次系统启动！");
            
            StartCoroutine(AlienWaveLoop());
        }
        
        public void StopAlienWaves()
        {
            m_IsSystemActive = false;
            StopAllCoroutines();
            
            if (m_EnableDebugLog)
                Debug.Log("[AlienWaveManager] 外星飞船波次系统已停止");
        }
        
        private IEnumerator AlienWaveLoop()
        {
            while (m_IsSystemActive)
            {
                yield return new WaitForSeconds(m_WaveInterval);
                
                if (m_GameManager == null)
                {
                    Debug.LogError("[AlienWaveManager] GameManager引用丢失");
                    break;
                }
                
                var aliveTanks = m_GameManager.GetAliveTanks();
                if (m_EnableDebugLog)
                    Debug.Log($"[AlienWaveManager] 存活坦克数量: {aliveTanks.Count}");
                
                if (aliveTanks.Count == 0)
                {
                    if (m_EnableDebugLog)
                        Debug.Log("[AlienWaveManager] 所有玩家坦克已被摧毁，停止系统");
                    break;
                }
                
                ExecuteWaveSpawn();
                m_CurrentWave++;
                
                int nextWaveAlienCount = Mathf.Min(
                    Mathf.RoundToInt(m_CurrentAlienCount * m_DifficultyMultiplier),
                    m_MaxAliensPerWave
                );
                m_CurrentAlienCount = nextWaveAlienCount;
            }
            
            if (m_EnableDebugLog)
                Debug.Log("[AlienWaveManager] 波次循环结束");
        }
        
        private void ExecuteWaveSpawn()
        {
            if (m_EnableDebugLog)
            {
                Debug.Log($"[AlienWaveManager] === 第{m_CurrentWave + 1}波外星飞船入侵！===");
                Debug.Log($"[AlienWaveManager] 预计生成数量：{m_CurrentAlienCount}");
            }

            // 获取存活的坦克作为目标
            var aliveTanks = m_GameManager.GetAliveTanks();
            if (aliveTanks.Count == 0) return;

            // 生成外星人
            for (int i = 0; i < m_CurrentAlienCount; i++)
            {
                SpawnAlien(i);
            }
            
            if (m_EnableDebugLog)
                Debug.Log($"[AlienWaveManager] 成功生成 {m_CurrentAlienCount} 个外星人");
        }

        private void SpawnAlien(int alienIndex)
        {
            // 在场景边缘随机生成位置
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            GameObject alien;
            
            if (m_AlienPrefab != null)
            {
                // 使用预制体生成
                alien = Instantiate(m_AlienPrefab, spawnPosition, Quaternion.identity);
                
                // 如果是Spaceship预制件，需要设置为外星人模式
                SetupSpaceshipAsAlien(alien);
            }
            else
            {
                // 创建占位符外星人
                alien = CreatePlaceholderAlien(spawnPosition, alienIndex);
            }
            
            // 设置外星人名称和标签
            alien.name = $"Alien_{m_CurrentWave + 1}_{alienIndex + 1}";
            
            // 安全设置标签，如果标签不存在则跳过
            try
            {
                alien.tag = "Alien";
            }
            catch (UnityException)
            {
                Debug.LogWarning("[AlienWaveManager] 'Alien' 标签未定义，请运行 Tanks → Setup Alien Tags");
            }
            
            // 添加到当前外星人列表
            m_CurrentAliens.Add(alien);
            
            if (m_EnableDebugLog)
                Debug.Log($"[AlienWaveManager] 生成外星人: {alien.name} 位置: {spawnPosition}");
        }
        
        private void SetupSpaceshipAsAlien(GameObject spaceship)
        {
            // 首先设置必要的预制体引用
            SetupRequiredPrefabReferences(spaceship);
            
            // 移除不需要的坦克组件
            var tankMovement = spaceship.GetComponent<TankMovement>();
            var tankAI = spaceship.GetComponent<TankAI>();
            var tankInputUser = spaceship.GetComponent<TankInputUser>();
            var powerUpDetector = spaceship.GetComponent<PowerUpDetector>();
            
            if (tankMovement != null) Destroy(tankMovement);
            if (tankAI != null) Destroy(tankAI);
            if (tankInputUser != null) Destroy(tankInputUser);
            if (powerUpDetector != null) Destroy(powerUpDetector);
            
            // 为Spaceship外星人创建UI
            SetupAlienUI(spaceship);
            
            // 临时禁用以防止组件初始化错误
            spaceship.SetActive(false);
            
            // 处理TankHealth组件
            var tankHealth = spaceship.GetComponent<TankHealth>();
            if (tankHealth == null)
            {
                tankHealth = spaceship.AddComponent<TankHealth>();
            }
            tankHealth.m_StartingHealth = m_AlienHealth;
            
            // 设置健康条UI引用
            var healthSlider = spaceship.GetComponentInChildren<Slider>();
            if (healthSlider != null)
            {
                tankHealth.m_Slider = healthSlider;
                var fillImage = healthSlider.fillRect?.GetComponent<UnityEngine.UI.Image>();
                if (fillImage != null)
                {
                    tankHealth.m_FillImage = fillImage;
                }
            }
            
            // 从参考坦克复制爆炸预制体引用
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && gameManager.m_Tank1Prefab != null)
            {
                var refHealth = gameManager.m_Tank1Prefab.GetComponent<TankHealth>();
                if (refHealth != null && refHealth.m_ExplosionPrefab != null)
                {
                    tankHealth.m_ExplosionPrefab = refHealth.m_ExplosionPrefab;
                }
            }
            
            // 处理TankShooting组件
            var tankShooting = spaceship.GetComponent<TankShooting>();
            if (tankShooting == null)
            {
                tankShooting = spaceship.AddComponent<TankShooting>();
            }
            ConfigureShootingComponent(tankShooting, spaceship);
            
            // 添加外星人AI组件
            var alienAI = spaceship.AddComponent<AlienShootingAI>();
            alienAI.m_MoveSpeed = m_AlienSpeed;
            alienAI.m_TankShooting = tankShooting;
            alienAI.m_TankHealth = tankHealth;
            
            // 设置正确的Layer - 使用Players layer让炮弹可以击中
            spaceship.layer = LayerMask.NameToLayer("Players");
            
            // 安全设置标签
            try
            {
                spaceship.tag = "Alien";
            }
            catch (UnityException)
            {
                Debug.LogWarning("[AlienWaveManager] 'Alien' 标签未定义，请运行 Tanks → Setup Alien Tags");
            }
            
            // 改变材质颜色以区分外星人 - 深蓝色 #111b2d
            var renderers = spaceship.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    // 设置为深蓝色 RGB(14, 27, 47) = #111b2d
                    renderer.material.color = new Color(14f/255f, 27f/255f, 47f/255f, 1f);
                }
            }
            
            // 重新启用GameObject
            spaceship.SetActive(true);
            
            Debug.Log($"[AlienWaveManager] 外星人 {spaceship.name} 配置完成，Layer: {LayerMask.LayerToName(spaceship.layer)}");
        }
        
        private void SetupAlienUI(GameObject alien)
        {
            // 创建Canvas作为UI根节点
            GameObject canvasObj = new GameObject("AlienCanvas");
            canvasObj.transform.SetParent(alien.transform);
            canvasObj.transform.localPosition = Vector3.zero;
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingLayerName = "UI";
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // 创建健康条滑块
            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(canvasObj.transform);
            sliderObj.transform.localPosition = new Vector3(0, 3, 0);
            sliderObj.transform.localScale = Vector3.one * 0.01f;
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = m_AlienHealth;
            slider.value = m_AlienHealth;
            
            // 创建滑块背景
            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObj.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = Vector3.one;
            
            var bgImage = background.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = Color.gray;
            
            // 创建滑块填充
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform);
            fillArea.transform.localPosition = Vector3.zero;
            fillArea.transform.localScale = Vector3.one;
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            fill.transform.localPosition = Vector3.zero;
            fill.transform.localScale = Vector3.one;
            
            var fillImage = fill.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = Color.red;
            
            // 设置滑块引用
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;
            
            Debug.Log($"[AlienWaveManager] 为 {alien.name} 创建了UI组件");
        }
        
        private void SetupRequiredPrefabReferences(GameObject spaceship)
        {
            // 首先确保有FireTransform（这是最重要的）
            Transform fireTransform = spaceship.transform.Find("FireTransform");
            if (fireTransform == null)
            {
                GameObject fireTransformObj = new GameObject("FireTransform");
                fireTransformObj.transform.SetParent(spaceship.transform);
                fireTransformObj.transform.localPosition = Vector3.forward * 2f;
                fireTransform = fireTransformObj.transform;
                Debug.Log($"[AlienWaveManager] 为 {spaceship.name} 创建了FireTransform");
            }
            
            // 获取参考的坦克预制体来复制必要的引用
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null || gameManager.m_Tank1Prefab == null)
            {
                Debug.LogWarning("[AlienWaveManager] 无法找到GameManager或Tank1Prefab，将使用默认设置");
                return;
            }
            
            var referenceTank = gameManager.m_Tank1Prefab;
            var refTankHealth = referenceTank.GetComponent<TankHealth>();
            var refTankShooting = referenceTank.GetComponent<TankShooting>();
            
            // 预设置TankHealth的爆炸预制体引用
            var existingHealth = spaceship.GetComponent<TankHealth>();
            if (existingHealth != null && refTankHealth != null)
            {
                existingHealth.m_ExplosionPrefab = refTankHealth.m_ExplosionPrefab;
            }
            
            // 预设置TankShooting的必要引用
            var existingShooting = spaceship.GetComponent<TankShooting>();
            if (existingShooting != null && refTankShooting != null)
            {
                existingShooting.m_Shell = refTankShooting.m_Shell;
                existingShooting.m_ChargingClip = refTankShooting.m_ChargingClip;
                existingShooting.m_FireClip = refTankShooting.m_FireClip;
                
                // 设置FireTransform引用
                existingShooting.m_FireTransform = fireTransform;
            }
        }
        
        private void ConfigureShootingComponent(TankShooting tankShooting, GameObject spaceship)
        {
            // 确保FireTransform设置正确
            if (tankShooting.m_FireTransform == null)
            {
                Transform fireTransform = spaceship.transform.Find("FireTransform");
                if (fireTransform != null)
                {
                    tankShooting.m_FireTransform = fireTransform;
                    Debug.Log($"[AlienWaveManager] 为 {spaceship.name} 的TankShooting设置了FireTransform");
                }
                else
                {
                    Debug.LogError($"[AlienWaveManager] {spaceship.name} 缺少FireTransform！");
                }
            }
            
            // 设置瞄准滑块（使用健康条滑块，但隐藏）
            if (tankShooting.m_AimSlider == null)
            {
                var healthSlider = spaceship.GetComponentInChildren<Slider>();
                if (healthSlider != null)
                {
                    tankShooting.m_AimSlider = healthSlider;
                }
                else
                {
                    // 创建一个临时的隐藏滑块
                    GameObject sliderObj = new GameObject("AimSlider");
                    sliderObj.transform.SetParent(spaceship.transform);
                    var slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
                    slider.gameObject.SetActive(false);
                    tankShooting.m_AimSlider = slider;
                }
            }
            
            // 基本参数设置
            tankShooting.m_MaxDamage = 30f; // 外星人伤害较低
            tankShooting.m_ShotCooldown = 1.5f; // 射击冷却
            tankShooting.m_IsComputerControlled = true; // 设为AI控制
            tankShooting.m_MinLaunchForce = 10f;
            tankShooting.m_MaxLaunchForce = 25f;
            tankShooting.m_MaxChargeTime = 0.75f;
            
            // 确保有AudioSource
            if (tankShooting.m_ShootingAudio == null)
            {
                var audioSource = spaceship.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = spaceship.AddComponent<AudioSource>();
                }
                tankShooting.m_ShootingAudio = audioSource;
            }
            
            // 从参考坦克复制必要的预制体引用
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && gameManager.m_Tank1Prefab != null)
            {
                var refShooting = gameManager.m_Tank1Prefab.GetComponent<TankShooting>();
                if (refShooting != null)
                {
                    if (tankShooting.m_Shell == null)
                        tankShooting.m_Shell = refShooting.m_Shell;
                    if (tankShooting.m_ChargingClip == null)
                        tankShooting.m_ChargingClip = refShooting.m_ChargingClip;
                    if (tankShooting.m_FireClip == null)
                        tankShooting.m_FireClip = refShooting.m_FireClip;
                }
            }
            
            Debug.Log($"[AlienWaveManager] {spaceship.name} 的TankShooting组件配置完成");
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // 在场景边缘随机选择生成位置
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = m_SpawnRadius;
            
            Vector3 spawnPos = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
            
            return spawnPos;
        }

        private GameObject CreatePlaceholderAlien(Vector3 position, int index)
        {
            // 创建简单的外星人占位符
            GameObject alien = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            alien.transform.position = position;
            alien.transform.localScale = Vector3.one * 2f;
            
            // 设置正确的Layer
            alien.layer = LayerMask.NameToLayer("Players");
            
            // 设置颜色以区分外星人 - 深蓝色 #111b2d
            var renderer = alien.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 设置为深蓝色 RGB(14, 27, 47) = #111b2d
                renderer.material.color = new Color(14f/255f, 27f/255f, 47f/255f, 1f);
            }
            
            // 添加基本的移动组件
            var rigidbody = alien.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = alien.AddComponent<Rigidbody>();
            }
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 1f;
            rigidbody.angularDamping = 5f;
            
            // 安全设置标签
            try
            {
                alien.tag = "Alien";
            }
            catch (UnityException)
            {
                Debug.LogWarning("[AlienWaveManager] 'Alien' 标签未定义，请运行 Tanks → Setup Alien Tags");
            }
            
            // 首先设置必要的引用
            SetupRequiredPrefabReferences(alien);
            
            // 创建UI组件（健康条滑块）
            SetupAlienUI(alien);
            
            // 临时禁用GameObject来防止组件在未完全设置时触发Awake/OnEnable
            alien.SetActive(false);
            
            // 添加标准的坦克健康组件
            var tankHealth = alien.AddComponent<TankHealth>();
            tankHealth.m_StartingHealth = m_AlienHealth;
            
            // 设置健康条UI引用
            var healthSlider = alien.GetComponentInChildren<Slider>();
            if (healthSlider != null)
            {
                tankHealth.m_Slider = healthSlider;
                var fillImage = healthSlider.fillRect?.GetComponent<UnityEngine.UI.Image>();
                if (fillImage != null)
                {
                    tankHealth.m_FillImage = fillImage;
                }
            }
            
            // 从参考坦克复制爆炸预制体引用
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && gameManager.m_Tank1Prefab != null)
            {
                var refHealth = gameManager.m_Tank1Prefab.GetComponent<TankHealth>();
                if (refHealth != null && refHealth.m_ExplosionPrefab != null)
                {
                    tankHealth.m_ExplosionPrefab = refHealth.m_ExplosionPrefab;
                }
            }
            
            // 添加简单的射击组件
            var tankShooting = alien.AddComponent<TankShooting>();
            ConfigureShootingComponent(tankShooting, alien);
            
            // 添加外星人AI组件
            var alienAI = alien.AddComponent<AlienShootingAI>();
            
            // 现在重新启用GameObject
            alien.SetActive(true);
            alienAI.m_MoveSpeed = m_AlienSpeed;
            alienAI.m_TankShooting = tankShooting;
            alienAI.m_TankHealth = tankHealth;
            
            return alien;
        }
        
        public void OnAlienDestroyed(GameObject alien)
        {
            if (m_CurrentAliens.Contains(alien))
            {
                m_CurrentAliens.Remove(alien);
                
                if (m_EnableDebugLog)
                    Debug.Log($"[AlienWaveManager] 外星人 {alien.name} 已被摧毁，剩余: {m_CurrentAliens.Count}");
                    
                // 延迟销毁以确保组件能正常清理
                Destroy(alien, 1f);
            }
        }
        
        public int GetCurrentWave() => m_CurrentWave + 1;
        public int GetCurrentAlienCount() => m_CurrentAlienCount;
        public int GetAliveAlienCount() => m_CurrentAliens.Count;
        public bool IsSystemActive() => m_IsSystemActive;
        
        private void OnValidate()
        {
            m_WaveInterval = Mathf.Max(5f, m_WaveInterval);
            m_InitialAlienCount = Mathf.Max(1, m_InitialAlienCount);
            m_DifficultyMultiplier = Mathf.Max(1f, m_DifficultyMultiplier);
            m_MaxAliensPerWave = Mathf.Max(1, m_MaxAliensPerWave);
            m_SpawnRadius = Mathf.Max(10f, m_SpawnRadius);
            m_AlienHealth = Mathf.Max(1f, m_AlienHealth);
            m_AlienSpeed = Mathf.Max(0.1f, m_AlienSpeed);
        }
    }
}
