using UnityEngine;

/// <summary>
/// 提供快速设置迷雾系统的工具方法
/// </summary>
public class FogOfWarSetup : MonoBehaviour
{
    [Header("迷雾设置")]
    public Transform playerTransform;
    public SpriteRenderer mapRenderer;  // 底图精灵渲染器
    public float visionRadius = 5f;
    public float fadeEdgeSize = 1f;
    public Color fogColor = new Color(0, 0, 0, 0.7f);

    public int fogSortingOrder = 1;
    
    [Header("初始化选项")]
    public bool setupOnStart = true;
    public bool debugMode = false;
    public int textureResolution = 1024;
    
    private FogOfWar fogOfWar;
    private SpriteRenderer fogLayerRenderer;
    
    private void Start()
    {
        if (setupOnStart)
        {
            SetupFogOfWar();
        }
    }
    
    /// <summary>
    /// 创建并设置完整的迷雾系统
    /// </summary>
    public void SetupFogOfWar()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("迷雾系统设置失败：引用缺失");
            return;
        }
        
        // 检查着色器是否存在
        if (Shader.Find("Custom/FogOfWarShader") == null)
        {
            Debug.LogError("找不到迷雾着色器! 请确保FogOfWarShader.shader文件存在且已编译。");
            return;
        }
        
        // 检查已存在的迷雾系统
        FogOfWar existingFogOfWar = FindObjectOfType<FogOfWar>();
        if (existingFogOfWar != null)
        {
            Debug.LogWarning("已存在迷雾系统，正在删除旧系统");
            Destroy(existingFogOfWar.gameObject);
        }
        
        // 检查已存在的迷雾层
        GameObject existingFogLayer = GameObject.Find("FogLayer");
        if (existingFogLayer != null)
        {
            Debug.LogWarning("已存在迷雾层，正在删除");
            Destroy(existingFogLayer);
        }
        
        // 创建迷雾层
        GameObject fogLayerObj = CreateFogLayer();
        
        // 创建并设置迷雾系统组件
        GameObject fogSystemObj = new GameObject("FogOfWarSystem");
        fogOfWar = fogSystemObj.AddComponent<FogOfWar>();
        
        // 设置引用
        fogOfWar.playerTransform = playerTransform;
        fogOfWar.fogLayer = fogLayerRenderer;
        
        // 设置参数
        fogOfWar.visionRadius = visionRadius;
        fogOfWar.fadeEdgeSize = fadeEdgeSize;
        fogOfWar.fogColor = fogColor;
        fogOfWar.debugMode = debugMode;
        fogOfWar.textureResolution = textureResolution;
        
        if (debugMode)
        {
            Debug.Log("迷雾系统设置完成, 使用着色器: Custom/FogOfWarShader" + 
                      "\n底图: " + mapRenderer.sprite.name + ", 尺寸: " + mapRenderer.sprite.rect.size + 
                      "\n位置: " + mapRenderer.transform.position +
                      "\n缩放: " + mapRenderer.transform.localScale);
        }
    }
    
    /// <summary>
    /// 验证所有必要的引用是否存在
    /// </summary>
    private bool ValidateReferences()
    {
        if (playerTransform == null)
        {
            Debug.LogError("玩家Transform引用缺失");
            return false;
        }
        
        if (mapRenderer == null)
        {
            Debug.LogError("地图精灵渲染器引用缺失");
            return false;
        }
        
        if (mapRenderer.sprite == null)
        {
            Debug.LogError("地图渲染器没有精灵");
            return false;
        }
        
        // 检查sprite的texture是否可读
        if (!mapRenderer.sprite.texture.isReadable)
        {
            Debug.LogWarning("地图精灵纹理不可读，这可能导致问题。请在纹理导入设置中启用'Read/Write Enabled'选项。");
        }
        
        return true;
    }
    
    /// <summary>
    /// 创建与地图精灵相同的迷雾层
    /// </summary>
    private GameObject CreateFogLayer()
    {
        // 创建迷雾层游戏对象
        GameObject fogLayerObj = new GameObject("FogLayer");
        
        // 添加精灵渲染器
        fogLayerRenderer = fogLayerObj.AddComponent<SpriteRenderer>();
        
        // 复制地图精灵设置
        fogLayerRenderer.sprite = mapRenderer.sprite;
        fogLayerRenderer.sortingOrder = fogSortingOrder; // 确保在所有节点和路径之上，但在玩家之下
        
        // 定位迷雾层与地图完全重叠
        fogLayerObj.transform.position = mapRenderer.transform.position;
        fogLayerObj.transform.rotation = mapRenderer.transform.rotation;
        fogLayerObj.transform.localScale = mapRenderer.transform.localScale;
        
        return fogLayerObj;
    }
    
    /// <summary>
    /// 提供在编辑器中测试迷雾系统的功能
    /// </summary>
    [ContextMenu("测试迷雾系统")]
    public void TestFogSystem()
    {
        SetupFogOfWar();
        
        if (fogOfWar != null)
        {
            Debug.Log("正在测试迷雾系统，在玩家位置清除迷雾");
            fogOfWar.ClearFogAtPosition(playerTransform.position);
        }
    }
    
    /// <summary>
    /// 重置迷雾系统
    /// </summary>
    [ContextMenu("重置迷雾")]
    public void ResetFogSystem()
    {
        FogOfWar existingFog = FindObjectOfType<FogOfWar>();
        if (existingFog != null)
        {
            Debug.Log("重置迷雾系统");
            existingFog.ResetFog();
        }
        else
        {
            Debug.LogWarning("没有找到迷雾系统，无法重置");
        }
    }
    
    /// <summary>
    /// 添加额外的测试功能，清除多个位置的迷雾
    /// </summary>
    [ContextMenu("创建测试迷雾模式")]
    public void CreateTestFogPattern()
    {
        FogOfWar existingFog = FindObjectOfType<FogOfWar>();
        if (existingFog == null)
        {
            SetupFogOfWar();
            existingFog = fogOfWar;
        }
        
        if (existingFog != null)
        {
            // 重置迷雾
            existingFog.ResetFog();
            
            // 在玩家位置清除迷雾
            existingFog.ClearFogAtPosition(playerTransform.position);
            
            // 在玩家周围多个位置清除迷雾
            float radius = visionRadius * 2;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector3 testPos = playerTransform.position + new Vector3(
                    radius * Mathf.Cos(angle),
                    radius * Mathf.Sin(angle),
                    0
                );
                existingFog.ClearFogAtPosition(testPos);
            }
            
            Debug.Log("创建了测试迷雾模式");
        }
    }
    
    /// <summary>
    /// 在编辑器中绘制视野范围视觉提示
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(playerTransform.position, visionRadius);
            
            Gizmos.color = new Color(0, 0.8f, 0, 0.15f);
            Gizmos.DrawSphere(playerTransform.position, visionRadius + fadeEdgeSize);
        }
    }
} 