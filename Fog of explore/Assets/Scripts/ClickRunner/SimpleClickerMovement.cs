using UnityEngine;
using JoostenProductions;
using DG.Tweening;
public class SimpleClickerMovement : OverridableMonoBehaviour
{
    [Header("点击设置")]
    private float clickingFactor = 0;
    public float factorDecreaseRate = 0.01f;
    public float factorIncreasePerClick = 0.1f;

    [Header("移动设置")]
    public bool isRunning = false;
    
    [Header("速度设置")]
    public float maxSpeed = 5f;
    public float minSpeed = 0.5f;
    private float currentSpeed = 0f;

    [Header("3D动画表现")]
    public Animator runningManAnimator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentSpeed = 0;
    }

    // Update is called once per frame
    public override void UpdateMe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickingFactor += factorIncreasePerClick;
            OnStep();
        }

        clickingFactor -= factorDecreaseRate * Time.deltaTime;
        clickingFactor = Mathf.Clamp01(clickingFactor);

        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, clickingFactor);

        if (clickingFactor <= 0)
        {
            isRunning = false;
            runningManAnimator.SetBool("isRunning", false);
            runningManAnimator.speed = 1;
        }
        else
        {
            isRunning = true;
            runningManAnimator.SetBool("isRunning", true);
            runningManAnimator.speed = Mathf.Lerp(0, 1f, clickingFactor);
        }

        if (isRunning)
        {
            MoveTowardsMouse(currentSpeed);
        }

        RotateTowardsMouse();
    }

    private void MoveTowardsMouse(float speed)
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 directionToMouse = (mousePosition - (Vector2)transform.position).normalized;
        transform.position += (Vector3)directionToMouse * speed * Time.deltaTime;
    }

    private void RotateTowardsMouse()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 directionToMouse = (mousePosition - (Vector2)transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, directionToMouse);
        runningManAnimator.transform.rotation = Quaternion.Euler(0, -transform.rotation.eulerAngles.z, 0);
    }

     private void OnStep()
    {
        // 角色进行步伐动画, 并设置初始大小
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f).OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
    }
}
