using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


namespace Tanks.Complete
{
    /// <summary>
    /// 处理坦克在计算机控制模式下的行为
    /// </summary>
    public class TankAI : MonoBehaviour
    {
        // 计算机控制坦克的可能状态：追击目标或逃离目标
        enum State
        {
            Seek,
            Flee
        }
    
        private TankMovement m_Movement;                // 移动脚本的引用
        private TankShooting m_Shooting;                // 射击脚本的引用
        
        private float m_PathfindTime = 0.5f;            // 路径查找的时间间隔，避免性能下降
        private float m_PathfindTimer = 0.0f;           // 距离下次路径查找的时间

        private Transform m_CurrentTarget = null;       // 坦克正在追踪的目标
        private float m_MaxShootingDistance = 0.0f;     // 基于TankShooting设置的最大射击距离

        private float m_TimeBetweenShot = 2.0f;         // AI坦克的射击冷却时间，避免连续射击
        private float m_ShotCooldown = 0.0f;            // 距离下次射击的剩余时间

        private Vector3 m_LastTargetPosition;           // 上一帧目标的位置
        private float m_TimeSinceLastTargetMove;        // 目标未移动的计时器，用于触发逃离状态

        private NavMeshPath m_CurrentPath = null;       // 坦克当前跟随的路径
        private int m_CurrentCorner = 0;                // 坦克当前正在前往的路径拐角
        private bool m_IsMoving = false;                // 坦克是否正在移动（坦克会停下来射击）

        private GameObject[] m_AllTanks;                // 场景中所有坦克的列表

        private State m_CurrentState = State.Seek;      // 坦克当前的AI状态
        private bool m_EnableDebugLog = true;           // 是否启用AI行为的调试日志

        private void Awake()
        {
            //Awake在组件被禁用时仍会被调用。为了让用户可以测试在单个坦克上禁用AI
            //我们确保在初始化之前组件没有被禁用
            if(!isActiveAndEnabled)
                return;
            
            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();

            // 确保移动和射击脚本都设置为"计算机控制"模式
            m_Movement.m_IsComputerControlled = true;
            m_Shooting.m_IsComputerControlled = true;
            
            // 为了避免所有计算机控制的坦克同时进行路径查找（这会增加CPU负担），AI坦克有一个随机的
            // 路径查找时间，使它们分散在多个帧中
            m_PathfindTime = Random.Range(0.3f, 0.6f);
            
            // 计算并存储这个坦克的射击能达到的最大距离。这将用于决定何时开始充能和何时释放射击
            m_MaxShootingDistance = Vector3.Distance(m_Shooting.GetProjectilePosition(1.0f), transform.position);
            
            // 我们使用FindObjectByType来获取所有坦克，这样就不依赖于GameManager，用户可以在一个
            // 还没有添加GameManager的空场景中尝试添加AI
            m_AllTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(e => e.gameObject).ToArray();
        }

        // 如果存在GameManager，它会在创建计算机控制的坦克后调用此函数。这只是用
        // GameManager中的坦克列表替换当前的坦克列表
        public void Setup(GameManager manager)
        {
            // 如果使用manager.m_SpawnPoints.ToArray()，它会得到一个TankManager数组，但m_AllTanks是一个Transform数组。
            // Select函数会对列表中的每个条目（这里是TankManager）调用作为参数传入的函数，并创建一个新列表
            // 包含每个返回值。我们在这里传入的函数，e => e.m_Instance，返回TankManager管理的坦克的Transform
            // 所以实际上manager.m_SpawnPoints.Select(e => e.m_Instance)给出了所有坦克transform的列表。
            m_AllTanks = manager.m_SpawnPoints.Select(e => e.m_Instance).ToArray();
        }

        public void TurnOff()
        {
            enabled = false;
        }

        void Update()
        {
            // 如果有冷却时间，我们将其减少上一帧经过的时间
            if(m_ShotCooldown > 0)
                m_ShotCooldown -= Time.deltaTime;
            
            // 增加自上次路径查找以来的时间。SeekUpdate将检查是否超过路径查找时间
            // 以及是否需要触发新的路径查找
            m_PathfindTimer += Time.deltaTime;

            switch (m_CurrentState)
            {
                case State.Seek:
                    SeekUpdate();
                    break;
                case State.Flee:
                    FleeUpdate();
                    break;
            }
        }

