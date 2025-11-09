using System;
using UnityEngine;

public class ObjectRotate : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public bool X_Rotate = false;
    public bool Y_Rotate = false;
    public bool Z_Rotate = false;
    private float X_Speed = 0f;
    private float Y_Speed = 0f;
    private float Z_Speed = 0f;
    // Update is called once per frame
    void Update()
    {
        // 속도 적용여부
        if (X_Rotate)
            X_Speed = rotationSpeed;
        else
            X_Speed = 0f;
        if (Y_Rotate)
            Y_Speed = rotationSpeed;
        else
            Y_Speed = 0f;
        if (Z_Rotate)
            Z_Speed = rotationSpeed;
        else
            Z_Speed = 0f;

        //회전 적용
        transform.Rotate(X_Speed, Y_Speed, Z_Speed);
    }
}
