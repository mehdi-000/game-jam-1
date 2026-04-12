using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[ExecuteAlways]
public class GroundShapeController : MonoBehaviour
{
    private const int CornersCount = 2;

    [SerializeField] private SpriteShapeController spriteShapeController;
    [SerializeField] private GameObject wavePointPrefab;
    [SerializeField] private GameObject wavePointsParent;

    [SerializeField] [Range(1, 100)] private int wavesCount = 40;

    public float springStiffness = 0.1f;
    public float dampening = 0.03f;
    public float spread = 0.006f;
    public float impactMultiplier = 0.05f;

    private List<GroundSpring> springs = new();

    private void OnValidate()
    {
        StartCoroutine(CreateWaves());
    }

    private IEnumerator CreateWaves()
    {
        foreach (Transform child in wavePointsParent.transform)
            StartCoroutine(DestroyDelayed(child.gameObject));

        yield return null;
        SetWaves();
        yield return null;
    }

    private IEnumerator DestroyDelayed(GameObject go)
    {
        yield return null;
        DestroyImmediate(go);
    }

    private void SetWaves()
    {
        Spline spline = spriteShapeController.spline;
        int pointCount = spline.GetPointCount();

        for (int i = CornersCount; i < pointCount - CornersCount; i++)
            spline.RemovePointAt(CornersCount);

        Vector3 topLeft = spline.GetPosition(1);
        Vector3 topRight = spline.GetPosition(2);
        float width = topRight.x - topLeft.x;
        float spacing = width / (wavesCount + 1);

        for (int i = wavesCount; i > 0; i--)
        {
            int index = CornersCount;
            float xPos = topLeft.x + spacing * i;
            Vector3 point = new Vector3(xPos, topLeft.y, topLeft.z);
            spline.InsertPointAt(index, point);
            spline.SetHeight(index, 0.1f);
            spline.SetCorner(index, false);
            spline.SetTangentMode(index, ShapeTangentMode.Continuous);
        }

        springs = new List<GroundSpring>();

        for (int i = 0; i <= wavesCount + 1; i++)
        {
            int index = i + 1;
            Smoothen(spline, index);

            GameObject wavePoint = Instantiate(wavePointPrefab, wavePointsParent.transform, false);
            wavePoint.transform.localPosition = spline.GetPosition(index);

            GroundSpring spring = wavePoint.GetComponent<GroundSpring>();
            spring.Init(spriteShapeController);
            springs.Add(spring);
        }
    }

    private void Smoothen(Spline spline, int index)
    {
        Vector3 position = spline.GetPosition(index);
        Vector3 positionPrev = position;
        Vector3 positionNext = position;

        if (index > 1)
            positionPrev = spline.GetPosition(index - 1);
        if (index - 1 <= wavesCount)
            positionNext = spline.GetPosition(index + 1);

        Vector3 forward = gameObject.transform.forward;
        float scale = Mathf.Min(
            (positionNext - position).magnitude,
            (positionPrev - position).magnitude
        ) * 0.33f;

        SplineUtility.CalculateTangents(
            position, positionPrev, positionNext, forward, scale,
            out Vector3 rightTangent, out Vector3 leftTangent
        );

        spline.SetLeftTangent(index, leftTangent);
        spline.SetRightTangent(index, rightTangent);
    }

    private void FixedUpdate()
    {
        foreach (GroundSpring spring in springs)
        {
            spring.WaveSpringUpdate(springStiffness, dampening);
            spring.WavePointUpdate();
        }

        UpdateSprings();
    }

    private void UpdateSprings()
    {
        int count = springs.Count;
        float[] leftDeltas = new float[count];
        float[] rightDeltas = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                leftDeltas[i] = spread * (springs[i].height - springs[i - 1].height);
                springs[i - 1].velocity += leftDeltas[i];
            }
            if (i < count - 1)
            {
                rightDeltas[i] = spread * (springs[i].height - springs[i + 1].height);
                springs[i + 1].velocity += rightDeltas[i];
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (springs.Count == 0) return;

        ContactPoint contact = collision.contacts[0];
        Vector3 localContact = spriteShapeController.transform.InverseTransformPoint(contact.point);

        int nearest = 0;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < springs.Count; i++)
        {
            float dist = Mathf.Abs(springs[i].transform.localPosition.x - localContact.x);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = i;
            }
        }

        float impactSpeed = collision.relativeVelocity.y;
        springs[nearest].velocity += impactSpeed * impactMultiplier;
    }
}
