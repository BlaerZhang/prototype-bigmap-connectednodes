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

    // 新增：耐力系统
    [Header("耐力值设置")]
    public float maxStamina = 100f;    // 耐力值上限
    public float currentStamina;       // 当前耐力值
    public float movementStaminaCost = 1f;  // 每单位距离消耗的耐力值
    public float staminaRecoveryAmount = 10f; // 每次回复的耐力值
    public float depletedEnergyStaminaPenalty = 2.0f; // 精力耗尽时耐力消耗的倍率

    [Header("耐力UI设置")]
    public Slider staminaSlider;        // 用于显示耐力值的滑动条
    public Image staminaFillImage;      // 耐力条的填充图像
    public Color fullStaminaColor = Color.cyan;  // 耐力充足时的颜色
    public Color lowStaminaColor = Color.blue;   // 耐力不足时的颜色

    private PlayerMovement playerMovement;
    private UIManager uiManager;
    
    void Start()
    {
        // 初始化精力值为最大值
        currentEnergy = maxEnergy;
        // 初始化耐力值为最大值
        currentStamina = maxStamina;
        
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
            if (staminaSlider != null)
            {
                staminaSlider.maxValue = maxStamina;
                staminaSlider.value = currentStamina;
            }
            if (energySlider == null)
            {
                Debug.LogWarning("未找到UIManager且未直接设置精力条！精力系统将无法显示UI。");
            }
            if (staminaSlider == null)
            {
                Debug.LogWarning("未找到UIManager且未直接设置耐力条！耐力系统将无法显示UI。");
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
            if (uiManager.staminaSlider != null)
            {
                staminaSlider = uiManager.staminaSlider;
                staminaFillImage = uiManager.staminaFillImage;
            }
        }
        
        // 更新UI显示
        UpdateEnergyUI();
        UpdateStaminaUI();
    }
    
    void Update()
    {
        // 按下Enter键恢复精力
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            RecoverEnergy(energyRecoveryAmount);
            RecoverStamina(staminaRecoveryAmount);
        }
        
        // 更新UI显示
        UpdateEnergyUI();
        UpdateStaminaUI();
    }
    
    // 消耗精力
    public bool ConsumeEnergy(float amount)
    {
        if (currentEnergy < amount)
        {
            Debug.Log($"精力不足！需要 {amount}，当前 {currentEnergy}");
            return false;
        }
        currentEnergy -= amount;
        currentEnergy = Mathf.Max(0, currentEnergy);
        UpdateEnergyUI();
        return true;
    }
    
    // 恢复精力
    public void RecoverEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
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
    
    // 更新精力UI显示
    private void UpdateEnergyUI()
    {
        if (uiManager != null)
        {
            uiManager.SetEnergyValue(currentEnergy, maxEnergy);
            float energyRatio = currentEnergy / maxEnergy;
            Color energyColor = Color.Lerp(lowEnergyColor, fullEnergyColor, energyRatio);
            uiManager.SetEnergyColor(energyColor);
        }
        else if (energySlider != null)
        {
            energySlider.value = currentEnergy;
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

    // ================== 新增：耐力相关方法 ==================
    // 消耗耐力
    public bool ConsumeStamina(float amount)
    {
        if (currentStamina < amount)
        {
            Debug.Log($"耐力不足！需要 {amount}，当前 {currentStamina}");
            return false;
        }
        currentStamina -= amount;
        currentStamina = Mathf.Max(0, currentStamina);
        UpdateStaminaUI();
        return true;
    }
    // 恢复耐力
    public void RecoverStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        UpdateStaminaUI();
        Debug.Log($"恢复了 {amount} 点耐力，当前耐力: {currentStamina}/{maxStamina}");
    }
    // 计算移动消耗的耐力
    public float CalculateMovementStaminaCost(float distance)
    {
        // 如果精力已耗尽，耐力消耗增加
        if (currentEnergy <= 0)
        {
            return distance * movementStaminaCost * depletedEnergyStaminaPenalty;
        }
        return distance * movementStaminaCost;
    }
    // 检查是否有足够的耐力进行移动
    public bool HasEnoughStaminaToMove(float distance)
    {
        float cost = CalculateMovementStaminaCost(distance);
        return currentStamina >= cost;
    }
    // 更新耐力UI显示
    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
            if (staminaFillImage != null)
            {
                float staminaRatio = currentStamina / maxStamina;
                staminaFillImage.color = Color.Lerp(lowStaminaColor, fullStaminaColor, staminaRatio);
            }
        }
    }
    // 获取当前耐力比例
    public float GetStaminaRatio()
    {
        return currentStamina / maxStamina;
    }
} 