using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    /// 全局音频管理器，负责背景音乐和音效的播放控制
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Background Music")]
        [Tooltip("背景音乐音频片段列表")]
        public AudioClip[] m_BackgroundMusicClips;
        [Tooltip("背景音乐音量")]
        [Range(0f, 1f)]
        public float m_BackgroundMusicVolume = 0.3f;
        [Tooltip("是否在开始时自动播放背景音乐")]
        public bool m_AutoPlayOnStart = true;
        
        [Header("Sound Effects")]
        [Tooltip("全局音效音量")]
        [Range(0f, 1f)]
        public float m_SoundEffectsVolume = 0.7f;
        
        [Header("Debug")]
        public bool m_EnableDebugLog = true;
        
        private AudioSource m_BackgroundMusicSource;
        private int m_CurrentMusicIndex = 0;
        
        // 单例模式
        private static AudioManager s_Instance;
        public static AudioManager Instance => s_Instance;
        
        private void Awake()
        {
            // 实现单例模式
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
                
                InitializeAudioSources();
                
                if (m_EnableDebugLog)
                    Debug.Log("[AudioManager] 音频管理器已初始化");
            }
            else if (s_Instance != this)
            {
                Debug.LogWarning("[AudioManager] 检测到重复的AudioManager，销毁此实例");
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (m_AutoPlayOnStart && m_BackgroundMusicClips.Length > 0)
            {
                PlayBackgroundMusic();
            }
        }
        
        private void InitializeAudioSources()
        {
            // 创建背景音乐AudioSource
            GameObject bgMusicObj = new GameObject("BackgroundMusicSource");
            bgMusicObj.transform.SetParent(transform);
            
            m_BackgroundMusicSource = bgMusicObj.AddComponent<AudioSource>();
            m_BackgroundMusicSource.loop = true;
            m_BackgroundMusicSource.playOnAwake = false;
            m_BackgroundMusicSource.volume = m_BackgroundMusicVolume;
            
            if (m_EnableDebugLog)
                Debug.Log("[AudioManager] 背景音乐AudioSource已创建");
        }
        
        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="musicIndex">音乐索引，-1表示使用当前索引</param>
        public void PlayBackgroundMusic(int musicIndex = -1)
        {
            if (m_BackgroundMusicClips == null || m_BackgroundMusicClips.Length == 0)
            {
                if (m_EnableDebugLog)
                    Debug.LogWarning("[AudioManager] 没有可用的背景音乐");
                return;
            }
            
            if (musicIndex >= 0 && musicIndex < m_BackgroundMusicClips.Length)
            {
                m_CurrentMusicIndex = musicIndex;
            }
            
            var clip = m_BackgroundMusicClips[m_CurrentMusicIndex];
            if (clip != null)
            {
                m_BackgroundMusicSource.clip = clip;
                m_BackgroundMusicSource.volume = m_BackgroundMusicVolume;
                m_BackgroundMusicSource.Play();
                
                if (m_EnableDebugLog)
                    Debug.Log($"[AudioManager] 开始播放背景音乐: {clip.name}");
            }
        }
        
        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (m_BackgroundMusicSource != null && m_BackgroundMusicSource.isPlaying)
            {
                m_BackgroundMusicSource.Stop();
                
                if (m_EnableDebugLog)
                    Debug.Log("[AudioManager] 背景音乐已停止");
            }
        }
        
        /// <summary>
        /// 暂停背景音乐
        /// </summary>
        public void PauseBackgroundMusic()
        {
            if (m_BackgroundMusicSource != null && m_BackgroundMusicSource.isPlaying)
            {
                m_BackgroundMusicSource.Pause();
                
                if (m_EnableDebugLog)
                    Debug.Log("[AudioManager] 背景音乐已暂停");
            }
        }
        
        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        public void ResumeBackgroundMusic()
        {
            if (m_BackgroundMusicSource != null && !m_BackgroundMusicSource.isPlaying)
            {
                m_BackgroundMusicSource.UnPause();
                
                if (m_EnableDebugLog)
                    Debug.Log("[AudioManager] 背景音乐已恢复");
            }
        }
        
        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        /// <param name="volume">音量值 (0-1)</param>
        public void SetBackgroundMusicVolume(float volume)
        {
            m_BackgroundMusicVolume = Mathf.Clamp01(volume);
            
            if (m_BackgroundMusicSource != null)
            {
                m_BackgroundMusicSource.volume = m_BackgroundMusicVolume;
            }
            
            if (m_EnableDebugLog)
                Debug.Log($"[AudioManager] 背景音乐音量设置为: {m_BackgroundMusicVolume}");
        }
        
        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="volume">音量值 (0-1)</param>
        public void SetSoundEffectsVolume(float volume)
        {
            m_SoundEffectsVolume = Mathf.Clamp01(volume);
            
            if (m_EnableDebugLog)
                Debug.Log($"[AudioManager] 音效音量设置为: {m_SoundEffectsVolume}");
        }
        
        /// <summary>
        /// 播放一次性音效
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="volume">音量 (可选)</param>
        public void PlayOneShot(AudioClip clip, float volume = -1f)
        {
            if (clip == null) return;
            
            float finalVolume = volume >= 0f ? volume : m_SoundEffectsVolume;
            
            // 创建临时AudioSource播放音效
            GameObject tempAudioObj = new GameObject($"TempAudio_{clip.name}");
            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            
            tempSource.clip = clip;
            tempSource.volume = finalVolume;
            tempSource.Play();
            
            // 音效播放完毕后销毁临时对象
            Destroy(tempAudioObj, clip.length + 0.1f);
            
            if (m_EnableDebugLog)
                Debug.Log($"[AudioManager] 播放音效: {clip.name}");
        }
        
        /// <summary>
        /// 检查背景音乐是否正在播放
        /// </summary>
        public bool IsBackgroundMusicPlaying()
        {
            return m_BackgroundMusicSource != null && m_BackgroundMusicSource.isPlaying;
        }
        
        /// <summary>
        /// 切换到下一首背景音乐
        /// </summary>
        public void NextBackgroundMusic()
        {
            if (m_BackgroundMusicClips.Length <= 1) return;
            
            m_CurrentMusicIndex = (m_CurrentMusicIndex + 1) % m_BackgroundMusicClips.Length;
            PlayBackgroundMusic();
        }
        
        /// <summary>
        /// 切换到上一首背景音乐
        /// </summary>
        public void PreviousBackgroundMusic()
        {
            if (m_BackgroundMusicClips.Length <= 1) return;
            
            m_CurrentMusicIndex = (m_CurrentMusicIndex - 1 + m_BackgroundMusicClips.Length) % m_BackgroundMusicClips.Length;
            PlayBackgroundMusic();
        }
    }
} 