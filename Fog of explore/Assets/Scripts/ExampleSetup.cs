using UnityEngine;
using HUDIndicator;
using TMPro;

/// <summary>
/// 示例场景设置脚本，用于演示如何设置和使用随机化Sprite指示器系统
/// </summary>
public class ExampleSetup : MonoBehaviour
{
    [Header("预制体设置")]
    public GameObject playerPrefab;
    public GameObject spritePrefab;
    public IndicatorOffScreen indicatorPrefab;
    
    [Header("场景生成设置")]
    public int spritesToGenerate = 10;
    public float spawnRadius = 20f;
    public TMP_Text instructionsText;
    
    private GameObject player;
    private IndicatorManager indicatorManager;
    
    void Start()
    {
        SetupPlayer();
        SetupIndicatorManager();
        GenerateRandomSprites();
        SetupInstructions();
    }
    
    void SetupPlayer()
    {
        // 创建玩家
        if (playerPrefab == null)
        {
            // 如果没有预制体，创建一个简单的玩家对象
            player = new GameObject("Player");
            var spriteRenderer = player.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.blue;
            
            // 添加一些控制脚本
            var controller = player.AddComponent<SimplePlayerController>();
        }
        else
        {
            player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        }
    }
    
    void SetupIndicatorManager()
    {
        // 创建指示器管理器
        GameObject indicatorObj = new GameObject("IndicatorManager");
        indicatorManager = indicatorObj.AddComponent<IndicatorManager>();
        indicatorManager.player = player.transform;
        indicatorManager.defaultIndicatorPrefab = indicatorPrefab;
        indicatorManager.displayDistance = spawnRadius * 0.5f; // 半径的一半为默认显示距离
    }
    
    void GenerateRandomSprites()
    {
        // 创建一个包含随机精灵的父对象
        GameObject spriteContainer = new GameObject("RandomSprites");
        
        // 生成随机精灵
        for (int i = 0; i < spritesToGenerate; i++)
        {
            // 计算随机位置
            Vector2 randomPosition = Random.insideUnitCircle * spawnRadius;
            GameObject spriteObj;
            
            if (spritePrefab != null)
            {
                spriteObj = Instantiate(spritePrefab, randomPosition, Quaternion.identity, spriteContainer.transform);
            }
            else
            {
                // 创建一个简单的精灵对象
                spriteObj = new GameObject($"Sprite_{i}");
                spriteObj.transform.SetParent(spriteContainer.transform);
                spriteObj.transform.position = randomPosition;
                
                var renderer = spriteObj.AddComponent<SpriteRenderer>();
                renderer.color = new Color(Random.value, Random.value, Random.value);
            }
            
            // 添加RandomizedSpriteIndicator组件
            var indicator = spriteObj.AddComponent<RandomizedSpriteIndicator>();
            indicator.showIndicator = true;
            indicator.indicatorPrefab = indicatorPrefab;
            indicator.minDistance = spawnRadius * 0.25f; // 从1/4半径开始显示
            indicator.maxDistance = spawnRadius * 2f;    // 到2倍半径停止显示
            
            // 手动设置指示器
            indicator.SetupIndicator();
        }
    }
    
    void SetupInstructions()
    {
        if (instructionsText != null)
        {
            instructionsText.text = "使用WASD键移动玩家\n在远离随机精灵一定距离时，将显示屏幕外指示器";
        }
    }
}

/// <summary>
/// 简单的玩家控制器，用于演示目的
/// </summary>
public class SimplePlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }
} 