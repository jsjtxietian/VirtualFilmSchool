using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using HTC.UnityPlugin.Vive;
using ExtensionMethods;
using UnityEngine.Events;


public class HandCamRecord
{
    /// 时间
    public float time;

    /// 对应时间的空间位置
    public Vector3 HandCamPos;

    /// 对应时间的旋转角度
    public Vector4 HandCamRot;

    public int ApertureLevel;
    public int ISOLevel;
    public int ShutterSpeedLevel;
    public float FocalLength;
    public float FocusDistance;

    public HandCamRecord(float inTime, Vector3 inHandCamPos, Quaternion inHandCamRot, int aperture, int iso, int ss, float fl, float fd)
    {
        HandCamRot.x = inHandCamRot.x;
        HandCamRot.y = inHandCamRot.y;
        HandCamRot.z = inHandCamRot.z;
        HandCamRot.w = inHandCamRot.w;

        time = inTime;
        HandCamPos = inHandCamPos;
        ApertureLevel = aperture;
        ISOLevel = iso;
        ShutterSpeedLevel = ss;
        FocalLength = fl;
        FocusDistance = fd;
    }
}

public enum CameraPara
{
    FocusDistance,
    FocalLength,
    Aperture,
    ShutterSpeed,
    ISO,
}

public enum CameraMode
{
    // Auto,
    A,
    S,
    M
}

// NOTE(xietian):如果直接用physical camera会有bug，镜头里面的阴影会闪动
public class CameraFunction : MonoBehaviour
{
    private List<float> apertureValues = new List<float> { 1, 1.4f, 2, 2.8f, 4, 5.6f, 8, 11, 16, 22, 32, 45, 64 }; //size: 13
    private List<float> shutterValues = new List<float> { 2000, 1000, 500, 250, 125, 60, 30, 15, 8, 4, 2, 1 };//size:11
    private List<float> isoValues = new List<float> { 25, 50, 100, 200, 400, 800, 1600, 3200, 6400 };//size:9
    private List<CameraPara> currentParas = new List<CameraPara>();
    private int currentParaIndex = 0;
    private DepthOfField depthOfField;
    private ColorGrading colorGrading;
    private Camera controlledCamera;
    private CameraMode _currentMode;


    public Action<CameraPara, string> UpdateCameraUI = null;
    public Action<CameraMode> OnModeChange = null;
    public Action<int> OnChosenChange = null;



    public CameraMode CurrentMode
    {
        get => _currentMode;
        private set
        {
            _currentMode = value;
            UpdateCamParaList();
        }
    }

    public float Aperture
    {
        get => apertureValues[ApertureLevel];
    }

    private int _apertureLevel;
    public int ApertureLevel
    {
        get => _apertureLevel;
        set
        {
            int diff = Mathf.Clamp(value, 0, apertureValues.Count - 1) - _apertureLevel;

            if (CurrentMode == CameraMode.A)
            {
                //光圈到极值，不管
                if (diff == 0)
                    return;

                ShutterLevel += diff;
            }
            else if (CurrentMode == CameraMode.S)
            {
                if (diff == 0)
                    ISOLevel -= value - _apertureLevel;
            }

            _apertureLevel = Mathf.Clamp(value, 0, apertureValues.Count - 1);
            UpdateCameraUI?.Invoke(CameraPara.Aperture, Aperture.ToString());
            UpdateCamPara();
        }

    }


    public float ISO
    {
        get => isoValues[_isoLevel];
    }

    private int _isoLevel;

    public int ISOLevel
    {
        get => _isoLevel;
        set
        {
            _isoLevel = Mathf.Clamp(value, 0, isoValues.Count - 1);
            UpdateCameraUI?.Invoke(CameraPara.ISO, ISO.ToString());
            UpdateCamPara();
        }
    }

    public float ShutterSpeed
    {
        get => 1 / shutterValues[_shutterLevel];
    }

    private int _shutterLevel;
    public int ShutterLevel
    {
        get => _shutterLevel;
        set
        {
            int diff = Mathf.Clamp(value, 0, shutterValues.Count - 1) - _shutterLevel;

            if (CurrentMode == CameraMode.A)
            {
                if (diff == 0)
                    ISOLevel += value - _shutterLevel;
            }
            else if (CurrentMode == CameraMode.S)
            {
                if (diff == 0)
                    return;
                ApertureLevel += diff;
            }

            _shutterLevel = Mathf.Clamp(value, 0, shutterValues.Count - 1);
            UpdateCameraUI?.Invoke(CameraPara.ShutterSpeed, shutterValues[_shutterLevel].ToString());
            UpdateCamPara();
        }
    }

    public float FocalLength
    {
        get => 15.2f / (2 * Mathf.Tan(Mathf.Deg2Rad * controlledCamera.fieldOfView / 2));
        set
        {
            float f = Mathf.Clamp(value, 14, 190);
            controlledCamera.fieldOfView = 2 * Mathf.Rad2Deg * Mathf.Atan(15.2f / (2 * f));
            UpdateCameraUI?.Invoke(CameraPara.FocalLength, ((int)f).ToString());
        }
    }

