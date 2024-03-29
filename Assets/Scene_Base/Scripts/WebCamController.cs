﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExtensionMethods;
using UnityEngine.EventSystems;
using Newtonsoft.Json;

//Attach to the shooting camera (FPS camera)
public class WebCamController : MonoBehaviour
{
    [SerializeField]
    private GameObject CameraUI;
    [SerializeField]
    private GameObject TransformControl;
    [SerializeField]
    private GameObject FPS;
    [SerializeField]
    private GameObject ViewFinder;
    [SerializeField]
    private float CamMoveSpeed;
    [SerializeField]
    private float FLSensitivity;
    [SerializeField]
    private GameObject directorCam;
    private CameraFunction shootCam;
    //Camera shooting
    private List<HandCamRecord> HandCamRecArray = new List<HandCamRecord>();

    //Camera UI Control
    private bool isFLInDrag;
    private Slider fdSlider;
    private bool isInFPS;

    //Transform Gizmos Control
    private bool isGizmosInDrag;
    private GameObject gizmos;
    private Vector3 originScreenPos, originMousePos, originShootCamPos;
    private int z1, z2;


    void Update()
    {
        MoveCamera(isInFPS ? transform : directorCam.transform);

        if (!isInFPS)
        {
            Ray ray = directorCam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            GameObject hittedObject;

            if (Input.GetMouseButtonDown(0))
            {

                if (Physics.Raycast(ray, out hit))
                {
                    hittedObject = hit.collider.gameObject;
                    if (hittedObject.name.Contains("Gizmos"))
                    {
                        gizmos = hittedObject;
                        isGizmosInDrag = true;
                        originScreenPos = directorCam.GetComponent<Camera>().WorldToScreenPoint(transform.position);
                        originMousePos = Input.mousePosition;
                        originShootCamPos = transform.position;

                        //只在按下去的瞬间判断鼠标和相机中心的位置关系
                        z1 = Input.mousePosition.x - originScreenPos.x < 0 ? -1 : 1;
                        z2 = Input.mousePosition.y - originScreenPos.y < 0 ? 1 : -1;
                    }
                }
            }

            if (isGizmosInDrag && Input.GetMouseButton(0))
            {
                if (gizmos.name.Contains("Position"))
                {
                    //获取屏幕空间鼠标增量，并加上拖拽物原始位置（屏幕空间计算）
                    Vector3 newScreenPos = originScreenPos + Input.mousePosition - originMousePos;
                    //将屏幕空间坐标转换为世界空间
                    Vector3 newWorldPos = directorCam.GetComponent<Camera>().ScreenToWorldPoint(newScreenPos);
                    //将世界空间位置赋予拖拽物
                    if (gizmos.name.Contains("X"))
                        transform.localPosition = Vector3.Project(transform.position, transform.forward) + Vector3.Project(transform.position, transform.up) + Vector3.Project(newWorldPos, transform.right);
                    else if (gizmos.name.Contains("Y"))
                        transform.localPosition = Vector3.Project(transform.position, transform.forward) + Vector3.Project(newWorldPos, transform.up) + Vector3.Project(transform.position, transform.right);
                    else if (gizmos.name.Contains("Z"))
                        transform.localPosition = Vector3.Project(newWorldPos, transform.forward) + Vector3.Project(transform.position, transform.up) + Vector3.Project(transform.position, transform.right);
                }
                else if (gizmos.name.Contains("Rotation"))
                {
                    bool angleLessThan90 = Vector3.Angle(gizmos.transform.forward, directorCam.transform.forward) < 90;

                    if (gizmos.name.Contains("X"))
                    {
                        Vector2 angle = CalcRotation(angleLessThan90 ? 1 : -1);
                        transform.Rotate(angle.x + angle.y, 0, 0);
                    }
                    else if (gizmos.name.Contains("Y"))
                    {
                        Vector2 angle = CalcRotation(angleLessThan90 ? -1 : 1);
                        transform.Rotate(0, -angle.x - angle.y, 0);
                    }
                    else if (gizmos.name.Contains("Z"))
                    {
                        Vector2 angle = CalcRotation(angleLessThan90 ? 1 : -1);
                        transform.Rotate(0, 0, -angle.x - angle.y);
                    }
                }
                else if (gizmos.name.Contains("Plane"))
                {
                    Vector3 newPos = originScreenPos + Input.mousePosition - originMousePos;
                    Vector3 newWorldPos = directorCam.GetComponent<Camera>().ScreenToWorldPoint(newPos);
                    transform.position = newWorldPos;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isGizmosInDrag = false;
            }
        }

    }

    public void ChangeCamParaInWeb()
    {
        shootCam.FocusDistance += 0.1f * Input.GetAxis("Mouse ScrollWheel");

        if (isFLInDrag == true)
        {
            shootCam.FocalLength += (fdSlider.value - 0.5f) * Time.deltaTime * 35;
        }
        else
        {
            if (fdSlider.value > 0.5f)
                fdSlider.value -= Time.deltaTime * 1.5f;
            if (fdSlider.value < 0.5f)
                fdSlider.value += Time.deltaTime * 1.5f;
        }
    }

    public void Record(float time)
    {
        if (HandCamRecArray.Count > 1)
        {
            if (HandCamRecArray[HandCamRecArray.Count - 1].time > time)
            {
                int i = HandCamRecArray.FindLastIndex((a) => { return a.time < time; });
                HandCamRecArray.RemoveRange(i + 1, HandCamRecArray.Count - 1 - i);
            }
        }
        HandCamRecArray.Add(new HandCamRecord(
                                time,
                                transform.localPosition,
                                transform.localRotation,
                                shootCam.ApertureLevel,
                                shootCam.ISOLevel,
                                shootCam.ShutterLevel,
                                shootCam.FocalLength,
                                shootCam.FocusDistance));
    }

    public void ReplayCam(float time)
    {
        //TODO:是不是还是用animation curve来deal with掉帧问题
        int i = Mathf.Max(HandCamRecArray.FindLastIndex((a) => { return a.time < time; }), 0);
        float timeDelta = Mathf.Abs(HandCamRecArray[i].time - time);
        if (i + 1 != HandCamRecArray.Count)
        {
            float nextTimeDelta = Mathf.Abs(HandCamRecArray[i + 1].time - time);
            if (timeDelta > nextTimeDelta)
                i = i + 1;
        }

        HandCamRecord record = HandCamRecArray[i];
        transform.localPosition = record.HandCamPos;
        transform.localRotation = new Quaternion(record.HandCamRot.x, record.HandCamRot.y, record.HandCamRot.z, record.HandCamRot.w);

        shootCam.ApertureLevel = record.ApertureLevel;
        shootCam.ShutterLevel = record.ShutterSpeedLevel;
        shootCam.ISOLevel = record.ISOLevel;
        shootCam.FocalLength = record.FocalLength;
        shootCam.FocusDistance = record.FocusDistance;
    }

    public void Init()
    {
        shootCam = GetComponent<CameraFunction>();
        FLSensitivity = 1 / shootCam.FocalLength * FLSensitivity;
        InitCam(); DirectorMode();
    }

    private void InitCam()
    {
        ExtensionClass.BindEventToUIButton("ViewFinder/ChangeToFPS", FPSMode);
        ExtensionClass.BindEventToUIButton("FPS/ChangeToDirector", DirectorMode);
        ExtensionClass.BindEventToUIButton("CamPara/Aperture/Left", () => { shootCam.ApertureLevel--; });
        ExtensionClass.BindEventToUIButton("CamPara/Aperture/Right", () => { shootCam.ApertureLevel++; });
        ExtensionClass.BindEventToUIButton("CamPara/ShutterSpeed/Left", () => { shootCam.ShutterLevel--; });
        ExtensionClass.BindEventToUIButton("CamPara/ShutterSpeed/Right", () => { shootCam.ShutterLevel++; });
        ExtensionClass.BindEventToUIButton("CamPara/ISO/Left", () => { shootCam.ISOLevel--; });
        ExtensionClass.BindEventToUIButton("CamPara/ISO/Right", () => { shootCam.ISOLevel++; });
        ExtensionClass.BindEventToUIButton("CamPara/AF-S", () => { shootCam.AutoFoucus(); });

        shootCam.UpdateCameraUI = (CameraPara para, string content) =>
        {
            switch (para)
            {
                case CameraPara.Aperture:
                    CameraUI.GetChildByName(para.ToString()).GetComponentInChildren<Text>().text = "F " + content;
                    break;
                case CameraPara.ShutterSpeed:
                    CameraUI.GetChildByName(para.ToString()).GetComponentInChildren<Text>().text = "1/" + content;
                    break;
                case CameraPara.ISO:
                    CameraUI.GetChildByName(para.ToString()).GetComponentInChildren<Text>().text = "ISO " + content;
                    break;
                case CameraPara.FocalLength:
                    CameraUI.GetChildByName(para.ToString()).GetComponentInChildren<Text>().text = content + " mm";
                    break;
                case CameraPara.FocusDistance:
                    CameraUI.GetChildByName(para.ToString()).GetComponentInChildren<Text>().text = content + "m";
                    break;
            }
        };

        CameraUI.GetChildByName("FocusDistance/Slider/Scrollbar").
            GetComponent<Scrollbar>().onValueChanged.AddListener(
                value => { shootCam.FocusDistance += value - 0.5f; }
            );

        fdSlider = CameraUI.GetChildByName("FocalLength/Slider").GetComponent<Slider>();
        ExtensionClass.AddEventTriggerListener(fdSlider.GetComponent<EventTrigger>(),
            EventTriggerType.BeginDrag,
            (e) => { isFLInDrag = true; });
        ExtensionClass.AddEventTriggerListener(fdSlider.GetComponent<EventTrigger>(),
            EventTriggerType.EndDrag,
            (e) => { isFLInDrag = false; });

        shootCam.Init();
    }


    private void FPSMode()
    {
        isInFPS = true;
        FPS.SetActive(true);
        TransformControl.SetActive(false);
        ViewFinder.SetActive(false);
    }

    private void DirectorMode()
    {
        isInFPS = false;
        FPS.SetActive(false);
        TransformControl.SetActive(true);
        ViewFinder.SetActive(true);
    }

    private void MoveCamera(Transform cam)
    {

        if (Input.GetKey(KeyCode.W))
        {
            cam.Translate(Vector3.forward * Time.deltaTime * CamMoveSpeed);
        }
        if (Input.GetKey(KeyCode.S))
        {
            cam.Translate(Vector3.forward * -Time.deltaTime * CamMoveSpeed);
        }
        if (Input.GetKey(KeyCode.A))
        {
            cam.Translate(Vector3.left * Time.deltaTime * CamMoveSpeed);
        }
        if (Input.GetKey(KeyCode.D))
        {
            cam.Translate(Vector3.right * Time.deltaTime * CamMoveSpeed);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            cam.position += new Vector3(0, 0.5f, 0) * Time.deltaTime * CamMoveSpeed;
        }
        if (Input.GetKey(KeyCode.E))
        {
            cam.position -= new Vector3(0, 0.5f, 0) * Time.deltaTime * CamMoveSpeed;
        }

        if (Input.GetMouseButton(1))
        {
            float x = cam.rotation.eulerAngles.x;
            float y = cam.rotation.eulerAngles.y;
            float z = cam.rotation.eulerAngles.z;

            float deltx = Input.GetAxis("Mouse Y") * FLSensitivity;
            x -= deltx;
            float delta = Input.GetAxis("Mouse X") * FLSensitivity;
            float rotationY = y + delta;
            cam.localEulerAngles = new Vector3(x, rotationY, z);
        }
    }

    private Vector2 CalcRotation(int i)
    {
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");
        return new Vector2(x * i * z2, y * i * z1);
    }
}
