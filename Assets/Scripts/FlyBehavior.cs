using UnityEngine;

public class FlyBehavior : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float directionChangeInterval = 1f;

    private Vector2 currentDirection;
    private float directionChangeTimer;

    private void Start()
    {
        PickNewDirection();
    }

    private void Update()
    {
        directionChangeTimer -= Time.deltaTime;

        if (directionChangeTimer <= 0f)
        {
            PickNewDirection();
        }

        Vector3 movement = new Vector3(currentDirection.x, currentDirection.y, 0f) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }

    private void PickNewDirection()
    {
        Vector2 newDirection;

        do
        {
            newDirection = Random.insideUnitCircle;
        }
        while (newDirection.sqrMagnitude < 0.01f);

        currentDirection = newDirection.normalized;
        directionChangeTimer = Random.Range(directionChangeInterval * 0.5f, directionChangeInterval * 1.5f);
    }
}