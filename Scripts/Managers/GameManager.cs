using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Tanks.Complete
{
    public class GameManager : MonoBehaviour
    {
        // Which state the game is currently in
        public enum GameState
        {
            MainMenu,
            Game
        }

        // Data about the selected tanks passed from the menu to the GameManager
        public class PlayerData
        {
            public bool IsComputer;
            public Color TankColor;
            public GameObject UsedPrefab;
            public int ControlIndex;
        }
        
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.

        [Header("Tanks Prefabs")]
        public GameObject m_Tank1Prefab;            // The Prefab used by the tank in Slot 1 of the Menu
        public GameObject m_Tank2Prefab;            // The Prefab used by the tank in Slot 2 of the Menu
        public GameObject m_Tank3Prefab;            // The Prefab used by the tank in Slot 3 of the Menu
        public GameObject m_Tank4Prefab;            // The Prefab used by the tank in Slot 4 of the Menu
        
        [FormerlySerializedAs("m_Tanks")] 
        public TankManager[] m_SpawnPoints;         // A collection of managers for enabling and disabling different aspects of the tanks.
        
        [Header("Alien Invasion System")]
        [Tooltip("外星飞船波次管理器")]
        public AlienWaveManager m_AlienWaveManager; // Reference to the alien wave manager for controlling alien invasions.
        
        [Tooltip("是否启用外星飞船入侵系统")]
        public bool m_EnableAlienInvasion = true;   // Whether to enable the alien invasion system.
        
        [Header("Audio System")]
        [Tooltip("背景音乐AudioSource")]
        public AudioSource m_BackgroundMusicAudio;
        [Tooltip("背景音乐音频片段")]
        public AudioClip m_BackgroundMusicClip;
        [Tooltip("背景音乐音量")]
        [Range(0f, 1f)]
        public float m_BackgroundMusicVolume = 0.3f;

        private GameState m_CurrentState;
        
        private int m_RoundNumber;                  // Which round the game is currently on.
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
        private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
        private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.

        private PlayerData[] m_TankData;            // Data passed from the menu about each selected tank (at least 2, max 4)
        private int m_PlayerCount = 0;              // The number of players (2 to 4), decided from the number of PlayerData passed by the menu
        private TextMeshProUGUI m_TitleText;        // The text used to display game message. Automatically found as part of the Menu prefab

        private void Start()
        {
            m_CurrentState = GameState.MainMenu;

            // Initialize background music
            InitializeBackgroundMusic();

            // Find the text used to display game info. Need to look at inactive object too, as the Menu prefab (which contains it) may be
            // disabled at the start when the user have a Title Screen which will enable the Menu.
            var textRef = FindAnyObjectByType<MessageTextReference>(FindObjectsInactive.Include);

            // If that text couldn't be found, we display an error and exit as it is required for the game manager to work
            if (textRef == null)
            {
                Debug.LogError("You need to add the Menus prefab in the scene to use the GameManager!");
                return;
            }

            m_TitleText = textRef.Text;
            m_TitleText.text = "";

            // The GameManager require 4 tanks prefabs, as the start menu have 4 fixed slot and need the 4 tanks to show there
            if (m_Tank1Prefab == null || m_Tank2Prefab == null || m_Tank3Prefab == null || m_Tank4Prefab == null)
            {
                Debug.LogError("You need to assign 4 tank prefab in the GameManager!");
            }
        }

        void GameStart()
        {
            Debug.Log("[GameManager] GameStart 执行开始");
            
            // Create the delays so they only have to be made once.
            m_StartWait = new WaitForSeconds (m_StartDelay);
            m_EndWait = new WaitForSeconds (m_EndDelay);

            Debug.Log("[GameManager] 开始生成坦克");
            SpawnAllTanks();
            
            Debug.Log("[GameManager] 设置摄像机目标");
            SetCameraTargets();

            // Initialize and start the alien invasion system if enabled
            if (m_EnableAlienInvasion && m_AlienWaveManager != null)
            {
                m_AlienWaveManager.Initialize(this);
                m_AlienWaveManager.StartAlienWaves();
                Debug.Log("[GameManager] 外星飞船入侵系统已启动");
            }
            else if (m_EnableAlienInvasion && m_AlienWaveManager == null)
            {
                Debug.LogWarning("[GameManager] 外星飞船入侵系统已启用，但未分配AlienWaveManager！");
            }

            Debug.Log("[GameManager] 开始游戏循环");
            // Once the tanks have been created and the camera is using them as targets, start the game.
            StartCoroutine (GameLoop ());
        }

        void ChangeGameState(GameState newState)
        {
            m_CurrentState = newState;

            switch (m_CurrentState)
            {
                case GameState.Game:
                    GameStart();
                    break;
            }
        }

        // Called by the menu, passing along the data from the selection made by the player in the menu
        public void StartGame(PlayerData[] playerData)
        {
            Debug.Log("[GameManager] StartGame 被调用");
            
            if (playerData == null || playerData.Length == 0)
            {
                Debug.LogError("[GameManager] 没有接收到坦克数据！");
                return;
            }
            
            Debug.Log($"[GameManager] 接收到 {playerData.Length} 个坦克的数据");
            
            m_TankData = playerData;
            m_PlayerCount = m_TankData.Length;
            
            Debug.Log($"[GameManager] 设置玩家数量为: {m_PlayerCount}");
            Debug.Log("[GameManager] 切换到游戏状态");
            
            ChangeGameState(GameState.Game);
        }


        private void SpawnAllTanks()
        {
            // For all the tanks...
            for (int i = 0; i < m_PlayerCount; i++)
            {
                var playerData = m_TankData[i];
                
                // ... create them, set their player number and references needed for control.
                m_SpawnPoints[i].m_Instance =
                    Instantiate(playerData.UsedPrefab, m_SpawnPoints[i].m_SpawnPoint.position, m_SpawnPoints[i].m_SpawnPoint.rotation) as GameObject;

                //this guard against possible user error : if they created a prefab with Is Computer Control set to true
                //then all of those prefab would be bots. So we ensure it's to false (the IsComputer from player data
                //will re-enable this if needed when the game start)
                var mov = m_SpawnPoints[i].m_Instance.GetComponent<TankMovement>();
                mov.m_IsComputerControlled = false;
                
                m_SpawnPoints[i].m_PlayerNumber = i + 1;
                m_SpawnPoints[i].ControlIndex = playerData.ControlIndex;
                m_SpawnPoints[i].m_PlayerColor = playerData.TankColor;
                m_SpawnPoints[i].m_ComputerControlled = playerData.IsComputer;
            }

            //we delayed setup after all tanks are created as they expect to have access to all other tanks in the manager
            foreach (var tank in m_SpawnPoints)
            {
                if(tank.m_Instance == null)
                    continue;
                
                tank.Setup(this);
            }
        }


        private void SetCameraTargets()
        {
            // Create a collection of transforms the same size as the number of tanks.
            Transform[] targets = new Transform[m_PlayerCount];

            // For each of these transforms...
            for (int i = 0; i < targets.Length; i++)
            {
                // ... set it to the appropriate tank transform.
                targets[i] = m_SpawnPoints[i].m_Instance.transform;
            }

            // These are the targets the camera should follow.
            m_CameraControl.m_Targets = targets;
        }


        // This is called from start and will run each phase of the game one after another.
        private IEnumerator GameLoop ()
        {
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundStarting ());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
            yield return StartCoroutine (RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found.
            if (m_GameWinner != null)
            {
                // If there is a game winner, restart the level.
                SceneManager.LoadScene (0);
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues.
                // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
                StartCoroutine (GameLoop ());
            }
        }


        private IEnumerator RoundStarting ()
        {
            // As soon as the round starts reset the tanks and make sure they can't move.
            ResetAllTanks ();
            DisableTankControl ();

            // Snap the camera's zoom and position to something appropriate for the reset tanks.
            m_CameraControl.SetStartPositionAndSize ();

            // Increment the round number and display text showing the players what round it is.
            m_RoundNumber++;
            m_TitleText.text = "ROUND " + m_RoundNumber;

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_StartWait;
        }


        private IEnumerator RoundPlaying ()
        {
            // As soon as the round begins playing let the players control the tanks.
            EnableTankControl ();

            // Clear the text from the screen.
            m_TitleText.text = string.Empty;

            // While there is not one tank left...
            while (!OneTankLeft())
            {
                // ... return on the next frame.
                yield return null;
            }
        }


        private IEnumerator RoundEnding ()
        {
            // Stop tanks from moving.
            DisableTankControl ();

            // Clear the winner from the previous round.
            m_RoundWinner = null;

            // See if there is a winner now the round is over.
            m_RoundWinner = GetRoundWinner ();

            // If there is a winner, increment their score.
            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            // Now the winner's score has been incremented, see if someone has one the game.
            m_GameWinner = GetGameWinner ();

            // Get a message based on the scores and whether or not there is a game winner and display it.
            string message = EndMessage ();
            m_TitleText.text = message;

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_EndWait;
        }


        // This is used to check if there is one or fewer tanks remaining and thus the round should end.
        private bool OneTankLeft()
        {
            // Start the count of tanks left at zero.
            int numTanksLeft = 0;

            // Go through all the tanks...
            for (int i = 0; i < m_PlayerCount; i++)
            {
                // ... and if they are active, increment the counter.
                if (m_SpawnPoints[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            // If there are one or fewer tanks remaining return true, otherwise return false.
            return numTanksLeft <= 1;
        }
        
        
        // This function is to find out if there is a winner of the round.
        // This function is called with the assumption that 1 or fewer tanks are currently active.
        private TankManager GetRoundWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_PlayerCount; i++)
            {
                // ... and if one of them is active, it is the winner so return it.
                if (m_SpawnPoints[i].m_Instance.activeSelf)
                    return m_SpawnPoints[i];
            }

            // If none of the tanks are active it is a draw so return null.
            return null;
        }


        // This function is to find out if there is a winner of the game.
        private TankManager GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_PlayerCount; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it.
                if (m_SpawnPoints[i].m_Wins == m_NumRoundsToWin)
                    return m_SpawnPoints[i];
            }

            // If no tanks have enough rounds to win, return null.
            return null;
        }


        // Returns a string message to display at the end of each round.
        private string EndMessage()
        {
            // By default when a round ends there are no winners so the default end message is a draw.
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that.
            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            // Add some line breaks after the initial message.
            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message.
            for (int i = 0; i < m_PlayerCount; i++)
            {
                message += m_SpawnPoints[i].m_ColoredPlayerText + ": " + m_SpawnPoints[i].m_Wins + " WINS\n";
            }

            // If there is a game winner, change the entire message to reflect that.
            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }


        // This function is used to turn all the tanks back on and reset their positions and properties.
        private void ResetAllTanks()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].Reset();
            }
        }


        private void EnableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].EnableControl();
            }
        }


        private void DisableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].DisableControl();
            }
        }
        
        /// <summary>
        /// 获取所有存活的玩家坦克（供外星飞船系统使用）
        /// </summary>
        /// <returns>存活的玩家坦克Transform列表</returns>
        public System.Collections.Generic.List<Transform> GetAliveTanks()
        {
            var aliveTanks = new System.Collections.Generic.List<Transform>();
            
            for (int i = 0; i < m_PlayerCount; i++)
            {
                // 检查坦克实例是否存在且激活
                if (m_SpawnPoints[i].m_Instance != null && m_SpawnPoints[i].m_Instance.activeInHierarchy)
                {
                    // 检查坦克是否还有生命值
                    var health = m_SpawnPoints[i].m_Instance.GetComponent<TankHealth>();
                    if (health != null && health.CurrentHealth > 0)
                    {
                        aliveTanks.Add(m_SpawnPoints[i].m_Instance.transform);
                    }
                }
            }
            
            return aliveTanks;
        }
        
        /// <summary>
        /// 检查是否所有玩家坦克都被摧毁（游戏结束条件）
        /// </summary>
        /// <returns>true表示所有玩家坦克都被摧毁</returns>
        public bool AllPlayerTanksDestroyed()
        {
            return GetAliveTanks().Count == 0;
        }

        private void InitializeBackgroundMusic()
        {
            // 优先使用AudioManager
            if (AudioManager.Instance != null)
            {
                Debug.Log("[GameManager] 使用AudioManager播放背景音乐");
                return; // AudioManager会自动处理背景音乐
            }
            
            // 如果没有AudioManager，使用本地音频系统
            Debug.Log("[GameManager] 未找到AudioManager，使用本地音频系统");
            
            // 如果没有指定AudioSource，自动创建一个
            if (m_BackgroundMusicAudio == null)
            {
                GameObject audioObj = new GameObject("BackgroundMusicAudio");
                audioObj.transform.SetParent(transform);
                m_BackgroundMusicAudio = audioObj.AddComponent<AudioSource>();
                
                // 设置音频源属性
                m_BackgroundMusicAudio.loop = true;
                m_BackgroundMusicAudio.playOnAwake = false;
                m_BackgroundMusicAudio.volume = m_BackgroundMusicVolume;
                
                Debug.Log("[GameManager] 已自动创建背景音乐AudioSource");
            }
            
            if (m_BackgroundMusicClip != null)
            {
                m_BackgroundMusicAudio.clip = m_BackgroundMusicClip;
                m_BackgroundMusicAudio.volume = m_BackgroundMusicVolume;
                m_BackgroundMusicAudio.loop = true;
                m_BackgroundMusicAudio.Play();
                
                Debug.Log("[GameManager] 背景音乐已开始播放");
            }
            else
            {
                Debug.LogWarning("[GameManager] 未分配背景音乐AudioClip，请在Inspector中设置m_BackgroundMusicClip");
            }
        }
        
        public void SetBackgroundMusicVolume(float volume)
        {
            m_BackgroundMusicVolume = Mathf.Clamp01(volume);
            if (m_BackgroundMusicAudio != null)
            {
                m_BackgroundMusicAudio.volume = m_BackgroundMusicVolume;
            }
        }
        
        public void PlayBackgroundMusic()
        {
            if (m_BackgroundMusicAudio != null && m_BackgroundMusicClip != null)
            {
                if (!m_BackgroundMusicAudio.isPlaying)
                {
                    m_BackgroundMusicAudio.Play();
                    Debug.Log("[GameManager] 背景音乐恢复播放");
                }
            }
        }
        
        public void StopBackgroundMusic()
        {
            if (m_BackgroundMusicAudio != null && m_BackgroundMusicAudio.isPlaying)
            {
                m_BackgroundMusicAudio.Stop();
                Debug.Log("[GameManager] 背景音乐已停止");
            }
        }
    }
}