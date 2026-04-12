using UnityEngine;

public class Enemyspanwner : MonoBehaviour
{
    [Header("Distance")]
    [Tooltip("Horizontal distance the player must travel before the first enemy can appear.")]
    public float initialDistanceBeforeFirstSpawn = 8f;

    [Tooltip("Random gap (world X) after each spawn before the next one is allowed.")]
    public float minDistanceBetweenSpawns = 11f;

    public float maxDistanceBetweenSpawns = 23f;

    [Header("Spawn placement")]
    [Tooltip("Enemy prefers to spawn this far ahead of the player on X (min/max). Always clamped past the camera's right edge.")]
    public float spawnAheadMin = 16f;

    public float spawnAheadMax = 48f;

    [Tooltip("Random extra height above the player (never below).")]
    public float verticalSpread = 6f;

    [Tooltip("Extra world units past the camera's right edge so the enemy is clearly off-screen.")]
    public float spawnOutsideMargin = 2f;

    [Header("References")]
    public Transform player;
    public GameObject[] enemyPrefabs;

    [Tooltip("If unset, uses Camera.main.")]
    public Camera spawnCamera;

    private float nextSpawnTriggerX;
    private int lastSpawnIndex = -1;

    void Start()
    {
        if (player == null) return;
        nextSpawnTriggerX = player.position.x + initialDistanceBeforeFirstSpawn;
    }

    void Update()
    {
        if (player == null || enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        if (player.position.x < nextSpawnTriggerX) return;

        SpawnEnemy();
        nextSpawnTriggerX = player.position.x + Random.Range(minDistanceBetweenSpawns, maxDistanceBetweenSpawns);
    }

    void SpawnEnemy()
    {
        int n = enemyPrefabs.Length;
        int index;
        if (n <= 1)
        {
            index = 0;
        }
        else
        {
            do
            {
                index = Random.Range(0, n);
            } while (index == lastSpawnIndex);
        }

        lastSpawnIndex = index;
        GameObject enemy = enemyPrefabs[index];
        float ahead = Random.Range(spawnAheadMin, spawnAheadMax);
        float spawnX = player.position.x + ahead;
        float spawnZ = 0f;

        Camera cam = spawnCamera != null ? spawnCamera : Camera.main;
        if (cam != null)
        {
            float minX = GetCameraRightWorldX(cam, spawnZ) + spawnOutsideMargin;
            spawnX = Mathf.Max(spawnX, minX);
        }

        float y = player.position.y + Random.Range(0f, verticalSpread);
        Vector3 spawnPosition = new Vector3(spawnX, y, spawnZ);
        Instantiate(enemy, spawnPosition, Quaternion.identity);
    }

    static float GetCameraRightWorldX(Camera cam, float worldZ)
    {
        if (cam.orthographic)
            return cam.transform.position.x + cam.orthographicSize * cam.aspect;

        Ray ray = cam.ViewportPointToRay(new Vector3(1f, 0.5f));
        float dz = ray.direction.z;
        if (Mathf.Abs(dz) < 1e-4f)
            return cam.transform.position.x;
        float t = (worldZ - ray.origin.z) / dz;
        return ray.origin.x + ray.direction.x * t;
    }
}
