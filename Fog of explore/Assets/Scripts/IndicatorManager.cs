using HUDIndicator;
using UnityEngine;
using System.Collections.Generic;
public class IndicatorManager : MonoBehaviour
{

    public List<IndicatorOffScreen> indicators;
    public Transform player;
    public float displayDistance = 10f;
    
    [Header("指示器设置")]
    public IndicatorOffScreen defaultIndicatorPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 初始化列表，避免空引用
        if (indicators == null)
        {
            indicators = new List<IndicatorOffScreen>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var indicator in indicators)
        {
            if (indicator == null) continue;
            
            // 获取指示器的自定义设置（如果有）
            RandomizedSpriteIndicator customSettings = indicator.GetComponentInParent<RandomizedSpriteIndicator>();
            
            // 计算距离
            float distance = Vector3.Distance(indicator.transform.position, player.position);
            
            // 使用自定义设置或默认设置
            if (customSettings != null)
            {
                // 只有当距离在设定范围内时才显示
                if (distance >= customSettings.minDistance && distance <= customSettings.maxDistance)
                {
                    indicator.enabled = customSettings.showIndicator;
                }
                else
                {
                    indicator.enabled = false;
                }
            }
            else
            {
                // 使用默认逻辑
                if (distance < displayDistance)
                {
                    indicator.enabled = true;
                }
                else
                {
                    indicator.enabled = false;
                }
            }
        }
    }
    
    // 添加指示器
    public void AddIndicator(IndicatorOffScreen indicator)
    {
        if (indicator != null && !indicators.Contains(indicator))
        {
            indicators.Add(indicator);
        }
    }
    
    // 移除指示器
    public void RemoveIndicator(IndicatorOffScreen indicator)
    {
        if (indicator != null && indicators.Contains(indicator))
        {
            indicators.Remove(indicator);
        }
    }
}
