using UnityEngine;

public class DragOnFlyBehavior : MonoBehaviour
{
    [SerializeField] private float movementBurstDistance = 0.5f;
    [SerializeField] private float moveInterval = 1f;
    [SerializeField] private float moveSpeed = 5f;

    private Vector3 targetPosition;
    private float moveTimer;

    private void Start()
    {
        targetPosition = transform.position;
        moveTimer = moveInterval;
    }

    private void Update()
    {
        moveTimer -= Time.deltaTime;

        if (moveTimer <= 0f)
        {
            moveTimer = moveInterval;

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(0f, movementBurstDistance);
            Vector3 offset = new Vector3(randomDirection.x, randomDirection.y, 0f) * randomDistance;

            targetPosition = transform.position + offset;
            targetPosition.z = transform.position.z;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
}