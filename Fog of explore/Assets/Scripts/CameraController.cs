using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("虚拟相机设置")]
    [Tooltip("玩家跟随相机")]
    public CinemachineCamera playerCamera;
    
    [Tooltip("全局俯视相机")]
    public CinemachineCamera globalCamera;
    
    [Header("优先级设置")]
    [Tooltip("激活状态下的相机优先级")]
    public int activePriority = 20;
    
    [Tooltip("非激活状态下的相机优先级")]
    public int inactivePriority = 10;
    
    [Header("其他设置")]
    [Tooltip("切换相机的按键")]
    public KeyCode toggleKey = KeyCode.T;
    
    [Tooltip("当前是否使用全局相机")]
    public bool useGlobalCamera = false;
    
    [Tooltip("是否在开始时使用全局相机")]
    public bool startWithGlobalCamera = false;
    
    [Tooltip("相机切换过渡时间")]
    [Range(0f, 5f)]
    public float blendTime = 1.0f;
    
    void Start()
    {
        // 检查相机引用
        if (playerCamera == null || globalCamera == null)
        {
            Debug.LogError("CameraController: 未设置虚拟相机引用！");
            return;
        }
        
        // 设置初始相机状态
        useGlobalCamera = startWithGlobalCamera;
        UpdateCameraPriorities();
    }
    
    void Update()
    {
        // 检测按键输入
        if (Input.GetKeyDown(toggleKey))
        {
            // 切换相机状态
            useGlobalCamera = !useGlobalCamera;
            UpdateCameraPriorities();
            
            // 显示切换提示
            Debug.Log($"相机已切换到: {(useGlobalCamera ? "全局相机" : "玩家相机")}");
        }
    }
    
    // 更新相机优先级
    private void UpdateCameraPriorities()
    {
        if (useGlobalCamera)
        {
            globalCamera.Priority = activePriority;
            playerCamera.Priority = inactivePriority;
        }
        else
        {
            playerCamera.Priority = activePriority;
            globalCamera.Priority = inactivePriority;
        }
    }
} 