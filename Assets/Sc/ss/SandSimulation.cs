using UnityEngine;
using UnityEngine.Events;

public class SandSimulations : MonoBehaviour
{
    [Header("Grid & Scale")]
    public int width = 128;
    public int height = 256;
    public float pixelsPerUnit = 100f;

    [Header("Simulation Speed")]
    [Tooltip("Target steps per second. 120 is fast and smooth.")]
    public float updatesPerSecond = 120f;
    private float timeAccumulator = 0f;

    [Header("Sand Setup")]
    public int floorGridY = 128;
    public RectInt spawnGridRect = new RectInt(32, 190, 64, 50);
    public bool generateSideWalls = true;

    [Header("Visual Effects")]
    public bool enableJumpEffect = true;
    public int sprayWidth = 4;
    [Range(0f, 1f)] public float magneticPull = 0.35f;

    [Header("Events")]
    public UnityEvent<int> onSandPixelDrained;

    public int TotalInitialSand { get; private set; }

    private Texture2D sandTexture;
    private SpriteRenderer spriteRenderer;
    private int[,] grid;
    private Color[] colors;
    
    // Magnetism and Catch Area Tracking
    private int currentTargetX = -1;
    private int activeCatchLeft = -1;
    private int activeCatchRight = -1;

    void Start()
    {
        sandTexture = new Texture2D(width, height);
        sandTexture.filterMode = FilterMode.Point;
        colors = new Color[width * height];
        grid = new int[width, height];

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Sprite.Create(sandTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        spriteRenderer.sortingOrder = 10;

        BuildStopperFloor();
        SpawnInitialSand();
    }

    void BuildStopperFloor()
    {
        for (int x = 0; x < width; x++) grid[x, floorGridY] = 2;

        if (generateSideWalls)
        {
            int leftWall = Mathf.Max(0, spawnGridRect.x - 1);
            int rightWall = Mathf.Min(width - 1, spawnGridRect.x + spawnGridRect.width);
            for (int y = floorGridY; y < height; y++)
            {
                grid[leftWall, y] = 2;
                grid[rightWall, y] = 2;
            }
        }
    }

    public void OpenGate(int leftGridX, int rightGridX)
    {
        int startX = Mathf.Clamp(leftGridX, 0, width);
        int endX = Mathf.Clamp(rightGridX, 0, width);
        for (int x = startX; x < endX; x++)
        {
            if (grid[x, floorGridY] == 2) grid[x, floorGridY] = 0;
        }
    }

    public void CloseAllGates()
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, floorGridY] == 1 && floorGridY > 0 && grid[x, floorGridY - 1] == 0) grid[x, floorGridY - 1] = 1;
            grid[x, floorGridY] = 2;
        }
    }

    public void SetTargetX(int gridX) { currentTargetX = gridX; }
    public void ClearTarget() { currentTargetX = -1; }

    // NEW: Lets the simulation know where it is safe to permanently delete sand
    public void SetActiveCatchArea(int leftX, int rightX)
    {
        activeCatchLeft = leftX;
        activeCatchRight = rightX;
    }

    public void ClearActiveCatchArea()
    {
        activeCatchLeft = -1;
        activeCatchRight = -1;
    }

    void SpawnInitialSand()
    {
        TotalInitialSand = 0;
        for (int x = spawnGridRect.x; x < spawnGridRect.x + spawnGridRect.width; x++)
        {
            for (int y = spawnGridRect.y; y < spawnGridRect.y + spawnGridRect.height; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height && Random.value > 0.1f)
                {
                    grid[x, y] = 1;
                    TotalInitialSand++; 
                }
            }
        }
    }

    void Update()
    {
        timeAccumulator += Time.deltaTime;
        float stepSize = 1f / updatesPerSecond;
        bool didSimulate = false;

        while (timeAccumulator >= stepSize)
        {
            UpdateSandPhysics();
            timeAccumulator -= stepSize;
            didSimulate = true;
        }

        if (didSimulate) DrawSand();
    }

    void UpdateSandPhysics()
    {
        for (int y = 1; y < height; y++)
        {
            bool leftToRight = Random.value > 0.5f;

            for (int i = 0; i < width; i++)
            {
                int x = leftToRight ? i : width - 1 - i;

                if (grid[x, y] == 1)
                {
                    // DRAIN EVENT
                    if (y == 1 && grid[x, 0] == 0)
                    {
                        grid[x, y] = 0;

                        // Check if the sand fell into an active, open container
                        bool isCaught = (activeCatchLeft != -1 && x >= activeCatchLeft && x <= activeCatchRight);

                        if (isCaught)
                        {
                            onSandPixelDrained?.Invoke(x); // Record it
                        }
                        else
                        {
                            RespawnSand(); // Refund it back to the top!
                        }
                        continue;
                    }

                    if (y < floorGridY && enableJumpEffect)
                    {
                        if (y == floorGridY - 1)
                        {
                            int spreadDir = Random.value > 0.5f ? 1 : -1;
                            int jumpDist = Random.Range(1, sprayWidth + 1);
                            int newX = Mathf.Clamp(x + (spreadDir * jumpDist), 0, width - 1);
                            if (grid[newX, y] == 0) { grid[x, y] = 0; grid[newX, y] = 1; x = newX; }
                        }
                        else if (currentTargetX != -1 && Random.value < magneticPull)
                        {
                            int dir = (int)Mathf.Sign(currentTargetX - x);
                            if (dir != 0)
                            {
                                int nextX = x + dir;
                                if (nextX >= 0 && nextX < width && grid[nextX, y] == 0) { grid[x, y] = 0; grid[nextX, y] = 1; x = nextX; }
                            }
                        }
                    }

                    if (grid[x, y - 1] == 0) { grid[x, y] = 0; grid[x, y - 1] = 1; }
                    else
                    {
                        bool canGoLeft = x > 0 && grid[x - 1, y - 1] == 0;
                        bool canGoRight = x < width - 1 && grid[x + 1, y - 1] == 0;
                        if (canGoLeft && canGoRight) { if (Random.value > 0.5f) { grid[x, y] = 0; grid[x - 1, y - 1] = 1; } else { grid[x, y] = 0; grid[x + 1, y - 1] = 1; } }
                        else if (canGoLeft) { grid[x, y] = 0; grid[x - 1, y - 1] = 1; }
                        else if (canGoRight) { grid[x, y] = 0; grid[x + 1, y - 1] = 1; }
                    }
                }
            }
        }
    }

    // NEW: Teleports lost sand back into the static container so it can be poured again
    void RespawnSand()
    {
        int rx = Random.Range(spawnGridRect.x, spawnGridRect.x + spawnGridRect.width);
        int startY = spawnGridRect.y + spawnGridRect.height - 1;

        // Search downwards from the ceiling to find an empty spot on top of the pile
        for (int y = startY; y >= floorGridY; y--)
        {
            if (grid[rx, y] == 0)
            {
                grid[rx, y] = 1;
                return;
            }
        }
    }

    void DrawSand()
    {
        Color sandColor = new Color(1f, 1f, 1f);
        Color emptyColor = Color.clear;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++) { colors[y * width + x] = grid[x, y] == 1 ? sandColor : emptyColor; }
        }
        sandTexture.SetPixels(colors);
        sandTexture.Apply();
    }

    public float GridToWorldY(float gridY) { return transform.position.y + ((gridY - (height / 2f)) / pixelsPerUnit) * transform.lossyScale.y; }
    public int WorldToGridX(float worldX) { return Mathf.RoundToInt(((worldX - transform.position.x) / transform.lossyScale.x) * pixelsPerUnit + (width / 2f)); }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        float localWidth = width / pixelsPerUnit;
        float localHeight = height / pixelsPerUnit;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(localWidth, localHeight, 0));

        Gizmos.color = Color.red;
        float floorLocalY = (floorGridY - (height / 2f)) / pixelsPerUnit;
        Gizmos.DrawLine(new Vector3(-localWidth / 2f, floorLocalY, 0), new Vector3(localWidth / 2f, floorLocalY, 0));

        Gizmos.color = Color.yellow;
        float spawnLocalX = (spawnGridRect.x + spawnGridRect.width / 2f - width / 2f) / pixelsPerUnit;
        float spawnLocalY = (spawnGridRect.y + spawnGridRect.height / 2f - height / 2f) / pixelsPerUnit;
        float spawnLocalW = spawnGridRect.width / pixelsPerUnit;
        float spawnLocalH = spawnGridRect.height / pixelsPerUnit;
        Gizmos.DrawWireCube(new Vector3(spawnLocalX, spawnLocalY, 0), new Vector3(spawnLocalW, spawnLocalH, 0));

        if (generateSideWalls)
        {
            Gizmos.color = Color.cyan;
            float leftWallLocalX = (spawnGridRect.x - 1 - width / 2f) / pixelsPerUnit;
            float rightWallLocalX = (spawnGridRect.x + spawnGridRect.width - width / 2f) / pixelsPerUnit;
            Gizmos.DrawLine(new Vector3(leftWallLocalX, floorLocalY, 0), new Vector3(leftWallLocalX, localHeight / 2f, 0));
            Gizmos.DrawLine(new Vector3(rightWallLocalX, floorLocalY, 0), new Vector3(rightWallLocalX, localHeight / 2f, 0));
        }
    }
}