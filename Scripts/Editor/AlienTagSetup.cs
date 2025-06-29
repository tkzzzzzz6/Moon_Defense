#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Tanks.Complete
{
    /// <summary>
    /// 自动设置外星人系统所需的标签和层级
    /// </summary>
    [InitializeOnLoad]
    public static class AlienTagSetup
    {
        static AlienTagSetup()
        {
            EditorApplication.delayCall += SetupAlienTags;
        }
        
        [MenuItem("Tanks/Setup Alien Tags", priority = 3)]
        public static void SetupAlienTags()
        {
            // 添加Alien标签
            AddTag("Alien");
            
            Debug.Log("[AlienTagSetup] 外星人标签设置完成");
        }
        
        private static void AddTag(string tagName)
        {
            // 获取TagManager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            // 检查标签是否已存在
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
                if (tag.stringValue == tagName)
                {
                    Debug.Log($"[AlienTagSetup] 标签 '{tagName}' 已存在");
                    return;
                }
            }
            
            // 添加新标签
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = tagName;
            
            tagManager.ApplyModifiedProperties();
            
            Debug.Log($"[AlienTagSetup] 已添加标签: {tagName}");
        }
    }
}
#endif 