        void SeekUpdate()
        {
            // 为了减轻CPU负担，坦克不会每一帧都进行路径查找。相反，它们
            // 在每次路径查找之间等待一段时间。它们会朝着一个"过时"的位置移动，但由于路径查找时间
            // 不到1秒，这在视觉上并不明显，而且比每秒尝试30多次路径查找要高效得多
            if (m_PathfindTimer > m_PathfindTime)
            {
                // 重置自上次路径查找以来的时间
                m_PathfindTimer = 0;

                Transform target = null;
                NavMeshPath targetPath = null;
                float shortestPath = float.MaxValue;

                // 优先级1: 寻找最近的外星人
                var aliens = FindObjectsOfType<AlienShootingAI>();
                foreach (var alien in aliens)
                {
                    if (alien == null || !alien.gameObject.activeInHierarchy) continue;

                    NavMeshPath pathToAlien = new NavMeshPath();
                    if (NavMesh.CalculatePath(transform.position, alien.transform.position, ~0, pathToAlien))
                    {
                        float pathLength = GetPathLength(pathToAlien);
                        if (pathLength < shortestPath)
                        {
                            shortestPath = pathLength;
                            target = alien.transform;
                            targetPath = pathToAlien;
                        }
                    }
                }

                // 如果没有找到外星人，AI坦克将进入待机状态，不攻击其他坦克
                if (target == null)
                {
                    // 清除当前目标和路径，进入待机模式
                    if (m_CurrentTarget != null)
                    {
                        m_CurrentTarget = null;
                        m_CurrentPath = null;
                        m_IsMoving = false;
                        
                        if (m_EnableDebugLog)
                            Debug.Log($"[TankAI] {gameObject.name} 未发现外星人目标，进入待机状态");
                    }
                    
                    // 在待机状态下，AI可以做一些简单的巡逻动作
                    StartIdlePatrol();
                }

                // 如果找到了目标（外星人或坦克）
                if (target != null)
                {
                    // 我们切换了目标。我们之前追击的坦克比另一个坦克更远了，这个新的
                    // 坦克成为我们的新目标，我们重置最后的位置，因为这是一个新目标
                    if (target != m_CurrentTarget)
                    {
                        m_CurrentTarget = target;
                        m_LastTargetPosition = m_CurrentTarget.position;
                        
                        // 输出调试信息
                        if (target.CompareTag("Alien"))
                        {
                            Debug.Log($"[TankAI] {gameObject.name} 现在瞄准外星人: {target.name}");
                        }
                        else
                        {
                            Debug.Log($"[TankAI] {gameObject.name} 现在瞄准坦克: {target.name} (无外星人目标)");
                        }
                    }

                    m_CurrentTarget = target;
                    m_CurrentPath = targetPath;
                    m_CurrentCorner = 1;
                    m_IsMoving = true;
                }
            }
            // 路径查找现在要么完成了，要么因为最近已经做过而没有在这一帧触发
            // SeekUpdate现在追击并尝试射击它的目标

            // 这个坦克有一个目标...
            if (m_CurrentTarget != null)
            {
                // 检查我们的目标自上次更新以来移动了多远
                float targetMovement = Vector3.Distance(m_CurrentTarget.position, m_LastTargetPosition);

                //目标没有（或几乎没有）移动...
                if (targetMovement < 0.0001f)
                {
                    // 所以我们增加计时器。这稍后会用到，如果我们正在射击的目标2秒内没有移动，我们就逃离
                    m_TimeSinceLastTargetMove += Time.deltaTime;
                }
                else
                {
                    //目标自上次以来移动了，所以我们将自上次移动以来的计时器重置为0
                    m_TimeSinceLastTargetMove = 0;
                }

                // 当前位置成为下一帧用来测试目标是否移动的最后位置
                m_LastTargetPosition = m_CurrentTarget.position;
                
                // 获取从这个坦克到其目标的向量
                Vector3 toTarget = m_CurrentTarget.position - transform.position;
                // 通过将y设为0，我们确保到目标的向量在地面的平面内
                toTarget.y = 0;
                
                float targetDistance = toTarget.magnitude;
                // 将目标向量标准化，使其长度为1，这对某些数学运算很有用
                toTarget.Normalize();
                
                // 两个标准化向量的点积是这两个向量之间角度的余弦值。这很有用，因为
                // 它允许我们测试这些向量的对齐程度：1表示同向，0表示90度角，-1表示反向。
                // 当我们计算前进向量和指向目标的向量之间的点积时，这告诉我们
                // 我们面向目标的程度：如果接近1，说明我们正对着目标。
                float dotToTarget = Vector3.Dot(toTarget, transform.forward);
                
                // 如果我们正在蓄力，检查当前射击是否能击中目标
                if (m_Shooting.IsCharging)
                {
                    // 获取当前蓄力值下弹道估计点
                    Vector3 currentShotTarget = m_Shooting.GetProjectilePosition(m_Shooting.CurrentChargeRatio);
                    // 从我们到该估计点的距离
                    float currentShotDistance = Vector3.Distance(currentShotTarget, transform.position);

                    // 如果我们正对着目标且蓄力足够击中目标，就释放射击
                    // 注意：我们从目标距离中减去2，因为我们的射击有溅射伤害，所以可以
                    // 提前释放射击
                    if (currentShotDistance >= targetDistance - 2 && dotToTarget > 0.99f)
                    {
                        m_IsMoving = false;
                        m_Shooting.StopCharging();
                        
                        // 我们刚刚射击，所以将冷却时间设置为射击间隔（这个值在update函数中每帧递减）
                        m_ShotCooldown = m_TimeBetweenShot;
                        
                        // 我们刚刚射击，而目标已经有一段时间没有移动了。这意味着他们可能也在瞄准和射击我们
                        // 我们进入逃跑模式，而不是站在那里作为静态目标
                        if (m_TimeSinceLastTargetMove > 2.0f)
                        {
                            StartFleeing();
                        }
                    }
                }
                else
                {
                    // 我们还没有开始蓄力，所以检查目标是否在我们的最大射击距离内，这意味着我们可以开始蓄力
                    // （一个"更智能"的解决方案是计算我们可以多早开始蓄力，以便在达到最大距离时已经蓄满力）
                    if (targetDistance < m_MaxShootingDistance)
                    {
                        // 这使用导航网格来检查我们和目标之间是否有障碍物。如果返回false
                        // 这意味着没有无障碍路径，所以存在障碍物，我们不应该开始射击
                        if (!NavMesh.Raycast(transform.position, m_CurrentTarget.position, out var hit, ~0))
                        {
                            // 我们停止移动，因为我们可以用射击击中目标
                            m_IsMoving = false;

                            // 如果我们的冷却时间不是0或以下，我们必须等待它达到0。如果它
                            // 小于0，我们开始蓄力
                            if (m_ShotCooldown <= 0.0f)
                            {
                                m_Shooting.StartCharging();
                            }
                        }
                    }
                }
            }
        }

