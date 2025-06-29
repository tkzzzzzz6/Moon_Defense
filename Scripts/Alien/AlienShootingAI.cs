using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Tanks.Complete
{
    /// <summary>
    /// 外星人AI - 参考TankAI实现，专门攻击坦克
    /// </summary>
    public class AlienShootingAI : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float m_MoveSpeed = 5f;
        public float m_RotationSpeed = 2f;
        public float m_DetectionRange = 30f;
        
        [Header("Combat Settings")]
        public float m_AttackRange = 20f; // 射击范围
        
        [HideInInspector] public TankShooting m_TankShooting;
        [HideInInspector] public TankHealth m_TankHealth;
        
        private Rigidbody m_Rigidbody;
        private Transform m_CurrentTarget = null;
        private GameManager m_GameManager;
        
        // AI控制相关
        private float m_PathfindTime = 0.7f;
        private float m_PathfindTimer = 0.0f;
        private NavMeshPath m_CurrentPath = null;
        private int m_CurrentCorner = 0;
        private bool m_IsMoving = false;
        private float m_MaxShootingDistance = 0.0f;
        
        private GameObject[] m_AllTanks;
        private bool m_IsInitialized = false;
        
        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null)
            {
                m_Rigidbody = gameObject.AddComponent<Rigidbody>();
                m_Rigidbody.mass = 1f;
                m_Rigidbody.linearDamping = 1f;
                m_Rigidbody.angularDamping = 5f;
            }
            
            m_GameManager = FindObjectOfType<GameManager>();
            
            // 随机化寻路时间以避免所有外星人同时寻路
            m_PathfindTime = Random.Range(0.5f, 1.0f);
        }
        
        private void Start()
        {
            // 延迟初始化，让其他组件先完成设置
            StartCoroutine(InitializeWithDelay());
        }
        
        private System.Collections.IEnumerator InitializeWithDelay()
        {
            // 等待一帧，确保所有组件都完成了设置
            yield return null;
            
            // 等待TankShooting组件设置完成
            if (m_TankShooting == null)
                m_TankShooting = GetComponent<TankShooting>();
                
            if (m_TankHealth == null)
                m_TankHealth = GetComponent<TankHealth>();
            
            // 安全计算最大射击距离
            if (m_TankShooting != null && m_TankShooting.m_FireTransform != null)
            {
                try
                {
                    m_MaxShootingDistance = Vector3.Distance(m_TankShooting.GetProjectilePosition(1.0f), transform.position);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[AlienShootingAI] {gameObject.name} 计算射击距离时出错: {e.Message}，使用默认值");
                    m_MaxShootingDistance = m_AttackRange; // 使用攻击范围作为默认值
                }
            }
            else
            {
                Debug.LogWarning($"[AlienShootingAI] {gameObject.name} TankShooting或FireTransform未设置，使用默认射击距离");
                m_MaxShootingDistance = m_AttackRange; // 使用攻击范围作为默认值
            }
            
            // 获取所有坦克
            if (m_GameManager != null)
            {
                m_AllTanks = m_GameManager.m_SpawnPoints.Select(e => e.m_Instance).ToArray();
            }
            else
            {
                m_AllTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Select(e => e.gameObject).ToArray();
            }
            
            m_IsInitialized = true;
            Debug.Log($"[AlienShootingAI] {gameObject.name} 初始化完成，最大射击距离: {m_MaxShootingDistance}");
        }
        
        private void Update()
        {
            // 等待初始化完成
            if (!m_IsInitialized) return;
            
            // 检查是否死亡
            if (m_TankHealth != null && m_TankHealth.CurrentHealth <= 0)
            {
                OnAlienDestroyed();
                return;
            }
            
            m_PathfindTimer += Time.deltaTime;
            
            SeekAndShootUpdate();
        }
        
        private void SeekAndShootUpdate()
        {
            try
            {
                // 定期寻找目标
                if (m_PathfindTimer > m_PathfindTime)
                {
                    m_PathfindTimer = 0;
                    FindNearestTank();
                }
                
                // 如果有目标，执行攻击逻辑
                if (m_CurrentTarget != null)
                {
                    Vector3 toTarget = m_CurrentTarget.position - transform.position;
                    toTarget.y = 0;
                    float targetDistance = toTarget.magnitude;
                    toTarget.Normalize();
                    
                    float dotToTarget = Vector3.Dot(toTarget, transform.forward);
                    
                    // 如果在射击范围内且瞄准了目标
                    if (targetDistance <= m_MaxShootingDistance && targetDistance <= m_AttackRange)
                    {
                        // 停止移动并射击
                        m_IsMoving = false;
                        
                        // 使用Physics射线检测而不是NavMesh检测
                        if (!Physics.Raycast(transform.position, (m_CurrentTarget.position - transform.position).normalized, 
                                            targetDistance, LayerMask.GetMask("Default", "Environment")))
                        {
                            // 开始蓄力射击
                            if (m_TankShooting != null && !m_TankShooting.IsCharging)
                            {
                                m_TankShooting.StartCharging();
                            }
                            
                            // 如果瞄准足够准确，释放射击
                            if (m_TankShooting != null && m_TankShooting.IsCharging && dotToTarget > 0.95f)
                            {
                                try
                                {
                                    Vector3 currentShotTarget = m_TankShooting.GetProjectilePosition(m_TankShooting.CurrentChargeRatio);
                                    float currentShotDistance = Vector3.Distance(currentShotTarget, transform.position);
                                    
                                    if (currentShotDistance >= targetDistance - 2f)
                                    {
                                        m_TankShooting.StopCharging();
                                        Debug.Log($"[AlienShootingAI] {gameObject.name} 射击目标: {m_CurrentTarget.name}");
                                    }
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogWarning($"[AlienShootingAI] {gameObject.name} 射击计算错误: {e.Message}");
                                    // 如果计算失败，直接射击
                                    if (m_TankShooting.IsCharging)
                                    {
                                        m_TankShooting.StopCharging();
                                    }
                                }
                            }
                        }
                    }
                    else if (targetDistance > m_AttackRange)
                    {
                        // 超出射击范围，继续移动接近目标
                        m_IsMoving = true;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AlienShootingAI] {gameObject.name} SeekAndShootUpdate发生错误: {e.Message}");
                // 发生错误时停止当前操作
                m_IsMoving = false;
                if (m_TankShooting != null && m_TankShooting.IsCharging)
                {
                    m_TankShooting.StopCharging();
                }
            }
        }
        
        private void FindNearestTank()
        {
            if (m_AllTanks == null || m_AllTanks.Length == 0) 
            {
                Debug.LogWarning($"[AlienShootingAI] {gameObject.name} 没有找到坦克列表");
                return;
            }
            
            Transform nearestTank = null;
            float nearestDistance = float.MaxValue;
            
            // 先用简单的距离检测找到最近的坦克，避免频繁的NavMesh计算
            foreach (var tank in m_AllTanks)
            {
                if (tank == null || !tank.activeInHierarchy) continue;
                
                // 跳过外星人（只攻击坦克）
                if (tank.CompareTag("Alien")) continue;
                
                // 检查坦克是否还活着
                var tankHealth = tank.GetComponent<TankHealth>();
                if (tankHealth != null && tankHealth.CurrentHealth <= 0) continue;
                
                float distance = Vector3.Distance(transform.position, tank.transform.position);
                if (distance < nearestDistance && distance <= m_DetectionRange)
                {
                    nearestDistance = distance;
                    nearestTank = tank.transform;
                }
            }
            
            // 如果找到了新目标，尝试计算路径
            if (nearestTank != null && nearestTank != m_CurrentTarget)
            {
                // 简化的移动方式：直接朝目标移动，不使用复杂的NavMesh路径
                m_CurrentTarget = nearestTank;
                m_IsMoving = true;
                
                Debug.Log($"[AlienShootingAI] {gameObject.name} 锁定新目标: {m_CurrentTarget.name}，距离: {nearestDistance:F1}");
            }
            else if (nearestTank == null && m_CurrentTarget != null)
            {
                // 如果失去了目标
                m_CurrentTarget = null;
                m_IsMoving = false;
                Debug.Log($"[AlienShootingAI] {gameObject.name} 失去目标");
            }
        }
        
        private void FixedUpdate()
        {
            if (!m_IsInitialized) return;
            
            var rb = m_Rigidbody;
            if (rb == null) return;
            
            // 如果有目标，直接朝目标移动
            if (m_CurrentTarget != null)
            {
                Vector3 toTarget = m_CurrentTarget.position - transform.position;
                toTarget.y = 0;
                toTarget.Normalize();
                
                Vector3 forward = rb.rotation * Vector3.forward;
                float orientDot = Vector3.Dot(forward, toTarget);
                float rotatingAngle = Vector3.SignedAngle(toTarget, forward, Vector3.up);
                
                // 移动（只有在面向目标时才移动）
                if (m_IsMoving && orientDot > 0.5f)
                {
                    float moveAmount = m_MoveSpeed * Time.deltaTime;
                    rb.MovePosition(rb.position + forward * moveAmount);
                }
                
                // 旋转朝向目标
                rotatingAngle = Mathf.Sign(rotatingAngle) * Mathf.Min(Mathf.Abs(rotatingAngle), m_RotationSpeed * 180f * Time.deltaTime);
                if (Mathf.Abs(rotatingAngle) > 0.001f)
                {
                    rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(-rotatingAngle, Vector3.up));
                }
            }
        }
        
        private float GetPathLength(NavMeshPath path)
        {
            float dist = 0;
            for (var i = 1; i < path.corners.Length; ++i)
            {
                dist += Vector3.Distance(path.corners[i-1], path.corners[i]);
            }
            return dist;
        }
        
        public void OnAlienDestroyed()
        {
            // 通知外星人管理器
            var alienManager = FindObjectOfType<AlienWaveManager>();
            if (alienManager != null)
            {
                alienManager.OnAlienDestroyed(gameObject);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // 绘制检测范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_DetectionRange);
            
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_AttackRange);
            
            // 绘制到目标的连线
            if (m_CurrentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, m_CurrentTarget.position);
            }
        }
    }
} 