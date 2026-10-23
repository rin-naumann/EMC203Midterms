using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// An endless ground. Fills tiles around the camera and clears the ones left behind.
/// Each cell always gets the same tile, picked from its coordinates.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Camera targetCamera;

    [Header("Tiles")]
    [SerializeField] private Sprite[] plainSprites;
    [SerializeField] private Sprite[] decoratedSprites;
    [Tooltip("Chance for a cell to use a decorated tile (0.1 = 10%)")]
    [SerializeField, Range(0f, 1f)] private float decoratedChance = 0.12f;

    [Header("Area")]
    [Tooltip("Extra cells filled past the screen edge")]
    [SerializeField] private int margin = 2;

    private Tile[] plainTiles;
    private Tile[] decoratedTiles;
    private int seed;

    // The cells currently filled
    private bool hasArea;
    private int minX, minY, maxX, maxY;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        // Tilemaps place Tile assets, so build them from the sprites
        plainTiles = MakeTiles(plainSprites);
        decoratedTiles = MakeTiles(decoratedSprites);
    }

    // Unsubscribe so the static event does not keep this object alive
    private void OnEnable() => GameManager.OnRunStarted += Rebuild;
    private void OnDisable() => GameManager.OnRunStarted -= Rebuild;

    private void Start() => Rebuild();

    // New run: new seed and a fresh ground
    private void Rebuild()
    {
        seed = Random.Range(int.MinValue, int.MaxValue);
        tilemap.ClearAllTiles();
        hasArea = false;
        Refresh();
    }

    // After the camera has moved
    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (targetCamera == null || tilemap == null) return;

        // Visible area in world units
        Vector3 center = targetCamera.transform.position;
        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        // Convert to cells and add the margin
        Vector3Int low = tilemap.WorldToCell(new Vector3(center.x - halfWidth, center.y - halfHeight, 0f));
        Vector3Int high = tilemap.WorldToCell(new Vector3(center.x + halfWidth, center.y + halfHeight, 0f));
        int newMinX = low.x - margin, newMinY = low.y - margin;
        int newMaxX = high.x + margin, newMaxY = high.y + margin;

        // Nothing to do if the camera is in the same cells
        if (hasArea && newMinX == minX && newMinY == minY && newMaxX == maxX && newMaxY == maxY) return;

        // Fill the new cells
        for (int x = newMinX; x <= newMaxX; x++)
            for (int y = newMinY; y <= newMaxY; y++)
            {
                if (hasArea && Inside(x, y, minX, minY, maxX, maxY)) continue;
                tilemap.SetTile(new Vector3Int(x, y, 0), PickTile(x, y));
            }

        // Clear the cells left behind
        if (hasArea)
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                {
                    if (Inside(x, y, newMinX, newMinY, newMaxX, newMaxY)) continue;
                    tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }

        minX = newMinX; minY = newMinY; maxX = newMaxX; maxY = newMaxY;
        hasArea = true;
    }

    private static bool Inside(int x, int y, int x0, int y0, int x1, int y1) => x >= x0 && x <= x1 && y >= y0 && y <= y1;

    private Tile PickTile(int x, int y)
    {
        uint hash = Hash(x, y, seed);

        // Low half picks plain or decorated, high half picks the tile
        bool decorated = decoratedTiles.Length > 0 && (hash & 0xFFFF) / 65535f < decoratedChance;
        Tile[] pool = decorated ? decoratedTiles : plainTiles;
        if (pool.Length == 0) return null;

        return pool[(int)((hash >> 16) % (uint)pool.Length)];
    }

    // The same cell and seed always give the same number
    private static uint Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)seed;
            h ^= (uint)x * 0x9E3779B1u;
            h = ((h << 13) | (h >> 19)) * 0x85EBCA6Bu;
            h ^= (uint)y * 0xC2B2AE35u;
            h ^= h >> 16; h *= 0x7FEB352Du;
            h ^= h >> 15; h *= 0x846CA68Bu;
            h ^= h >> 16;
            return h;
        }
    }

    private static Tile[] MakeTiles(Sprite[] sprites)
    {
        if (sprites == null) return new Tile[0];

        Tile[] tiles = new Tile[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            tiles[i] = ScriptableObject.CreateInstance<Tile>();
            tiles[i].sprite = sprites[i];
            tiles[i].colliderType = Tile.ColliderType.None;
        }
        return tiles;
    }
}