using UnityEngine;
using JoostenProductions;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using Unity.Cinemachine;

public class ClickerMovement : OverridableMonoBehaviour
{
    [Header("速度设置")]
    public float maxSpeed = 5f;
    public float minSpeed = 0.5f;
    private float currentSpeed = 0f;
    [Tooltip("低于此速度时，如果超时未点击可以安全停止不会绊倒")]
    public float safeStopSpeed = 1.5f;
    [Tooltip("控制速度曲线的非线性程度，值越大曲线越陡峭")]
    [Range(0.01f, 5f)]
    public float speedCurveExponent = 2f;

    [Header("点击设置")]
    public float maxClickInterval = 1f; // 最大点击间隔对应最低速度
    public float minClickInterval = 0.1f; // 最小点击间隔对应最高速度
    private float currentClickInterval = 1f; // 当前期望的点击间隔
    private float lastActualInterval = 1f; // 最后一次实际点击间隔
    public float clickTimeTolerance = 0.5f; // 点击时间容差
    private bool lastClickButtonWasLeft = true;
    private float lastClickTime = 0f;
    private bool isRunning = false;
    private SpriteRenderer playerSpriteRenderer;

    [Header("步伐调整")]
    public float speedChangeRate = 0.2f; // 速度变化率，用于平滑速度变化
    public float lowSpeedStopDelay = 1.5f; // 低速状态停止延迟时间
    private float lowSpeedTimer = 0f; // 计时器，跟踪低速状态持续时间
    
    [Header("调试")]
    public bool showDebugInfo = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentClickInterval = maxClickInterval;
        lastActualInterval = maxClickInterval;
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    public override void UpdateMe()
    {
        if (isRunning)
        {
            // 处理低速状态 - 只在超时未点击的情况下才考虑
            if (currentSpeed <= safeStopSpeed)
            {
                lowSpeedTimer += Time.deltaTime;
                
                // 如果在低速状态超过指定时间，安全停止
                if (lowSpeedTimer >= lowSpeedStopDelay)
                {
                    SafeStop();
                    return;
                }
                
                // 如果超过容忍时间未点击但速度低，则安全停止
                if (Time.time > lastClickTime + currentClickInterval + clickTimeTolerance)
                {
                    SafeStop();
                    return;
                }
            }
            else
            {
                // 重置低速计时器
                lowSpeedTimer = 0f;
                
                // 如果超过容忍时间未点击且速度高，则绊倒
                if (Time.time > lastClickTime + currentClickInterval + clickTimeTolerance)
                {
                    OnStumble();
                    return;
                }
            }
        }
        
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            bool isLeftClick = Input.GetMouseButtonDown(0);
            
            if (isRunning)
            {
                // 检查是否使用了与上次相同的按钮 - 无论速度如何都绊倒
                if (isLeftClick == lastClickButtonWasLeft)
                {
                    Debug.Log("使用相同的按钮 - 绊倒");
                    OnStumble();
                }
                else
                {
                    float timeSinceLastClick = Time.time - lastClickTime;
                    
                    // 检查点击是否在容差范围内
                    if (timeSinceLastClick >= currentClickInterval - clickTimeTolerance && 
                        timeSinceLastClick <= currentClickInterval + clickTimeTolerance)
                    {
                        // 记录实际点击间隔并更新速度
                        lastActualInterval = timeSinceLastClick;
                        UpdateSpeedBasedOnInterval(lastActualInterval);
                        
                        lastClickButtonWasLeft = isLeftClick;
                        lastClickTime = Time.time;
                        lowSpeedTimer = 0f; // 重置低速计时器
                        OnStep();
                    }
                    else
                    {
                        // 点击时机不对 - 无论速度如何都绊倒
                        Debug.Log("点击时机不对 - 无论速度如何都绊倒");
                        OnStumble();
                    }
                }
            }
            else
            {
                // 第一次点击，开始奔跑
                isRunning = true;
                lastClickButtonWasLeft = isLeftClick;
                lastClickTime = Time.time;
                lowSpeedTimer = 0f;
                // 初始速度设为最低速度
                currentSpeed = minSpeed;
                currentClickInterval = maxClickInterval;
                OnStep();
            }
        }

        MoveTowardsMouse(currentSpeed);
    }

    private void UpdateSpeedBasedOnInterval(float actualInterval)
    {
        // 将实际点击间隔映射到0-1范围（归一化）
        float normalizedInterval = Mathf.InverseLerp(minClickInterval, maxClickInterval, actualInterval);
        
        // 应用非线性曲线，使小间隔时获得更大的加速效果
        // 使用幂函数创建非线性曲线：f(x) = 1 - x^speedCurveExponent
        float speedFactor = 1f - Mathf.Pow(normalizedInterval, speedCurveExponent);
        
        // 计算目标速度
        float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, speedFactor);
        
        if (showDebugInfo)
        {
            Debug.Log($"间隔: {actualInterval:F2}s, 归一化: {normalizedInterval:F2}, 速度因子: {speedFactor:F2}, 目标速度: {targetSpeed:F2}");
        }
        
        // 平滑过渡到目标速度
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedChangeRate);
        
        // 更新点击间隔目标 - 使用相同的非线性映射关系
        // 当速度高时，期望间隔更小
        float nextIntervalFactor = 1f - Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
        nextIntervalFactor = Mathf.Pow(nextIntervalFactor, 1f/speedCurveExponent); // 应用逆幂函数
        
        currentClickInterval = Mathf.Lerp(minClickInterval, maxClickInterval, nextIntervalFactor);
    }

    private void OnStep()
    {
        // 角色进行步伐动画, 并设置初始大小
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f).OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
    }

    private void SafeStop()
    {
        isRunning = false;
        currentSpeed = 0f;
        
        // 重置到初始点击间隔，但给玩家一个稍微宽松的开始点
        currentClickInterval = maxClickInterval * 0.8f;
        lastActualInterval = maxClickInterval;
        lowSpeedTimer = 0f;
        
        // 轻微的视觉反馈，但不是红色（表示没有出错）
        playerSpriteRenderer.DOColor(Color.yellow, 0.3f)
            .OnComplete(() => playerSpriteRenderer.DOColor(Color.white, 0.2f));
            
        // 轻微的停止动画
        transform.DOScale(new Vector3(0.95f, 0.95f, 1f), 0.2f)
            .OnComplete(() => transform.DOScale(Vector3.one, 0.2f));
    }

    private void OnStumble()
    {
        isRunning = false;
        currentSpeed = 0f;
        
        // 重置节奏和间隔
        currentClickInterval = maxClickInterval;
        lastActualInterval = maxClickInterval;
        lowSpeedTimer = 0f;
        
        // 视觉反馈
        playerSpriteRenderer.DOColor(Color.red, 0.5f).SetEase(Ease.Flash, 10, 2)
            .OnComplete(() => playerSpriteRenderer.DOColor(Color.white, 0.3f));
    }

    private void MoveTowardsMouse(float speed)
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 directionToMouse = (mousePosition - (Vector2)transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, directionToMouse);
        transform.position += (Vector3)directionToMouse * speed * Time.deltaTime;
    }
}
