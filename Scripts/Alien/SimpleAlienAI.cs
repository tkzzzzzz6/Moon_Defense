using UnityEngine;
using System.Collections.Generic;

namespace Tanks.Complete
{
    public class SimpleAlienAI : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float m_MoveSpeed = 5f;
        public float m_RotationSpeed = 2f;
        public float m_DetectionRange = 30f;
        
        [Header("Combat Settings")]
        public float m_AttackRange = 5f;
        public float m_AttackDamage = 25f;
        public float m_AttackCooldown = 2f;
        
        private Rigidbody m_Rigidbody;
        private Transform m_TargetTank;
        private GameManager m_GameManager;
        private float m_LastAttackTime;
        
        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_GameManager = FindObjectOfType<GameManager>();
            
            if (m_Rigidbody == null)
            {
                Debug.LogWarning($"[SimpleAlienAI] {gameObject.name} 缺少Rigidbody组件");
            }
            
            Debug.Log($"[SimpleAlienAI] {gameObject.name} 初始化完成");
        }
        
        private void Update()
        {
            FindNearestTank();
            
            if (m_TargetTank != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, m_TargetTank.position);
                
                if (distanceToTarget <= m_AttackRange)
                {
                    // 攻击范围内 - 尝试攻击
                    TryAttack();
                }
                else if (distanceToTarget <= m_DetectionRange)
                {
                    // 检测范围内 - 移动到目标
                    MoveTowardsTarget();
                }
            }
        }
        
        private void FindNearestTank()
        {
            if (m_GameManager == null) return;
            
            var aliveTanks = m_GameManager.GetAliveTanks();
            if (aliveTanks.Count == 0)
            {
                m_TargetTank = null;
                return;
            }
            
            Transform nearestTank = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var tank in aliveTanks)
            {
                if (tank == null) continue;
                
                float distance = Vector3.Distance(transform.position, tank.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTank = tank;
                }
            }
            
            m_TargetTank = nearestTank;
        }
        
        private void MoveTowardsTarget()
        {
            if (m_TargetTank == null || m_Rigidbody == null) return;
            
            Vector3 direction = (m_TargetTank.position - transform.position).normalized;
            
            // 旋转朝向目标
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_RotationSpeed * Time.deltaTime);
            }
            
            // 向目标移动
            Vector3 moveForce = direction * m_MoveSpeed;
            moveForce.y = 0; // 确保不在Y轴上移动
            
            m_Rigidbody.AddForce(moveForce, ForceMode.Acceleration);
        }
        
        private void TryAttack()
        {
            if (Time.time - m_LastAttackTime < m_AttackCooldown) return;
            
            if (m_TargetTank != null)
            {
                // 尝试对坦克造成伤害
                var tankHealth = m_TargetTank.GetComponent<TankHealth>();
                if (tankHealth != null)
                {
                    tankHealth.TakeDamage(m_AttackDamage);
                    Debug.Log($"[SimpleAlienAI] {gameObject.name} 攻击了 {m_TargetTank.name} 造成 {m_AttackDamage} 点伤害");
                }
                
                m_LastAttackTime = Time.time;
                
                // 添加攻击特效 (可选)
                ShowAttackEffect();
            }
        }
        
        private void ShowAttackEffect()
        {
            // 简单的攻击特效 - 临时改变颜色
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                StartCoroutine(FlashAttackEffect(renderer));
            }
        }
        
        private System.Collections.IEnumerator FlashAttackEffect(Renderer renderer)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.yellow;
            yield return new WaitForSeconds(0.2f);
            renderer.material.color = originalColor;
        }
        
        public void OnAlienDestroyed()
        {
            // 通知外星人管理器该外星人已被摧毁
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
            if (m_TargetTank != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, m_TargetTank.position);
            }
        }
    }
} 