using UnityEngine;

public class MovedCubeAnomaly : MonoBehaviour
{
    public Transform cube;           // The cube to move
    public Transform destination;    // Where it moves to
    public float moveDuration = 1.0f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 _originalPos;
    private Quaternion _originalRot;
    private bool _moved;
    private bool _moving;

    private void Start()
    {
        if (cube != null)
        {
            _originalPos = cube.position;
            _originalRot = cube.rotation;
        }

        // Automatically move when the scene starts
        TriggerAnomaly();
    }

    private void Update()
    {
        // Press Space to "resolve" (move it back)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_moved) Restore();
            else TriggerAnomaly();
        }
    }

    public void TriggerAnomaly()
    {
        if (_moving || cube == null || destination == null) return;
        StartCoroutine(MoveCube(cube.position, destination.position, cube.rotation, destination.rotation, true));
    }

    public void Restore()
    {
        if (_moving || cube == null) return;
        StartCoroutine(MoveCube(cube.position, _originalPos, cube.rotation, _originalRot, false));
    }

    private System.Collections.IEnumerator MoveCube(Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot, bool forward)
    {
        _moving = true;
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float u = moveCurve.Evaluate(t / moveDuration);
            cube.position = Vector3.Lerp(fromPos, toPos, u);
            cube.rotation = Quaternion.Slerp(fromRot, toRot, u);
            yield return null;
        }

        cube.position = toPos;
        cube.rotation = toRot;
        _moving = false;
        _moved = forward;
    }
}
