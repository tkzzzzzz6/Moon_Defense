#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Tanks.Complete
{
    /// <summary>
    /// 游戏系统诊断工具，用于检测常见的游戏启动和音频问题
    /// </summary>
    public class GameSystemDiagnostic : EditorWindow
    {
        [MenuItem("Tanks/Game System Diagnostic", priority = 2)]
        public static void ShowWindow()
        {
            GetWindow<GameSystemDiagnostic>("游戏系统诊断");
        }
        
        private Vector2 scrollPosition;
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("游戏系统诊断工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 基础系统检查
            EditorGUILayout.LabelField("基础系统检查", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("检查GameManager设置"))
            {
                CheckGameManager();
            }
            
            if (GUILayout.Button("检查UI系统"))
            {
                CheckUISystem();
            }
            
            if (GUILayout.Button("检查音频系统"))
            {
                CheckAudioSystem();
            }
            
            if (GUILayout.Button("检查外星人系统"))
            {
                CheckAlienSystem();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("快速修复", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("创建AudioManager"))
            {
                CreateAudioManager();
            }
            
            if (GUILayout.Button("设置外星人标签"))
            {
                AlienTagSetup.SetupAlienTags();
            }
            
            if (GUILayout.Button("检查场景完整性"))
            {
                CheckSceneIntegrity();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void CheckGameManager()
        {
            var gameManager = FindObjectOfType<GameManager>();
            
            if (gameManager == null)
            {
                Debug.LogError("[诊断] 场景中没有找到GameManager！");
                return;
            }
            
            Debug.Log("[诊断] ========== GameManager检查开始 ==========");
            Debug.Log($"[诊断] GameManager名称: {gameManager.name}");
            Debug.Log($"[诊断] 是否启用: {gameManager.enabled}");
            Debug.Log($"[诊断] GameObject是否激活: {gameManager.gameObject.activeSelf}");
            
            // 检查坦克预制体
            bool tankPrefabsOK = true;
            if (gameManager.m_Tank1Prefab == null) { Debug.LogError("[诊断] Tank1Prefab未分配"); tankPrefabsOK = false; }
            if (gameManager.m_Tank2Prefab == null) { Debug.LogError("[诊断] Tank2Prefab未分配"); tankPrefabsOK = false; }
            if (gameManager.m_Tank3Prefab == null) { Debug.LogError("[诊断] Tank3Prefab未分配"); tankPrefabsOK = false; }
            if (gameManager.m_Tank4Prefab == null) { Debug.LogError("[诊断] Tank4Prefab未分配"); tankPrefabsOK = false; }
            
            if (tankPrefabsOK)
                Debug.Log("[诊断] ✓ 所有坦克预制体已正确分配");
                
            // 检查摄像机控制
            if (gameManager.m_CameraControl == null)
                Debug.LogError("[诊断] CameraControl未分配");
            else
                Debug.Log("[诊断] ✓ CameraControl已分配");
                
            // 检查生成点
            if (gameManager.m_SpawnPoints == null || gameManager.m_SpawnPoints.Length == 0)
                Debug.LogError("[诊断] SpawnPoints未设置");
            else
                Debug.Log($"[诊断] ✓ 生成点数量: {gameManager.m_SpawnPoints.Length}");
                
            // 检查外星人系统
            Debug.Log($"[诊断] 外星人入侵系统启用: {gameManager.m_EnableAlienInvasion}");
            if (gameManager.m_AlienWaveManager == null)
                Debug.LogWarning("[诊断] AlienWaveManager未分配");
            else
                Debug.Log("[诊断] ✓ AlienWaveManager已分配");
                
            Debug.Log("[诊断] ========== GameManager检查结束 ==========");
        }
        
        private void CheckUISystem()
        {
            var uiHandler = FindObjectOfType<GameUIHandler>();
            
            if (uiHandler == null)
            {
                Debug.LogError("[诊断] 场景中没有找到GameUIHandler！");
                return;
            }
            
            Debug.Log("[诊断] ========== UI系统检查开始 ==========");
            Debug.Log($"[诊断] GameUIHandler名称: {uiHandler.name}");
            Debug.Log($"[诊断] 是否启用: {uiHandler.enabled}");
            
            // 检查GameManager引用
            if (uiHandler.m_GameManager == null)
                Debug.LogError("[诊断] GameUIHandler的GameManager引用为空！");
            else
                Debug.Log("[诊断] ✓ GameManager引用正确");
                
            // 检查开始按钮
            if (uiHandler.m_StartButton == null)
                Debug.LogError("[诊断] 开始按钮未分配");
            else
            {
                Debug.Log($"[诊断] ✓ 开始按钮已分配，可交互: {uiHandler.m_StartButton.interactable}");
                
                // 检查按钮事件
                var buttonEvents = uiHandler.m_StartButton.onClick.GetPersistentEventCount();
                Debug.Log($"[诊断] 开始按钮持久事件数量: {buttonEvents}");
            }
            
            // 检查玩家槽位
            if (uiHandler.m_PlayerSlots == null || uiHandler.m_PlayerSlots.Length == 0)
                Debug.LogError("[诊断] 玩家槽位未设置");
            else
                Debug.Log($"[诊断] ✓ 玩家槽位数量: {uiHandler.m_PlayerSlots.Length}");
                
            Debug.Log("[诊断] ========== UI系统检查结束 ==========");
        }
        
        private void CheckAudioSystem()
        {
            Debug.Log("[诊断] ========== 音频系统检查开始 ==========");
            
            // 检查AudioManager
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("[诊断] 场景中没有AudioManager");
                Debug.Log("[诊断] 建议：点击'创建AudioManager'按钮来添加音频管理器");
            }
            else
            {
                Debug.Log($"[诊断] ✓ AudioManager已找到: {audioManager.name}");
                Debug.Log($"[诊断] 背景音乐片段数量: {(audioManager.m_BackgroundMusicClips?.Length ?? 0)}");
                Debug.Log($"[诊断] 自动播放: {audioManager.m_AutoPlayOnStart}");
            }
            
            // 检查AudioListener
            var audioListener = FindObjectOfType<AudioListener>();
            if (audioListener == null)
                Debug.LogError("[诊断] 场景中没有AudioListener！");
            else
                Debug.Log($"[诊断] ✓ AudioListener已找到: {audioListener.name}");
                
            // 检查摄像机上的AudioListener
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var cameraListener = mainCamera.GetComponent<AudioListener>();
                if (cameraListener == null)
                    Debug.LogWarning("[诊断] 主摄像机上没有AudioListener");
                else
                    Debug.Log("[诊断] ✓ 主摄像机上有AudioListener");
            }
            
            Debug.Log("[诊断] ========== 音频系统检查结束 ==========");
        }
        
        private void CheckAlienSystem()
        {
            Debug.Log("[诊断] ========== 外星人系统检查开始 ==========");
            
            var alienManager = FindObjectOfType<AlienWaveManager>();
            if (alienManager == null)
            {
                Debug.LogWarning("[诊断] 场景中没有AlienWaveManager");
            }
            else
            {
                Debug.Log($"[诊断] ✓ AlienWaveManager已找到: {alienManager.name}");
                Debug.Log($"[诊断] 系统启用: {alienManager.m_EnableAlienSystem}");
                Debug.Log($"[诊断] 外星人预制体: {(alienManager.m_AlienPrefab != null ? "已分配" : "未分配")}");
                Debug.Log($"[诊断] 波次间隔: {alienManager.m_WaveInterval}秒");
            }
            
            // 检查Alien标签
            CheckAlienTag();
            
            Debug.Log("[诊断] ========== 外星人系统检查结束 ==========");
        }
        
        private void CreateAudioManager()
        {
            var existing = FindObjectOfType<AudioManager>();
            if (existing != null)
            {
                Debug.LogWarning("[诊断] AudioManager已存在，无需重复创建");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            GameObject audioManagerObj = new GameObject("AudioManager");
            var audioManager = audioManagerObj.AddComponent<AudioManager>();
            
            // 设置默认值
            audioManager.m_BackgroundMusicVolume = 0.3f;
            audioManager.m_SoundEffectsVolume = 0.7f;
            audioManager.m_AutoPlayOnStart = true;
            audioManager.m_EnableDebugLog = true;
            
            // 选中创建的对象
            Selection.activeGameObject = audioManagerObj;
            
            Debug.Log("[诊断] ✓ AudioManager已创建！请在Inspector中分配背景音乐AudioClip");
        }
        
        private void CheckSceneIntegrity()
        {
            Debug.Log("[诊断] ========== 场景完整性检查开始 ==========");
            
            // 检查基本组件
            CheckGameManager();
            CheckUISystem();
            CheckAudioSystem();
            
            // 检查Input System
            var inputSystemUI = FindObjectOfType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputSystemUI == null)
                Debug.LogWarning("[诊断] 没有找到Input System UI Input Module");
            else
                Debug.Log("[诊断] ✓ Input System UI配置正确");
                
            // 检查EventSystem
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
                Debug.LogError("[诊断] 没有找到EventSystem！UI交互将无法工作");
            else
                Debug.Log("[诊断] ✓ EventSystem已配置");
                
            Debug.Log("[诊断] ========== 场景完整性检查结束 ==========");
        }
        
        private void CheckAlienTag()
        {
            // 检查Alien标签是否存在
            try
            {
                var testObj = new GameObject("TempTagTest");
                testObj.tag = "Alien";
                DestroyImmediate(testObj);
                Debug.Log("[诊断] ✓ Alien标签已正确设置");
            }
            catch (UnityException)
            {
                Debug.LogError("[诊断] Alien标签未定义！请点击'设置外星人标签'按钮");
            }
        }
    }
}
#endif 