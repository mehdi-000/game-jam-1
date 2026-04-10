using UnityEngine;

public class Enemyspawner : MonoBehaviour
{
    [Header("Spawner Settings")] 
    public Transform player;
    public GameObject[] enemyPrefabs;
    public float spawnInterval = 1f;
    public float spawnRadiusX = 10f;
    private float spawnTimer = 0f;
    public float offsetUp = 1f;


    void Start()
    {
        
    }

    void Update()
    {
        if (player == null) return;
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }
    }

    void SpawnEnemy()
    {
        GameObject enemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        float offsetRight = Random.Range(spawnRadiusX * 1.2f, spawnRadiusX * 1.5f);
        Vector3 spawnPosition = new Vector3(player.position.x + offsetRight, player.position.y + Random.Range(0f, offsetUp), 0f);
        Instantiate(enemy, spawnPosition, Quaternion.identity);
    }
}
