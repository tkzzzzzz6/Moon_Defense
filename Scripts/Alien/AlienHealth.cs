using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class AlienHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public float m_StartingHealth = 100f;
        public bool m_ShowHealthBar = true;
        
        [Header("Health Bar UI")]
        public Slider m_HealthSlider;
        public Image m_FillImage;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;
        
        [Header("Death Effects")]
        public GameObject m_ExplosionPrefab;
        public AudioClip m_DeathSound;
        
        private float m_CurrentHealth;
        private bool m_Dead = false;
        private AudioSource m_AudioSource;
        
        public float CurrentHealth => m_CurrentHealth;
        public bool IsDead => m_Dead;
        
        private void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // 如果没有指定健康条，创建一个简单的
            if (m_ShowHealthBar && m_HealthSlider == null)
            {
                CreateHealthBar();
            }
        }
        
        private void OnEnable()
        {
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            
            UpdateHealthUI();
        }
        
        public void TakeDamage(float amount)
        {
            if (m_Dead) return;
            
            m_CurrentHealth -= amount;
            
            Debug.Log($"[AlienHealth] {gameObject.name} 受到 {amount} 点伤害，剩余生命: {m_CurrentHealth}");
            
            UpdateHealthUI();
            
            if (m_CurrentHealth <= 0f && !m_Dead)
            {
                OnDeath();
            }
        }
        
        private void UpdateHealthUI()
        {
            if (m_HealthSlider != null)
            {
                m_HealthSlider.value = m_CurrentHealth / m_StartingHealth;
                
                if (m_FillImage != null)
                {
                    m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_HealthSlider.value);
                }
            }
        }
        
        private void OnDeath()
        {
            m_Dead = true;
            
            Debug.Log($"[AlienHealth] {gameObject.name} 被摧毁！");
            
            // 播放死亡音效
            if (m_DeathSound != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(m_DeathSound);
            }
            
            // 生成爆炸效果
            if (m_ExplosionPrefab != null)
            {
                GameObject explosion = Instantiate(m_ExplosionPrefab, transform.position, transform.rotation);
                Destroy(explosion, 2f);
            }
            
            // 通知外星人管理器
            var alienAI = GetComponent<SimpleAlienAI>();
            if (alienAI != null)
            {
                alienAI.OnAlienDestroyed();
            }
            
            // 延迟销毁以确保音效播放完毕
            Destroy(gameObject, 0.5f);
        }
        
        private void CreateHealthBar()
        {
            // 创建Canvas
            GameObject canvasGO = new GameObject("AlienHealthCanvas");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = Vector3.up * 3f;
            canvasGO.transform.localScale = Vector3.one * 0.01f;
            
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            // 创建Slider
            GameObject sliderGO = new GameObject("HealthSlider");
            sliderGO.transform.SetParent(canvasGO.transform);
            sliderGO.transform.localPosition = Vector3.zero;
            sliderGO.transform.localScale = Vector3.one;
            
            m_HealthSlider = sliderGO.AddComponent<Slider>();
            m_HealthSlider.minValue = 0f;
            m_HealthSlider.maxValue = 1f;
            m_HealthSlider.value = 1f;
            
            // 创建背景
            GameObject backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(sliderGO.transform);
            backgroundGO.transform.localPosition = Vector3.zero;
            backgroundGO.transform.localScale = Vector3.one;
            
            var bgImage = backgroundGO.AddComponent<Image>();
            bgImage.color = Color.black;
            bgImage.rectTransform.sizeDelta = new Vector2(100, 10);
            
            // 创建Fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(sliderGO.transform);
            fillGO.transform.localPosition = Vector3.zero;
            fillGO.transform.localScale = Vector3.one;
            
            m_FillImage = fillGO.AddComponent<Image>();
            m_FillImage.color = m_FullHealthColor;
            m_FillImage.rectTransform.sizeDelta = new Vector2(100, 10);
            
            // 设置Slider的引用
            m_HealthSlider.fillRect = m_FillImage.rectTransform;
            m_HealthSlider.targetGraphic = m_FillImage;
            
            // 让健康条始终面向摄像机
            canvasGO.AddComponent<Billboard>();
        }
    }
    
    // 让UI始终面向摄像机的组件
    public class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform.position, Vector3.up);
                transform.Rotate(0, 180, 0);
            }
        }
    }
} 