using UnityEngine;

public class RobotRotate : MonoBehaviour
{
    public float rotationSpeed = 100f;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0,rotationSpeed * Time.deltaTime);
    }
}