        private void FleeUpdate()
        {
            // 当逃跑时，坦克会向远离目标的随机点移动。当我们到达路径的最后一个拐角
            // （即点）时，我们可以回到追击模式
            if(m_CurrentCorner >= m_CurrentPath.corners.Length)
                m_CurrentState = State.Seek;
        }

        private void StartFleeing()
        {
            // 要逃跑，我们需要选择一个远离当前目标的点
            
            // 首先获取指向目标的向量...
            var toTarget = (m_CurrentTarget.position - transform.position).normalized;
            
            // 然后将该向量旋转90到180度之间的随机角度，这将给我们一个随机的
            // 相反方向
            toTarget = Quaternion.AngleAxis(Random.Range(90.0f, 180.0f) * Mathf.Sign(Random.Range(-1.0f, 1.0f)),
                Vector3.up) * toTarget;

            // 然后我们在那个随机方向上选择一个5到20单位之间的随机距离的点
            toTarget *= Random.Range(5.0f, 20.0f);

            // 最后我们计算到那个随机点的路径，这成为我们的新当前路径
            if (NavMesh.CalculatePath(transform.position, transform.position + toTarget, NavMesh.AllAreas,
                    m_CurrentPath))
            {
                m_CurrentState = State.Flee;
                m_CurrentCorner = 1;

                m_IsMoving = true;
            }
        }

