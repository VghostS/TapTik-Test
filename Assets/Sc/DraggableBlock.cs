using UnityEngine;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class DraggableBlock : MonoBehaviour
{
    public float gridSize = 0.5f;
    
    [Header("Dynamic Drain Settings")]
    public SandSimulations sandSim;
    public float catchWidthWorld = 1.0f; 
    public float verticalActivationDistance = 0.6f; 
    
    [Header("Physics Dragging")]
    public float dragSpeed = 25f;

    [Header("Visuals & UI")]
    public TextMeshPro percentageText;
    public Animator fillAnimator;
    public string fillStateName = "FillAnimation";
    
    [Range(0.8f, 1.0f)]
    public float autoCompleteThreshold = 0.98f;

    private Rigidbody2D rb;
    private bool isDragging = false;
    private Vector3 targetPosition;
    private Vector3 grabOffset;

    private bool isActivelyCatching = false;
    private int myLeftGridX;
    private int myRightGridX;
    private int sandCaughtAmount = 0;

    public UnityEvent onFilled;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        targetPosition = transform.position;

        if (fillAnimator != null)
        {
            fillAnimator.speed = 0f; 
            fillAnimator.Play(fillStateName, 0, 0f);
        }

        if (percentageText != null) percentageText.text = "0%";
        if (sandSim != null) sandSim.onSandPixelDrained.AddListener(OnSandFell);
    }

    void OnMouseDown()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        grabOffset = transform.position - mousePos;
        isDragging = true;
        
        isActivelyCatching = false; 
        if (sandSim != null)
        {
            sandSim.CloseAllGates();
            sandSim.ClearTarget(); 
            // NEW: Tell the simulation we are no longer catching sand here
            sandSim.ClearActiveCatchArea();
        }
    }

    void OnMouseDrag()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        targetPosition = mousePos + grabOffset;
    }

    void OnMouseUp()
    {
        isDragging = false;
        SnapToGrid();
    }

    void FixedUpdate()
    {
        if (isDragging) rb.linearVelocity = (targetPosition - transform.position) * dragSpeed;
        else rb.linearVelocity = Vector2.zero;
    }

    void SnapToGrid()
    {
        float snapX = Mathf.Round(transform.position.x / gridSize) * gridSize;
        float snapY = Mathf.Round(transform.position.y / gridSize) * gridSize;

        targetPosition = new Vector3(snapX, snapY, transform.position.z);
        rb.MovePosition(targetPosition); 
        
        CheckForDrain();
    }

    void CheckForDrain()
    {
        if (sandSim == null) return;
        
        float floorWorldY = sandSim.GridToWorldY(sandSim.floorGridY);
        
        if (Mathf.Abs(transform.position.y - floorWorldY) <= verticalActivationDistance)
        {
            float leftWorldX = transform.position.x - (catchWidthWorld / 2f);
            float rightWorldX = transform.position.x + (catchWidthWorld / 2f);
            
            myLeftGridX = sandSim.WorldToGridX(leftWorldX);
            myRightGridX = sandSim.WorldToGridX(rightWorldX);
            
            sandSim.OpenGate(myLeftGridX, myRightGridX);
            sandSim.SetTargetX(sandSim.WorldToGridX(transform.position.x));

            // NEW: Register this block's bounds so the simulation knows not to refund sand here
            sandSim.SetActiveCatchArea(myLeftGridX, myRightGridX);

            isActivelyCatching = true;
        }
    }

    void OnSandFell(int sandGridX)
    {
        if (!isActivelyCatching || sandSim.TotalInitialSand <= 0) return;

        // Because the simulation now handles bounds checking before invoking, this is mostly a safety check
        if (sandGridX >= myLeftGridX && sandGridX <= myRightGridX)
        {
            sandCaughtAmount++;
            UpdateFillVisuals();
        }
    }

    void UpdateFillVisuals()
    {
        float percentage = (float)sandCaughtAmount / sandSim.TotalInitialSand;

        if (percentage >= autoCompleteThreshold)
        {
            percentage = 1.0f;
            isActivelyCatching = false; 

            onFilled?.Invoke(); 
            fillAnimator.Play("complete");
            fillAnimator.speed = 1f; 
        }

        percentage = Mathf.Clamp01(percentage);

        if (percentageText != null) percentageText.text = Mathf.FloorToInt(percentage * 100) + "%";

        if (fillAnimator != null && percentage < autoCompleteThreshold)
        {
            float animTime = Mathf.Clamp(percentage, 0f, 0.999f);
            fillAnimator.Play(fillStateName, 0, animTime);
        }
    }
}