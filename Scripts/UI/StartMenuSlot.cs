using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace Tanks.Complete
{
    // 处理主菜单中的"槽位"类，这是一个显示坦克预览、显示其状态并允许
    // 将该坦克添加到游戏中或更改其控制者（玩家1、玩家2或电脑）的条目
    public class StartMenuSlot : MonoBehaviour
    {
        public Color m_SlotColor;                       // 该槽位中坦克将使用的颜色
        
        [Header("References")]
        public RectTransform m_TankPreviewPosition;     // 用于放置坦克预览的Transform，使其在屏幕上正确显示
        public TextMeshProUGUI m_TankStats;             // 用于显示坦克状态的文本
        public Button m_AddControlButton;               // 点击时将该坦克添加到当前游戏的按钮

        public RectTransform m_ControlChoiceRoot;       // 所有控制选择按钮的父级根节点
        public Button m_P1ControlButton;                // 使该坦克由玩家1控制的按钮
        public Button m_P2ControlButton;                // 使该坦克由玩家2控制的按钮
        public Button m_ComputerControlButton;          // 使该坦克由电脑控制的按钮
        public Button m_OffControlButton;               // 从当前使用的坦克中移除该坦克的按钮

        public Image BackgroundImage;                   // 整个槽位的背景图像
        public Sprite OpenSlotBackground;               // 槽位开放时使用的精灵图（尚未被任何人使用）
        public Sprite UsedSlotBackground;               // 槽位被使用时使用的精灵图（由玩家1/2或电脑控制）

        public GameObject TankPreview { get; set; }         // 在菜单中旋转显示该坦克的预览实例
        public GameObject TankPrefab { get; private set; }  // 该槽位基于的预制体
        public int PlayerControlling { get; set; }          // 控制该坦克的玩家，1或2（电脑为-1）
        public bool IsOpen { get; set; }                    // 槽位是否开放（尚未加入游戏）或已使用（已分配给玩家1/2或电脑）
        public bool IsComputer { get; set; }                // 该槽位是否被电脑控制的坦克使用
        
        private Camera m_MenuCamera;                        // 用于显示菜单的相机

        // 在MonoBehaviour创建后第一次执行Update之前调用一次
        void Awake()
        {
            m_MenuCamera = GetComponentInParent<Camera>();
            IsOpen = true;

            //在移动平台上只能使用一个玩家，所以我们禁用了玩家2
            if (Application.isMobilePlatform)
            {
                m_P2ControlButton.gameObject.SetActive(false);
            }

            BackgroundImage.sprite = OpenSlotBackground;
        }
        
        private void Update()
        {
            // 如果有预览，则缓慢旋转它
            if (TankPreview != null)
            {
                TankPreview.transform.Rotate(Vector3.up, 45.0f * Time.deltaTime);
            }
        }

        public void AddTank()
        {
            m_AddControlButton.gameObject.SetActive(false);
            m_ControlChoiceRoot.gameObject.SetActive(true);

            IsOpen = false;
            BackgroundImage.sprite = UsedSlotBackground;
        }

        public void RemoveTank()
        {
            m_AddControlButton.gameObject.SetActive(true);
            m_ControlChoiceRoot.gameObject.SetActive(false);

            SetPlayerControlling(-1);

            IsOpen = true;
            BackgroundImage.sprite = OpenSlotBackground;
        }

        public void SetPlayerControlling(int playerNumber)
        {
            //重新启用当前控制器的按钮，因为我们现在可以重新选择它
            if (PlayerControlling == 1)
                m_P1ControlButton.interactable = true;
            else if (PlayerControlling == 2)
                m_P2ControlButton.interactable = true;
            else if (PlayerControlling == -1)
                m_ComputerControlButton.interactable = true;
            
            // 更改控制器
            PlayerControlling = playerNumber;
            
            // 然后禁用相关按钮并设置是否为电脑控制
            switch(playerNumber)
            {
                case 1:
                    m_P1ControlButton.interactable = false;
                    IsComputer = false;
                    break;
                case 2:
                    m_P2ControlButton.interactable = false;
                    IsComputer = false;
                    break;
                case -1:
                    m_ComputerControlButton.interactable = false;
                    IsComputer = true;
                    break;
            }
        }

        public void SetTankPreview(GameObject prefab)
        {
            // 如果已经有坦克预览，则销毁它
            if (TankPreview != null)
            {
                Destroy(TankPreview);
            }

            //分配正确的预制体
            TankPrefab = prefab;
            //然后将其实例化为预览
            TankPreview = Instantiate(prefab);
            
            // 获取所有组件的引用
            var move = TankPreview.GetComponent<TankMovement> ();
            var shoot = TankPreview.GetComponent<TankShooting> ();
            var health = TankPreview.GetComponent<TankHealth>();

            // 禁用它们，因为这只是视觉预览，不需要响应用户输入等游戏玩法
            move.enabled = false;
            shoot.enabled = false;

            // 用这个坦克的状态更新坦克状态文本
            m_TankStats.text = $"Speed {move.m_Speed}\nDamage {shoot.m_MaxDamage}\nHealth: {health.m_StartingHealth}";
            
            //将其移动到正确的预览位置，使其在屏幕上正确显示
            var position = m_MenuCamera.WorldToScreenPoint(m_TankPreviewPosition.position);
            TankPreview.transform.position =
                m_MenuCamera.ScreenToWorldPoint(position) + Vector3.back * 3.0f;
            
            // 遍历该坦克的所有渲染器
            MeshRenderer[] renderers = TankPreview.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                for (int j = 0; j < renderer.materials.Length; ++j)
                {
                    // 然后当我们找到TankColor材质时
                    if (renderer.materials[j].name.Contains("TankColor"))
                    {
                        // 将其颜色设置为槽位颜色
                        renderer.materials[j].color = m_SlotColor;
                    }
                }
            }
            
            //禁用所有音频
            var audioSource = TankPreview.GetComponentsInChildren<AudioSource>();
            foreach (var source in audioSource)
            {
                Destroy(source);
            }
        }
    }
}