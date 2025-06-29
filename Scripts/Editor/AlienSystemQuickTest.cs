#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Tanks.Complete
{
    public class AlienSystemQuickTest : EditorWindow
    {
        [MenuItem("Tools/Quick Alien Test")]
        public static void ShowWindow()
        {
            GetWindow<AlienSystemQuickTest>("外星人快速测试");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("外星人系统快速测试", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("立即生成外星人 (测试)"))
            {
                if (Application.isPlaying)
                {
                    TestSpawnAliens();
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "需要在播放模式下进行测试！", "确定");
                }
            }
            
            if (GUILayout.Button("清除所有外星人"))
            {
                ClearAllAliens();
            }
            
            EditorGUILayout.Space();
            
            if (Application.isPlaying)
            {
                var alienManager = FindObjectOfType<AlienWaveManager>();
                if (alienManager != null)
                {
                    EditorGUILayout.LabelField("系统状态:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"系统激活: {alienManager.IsSystemActive()}");
                    EditorGUILayout.LabelField($"当前波次: {alienManager.GetCurrentWave()}");
                    EditorGUILayout.LabelField($"预计外星人数量: {alienManager.GetCurrentAlienCount()}");
                    
                    EditorGUILayout.Space();
                    
                    var aliens = FindObjectsOfType<AlienShootingAI>();
                    EditorGUILayout.LabelField($"场景中外星人数量: {aliens.Length}");
                    
                    // 显示坦克信息
                    var gameManager = FindObjectOfType<GameManager>();
                    if (gameManager != null)
                    {
                        var aliveTanks = gameManager.GetAliveTanks();
                        EditorGUILayout.LabelField($"存活坦克数量: {aliveTanks.Count}");
                        
                        // 检查坦克AI是否优先攻击外星人
                        var tankAIs = FindObjectsOfType<TankAI>();
                        EditorGUILayout.LabelField($"AI坦克数量: {tankAIs.Length}");
                    }
                    
                    if (aliens.Length > 0)
                    {
                        EditorGUILayout.LabelField("外星人列表:", EditorStyles.boldLabel);
                        foreach (var alien in aliens)
                        {
                            var health = alien.m_TankHealth;
                            string healthInfo = health != null ? $" (生命: {health.CurrentHealth:F0})" : "";
                            EditorGUILayout.LabelField($"  - {alien.gameObject.name}{healthInfo}");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("未找到 AlienWaveManager", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("需要在播放模式下查看状态", MessageType.Info);
            }
        }
        
        private void TestSpawnAliens()
        {
            var alienManager = FindObjectOfType<AlienWaveManager>();
            if (alienManager == null)
            {
                Debug.LogError("[QuickTest] 未找到 AlienWaveManager!");
                return;
            }
            
            // 直接调用生成方法进行测试
            for (int i = 0; i < 3; i++)
            {
                Vector3 spawnPos = new Vector3(
                    Random.Range(-20f, 20f),
                    0f,
                    Random.Range(-20f, 20f)
                );
                
                GameObject alien = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                alien.transform.position = spawnPos;
                alien.transform.localScale = Vector3.one * 2f;
                alien.name = $"TestAlien_{i + 1}";
                
                // 设置颜色 - 深蓝色 #111b2d
                var renderer = alien.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // 设置为深蓝色 RGB(14, 27, 47) = #111b2d
                    renderer.material.color = new Color(14f/255f, 27f/255f, 47f/255f, 1f);
                }
                
                // 添加物理组件
                var rigidbody = alien.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    rigidbody = alien.AddComponent<Rigidbody>();
                }
                rigidbody.mass = 1f;
                rigidbody.linearDamping = 1f;
                rigidbody.angularDamping = 5f;
                
                // 设置标签和Layer
                alien.tag = "Alien";
                alien.layer = LayerMask.NameToLayer("Players");
                
                // 添加坦克健康组件
                var tankHealth = alien.AddComponent<TankHealth>();
                tankHealth.m_StartingHealth = 100f;
                
                // 添加射击组件
                var tankShooting = alien.AddComponent<TankShooting>();
                tankShooting.m_MaxDamage = 30f;
                tankShooting.m_ShotCooldown = 1.5f;
                tankShooting.m_IsComputerControlled = true;
                
                // 添加外星人AI组件
                var alienAI = alien.AddComponent<AlienShootingAI>();
                alienAI.m_MoveSpeed = 5f;
                alienAI.m_TankShooting = tankShooting;
                alienAI.m_TankHealth = tankHealth;
                
                Debug.Log($"[QuickTest] 生成测试外星人: {alien.name} 位置: {spawnPos}");
            }
            
            Debug.Log("[QuickTest] 完成生成 3 个测试外星人");
        }
        
        private void ClearAllAliens()
        {
            var aliens = FindObjectsOfType<AlienShootingAI>();
            int count = 0;
            
            foreach (var alien in aliens)
            {
                if (Application.isPlaying)
                {
                    Destroy(alien.gameObject);
                }
                else
                {
                    DestroyImmediate(alien.gameObject);
                }
                count++;
            }
            
            Debug.Log($"[QuickTest] 清除了 {count} 个外星人");
        }
    }
}
#endif 