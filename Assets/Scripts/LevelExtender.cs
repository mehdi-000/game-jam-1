using UnityEngine;

public class LevelExtender : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject levelPart;

    [SerializeField] private float spawnMetersAhead = 50f, levelLength = 1000f;
    public int currentLevelPartIndex = 0;

    void Update()
    {
        if (playerTransform == null)
            return;
        float spawnX = currentLevelPartIndex * levelLength;
        if (spawnX - playerTransform.position.x < spawnMetersAhead)
        {
            Debug.Log("Spawning Level");
            Instantiate(levelPart, new Vector3(spawnX, 0f, 0f), Quaternion.identity);
            currentLevelPartIndex++;
        }

        // Optional: Clean up old level parts that are far behind the player to save memory.
        float destroyX = (currentLevelPartIndex - 2) * levelLength; // Keep 2 parts behind the player.
        foreach (var oldPart in GameObject.FindGameObjectsWithTag("LevelPart"))
        {
            if (-1f < oldPart.transform.position.x && oldPart.transform.position.x < 1f) { return; } // Don't destroy the part if it's near the origin, which is where the player starts.

            else if (oldPart.transform.position.x < destroyX)
            {
                Destroy(oldPart);
            }
        }
    }
}
