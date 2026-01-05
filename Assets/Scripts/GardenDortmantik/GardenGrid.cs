using UnityEngine;
using System.Collections.Generic;

namespace GardenDortmantik
{
    public class GardenGrid : MonoBehaviour
    {
        public int gridWidth = 10;
        public int gridHeight = 10;
        public float tileSize = 1.1f;

        private Dictionary<Vector2Int, GardenTile> tiles = new Dictionary<Vector2Int, GardenTile>();
        private HashSet<Vector2Int> validPlacements = new HashSet<Vector2Int>();

        public void Initialize()
        {
            // Start with center position as valid
            validPlacements.Add(Vector2Int.zero);
        }

        public bool CanPlaceTile(Vector2Int position)
        {
            return validPlacements.Contains(position) && !tiles.ContainsKey(position);
        }

        public GardenTile PlaceTile(GardenTileType type, Vector2Int position)
        {
            if (!CanPlaceTile(position))
                return null;

            GameObject tileObj = new GameObject($"Tile_{position.x}_{position.y}");
            tileObj.transform.parent = transform;
            tileObj.transform.position = GridToWorld(position);

            GardenTile tile = tileObj.AddComponent<GardenTile>();
            tile.Initialize(type, position);

            tiles[position] = tile;
            validPlacements.Remove(position);

            // Add adjacent positions as valid placements
            Vector2Int[] neighbors = GetNeighborPositions(position);
            foreach (var neighbor in neighbors)
            {
                if (!tiles.ContainsKey(neighbor))
                {
                    validPlacements.Add(neighbor);
                }
            }

            return tile;
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / tileSize),
                Mathf.RoundToInt(worldPos.y / tileSize)
            );
        }

        public Vector2Int[] GetNeighborPositions(Vector2Int position)
        {
            return new Vector2Int[]
            {
                position + Vector2Int.up,
                position + Vector2Int.down,
                position + Vector2Int.left,
                position + Vector2Int.right
            };
        }

        public GardenTile[] GetNeighborTiles(Vector2Int position)
        {
            Vector2Int[] positions = GetNeighborPositions(position);
            GardenTile[] neighbors = new GardenTile[4];

            for (int i = 0; i < 4; i++)
            {
                tiles.TryGetValue(positions[i], out neighbors[i]);
            }

            return neighbors;
        }

        public HashSet<Vector2Int> GetValidPlacements()
        {
            return validPlacements;
        }

        public int GetTileCount()
        {
            return tiles.Count;
        }
    }
}