    private float _focusDistance;
    public float FocusDistance
    {
        get => _focusDistance;
        set
        {
            _focusDistance = Mathf.Clamp(value, 0.01f, 10f);
            UpdateCameraUI?.Invoke(CameraPara.FocusDistance, FocusDistance.ToString("f2"));
            UpdateCamPara();
        }
    }

    void Awake()
    {
        PostProcessVolume postProcessVolume = GetComponent<PostProcessVolume>();
        depthOfField = postProcessVolume.profile.GetSetting<DepthOfField>();
        colorGrading = postProcessVolume.profile.GetSetting<ColorGrading>();
        controlledCamera = GetComponent<Camera>();
    }

    public void Init()
    {
        CurrentMode = CameraMode.M;
        ISOLevel = 2;
        ShutterLevel = 6;
        ApertureLevel = 3;
        FocusDistance = 5;
        FocalLength = 15.2f / (2 * Mathf.Tan(Mathf.Deg2Rad * controlledCamera.fieldOfView / 2));
    }

    private void UpdateCamPara()
    {
        //magic number保证初始曝光为0,int截断保证整数
        //TODO:检查下int截断，是不是最好找到最近的整数而不是截断
        float exposure = (int)((Mathf.Log(ISO * ShutterSpeed / (Aperture * Aperture), 2)) + 1.25f);
        colorGrading.postExposure.value = exposure;
        depthOfField.aperture.value = Aperture;
        depthOfField.focalLength.value = FocalLength;
        depthOfField.focusDistance.value = FocusDistance;
    }

    public void ChangeCameraParaInVR(JoystickButton joystickButton)
    {
        //change camera mode
        if (ViveInput.GetPressDownEx(HandRole.RightHand, ControllerButton.Grip) || Input.GetKeyDown(KeyCode.G))
        {
            CurrentMode = CurrentMode.CircleNext();
            OnModeChange?.Invoke(CurrentMode);
            locateIndicator();
        }

        //change focus
        if (ViveInput.GetPressDownEx(HandRole.RightHand, ControllerButton.BKey) || Input.GetKeyDown(KeyCode.B))
            AutoFoucus();

        //change camera para
        if (ViveInput.GetPressDownEx(HandRole.RightHand, ControllerButton.AKey) || Input.GetKeyDown(KeyCode.A))
        {
            currentParaIndex = (currentParaIndex + 1) % (currentParas.Count);
            locateIndicator();
        }

        switch (currentParas[currentParaIndex])
        {
            case CameraPara.Aperture:
                if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Up))
                    ApertureLevel++;
                else if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Down))
                    ApertureLevel--;
                break;
            case CameraPara.ISO:
                if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Up))
                    ISOLevel++;
                else if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Down))
                    ISOLevel--;
                break;
            case CameraPara.ShutterSpeed:
                if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Up))
                    ShutterLevel++;
                else if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Down))
                    ShutterLevel--;
                break;
            case CameraPara.FocusDistance:
                float fdChange = Input.GetAxis("RTrackpadY");
                if (Mathf.Abs(fdChange) > 0.6f)
                    FocusDistance += fdChange * 0.01f;
                break;
            case CameraPara.FocalLength:
                float flChange = Input.GetAxis("RTrackpadY");
                if (flChange > 0.6f) FocalLength += Time.deltaTime * 15;
                else if (flChange < -0.6f) FocalLength -= Time.deltaTime * 15;
                break;
        }
    }

    public void AutoFoucus(GameObject model = null)
    {
        if (model != null)
        {
            FocusDistance = Vector3.Distance(controlledCamera.transform.position, model.transform.position);
        }
        else
        {
            RaycastHit hit;
            Vector2 v = new Vector2(controlledCamera.targetTexture.width / 2, controlledCamera.targetTexture.height / 2); //屏幕中心点
            // LineRenderer lr = GetComponent<LineRenderer>();
            // lr.positionCount = 2;
            // lr.SetPosition(0, controlledCamera.transform.position);
            // lr.SetPosition(1, controlledCamera.transform.position + controlledCamera.transform.forward * 5);

            // layer mask is 1, only check for default layer
            if (Physics.Raycast(controlledCamera.ScreenPointToRay(v), out hit, 20f, 1))
            {
                FocusDistance = hit.distance;
            }
            else
            {
                Debug.Log("failed to focus!");
                FocusDistance = 5;
            }
        }
    }

    private void UpdateCamParaList()
    {
        currentParas.Clear();

        currentParas.Add(CameraPara.FocusDistance);
        currentParas.Add(CameraPara.FocalLength);

        switch (CurrentMode)
        {
            case CameraMode.A:
                currentParas.Add(CameraPara.Aperture);
                break;
            case CameraMode.S:
                currentParas.Add(CameraPara.ShutterSpeed);
                break;
            case CameraMode.M:
                currentParas.Add(CameraPara.Aperture);
                currentParas.Add(CameraPara.ShutterSpeed);
                currentParas.Add(CameraPara.ISO);
                break;
        }

        currentParaIndex = Mathf.Clamp(currentParaIndex, 0, currentParas.Count - 1);
    }

    private void locateIndicator()
    {
        OnChosenChange?.Invoke(currentParas[currentParaIndex].GetIndex());
    }

}
