using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    [Header("引用设置")]
    public Transform playerTransform;  // 玩家位置引用
    public SpriteRenderer fogLayer;    // 迷雾层精灵渲染器

    [Header("视野设置")]
    public float visionRadius = 5f;     // 视野半径
    public float fadeEdgeSize = 1f;     // 视野边缘渐变大小
    public Color fogColor = new Color(0, 0, 0, 0.7f); // 迷雾颜色和透明度
    
    [Header("纹理设置")]
    public int textureResolution = 1024; // 纹理分辨率
    
    // 私有变量
    private Texture2D maskTexture;      // 迷雾蒙版纹理
    private Vector2 textureCenter;      // 纹理中心点
    private float textureToWorldRatio;  // 纹理到世界坐标的比例
    private Vector2 lastPlayerPos;      // 上一次玩家的位置
    private Material fogMaterial;       // 迷雾材质
    private Vector2 spriteScale;        // 精灵的实际缩放比例
    private Vector2 aspectRatio;        // 纹理的宽高比
    
    // 调试选项
    [Header("调试")]
    public bool debugMode = false;
    
    private void Start()
    {
        InitializeFogSystem();
    }
    
    private void Update()
    {
        // 如果玩家位置发生变化，标记需要更新
        if (playerTransform != null && Vector2.Distance((Vector2)playerTransform.position, lastPlayerPos) > 0.1f)
        {
            UpdateFogOfWar();
        }
    }

    public void UpdateFogOfWar()
    {
        if (debugMode)
        {
            Debug.Log("检测到玩家移动，正在清除位置: " + playerTransform.position);
        }

        ClearFogAtPosition(playerTransform.position);
        lastPlayerPos = playerTransform.position;
    }

    // 初始化迷雾系统

    private void InitializeFogSystem()
    {
        if (fogLayer == null)
        {
            Debug.LogError("没有指定迷雾层!");
            return;
        }
        
        // 获取精灵大小和比例
        Sprite fogSprite = fogLayer.sprite;
        if (fogSprite == null)
        {
            Debug.LogError("迷雾层没有精灵!");
            return;
        }

        // 计算精灵的实际尺寸（考虑缩放）
        spriteScale = new Vector2(
            fogLayer.transform.lossyScale.x * fogSprite.rect.width / fogSprite.pixelsPerUnit,
            fogLayer.transform.lossyScale.y * fogSprite.rect.height / fogSprite.pixelsPerUnit
        );
        
        // 计算像素纵横比 - 保持不变，这是用于坐标转换的
        aspectRatio = new Vector2(
            fogSprite.rect.width / fogSprite.rect.height,
            1.0f
        );

        if (debugMode)
        {
            Debug.Log($"精灵尺寸: {fogSprite.rect.width}x{fogSprite.rect.height}, 像素比: {aspectRatio.x}:1");
            Debug.Log($"世界尺寸: {spriteScale}");
        }
        
        // 创建自定义材质，使用我们的迷雾着色器
        Shader fogShader = Shader.Find("Custom/FogOfWarShader");
        if (fogShader == null)
        {
            Debug.LogError("找不到迷雾着色器! 请确保着色器文件名正确并且已编译。");
            fogShader = Shader.Find("Sprites/Default");
        }
        
        fogMaterial = new Material(fogShader);
        fogLayer.material = fogMaterial;
        
        // 创建与精灵纵横比相匹配的纹理
        int texWidth, texHeight;
        if (fogSprite.rect.width >= fogSprite.rect.height)
        {
            texWidth = textureResolution;
            texHeight = Mathf.RoundToInt(textureResolution / aspectRatio.x);
        }
        else
        {
            texHeight = textureResolution;
            texWidth = Mathf.RoundToInt(textureResolution * aspectRatio.x);
        }
        
        maskTexture = new Texture2D(texWidth, texHeight, TextureFormat.R8, false);
        maskTexture.filterMode = FilterMode.Bilinear;
        
        // 用黑色填充纹理 (全部是迷雾)
        Color[] colors = new Color[texWidth * texHeight];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.black;
        }
        maskTexture.SetPixels(colors);
        maskTexture.Apply();
        
        // 设置纹理和颜色到材质
        fogMaterial.mainTexture = fogSprite.texture;
        fogMaterial.SetTexture("_MaskTex", maskTexture);
        fogMaterial.SetColor("_FogColor", fogColor);
        
        // 计算纹理中心和比例
        CalculateTextureParameters();
        
        if (debugMode)
        {
            Debug.Log($"迷雾系统初始化完成，纹理分辨率: {texWidth}x{texHeight}");
        }
        
        // 如果有玩家位置，清除其周围的迷雾
        if (playerTransform != null)
        {
            ClearFogAtPosition(playerTransform.position);
            lastPlayerPos = playerTransform.position;
        }
    }
    
    // 计算纹理参数
    private void CalculateTextureParameters()
    {
        if (fogLayer == null || fogLayer.sprite == null || maskTexture == null) return;
        
        // 计算纹理中心
        textureCenter = new Vector2(maskTexture.width / 2f, maskTexture.height / 2f);
        
        // 计算世界单位到纹理像素的转换比例
        Bounds spriteBounds = fogLayer.sprite.bounds;
        Vector2 worldSize = new Vector2(
            spriteBounds.size.x * fogLayer.transform.lossyScale.x,
            spriteBounds.size.y * fogLayer.transform.lossyScale.y
        );
        
        // 使用较小的比例确保圆形不会被截断
        float xRatio = maskTexture.width / worldSize.x;
        float yRatio = maskTexture.height / worldSize.y;
        textureToWorldRatio = Mathf.Min(xRatio, yRatio);
        
        if (debugMode)
        {
            Debug.Log($"世界大小: {worldSize}, 转换比例: {textureToWorldRatio}");
        }
    }
    
    // 将世界坐标转换为纹理坐标
    private Vector2Int WorldToTexturePosition(Vector3 worldPos)
    {
        if (fogLayer == null || maskTexture == null) return Vector2Int.zero;
        
        // 转换到相对于迷雾层的局部坐标
        Vector3 localPos = fogLayer.transform.InverseTransformPoint(worldPos);
        
        // 获取精灵边界
        Bounds spriteBounds = fogLayer.sprite.bounds;
        
        // 计算在精灵内的归一化位置 (-0.5 到 0.5)
        Vector2 normalizedInBounds = new Vector2(
            localPos.x / (spriteBounds.size.x * fogLayer.transform.localScale.x),
            localPos.y / (spriteBounds.size.y * fogLayer.transform.localScale.y)
        );
        
        // 转换到纹理坐标 (0 到 width/height)
        int texX = Mathf.RoundToInt((normalizedInBounds.x + 0.5f) * maskTexture.width);
        int texY = Mathf.RoundToInt((normalizedInBounds.y + 0.5f) * maskTexture.height);
        
        // 限制在纹理范围内
        texX = Mathf.Clamp(texX, 0, maskTexture.width - 1);
        texY = Mathf.Clamp(texY, 0, maskTexture.height - 1);
        
        if (debugMode && Time.frameCount % 60 == 0)
        {
            Debug.Log($"世界位置 {worldPos} -> 局部位置 {localPos} -> 归一化 {normalizedInBounds} -> 纹理坐标 {texX},{texY}");
        }
        
        return new Vector2Int(texX, texY);
    }
    
    // 在指定位置清除迷雾
    public void ClearFogAtPosition(Vector3 worldPosition)
    {
        if (maskTexture == null) 
        {
            if (debugMode) Debug.LogError("迷雾蒙版纹理为空!");
            return;
        }
        
        // 转换世界坐标到纹理坐标
        Vector2Int texturePos = WorldToTexturePosition(worldPosition);
        
        // 计算视野半径在纹理上的像素数
        // 注意：为确保在任何纵横比下都是圆形，我们使用相同的半径值
        float pixelRadius = visionRadius * textureToWorldRatio;
        float fadeEdgePixels = fadeEdgeSize * textureToWorldRatio;
        
        if (debugMode)
        {
            Debug.Log($"清除迷雾位置: {texturePos}, 像素半径: {pixelRadius}");
        }
        
        // 计算视野圆形的边界框
        int startX = Mathf.Max(0, Mathf.RoundToInt(texturePos.x - pixelRadius - fadeEdgePixels));
        int endX = Mathf.Min(maskTexture.width - 1, Mathf.RoundToInt(texturePos.x + pixelRadius + fadeEdgePixels));
        int startY = Mathf.Max(0, Mathf.RoundToInt(texturePos.y - pixelRadius - fadeEdgePixels));
        int endY = Mathf.Min(maskTexture.height - 1, Mathf.RoundToInt(texturePos.y + pixelRadius + fadeEdgePixels));
        
        // 安全检查
        if (startX >= endX || startY >= endY)
        {
            if (debugMode) Debug.LogError("无效的纹理边界框!");
            return;
        }
        
        // 获取区域内的像素
        Color[] pixels = maskTexture.GetPixels(startX, startY, endX - startX, endY - startY);
        bool changed = false;
        
        // 遍历边界框内的所有像素
        for (int y = 0; y < endY - startY; y++)
        {
            for (int x = 0; x < endX - startX; x++)
            {
                // 计算当前像素到中心的距离
                float dx = x + startX - texturePos.x;
                float dy = y + startY - texturePos.y;
                
                // 计算欧几里得距离（保持圆形）
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                // 如果在视野范围内
                if (dist <= pixelRadius + fadeEdgePixels)
                {
                    float alpha = pixels[y * (endX - startX) + x].r;
                    float newAlpha;
                    
                    if (dist < pixelRadius)
                    {
                        // 完全清除迷雾
                        newAlpha = 1.0f;
                    }
                    else
                    {
                        // 在边缘创建渐变效果
                        float t = 1.0f - (dist - pixelRadius) / fadeEdgePixels;
                        newAlpha = Mathf.Lerp(alpha, 1.0f, t);
                    }
                    
                    if (newAlpha > alpha)
                    {
                        pixels[y * (endX - startX) + x] = new Color(newAlpha, newAlpha, newAlpha, 1);
                        changed = true;
                    }
                }
            }
        }
        
        // 如果有像素被修改，更新纹理
        if (changed)
        {
            maskTexture.SetPixels(startX, startY, endX - startX, endY - startY, pixels);
            maskTexture.Apply();
        }
    }
    
    // 重置迷雾（全部覆盖）
    public void ResetFog()
    {
        if (maskTexture == null) return;
        
        // 重新填充纹理为黑色（全部是迷雾）
        Color[] colors = new Color[maskTexture.width * maskTexture.height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.black;
        }
        maskTexture.SetPixels(colors);
        maskTexture.Apply();
        
        if (debugMode)
        {
            Debug.Log("迷雾已重置");
        }
    }
    
    // 清理资源
    private void OnDestroy()
    {
        if (maskTexture != null)
        {
            Destroy(maskTexture);
        }
        
        if (fogMaterial != null)
        {
            Destroy(fogMaterial);
        }
    }
} 