        private void StartIdlePatrol()
        {
            // 在待机状态下，AI做简单的巡逻动作
            if (m_CurrentPath == null || m_CurrentCorner >= m_CurrentPath.corners.Length)
            {
                // 随机选择一个巡逻点
                Vector3 randomDirection = Random.insideUnitSphere * 15f; // 15米范围内的随机点
                randomDirection.y = 0; // 保持在地面上
                Vector3 patrolPoint = transform.position + randomDirection;
                
                NavMeshPath patrolPath = new NavMeshPath();
                if (NavMesh.CalculatePath(transform.position, patrolPoint, NavMesh.AllAreas, patrolPath))
                {
                    m_CurrentPath = patrolPath;
                    m_CurrentCorner = 1;
                    m_IsMoving = true;
                    
                    if (m_EnableDebugLog && Random.Range(0f, 1f) < 0.1f) // 只在10%的情况下记录，避免刷屏
                        Debug.Log($"[TankAI] {gameObject.name} 开始巡逻到: {patrolPoint}");
                }
            }
        }

        // 与Update不同（Update在每一帧都被调用，所以每秒调用的次数取决于游戏渲染速度），
        // FixedUpdate在项目物理设置中定义的固定时间间隔被调用。所有物理代码都应该放在这里。
        private void FixedUpdate()
        {
            // 如果坦克当前没有路径，提前退出
            if(m_CurrentPath == null || m_CurrentPath.corners.Length == 0)
                return;
            
            var rb = m_Movement.Rigidbody;
            
            // 我们要朝向的点。默认是路径中的当前拐角
            Vector3 orientTarget = m_CurrentPath.corners[Mathf.Min(m_CurrentCorner, m_CurrentPath.corners.Length - 1)];

            // 如果我们不移动，我们就朝向目标
            if (!m_IsMoving)
                orientTarget = m_CurrentTarget.position;

            Vector3 toOrientTarget = orientTarget - transform.position;
            toOrientTarget.y = 0;
            toOrientTarget.Normalize();

            Vector3 forward = rb.rotation * Vector3.forward;

            float orientDot = Vector3.Dot(forward, toOrientTarget);
            float rotatingAngle = Vector3.SignedAngle(toOrientTarget, forward, Vector3.up);

            // 如果我们正在移动，我们以最大速度向前移动
            float moveAmount = Mathf.Clamp01(orientDot) * m_Movement.m_Speed * Time.deltaTime;
            if (m_IsMoving && moveAmount > 0.000001f)
            {
                rb.MovePosition(rb.position + forward * moveAmount);
            }

            // 这一帧的实际旋转角度是该时间帧的最大转向速度和角度本身之间的较小值
            // 乘以角度的符号以确保我们向正确的方向旋转
            rotatingAngle = Mathf.Sign(rotatingAngle) * Mathf.Min(Mathf.Abs(rotatingAngle), m_Movement.m_TurnSpeed * Time.deltaTime);
            
            if(Mathf.Abs(rotatingAngle) > 0.000001f)
                rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(-rotatingAngle, Vector3.up));

            // 如果我们到达当前目标，我们增加拐角计数。当目标是另一个坦克时，
            // 我们永远不会到达目标，因为我们会提前停止
            if (Vector3.Distance(rb.position, orientTarget) < 0.5f)
            {
                m_CurrentCorner += 1;
            }
        }

        // 工具函数，将给定路径的所有段的长度相加得到其有效长度
        float GetPathLength(NavMeshPath path)
        {
            float dist = 0;
            for (var i = 1; i < path.corners.Length; ++i)
            {
                dist += Vector3.Distance(path.corners[i-1], path.corners[i]);
            }

            return dist;
        }
    }
}