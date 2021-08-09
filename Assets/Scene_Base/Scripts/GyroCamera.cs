using UnityEngine;
using System.Collections;

//code for absolute position
//https://gist.github.com/kormyen/a1e3c144a30fc26393f14f09989f03e1#file-gyrocamera-cs-L6
//sensor fusion(not so good):
//https://github.com/unitycoder/UnitySensorFusion
public class GyroCamera : MonoBehaviour
{
    private Quaternion preGyro;
    private Quaternion destination;

    private Quaternion Rotation_Origin_Addend = Quaternion.Euler(0, 0, 180);
    private Quaternion Gyroscope_Attitude_Difference_Addend = Quaternion.Euler(180, 180, 0);

    // SETTINGS
    [SerializeField] private float _smoothing = 0.1f;


    void Awake()
    {
        Input.gyro.enabled = true;
    }

    private void OnEnable()
    {
        destination = transform.rotation;
        preGyro = Input.gyro.attitude;
    }

    private void Update()
    {
        ApplyGyroRotation();
        transform.rotation = Quaternion.Slerp(transform.rotation, destination, _smoothing);
    }

    private void ApplyGyroRotation()
    {
        Quaternion difference = Quaternion.Inverse(preGyro * Rotation_Origin_Addend) * Input.gyro.attitude * Gyroscope_Attitude_Difference_Addend;
        destination = destination * difference;
        preGyro = Input.gyro.attitude;
    }

}