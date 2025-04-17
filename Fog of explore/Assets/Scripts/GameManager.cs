using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    [Header("地图参考")]
    public GameObject playerPrefab;
    public float worldSize = 100f; // 游戏世界大小
    
    // 全局游戏设置
    public static GameManager Instance { get; private set; }
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化游戏
        InitializeGame();
    }
    
    private void InitializeGame()
    {
        // 确保场景中有玩家
        if (FindObjectOfType<PlayerMovement>() == null && playerPrefab != null)
        {
            Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        }
    }
    
    // 游戏逻辑和管理方法
    void Update()
    {
        // 这里可以添加游戏整体逻辑
        // 比如检测胜利条件，管理游戏状态等

        // Press R to restart
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Press number 1 to load map 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SceneManager.LoadScene("Map1");
        }

        // Press number 2 to load map 2
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SceneManager.LoadScene("Map2");
        }

        // Press number 3 to load map 3
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SceneManager.LoadScene("Map3");
        }
    }
} 