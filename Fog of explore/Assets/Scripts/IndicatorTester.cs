using UnityEngine;
using HUDIndicator;
using System.Collections.Generic;

public class IndicatorTester : MonoBehaviour
{
    [Header("基本设置")]
    public IndicatorManager indicatorManager;
    public GameObject testObject;
    public IndicatorOffScreen indicatorPrefab;
    
    [Header("测试选项")]
    public bool useObjectSprite = true;
    public Sprite customTestSprite;
    public int testCount = 5;
    public float radius = 20f;
    
    [Header("指示器大小设置")]
    public float indicatorWidth = 32f;
    public float indicatorHeight = 32f;
    public bool maintainAspectRatio = true;
    [Range(0.1f, 3f)]
    public float sizeScale = 1f;
    
    private List<GameObject> testObjects = new List<GameObject>();
    
    void Start()
    {
        // 如果没有指定IndicatorManager，尝试查找
        if (indicatorManager == null)
        {
            indicatorManager = FindObjectOfType<IndicatorManager>();
            if (indicatorManager == null)
            {
                Debug.LogError("未找到IndicatorManager，请在场景中添加或者指定");
                return;
            }
        }
        
        // 检查indicatorPrefab
        if (indicatorPrefab == null)
        {
            Debug.LogError("未指定indicatorPrefab");
            return;
        }
        
        // 确保指示器预制体设置了必要的样式组件
        if (indicatorPrefab.style == null)
        {
            indicatorPrefab.style = new IndicatorIconStyle();
        }
        
        if (indicatorPrefab.arrowStyle == null)
        {
            indicatorPrefab.arrowStyle = new IndicatorArrowStyle();
        }
        
        // 创建测试对象
        CreateTestObjects();
    }
    
    [ContextMenu("创建测试对象")]
    public void CreateTestObjects()
    {
        // 清理已有的测试对象
        foreach (var obj in testObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        testObjects.Clear();
        
        // 如果未指定测试对象，创建一个简单的
        GameObject prefabToUse = testObject;
        if (prefabToUse == null)
        {
            prefabToUse = new GameObject("TestSprite");
            var spriteRenderer = prefabToUse.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = customTestSprite;
        }
        
        // 创建测试对象
        for (int i = 0; i < testCount; i++)
        {
            // 生成随机位置
            Vector2 randomPos = Random.insideUnitCircle * radius;
            
            // 实例化对象
            GameObject instance = Instantiate(prefabToUse, new Vector3(randomPos.x, randomPos.y, 0), Quaternion.identity);
            instance.name = $"TestObject_{i}";
            
            // 添加指示器组件
            RandomizedSpriteIndicator indicator = instance.AddComponent<RandomizedSpriteIndicator>();
            indicator.indicatorPrefab = indicatorPrefab;
            indicator.showIndicator = true;
            indicator.useCustomSprite = !useObjectSprite;
            
            // 设置指示器大小
            indicator.indicatorWidth = indicatorWidth;
            indicator.indicatorHeight = indicatorHeight;
            indicator.maintainAspectRatio = maintainAspectRatio;
            indicator.sizeScale = sizeScale;
            
            if (!useObjectSprite && customTestSprite != null)
            {
                indicator.customSprite = customTestSprite;
            }
            
            // 手动调用SetupIndicator
            indicator.SetupIndicator();
            
            // 添加到列表
            testObjects.Add(instance);
        }
        
        Debug.Log($"已创建 {testCount} 个测试对象");
    }
    
    void OnDrawGizmos()
    {
        // 显示生成范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
} 