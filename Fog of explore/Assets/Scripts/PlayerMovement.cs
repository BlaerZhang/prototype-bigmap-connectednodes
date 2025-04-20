using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HUDIndicator;
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    
    [Header("控制设置")]
    public bool useMouseControl = true;    // 是否启用鼠标控制
    public float mouseControlDistance = 1f; // 鼠标方向影响的距离阈值
    
    // 路径移动状态
    private MapNode currentNode = null;
    private MapPath currentPath = null;
    private float currentT = 0f; // 当前在路径上的位置 (0-1)
    private int moveDirection = 1; // 1表示向前（从节点A到B），-1表示向后
    private bool isMoving = false;
    private bool isClimbing = false;
    private bool isWatching = false;
    public Transform target;
    
    // 精力系统
    private EnergySystem energySystem;
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;
    private const float ENERGY_CHECK_THRESHOLD = 0.1f; // 每累积多少距离检查一次精力消耗
    
    // 输入控制
    private Vector2 movementInput;
    private Vector2 mouseDirection;
    private bool isMousePressed = false;
    private Camera mainCamera;
    private FogOfWar fogOfWar;

    private GameObject visionMask;
    
    
    private void Start()
    {
        // 找到最近的节点作为起始点
        FindClosestNodeAtStart();
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
        
        // 记录初始位置用于计算移动距离
        lastPosition = transform.position;
    }
    
    private void Update()
    {
        // 获取键盘移动输入
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        
        // 处理鼠标输入
        HandleMouseInput();
        
        // 选择使用哪种输入方式移动
        Vector2 finalInput = movementInput;
        if (useMouseControl && isMousePressed && mouseDirection.magnitude > 0.1f)
        {
            finalInput = mouseDirection;
        }
        
        // 处理移动逻辑
        HandleMovement(finalInput);

        // 处理观察逻辑
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isClimbing = !isClimbing;
            if (isClimbing)
            {
                // fogOfWar.visionRadius *= 3f;
                visionMask.transform.localScale *= 2f;
            }
            else
            {
                // fogOfWar.visionRadius /= 3f;
                visionMask.transform.localScale /= 2f;
            }
            
            // fogOfWar.UpdateFogOfWar();
        }

        // 处理瞭望逻辑
        if (Input.GetKeyDown(KeyCode.E))
        {
            isWatching = !isWatching;
            if (isWatching)
            {
                target.GetComponent<IndicatorOffScreen>().enabled = true;
            }
            else
            {
                target.GetComponent<IndicatorOffScreen>().enabled = false;
            }
        }
        
        // 计算移动距离并消耗精力
        if (energySystem != null && isMoving)
        {
            CalculateAndConsumeEnergy();
        }
    }
    
    // 处理鼠标输入
    private void HandleMouseInput()
    {
        // 检测鼠标左键是否按下
        isMousePressed = Input.GetMouseButton(0);
        
        if (isMousePressed && mainCamera != null)
        {
            // 获取鼠标在世界空间中的位置
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z; // 保持相同的z坐标
            
            // 计算从玩家到鼠标位置的方向向量
            mouseDirection = (mouseWorldPos - transform.position).normalized;
            
            // 可视化鼠标方向（调试用）
            Debug.DrawRay(transform.position, mouseDirection * 2f, Color.red);
        }
    }
    
    // 计算移动距离并消耗精力
    private void CalculateAndConsumeEnergy()
    {
        // 计算从上次位置到当前位置的距离
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        
        // 累积距离
        accumulatedDistance += distanceMoved;
        
        // 当累积距离达到阈值时，消耗精力
        if (accumulatedDistance >= ENERGY_CHECK_THRESHOLD)
        {
            // 计算并消耗精力
            float energyCost = energySystem.CalculateMovementEnergyCost(accumulatedDistance);
            
            // 尝试消耗精力
            bool energyConsumed = energySystem.ConsumeEnergy(energyCost);
            
            // 如果精力不足，停止移动
            if (!energyConsumed)
            {
                isMoving = false;
                Debug.Log("精力不足，无法继续移动！");
            }
            
            // 重置累积距离
            accumulatedDistance = 0f;
        }
        
        // 更新上次位置
        lastPosition = transform.position;
    }
    
    // 寻找起始点最近的节点
    private void FindClosestNodeAtStart()
    {
        MapNode[] allNodes = FindObjectsOfType<MapNode>();
        
        if (allNodes.Length == 0)
        {
            Debug.LogWarning("场景中没有任何节点！");
            return;
        }
        
        MapNode closestNode = null;
        float closestDistance = float.MaxValue;
        
        foreach (MapNode node in allNodes)
        {
            float distance = Vector2.Distance(transform.position, node.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = node;
            }
        }
        
        if (closestNode != null)
        {
            currentNode = closestNode;
            transform.position = closestNode.transform.position;
            lastPosition = transform.position;
        }
    }
    
    // 处理移动逻辑
    private void HandleMovement(Vector2 inputDirection)
    {
        if (inputDirection.magnitude > 0.1f)
        {
            // 有输入，尝试移动
            
            if (currentPath == null)
            {
                // 如果我们在节点上且没有行走的路径
                if (currentNode != null)
                {
                    // 根据输入方向选择一条路径
                    MapPath newPath = currentNode.GetPathInDirection(inputDirection);
                    
                    if (newPath != null)
                    {
                        // 预先计算移动所需的精力
                        MapNode targetNode = (currentNode == newPath.nodeA) ? newPath.nodeB : newPath.nodeA;
                        float pathDistance = Vector3.Distance(currentNode.transform.position, targetNode.transform.position);
                        
                        // 检查是否有足够的精力移动
                        if (energySystem != null && !energySystem.HasEnoughEnergyToMove(pathDistance))
                        {
                            Debug.Log("精力不足，无法移动到下一个节点！");
                            return;
                        }
                        
                        // 找到了一条合适的路径
                        currentPath = newPath;
                        
                        // 设置初始的T值和移动方向
                        if (currentNode == currentPath.nodeA)
                        {
                            currentT = 0f;
                            moveDirection = 1; // 向B节点移动
                        }
                        else
                        {
                            currentT = 1f;
                            moveDirection = -1; // 向A节点移动
                        }
                        
                        isMoving = true;
                        currentNode = null; // 我们现在在路径上，不在节点上
                    }
                }
            }
            else
            {
                // 如果我们已经在路径上
                
                // 检查是否需要改变方向
                Vector2 pathDirection = (Vector2)currentPath.GetDirectionAtPoint(currentT);
                float dotProduct = Vector2.Dot(pathDirection * moveDirection, inputDirection.normalized);
                
                if (dotProduct < -0.5f) // 如果输入与当前移动方向大致相反
                {
                    // 反转方向
                    moveDirection *= -1;
                }
                
                // 继续沿路径移动
                isMoving = true;
            }
        }
        else
        {
            // 没有输入，停止移动
            isMoving = false;
        }
        
        // 执行路径移动
        if (isMoving && currentPath != null)
        {
            MoveAlongPath();
        }
    }
    
    // 沿当前路径移动
    private void MoveAlongPath()
    {
        // 计算移动距离
        float distanceToMove = moveSpeed * Time.deltaTime;
        
        // 估计路径的总长度，用于将距离转换为T的增量
        float pathLength = Vector3.Distance(currentPath.nodeA.transform.position, currentPath.nodeB.transform.position);
        if (currentPath.pathType != MapPath.PathType.Straight)
        {
            // 对于曲线或折线，我们需要更精确的估计
            pathLength *= 1.5f; // 简单估计，实际应基于路径形状
        }
        
        // 计算t的增量
        float deltaT = distanceToMove / pathLength;
        
        // 更新t值
        currentT += deltaT * moveDirection;
        
        // 边界检查
        if (currentT >= 1f)
        {
            // 到达了B节点
            currentNode = currentPath.nodeB;
            currentPath = null;
            transform.position = currentNode.transform.position;
            isMoving = false;
        }
        else if (currentT <= 0f)
        {
            // 到达了A节点
            currentNode = currentPath.nodeA;
            currentPath = null;
            transform.position = currentNode.transform.position;
            isMoving = false;
        }
        else
        {
            // 仍在路径上移动
            transform.position = currentPath.GetPointOnPath(currentT);
        }
    }
    
    // 在Unity编辑器中绘制辅助信息
    private void OnDrawGizmos()
    {
        if (useMouseControl && isMousePressed && mouseDirection.magnitude > 0.1f)
        {
            // 绘制鼠标方向
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, mouseDirection * 2f);
        }
    }
} 