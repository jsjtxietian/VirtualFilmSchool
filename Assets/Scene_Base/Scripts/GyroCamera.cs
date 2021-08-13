using UnityEngine;
using System.Collections;

//code for absolute position
//https://gist.github.com/kormyen/a1e3c144a30fc26393f14f09989f03e1#file-gyrocamera-cs-L6
//sensor fusion(not so good):
//https://github.com/unitycoder/UnitySensorFusion
public class GyroCamera : MonoBehaviour
{
    private float initY;
    private GameObject proxyGameObject;

    // SETTINGS
    [SerializeField] private float _smoothing = 0.1f;


    void Awake()
    {
        Input.gyro.enabled = true;
    }

    private void OnEnable()
    {
        if (proxyGameObject == null)
            proxyGameObject = new GameObject("GyroRotationProxy");

        setProxyRotation();
        initY = transform.rotation.eulerAngles.y - proxyGameObject.transform.eulerAngles.y;
    }

    private void Update()
    {
        ApplyGyroRotation();
        transform.rotation = Quaternion.Slerp(transform.rotation, proxyGameObject.transform.rotation, _smoothing);
    }

    private void ApplyGyroRotation()
    {
        setProxyRotation();
        proxyGameObject.transform.Rotate(0, initY, 0, Space.World);
    }

    private void setProxyRotation()
    {
        proxyGameObject.transform.rotation = Input.gyro.attitude;
        proxyGameObject.transform.Rotate(0f, 0f, 180f, Space.Self); // Swap "handedness" of quaternion from gyro.
        proxyGameObject.transform.Rotate(90f, 180f, 0f, Space.World); // Rotate to make sense as a camera pointing out the back of your device.
    }

}