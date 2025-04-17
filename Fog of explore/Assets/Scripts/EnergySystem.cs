using UnityEngine;
using UnityEngine.UI;

public class EnergySystem : MonoBehaviour
{
    [Header("精力值设置")]
    public float maxEnergy = 100f;    // 精力值上限
    public float currentEnergy;       // 当前精力值
    public float movementEnergyCost = 1f;  // 每单位距离消耗的精力值
    public float energyRecoveryAmount = 10f; // 每次回复的精力值
    
    [Header("UI设置")]
    public Slider energySlider;        // 用于显示精力值的滑动条（可选，优先使用UIManager）
    public Image energyFillImage;      // 精力条的填充图像（可选，优先使用UIManager）
    public Color fullEnergyColor = Color.green;  // 精力充足时的颜色
    public Color lowEnergyColor = Color.red;     // 精力不足时的颜色
    
    private PlayerMovement playerMovement;
    private UIManager uiManager;
    
    void Start()
    {
        // 初始化精力值为最大值
        currentEnergy = maxEnergy;
        
        // 获取玩家移动组件引用
        playerMovement = GetComponent<PlayerMovement>();
        
        // 尝试获取UI管理器
        uiManager = UIManager.Instance;
        
        // 如果没有通过UIManager设置UI，则尝试使用直接引用
        if (uiManager == null)
        {
            // 如果没有设置UIManager，使用直接引用的UI组件
            if (energySlider != null)
            {
                energySlider.maxValue = maxEnergy;
                energySlider.value = currentEnergy;
            }
            else
            {
                Debug.LogWarning("未找到UIManager且未直接设置精力条！精力系统将无法显示UI。");
            }
        }
        else
        {
            // 优先使用UIManager中的引用
            if (uiManager.energySlider != null)
            {
                energySlider = uiManager.energySlider;
                energyFillImage = uiManager.energyFillImage;
            }
        }
        
        // 更新UI显示
        UpdateEnergyUI();
    }
    
    void Update()
    {
        // 按下Enter键恢复精力
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            RecoverEnergy(energyRecoveryAmount);
        }
        
        // 更新UI显示
        UpdateEnergyUI();
    }
    
    // 消耗精力
    public bool ConsumeEnergy(float amount)
    {
        // 检查是否有足够的精力
        if (currentEnergy < amount)
        {
            Debug.Log($"精力不足！需要 {amount}，当前 {currentEnergy}");
            return false; // 精力不足
        }
        
        // 消耗精力
        currentEnergy -= amount;
        
        // 确保精力值不小于0
        currentEnergy = Mathf.Max(0, currentEnergy);
        
        // 更新UI
        UpdateEnergyUI();
        
        return true; // 精力消耗成功
    }
    
    // 恢复精力
    public void RecoverEnergy(float amount)
    {
        // 增加精力值
        currentEnergy += amount;
        
        // 确保精力值不超过最大值
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        
        // 更新UI
        UpdateEnergyUI();
        
        Debug.Log($"恢复了 {amount} 点精力，当前精力: {currentEnergy}/{maxEnergy}");
    }
    
    // 计算移动消耗的精力
    public float CalculateMovementEnergyCost(float distance)
    {
        return distance * movementEnergyCost;
    }
    
    // 检查是否有足够的精力进行移动
    public bool HasEnoughEnergyToMove(float distance)
    {
        float cost = CalculateMovementEnergyCost(distance);
        return currentEnergy >= cost;
    }
    
    // 更新UI显示
    private void UpdateEnergyUI()
    {
        // 优先使用UIManager更新UI
        if (uiManager != null)
        {
            uiManager.SetEnergyValue(currentEnergy, maxEnergy);
            
            // 计算颜色
            float energyRatio = currentEnergy / maxEnergy;
            Color energyColor = Color.Lerp(lowEnergyColor, fullEnergyColor, energyRatio);
            
            uiManager.SetEnergyColor(energyColor);
        }
        else if (energySlider != null)
        {
            // 使用直接引用的UI组件
            energySlider.value = currentEnergy;
            
            // 根据精力值比例更新颜色
            if (energyFillImage != null)
            {
                float energyRatio = currentEnergy / maxEnergy;
                energyFillImage.color = Color.Lerp(lowEnergyColor, fullEnergyColor, energyRatio);
            }
        }
    }
    
    // 获取当前精力比例
    public float GetEnergyRatio()
    {
        return currentEnergy / maxEnergy;
    }
} 