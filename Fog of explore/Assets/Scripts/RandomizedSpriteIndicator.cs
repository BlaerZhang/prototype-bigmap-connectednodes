using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HUDIndicator;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
public class RandomizedSpriteIndicator : MonoBehaviour
{
    [Header("指示器设置")]
    public bool showIndicator = true;
    public IndicatorOffScreen indicatorPrefab;
    private IndicatorOffScreen myIndicator;

    [Header("指示器图像设置")]
    public bool useCustomSprite = false;
    public Sprite customSprite;
    
    [Tooltip("如果未指定自定义纹理，将从Sprite自动生成")]
    public Texture2D customTexture;
    
    [Header("指示器大小设置")]
    [Tooltip("指示器图标的宽度")]
    public float indicatorWidth = 32f;
    [Tooltip("指示器图标的高度")]
    public float indicatorHeight = 32f;
    [Tooltip("是否保持原始图像比例")]
    public bool maintainAspectRatio = true;
    [Range(0.1f, 3f)]
    [Tooltip("整体缩放比例")]
    public float sizeScale = 1f;

    [Header("显示条件")]
    public bool autoRegisterToManager = true;
    public float minDistance = 0f; // 最小显示距离
    public float maxDistance = Mathf.Infinity; // 最大显示距离

    private SpriteRenderer spriteRenderer;
    private IndicatorManager indicatorManager;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (autoRegisterToManager)
        {
            indicatorManager = FindObjectOfType<IndicatorManager>();
            if (indicatorManager != null)
            {
                SetupIndicator();
            }
            else
            {
                Debug.LogWarning("没有找到IndicatorManager，无法自动注册指示器");
            }
        }
    }

    public void SetupIndicator()
    {
        if (!showIndicator || indicatorPrefab == null) return;

        // 创建指示器
        myIndicator = Instantiate(indicatorPrefab, transform.position, Quaternion.identity);
        myIndicator.transform.SetParent(transform);
        myIndicator.transform.localPosition = Vector3.zero;
        
        // 默认关闭，由IndicatorManager控制显示
        myIndicator.enabled = false;

        // 设置指示器的图像
        SetIndicatorSprite();

        // 添加到管理器
        if (indicatorManager != null && !indicatorManager.indicators.Contains(myIndicator))
        {
            indicatorManager.indicators.Add(myIndicator);
        }
    }

    private void SetIndicatorSprite()
    {
        if (myIndicator == null) return;

        // 获取要使用的Sprite
        Sprite spriteToUse = useCustomSprite && customSprite != null ? 
                              customSprite : 
                              spriteRenderer.sprite;
        
        if (spriteToUse == null)
        {
            Debug.LogWarning("RandomizedSpriteIndicator: No sprite available for indicator", this);
            return;
        }

        // 获取或创建Texture
        Texture2D textureToUse = null;
        
        if (customTexture != null)
        {
            // 使用自定义纹理
            textureToUse = customTexture;
        }
        else
        {
            // 从Sprite创建Texture
            textureToUse = SpriteToTexture(spriteToUse);
        }
        
        if (textureToUse != null)
        {
            // 设置指示器样式
            if (myIndicator.style == null)
            {
                myIndicator.style = new HUDIndicator.IndicatorIconStyle();
            }
            
            myIndicator.style.texture = textureToUse;
            myIndicator.style.color = Color.white;
            
            // 计算指示器大小
            float width, height;
            
            if (maintainAspectRatio)
            {
                // 保持原始宽高比
                float aspectRatio = (float)textureToUse.width / textureToUse.height;
                
                if (indicatorWidth > 0 && indicatorHeight <= 0)
                {
                    // 只设置了宽度，根据比例计算高度
                    width = indicatorWidth * sizeScale;
                    height = width / aspectRatio;
                }
                else if (indicatorWidth <= 0 && indicatorHeight > 0)
                {
                    // 只设置了高度，根据比例计算宽度
                    height = indicatorHeight * sizeScale;
                    width = height * aspectRatio;
                }
                else if (indicatorWidth > 0 && indicatorHeight > 0)
                {
                    // 同时设置了宽度和高度，但保持比例，以较小的缩放比例为准
                    float scaleWidth = indicatorWidth / (textureToUse.width / sizeScale);
                    float scaleHeight = indicatorHeight / (textureToUse.height / sizeScale);
                    float scale = Mathf.Min(scaleWidth, scaleHeight);
                    
                    width = textureToUse.width * scale;
                    height = textureToUse.height * scale;
                }
                else
                {
                    // 都没设置，使用原始尺寸乘以缩放比例
                    width = textureToUse.width * sizeScale;
                    height = textureToUse.height * sizeScale;
                }
            }
            else
            {
                // 不保持比例，直接使用设置的宽高
                width = (indicatorWidth > 0 ? indicatorWidth : textureToUse.width) * sizeScale;
                height = (indicatorHeight > 0 ? indicatorHeight : textureToUse.height) * sizeScale;
            }
            
            // 应用指示器大小
            myIndicator.style.width = width;
            myIndicator.style.height = height;
            
            // 确保箭头样式也存在
            if (myIndicator.arrowStyle == null)
            {
                myIndicator.arrowStyle = new HUDIndicator.IndicatorArrowStyle();
            }
        }
        else
        {
            Debug.LogError("RandomizedSpriteIndicator: Failed to create texture for indicator", this);
        }
    }

    private Texture2D SpriteToTexture(Sprite sprite)
    {
        if (sprite == null)
            return null;

        try
        {
            // 从Sprite获取像素数据
            Texture2D origTex = sprite.texture;
            
            // 如果已经是Texture2D并且完整包含Sprite，直接返回
            if (sprite.rect.width == origTex.width && sprite.rect.height == origTex.height)
            {
                return origTex;
            }
            
            // 创建新纹理
            Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
            
            // 复制Sprite区域的像素
            Color[] pixels = origTex.GetPixels(
                (int)sprite.rect.x, 
                (int)sprite.rect.y, 
                (int)sprite.rect.width, 
                (int)sprite.rect.height);
                
            newText.SetPixels(pixels);
            newText.Apply();
            
            return newText;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error converting sprite to texture: {e.Message}");
            return null;
        }
    }

    private void OnDestroy()
    {
        // 从管理器中移除
        if (indicatorManager != null && myIndicator != null)
        {
            indicatorManager.indicators.Remove(myIndicator);
        }
    }
} 