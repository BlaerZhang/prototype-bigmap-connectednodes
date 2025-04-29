using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float accelerationTime = 0.2f;
    [SerializeField] private float decelerationTime = 0.1f;
    
    // 精力系统
    private EnergySystem energySystem;
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;
    private const float ENERGY_CHECK_THRESHOLD = 0.1f;
    
    private bool isWatching = false;
    private Camera mainCamera;
    private FogOfWar fogOfWar;
    private GameObject visionMask;

    // Movement components
    private Rigidbody2D rb;
    private float currentSpeed;
    private float targetSpeed;
    private Vector2 moveDirection;
    private bool isMoving;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SetupRigidbody();
    }
    
    private void SetupRigidbody()
    {
        rb.gravityScale = 0f;
        rb.linearDamping = 1f;
        rb.angularDamping = 5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    
    private void Start()
    {
        fogOfWar = FindObjectOfType<FogOfWar>();
        visionMask = transform.GetChild(0).gameObject;
        
        // 获取精力系统
        energySystem = GetComponent<EnergySystem>();
        if (energySystem == null)
        {
            Debug.LogWarning("未找到精力系统组件！请添加EnergySystem组件到玩家对象上。");
        }
        
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("未找到主摄像机！鼠标控制可能无法正常工作。");
        }
        
        lastPosition = transform.position;
    }
    
    private void Update()
    {
        HandleMouseInput();
        HandleWatchingMode();
        
        if (energySystem != null && isMoving)
        {
            CalculateAndConsumeEnergy();
        }
    }

    private void HandleMouseInput()
    {
        // 获取鼠标位置
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;
        Vector3 targetPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        
        // 计算方向
        Vector2 direction = (targetPosition - transform.position).normalized;
        
        // 处理旋转
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        // 处理移动
        isMoving = Input.GetMouseButton(0); // 鼠标左键按住时移动
        targetSpeed = isMoving ? moveSpeed : 0f;
        
        // 平滑插值当前速度
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref moveDirection.y, 
            isMoving ? accelerationTime : decelerationTime);
        
        // 应用移动
        Vector2 movement = direction * currentSpeed;
        transform.Translate(movement * Time.deltaTime, Space.World);
    }
    
    private void HandleWatchingMode()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isWatching = !isWatching;
            if (isWatching)
            {
                visionMask.transform.localScale *= 2f;
            }
            else
            {
                visionMask.transform.localScale /= 2f;
            }
        }
    }
    
    private void CalculateAndConsumeEnergy()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        accumulatedDistance += distanceMoved;
        
        if (accumulatedDistance >= ENERGY_CHECK_THRESHOLD)
        {
            // 精力消耗
            float energyCost = energySystem.CalculateMovementEnergyCost(accumulatedDistance);
            energySystem.ConsumeEnergy(energyCost);

            // 耐力消耗
            float staminaCost = energySystem.CalculateMovementStaminaCost(accumulatedDistance);
            energySystem.ConsumeStamina(staminaCost);

            accumulatedDistance = 0f;
        }
        
        lastPosition = transform.position;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 碰撞时停止移动
        currentSpeed = 0f;
        targetSpeed = 0f;
    }
} 