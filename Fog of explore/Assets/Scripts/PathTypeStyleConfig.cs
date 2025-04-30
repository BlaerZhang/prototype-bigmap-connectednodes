using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PathTypeStyles", menuName = "Map/Path Type Style Config", order = 1)]
public class PathTypeStyleConfig : ScriptableObject
{
    [System.Serializable]
    public class PathTypeStyle
    {
        public Color color = Color.black;
        public float width = 0.1f;
        public Material material;
        public string displayName;
        
        public PathTypeStyle(Color color, float width, string displayName)
        {
            this.color = color;
            this.width = width;
            this.displayName = displayName;
        }
    }
    
    [System.Serializable]
    public class StyleMapping
    {
        public MapPath.RoadType roadType;
        public PathTypeStyle style;
    }
    
    [SerializeField]
    public List<StyleMapping> styleMap = new List<StyleMapping>();
    
    private Dictionary<MapPath.RoadType, PathTypeStyle> _styleCache;
    
    public PathTypeStyle GetStyleForRoadType(MapPath.RoadType roadType)
    {
        // 初始化或更新缓存
        if (_styleCache == null)
        {
            UpdateCache();
        }
        
        // 查找样式
        if (_styleCache.TryGetValue(roadType, out PathTypeStyle style))
        {
            return style;
        }
        
        // 如果找不到，返回默认样式
        return new PathTypeStyle(Color.black, 0.1f, roadType.ToString());
    }
    
    public void UpdateCache()
    {
        _styleCache = new Dictionary<MapPath.RoadType, PathTypeStyle>();
        foreach (var mapping in styleMap)
        {
            _styleCache[mapping.roadType] = mapping.style;
        }
    }
    
    // 初始化默认样式
    public void InitializeDefaultStyles()
    {
        styleMap.Clear();
        
        styleMap.Add(new StyleMapping { 
            roadType = MapPath.RoadType.Main, 
            style = new PathTypeStyle(Color.black, 0.15f, "主路") 
        });
        
        styleMap.Add(new StyleMapping { 
            roadType = MapPath.RoadType.Secondary, 
            style = new PathTypeStyle(Color.gray, 0.1f, "次要道路") 
        });
        
        styleMap.Add(new StyleMapping { 
            roadType = MapPath.RoadType.Trail, 
            style = new PathTypeStyle(new Color(0.7f, 0.5f, 0.2f), 0.08f, "小径") 
        });
        
        styleMap.Add(new StyleMapping { 
            roadType = MapPath.RoadType.Gravel, 
            style = new PathTypeStyle(new Color(0.5f, 0.5f, 1f), 0.12f, "碎石路") 
        });
        
        styleMap.Add(new StyleMapping { 
            roadType = MapPath.RoadType.Unpaved, 
            style = new PathTypeStyle(new Color(0.5f, 0.5f, 0.5f), 0.12f, "土路") 
        });
        
        UpdateCache();
    }
    
    private void OnEnable()
    {
        // 当资产被加载时更新缓存
        UpdateCache();
    }
}

#if UNITY_EDITOR
// 为ScriptableObject添加创建菜单项
public static class PathTypeStyleConfigUtility
{
    [UnityEditor.MenuItem("Tools/Map/Create Path Type Style Config")]
    public static void CreatePathTypeStyleConfig()
    {
        // 创建资产
        PathTypeStyleConfig config = ScriptableObject.CreateInstance<PathTypeStyleConfig>();
        
        // 初始化默认样式
        config.InitializeDefaultStyles();
        
        // 创建资产文件
        string path = "Assets/Resources/PathTypeStyles.asset";
        
        // 确保Resources文件夹存在
        if (!System.IO.Directory.Exists("Assets/Resources"))
        {
            System.IO.Directory.CreateDirectory("Assets/Resources");
        }
        
        // 检查文件是否已存在
        if (System.IO.File.Exists(path))
        {
            bool overwrite = UnityEditor.EditorUtility.DisplayDialog(
                "文件已存在", 
                "路径样式配置文件已存在，是否覆盖？", 
                "覆盖", 
                "取消");
                
            if (!overwrite)
            {
                return;
            }
        }
        
        // 创建资产文件
        UnityEditor.AssetDatabase.CreateAsset(config, path);
        UnityEditor.AssetDatabase.SaveAssets();
        
        // 在项目窗口中显示
        UnityEditor.EditorUtility.FocusProjectWindow();
        UnityEditor.Selection.activeObject = config;
        
        UnityEditor.EditorUtility.DisplayDialog("成功", $"路径样式配置已创建在: {path}", "确定");
    }
}
#endif 