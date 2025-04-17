using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 基于泊松分布的随机暗雷触发系统
/// 此系统完全独立于其他游戏系统，仅通过观察玩家位置变化来触发事件
/// </summary>
public class RandomEncounterSystem : MonoBehaviour
{
    [Header("暗雷设置")]
    [Tooltip("平均每移动多少单位距离触发一次暗雷（泊松分布的λ参数）")]
    public float averageDistancePerEncounter = 10f;
    
    [Tooltip("触发暗雷的最小移动距离")]
    public float minimumDistanceForEncounter = 2f;
    
    [Tooltip("暗雷触发几率随时间增加的系数，设为0表示不随时间增加")]
    [Range(0f, 1f)]
    public float timeFactorCoefficient = 0.1f;
    
    [Header("UI设置")]
    [Tooltip("暗雷触发提示UI面板")]
    public GameObject encounterUIPanel;
    
    [Tooltip("提示文本组件")]
    public TMP_Text encounterMessageText;
    
    [Header("调试设置")]
    [Tooltip("启用调试模式")]
    public bool debugMode = false;
    
    // 跟踪玩家
    private Transform playerTransform;
    private Vector3 lastPlayerPosition;
    
    // 暗雷触发状态
    private float accumulatedDistance = 0f;
    private float timeSinceLastEncounter = 0f;
    private System.Random randomGenerator;
    
    // 暗雷提示消息
    private string[] encounterMessages = new string[] {
        "Suprise!",
        "An Lei!",
        "Run",
        "It's me again",
        "Roll the dice",
    };
    
    private void Start()
    {
        // 查找玩家
        playerTransform = FindObjectOfType<PlayerMovement>()?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("未找到玩家对象！RandomEncounterSystem需要场景中有PlayerMovement组件。");
            enabled = false;
            return;
        }
        
        // 初始化位置记录
        lastPlayerPosition = playerTransform.position;
        
        // 初始化随机数生成器
        randomGenerator = new System.Random();
        
        // 确保UI面板初始状态为隐藏
        if (encounterUIPanel != null)
        {
            encounterUIPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("未设置暗雷触发UI面板！请在Inspector中设置。");
        }
        
        if (debugMode)
        {
            Debug.Log("随机暗雷系统已初始化，平均每" + averageDistancePerEncounter + "单位距离触发一次。");
        }
    }
    
    private void Update()
    {
        if (playerTransform == null) return;
        
        // 计算玩家移动距离
        float distanceMoved = Vector3.Distance(playerTransform.position, lastPlayerPosition);
        
        // 更新位置记录
        lastPlayerPosition = playerTransform.position;
        
        // 累计移动距离
        accumulatedDistance += distanceMoved;
        
        // 累计时间
        timeSinceLastEncounter += Time.deltaTime;
        
        // 检查是否应该触发暗雷
        CheckForEncounter();
    }
    
    /// <summary>
    /// 检查是否应该触发暗雷，基于泊松分布的概率计算
    /// </summary>
    private void CheckForEncounter()
    {
        // 如果累计距离小于最小触发距离，不进行检查
        if (accumulatedDistance < minimumDistanceForEncounter)
            return;
        
        // 计算基于泊松分布的概率
        float lambda = accumulatedDistance / averageDistancePerEncounter;
        
        // 考虑时间因素增加触发概率
        lambda += timeSinceLastEncounter * timeFactorCoefficient;
        
        // 泊松分布计算出触发概率
        float probability = PoissonProbability(lambda, 1);
        
        // 生成0-1之间的随机数
        float randomValue = (float)randomGenerator.NextDouble();
        
        if (debugMode)
        {
            Debug.Log($"暗雷检查：累计距离={accumulatedDistance:F2}，λ={lambda:F2}，概率={probability:F3}，随机值={randomValue:F3}");
        }
        
        // 如果随机数小于触发概率，触发暗雷
        if (randomValue < probability)
        {
            TriggerEncounter();
            
            // 重置累计距离和时间
            accumulatedDistance = 0f;
            timeSinceLastEncounter = 0f;
        }
    }
    
    /// <summary>
    /// 触发暗雷事件
    /// </summary>
    private void TriggerEncounter()
    {
        if (debugMode)
        {
            Debug.Log("触发暗雷！");
        }
        
        // 显示UI面板
        if (encounterUIPanel != null)
        {
            encounterUIPanel.SetActive(true);
            
            // 设置随机消息
            if (encounterMessageText != null)
            {
                int messageIndex = randomGenerator.Next(0, encounterMessages.Length);
                encounterMessageText.text = encounterMessages[messageIndex];
            }
            
            // 3秒后自动隐藏UI
            StartCoroutine(HideEncounterUIAfterDelay(3f));
        }
        
        // 这里可以添加其他触发效果，如声音、粒子效果等
    }
    
    /// <summary>
    /// 延迟隐藏UI面板
    /// </summary>
    private IEnumerator HideEncounterUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (encounterUIPanel != null)
        {
            encounterUIPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 计算泊松分布的概率
    /// P(X=k) = (e^-λ) * (λ^k) / k!
    /// 这里我们计算P(X>=1) = 1 - P(X=0)
    /// </summary>
    private float PoissonProbability(float lambda, int k)
    {
        if (k == 1)
        {
            // P(X>=1) = 1 - P(X=0)
            // P(X=0) = e^-λ
            return 1 - Mathf.Exp(-lambda);
        }
        else
        {
            // 完整计算公式
            float numerator = Mathf.Pow(lambda, k) * Mathf.Exp(-lambda);
            float denominator = Factorial(k);
            return numerator / denominator;
        }
    }
    
    /// <summary>
    /// 计算阶乘
    /// </summary>
    private float Factorial(int n)
    {
        float result = 1f;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }
    
    /// <summary>
    /// 在编辑器中添加测试按钮
    /// </summary>
    [ContextMenu("测试暗雷触发")]
    private void TestEncounter()
    {
        TriggerEncounter();
    }
    
    /// <summary>
    /// 手动设置UI面板引用
    /// </summary>
    public void SetUIPanel(GameObject panel, TMP_Text messageText = null)
    {
        encounterUIPanel = panel;
        if (messageText != null)
        {
            encounterMessageText = messageText;
        }
    }
}