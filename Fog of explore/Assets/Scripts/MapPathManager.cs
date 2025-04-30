using UnityEngine;
using System.Collections.Generic;

public class MapPathManager : MonoBehaviour
{
    // 单例实例
    public static MapPathManager Instance { get; private set; }
    
    // 样式配置
    [SerializeField]
    private PathTypeStyleConfig styleConfig;
    
    // 配置资源路径
    private const string CONFIG_RESOURCE_PATH = "PathTypeStyles";
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 加载配置
            LoadStyleConfig();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 从Resources加载配置
    private void LoadStyleConfig()
    {
        if (styleConfig == null)
        {
            styleConfig = Resources.Load<PathTypeStyleConfig>(CONFIG_RESOURCE_PATH);
            
            if (styleConfig == null)
            {
                Debug.LogWarning("找不到路径样式配置资源，将使用默认样式。请在Resources文件夹中创建PathTypeStyles资产。");
                
                // 创建一个临时配置
                styleConfig = ScriptableObject.CreateInstance<PathTypeStyleConfig>();
                styleConfig.InitializeDefaultStyles();
            }
        }
    }
    
    // 应用样式到路径
    public void ApplyStylesToAllPaths()
    {
        // 查找所有路径并应用样式
        MapPath[] allPaths = FindObjectsOfType<MapPath>();
        foreach (MapPath path in allPaths)
        {
            ApplyStyleToPath(path);
        }
    }
    
    // 应用样式到单个路径
    public void ApplyStyleToPath(MapPath path)
    {
        if (path == null) return;
        
        // 确保配置已加载
        if (styleConfig == null)
        {
            LoadStyleConfig();
        }
        
        MapPath.RoadType roadType = path.roadType;
        PathTypeStyleConfig.PathTypeStyle style = styleConfig.GetStyleForRoadType(roadType);
        
        path.ApplyRoadTypeStyle(style.color, style.width, style.material);
    }
    
    // 返回指定路径类型的样式
    public PathTypeStyleConfig.PathTypeStyle GetStyleForRoadType(MapPath.RoadType roadType)
    {
        // 确保配置已加载
        if (styleConfig == null)
        {
            LoadStyleConfig();
        }
        
        return styleConfig.GetStyleForRoadType(roadType);
    }
} 