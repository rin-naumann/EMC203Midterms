using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// An endless ground. Keeps the area around the camera (plus a margin) filled with tiles and clears the tiles
/// that fall behind, so the Tilemap never holds more than a screenful no matter how far the player walks.
/// Which tile goes in a cell is decided by a hash of the cell's coordinates, not by remembering what was placed,
/// so nothing is stored and a cell always gets the same tile.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [Tooltip("Leave empty to use the main camera.")]
    [SerializeField] private Camera targetCamera;

    [Header("Tiles")]
    [Tooltip("The clean ground tiles (the top 4 rows of the sheet).")]
    [SerializeField] private Sprite[] plainSprites;
    [Tooltip("The tiles with pebbles, flowers or stone (the bottom 4 rows).")]
    [SerializeField] private Sprite[] decoratedSprites;
    [Tooltip("Chance for a cell to use a decorated tile. 0.1 is 10%.")]
    [SerializeField, Range(0f, 1f)] private float decoratedChance = 0.12f;

    [Header("Area")]
    [Tooltip("Extra cells filled beyond the screen edge on every side, so a tile exists before it can be seen.")]
    [SerializeField] private int margin = 2;

    private Tile[] plainTiles;
    private Tile[] decoratedTiles;
    private int seed;

    // The cells currently filled, as an inclusive rectangle.
    private bool hasArea;
    private int minX, minY, maxX, maxY;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        // The Tilemap places Tile assets, not sprites. Building them here means you can drag the sliced sprites
        // straight into the lists, with no Tile Palette step.
        plainTiles = MakeTiles(plainSprites);
        decoratedTiles = MakeTiles(decoratedSprites);
    }

    // Static events outlive this object, so always unsubscribe in OnDisable.
    private void OnEnable() => GameManager.OnRunStarted += Rebuild;
    private void OnDisable() => GameManager.OnRunStarted -= Rebuild;

    private void Start() => Rebuild();

    // New run: a new seed gives a different ground, and the old tiles are thrown away.
    private void Rebuild()
    {
        seed = Random.Range(int.MinValue, int.MaxValue);
        tilemap.ClearAllTiles();
        hasArea = false;
        Refresh();
    }

    // After the camera has moved this frame, so the ground is never a frame behind it.
    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (targetCamera == null || tilemap == null) return;

        // The visible area in world units, from the camera. Read every frame, so a resized window just works.
        Vector3 center = targetCamera.transform.position;
        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        // To cells, then widened by the margin.
        Vector3Int low = tilemap.WorldToCell(new Vector3(center.x - halfWidth, center.y - halfHeight, 0f));
        Vector3Int high = tilemap.WorldToCell(new Vector3(center.x + halfWidth, center.y + halfHeight, 0f));
        int newMinX = low.x - margin, newMinY = low.y - margin;
        int newMaxX = high.x + margin, newMaxY = high.y + margin;

        // Most frames the camera is still inside the same cells, so there is nothing to do.
        if (hasArea && newMinX == minX && newMinY == minY && newMaxX == maxX && newMaxY == maxY) return;

        // Fill the cells that are new...
        for (int x = newMinX; x <= newMaxX; x++)
            for (int y = newMinY; y <= newMaxY; y++)
            {
                if (hasArea && Inside(x, y, minX, minY, maxX, maxY)) continue;
                tilemap.SetTile(new Vector3Int(x, y, 0), PickTile(x, y));
            }

        // ...and clear the ones that were left behind.
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

        // Two independent-looking pieces of the same hash: the low half decides plain or decorated, the high half which tile.
        bool decorated = decoratedTiles.Length > 0 && (hash & 0xFFFF) / 65535f < decoratedChance;
        Tile[] pool = decorated ? decoratedTiles : plainTiles;
        if (pool.Length == 0) return null;

        return pool[(int)((hash >> 16) % (uint)pool.Length)];
    }

    // Turns a cell and the seed into a well-scrambled number. The same input always gives the same output.
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