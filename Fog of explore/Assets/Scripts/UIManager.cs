using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("精力UI")]
    public Slider energySlider;   // 精力条滑动条
    public Image energyFillImage; // 精力条填充图像
    public Text energyText;       // 精力值文本（可选）
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 初始化UI
        InitializeUI();
    }
    
    // 初始化UI组件
    private void InitializeUI()
    {
        if (energySlider == null)
        {
            Debug.LogWarning("精力条Slider未设置!");
        }
        
        if (energyFillImage == null && energySlider != null)
        {
            // 尝试自动获取fillImage
            energyFillImage = energySlider.fillRect.GetComponent<Image>();
        }
    }
    
    // 设置精力值，用于更新UI
    public void SetEnergyValue(float currentValue, float maxValue)
    {
        if (energySlider != null)
        {
            energySlider.maxValue = maxValue;
            energySlider.value = currentValue;
            
            // 更新文本（如果有）
            if (energyText != null)
            {
                energyText.text = $"{Mathf.Round(currentValue)}/{Mathf.Round(maxValue)}";
            }
        }
    }
    
    // 设置精力条颜色
    public void SetEnergyColor(Color color)
    {
        if (energyFillImage != null)
        {
            energyFillImage.color = color;
        }
    }
} 