using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using HTC.UnityPlugin.Vive;
using Newtonsoft.Json;
using ExtensionMethods;
using TMPro;

public class MainFlowControl : MonoBehaviour
{
    static int MainProgress = 0;
    public Afanty Afanty;
    public GameObject CameraUI;

    public Image BK_BKGD;
    public AnimationCurve fadeCurv;
    public PlayableDirector mainTimeline;
    public float globalAniTime, playbackTime;
    public GameObject mainCanvas, LHandCtrl, RHandCtrl, RHandCam, HandCamObj;
    CameraFunction HandCamFunction = null;
    AudioSource VoiceAS, movieSound;
    GameObject UI_cameraView, UI_timeSlider;
    bool animationPlayback = false;
    /// 相机锁定模式：0-自由移动旋转；1-完全固定；2-只能旋转
    int camLockState = 0;
    bool allowCamMove = true, allowCamRot = true;
    public GameObject[] tripodModeObjs, lockModeObjs, recModeObjs;
    //拍摄相关
    bool inRecMode = false, inPlaybackMode = false, playBackModePlaying = false;
    bool lastFrameLTriggerDown = false, lastFrameRTriggerDown = false;
    public GameObject recordLengthSpan;
    public Slider timeSlider;
    /// 总时间长度，通过MainTimeline的Duration来决定
    float totalTimelineLength;
    /// 目前已经完成拍摄的影片长度
    float RecordLength;
    List<HandCamRecord> HandCamRecArray = new List<HandCamRecord>(), tempHandCamRecArray = new List<HandCamRecord>();
    //回放相关
    public AnimationCurve PosX, PosY, PosZ, RotX, RotY, RotZ, RotW;
    public Dictionary<CameraPara, AnimationCurve> CameraParaCurve = new Dictionary<CameraPara, AnimationCurve>();
    GameObject timeFunctions;
    /// UI界面上的按钮，编号如下：0-到头；1-快退；2-逐帧退；3-播放/暂停；4-逐帧进；5-快进；6-到尾；7-清空；8-完成
    GameObject[] buttons = new GameObject[9];
    /// 当前活动按钮编号：0-到头；1-快退；2-逐帧退；3-播放/暂停；4-逐帧进；5-快进；6-到尾；7-清空；8-完成
    int currentButton = 3;
    Vector3[] originalScales = new Vector3[9];
    GameObject playBackMode_Indicator, FinishingHintBKGD, FinishingHintDot, TrashHintBKGD, TrashHintDot;
    public Sprite playButtSprite, pauseButtSprite;
    public RuntimeAnimatorController playIndicator, pauseIndicator;
    bool allowFinishing = false, ShowFinishConfWin = false, finishingConfirm = false, ShowTrashConfWin = false, trashConfirm = false;
    bool lastMerged = false;
    //最后电影院相关，时间关系全部用binding了

    public Material cinemaMaterial;
    public RenderTexture handCamRT;
    public GameObject congraText;
    public Transform cinemaCamPos;

    FadeFinFlag firstBKFade = new FadeFinFlag(), modeChangeBKFade = new FadeFinFlag(), lastBKFade = new FadeFinFlag();

    Vector3 RHandCamPosDampVel;
    public GameObject HelpMenu;

    /// 平滑度，值越大越平滑
    const float DampingTime = 0.1f;
    private JoystickButton joystickButton;
    private bool init = false;

    void Awake()
    {
        StartCoroutine(LoadAdditive());
    }

    IEnumerator LoadAdditive()
    {
        UnityEngine.XR.XRSettings.LoadDeviceByName("OpenVR");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Afanty", LoadSceneMode.Additive);

        yield return new WaitForEndOfFrame();
        UnityEngine.XR.XRSettings.enabled = true;

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        Init();
    }

    void Init()
    {
        Afanty = GameObject.Find("RootObject").GetComponent<Afanty>();
        VoiceAS = GameObject.Find("VoiceAS").GetComponent<AudioSource>();

        mainTimeline = Afanty.timeline;
        movieSound = Afanty.movieSound;
        movieSound.loop = false;

        MainProgress = 0;
        globalAniTime = -1;
        RecordLength = 0;

        RenderSettings.fog = true;
        RenderSettings.ambientSkyColor = new Color32(0xA9, 0xBF, 0xFF, 0xFF);
        RenderSettings.ambientEquatorColor = new Color32(0xC5, 0xA3, 0xFF, 0xFF);
        RenderSettings.ambientGroundColor = new Color32(0xFF, 0x64, 0x00, 0xFF);

        VRInit();

        init = true;
    }


    private void VRInit()
    {
        joystickButton = GetComponent<JoystickButton>();

        HandCamFunction = HandCamObj.GetComponent<CameraFunction>();
        //开始时隐藏手柄
        LHandCtrl.SetActive(false);
        RHandCam.SetActive(false);
        RHandCtrl.SetActive(false);
        transform.position = new Vector3(0, 0, 0);
        //隐藏相机模式标志
        foreach (var currObj in tripodModeObjs) currObj.SetActive(false);
        foreach (var currObj in lockModeObjs) currObj.SetActive(false);
        foreach (var currObj in recModeObjs) currObj.SetActive(false);
       
        //UI元素
        UI_cameraView = mainCanvas.transform.Find("CameraView").gameObject;
        UI_timeSlider = UI_cameraView.transform.Find("TimeSlider").gameObject;
        UI_cameraView.SetActive(false);
        UI_timeSlider.SetActive(false);
        modeChangeBKFade.done = true;
        timeFunctions = UI_cameraView.transform.Find("TimeFunctions").gameObject;
        timeFunctions.SetActive(false);

        buttons[0] = timeFunctions.transform.Find("func_ToStart").gameObject;
        buttons[1] = timeFunctions.transform.Find("func_FastBackward").gameObject;
        buttons[2] = timeFunctions.transform.Find("func_FrameBackward").gameObject;
        buttons[3] = timeFunctions.transform.Find("func_PlayPause").gameObject;
        buttons[4] = timeFunctions.transform.Find("func_FrameForward").gameObject;
        buttons[5] = timeFunctions.transform.Find("func_FastForward").gameObject;
        buttons[6] = timeFunctions.transform.Find("func_ToEnd").gameObject;
        buttons[7] = timeFunctions.transform.Find("func_Trash").gameObject;
        buttons[8] = timeFunctions.transform.Find("func_Finish").gameObject;

        for (int i = 0; i < 9; i++) originalScales[i] = buttons[i].transform.localScale;

        playBackMode_Indicator = timeFunctions.transform.Find("playBackMode_Indicator").gameObject;
        FinishingHintBKGD = UI_cameraView.transform.Find("FinishingHintBKGD").gameObject;
        FinishingHintDot = FinishingHintBKGD.transform.Find("FinishingHint/FinishingHintDot").gameObject;
        FinishingHintBKGD.SetActive(false);
        TrashHintBKGD = UI_cameraView.transform.Find("TrashHintBKGD").gameObject;
        TrashHintDot = TrashHintBKGD.transform.Find("TrashHint/TrashHintDot").gameObject;
        TrashHintBKGD.SetActive(false);

        totalTimelineLength = (float)mainTimeline.duration;
        timeSlider.maxValue = totalTimelineLength;

        congraText.SetActive(false);

        foreach (CameraPara p in CameraPara.GetValues(typeof(CameraPara)))
        {
            CameraParaCurve[p] = new AnimationCurve();
        }

        VideoCapture.Instance.Init();

        HandCamFunction.UpdateCameraUI = (CameraPara para, string content) =>
        {
            if (para == CameraPara.ShutterSpeed)
            {
                CameraUI.GetChildByName(para.ToString()).GetComponent<TextMeshPro>().text = "1/" + content;
            }
            else
            {
                CameraUI.GetChildByName(para.ToString()).GetComponent<TextMeshPro>().text = content;
            }
        };
        HandCamFunction.OnModeChange = (CameraMode mode) =>
        {
            CameraUI.GetChildByName("Mode").GetComponent<TextMeshPro>().text = mode.ToString();
        };
        HandCamFunction.OnChosenChange = (int index) =>
        {
            Transform indicator = CameraUI.GetChildByName("Indicator").transform;
            Vector3 initPos = new Vector3(-3.68f, 3.62f, 0);
            Vector3 diff = new Vector3(0, 1.8f, 0);
            indicator.localPosition = initPos - index * diff;
        };

        HandCamFunction.Init();

        //根据起始时的头部高度决定场景高度
        Afanty.setRootPos(new Vector3(0.76f, UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head).y - 0.3f, 0.6f));

    }

    void Update()
    {
        if (!init)
            return;

        switch (MainProgress)
        {
#if DEVELOPMENT
            case 0: //开场标题淡入
                StartCoroutine("TargetAlphaChange", new ImageAlphaFadePara(BK_BKGD, 0f, 1f, 2f, fadeCurv, firstBKFade));
                MainProgress = 300;
                break;
#else
            //TODO:新手引导
#endif
            case 300: //进入拍摄阶段
                //设为不在拍摄状态
                inRecMode = false;  //设置为不在拍摄状态
                foreach (var currObj in recModeObjs) currObj.SetActive(false);     //隐藏全部正在拍摄状态提示

                //初始化所有相关参数
                RecordLength = 0;   //总已拍摄长度
                timeSlider.value = 0;   //时间滑块进度
                recordLengthSpan.transform.localScale = new Vector3(0, 1, 1);  //总已拍摄长度的进度条长度
                HandCamRecArray = new List<HandCamRecord>();    //建立新的相机信息列表
                movieSound.Stop();  //停止播放声音
                globalAniTime = 0;  //全局时间归零
                aniPause(); //停止播放动画
                MainProgress = 302;

                break;
            case 301:  //等待语音提示播放完毕 
                break;
            case 302:   //主拍摄阶段
                //显示手柄、虚拟相机与UI控件
                UI_cameraView.SetActive(true);  //显示UI
                RHandCam.SetActive(true);       //显示右手虚拟相机
                LHandCtrl.SetActive(true);      //显示左手手柄
                UI_timeSlider.SetActive(true);  //显示进度条

                if (!inPlaybackMode)
                {
                    Shooting();
                    timeFunctions.SetActive(false);
                }
                else
                {
                    if (modeChangeBKFade.done)
                    {
                        timeFunctions.SetActive(true);
                        Playback();
                    }
                }
                break;
            case 400:  //拍摄结束，开始黑场淡入
                UI_cameraView.SetActive(false);
                StartCoroutine("TargetAlphaChange", new ImageAlphaFadePara(BK_BKGD, 1f, 0f, 1f, fadeCurv));
                StartCoroutine("editMainProgress", new editMainProgressPara(1.2f, 402));
                MainProgress = 401;
                break;
            case 401:   //等待黑场淡入完成
                break;
            case 402:   //跳到影院内，开始黑场淡出
                Camera.main.transform.SetParent(cinemaCamPos);
                RenderSettings.fog = false;
                RenderSettings.ambientSkyColor = RenderSettings.ambientEquatorColor = RenderSettings.ambientGroundColor = Color.black;
                StartCoroutine("TargetAlphaChange", new ImageAlphaFadePara(BK_BKGD, 0f, 1f, 1f, fadeCurv));
                StartCoroutine("editMainProgress", new editMainProgressPara(1.2f, 404));
                MainProgress = 403;
                break;
            case 403:   //等待黑场淡出完成
                break;
            case 404:   
                VideoCapture.Instance.StartCapture();
                cinemaMaterial.SetTexture("_MainTex", handCamRT);
                playbackTime = 2;
                aniGoto(playbackTime);
                movieSound.Play();
                movieSound.time = playbackTime;
                MainProgress = 406;
                break;

            case 406: //播放动画
                if (playbackTime + Time.deltaTime <= RecordLength)
                    playbackTime += Time.deltaTime;
                else
                {
                    playbackTime = RecordLength;
                    movieSound.Stop();
                }
                updatePlaybackTime();

                if (playbackTime == RecordLength)
                {
                    MainProgress = 407;
                    System.IO.File.WriteAllText(Application.persistentDataPath + "/CameraPara.json", JsonConvert.SerializeObject(HandCamRecArray));
                }

                break;

            default:
                break;
        }

        //更新手柄位置角度
        if (!inPlaybackMode)
        {
            LHandCtrl.transform.localPosition = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.LeftHand);
            LHandCtrl.transform.localRotation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.LeftHand) * Quaternion.Euler(0f, 180f, 0f);
            if (allowCamMove)
                RHandCam.transform.localPosition = Vector3.SmoothDamp(RHandCam.transform.localPosition, UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.RightHand), ref RHandCamPosDampVel, DampingTime);
            if (allowCamRot)
                RHandCam.transform.localRotation = Quaternion.Slerp(RHandCam.transform.localRotation, UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.RightHand), 0.05f);
        }


        //记录前一帧左右手按钮状态记录
        if (Input.GetAxis("LTrigger") == 1) lastFrameLTriggerDown = true; else lastFrameLTriggerDown = false;
        if (Input.GetAxis("RTrigger") == 1) lastFrameRTriggerDown = true; else lastFrameRTriggerDown = false;

        CameraUI.transform.LookAt(Camera.main.gameObject.transform);
        //更新动画播放
        aniUpdate();
        Afanty.SetAniTime(globalAniTime);
    }


    /// 在拍摄方式（非回放方式）下执行的所有指令
    void Shooting()
    {
        HandCamFunction.ChangeCameraParaInVR(joystickButton);

        //定位相机锁定相关标志
        tripodModeObjs[0].transform.position = HandCamObj.transform.parent.position + new Vector3(0, 0.075f, 0);
        tripodModeObjs[0].transform.LookAt(Camera.main.gameObject.transform);
        tripodModeObjs[0].transform.Rotate(new Vector3(0, 180, 0));
        lockModeObjs[0].transform.position = HandCamObj.transform.parent.position + new Vector3(0, 0.075f, 0);
        lockModeObjs[0].transform.LookAt(Camera.main.gameObject.transform);
        lockModeObjs[0].transform.Rotate(new Vector3(0, 180, 0));

        //相机锁定相关
        if (Input.GetButtonDown("RTrackpadPress"))
        {
            camLockState = camLockState == 2 ? 0 : camLockState + 1;
            if (camLockState == 0)
            {
                allowCamMove = allowCamRot = true;
                foreach (var currObj in lockModeObjs) currObj.SetActive(false);
                foreach (var currObj in tripodModeObjs) currObj.SetActive(false);
            }
            else if (camLockState == 1)
            {
                allowCamMove = allowCamRot = false;
                foreach (var currObj in tripodModeObjs) currObj.SetActive(false);
                foreach (var currObj in lockModeObjs) currObj.SetActive(true);
            }
            else if (camLockState == 2)
            {
                allowCamMove = false; allowCamRot = true;
                foreach (var currObj in tripodModeObjs) currObj.SetActive(true);
                foreach (var currObj in lockModeObjs) currObj.SetActive(false);
            }
        }

        //开始停止拍摄
        if (Input.GetAxis("RTrigger") == 1)
        {
            playbackTime = globalAniTime;
            //加入已经完成了完整长度的拍摄
            if (globalAniTime == totalTimelineLength)
            {
                RecordLength = totalTimelineLength;
                inRecMode = false;
                aniPause();
                //隐藏所有与录像有关的视觉元素
                foreach (var currObj in recModeObjs) currObj.SetActive(false);
            }
            else
            {
                mainTimeline.Play();
                inRecMode = true;
                //显示所有与录像有关的视觉元素
                foreach (var currObj in recModeObjs) currObj.SetActive(true);
                //在按下后的那一帧，创建一个新的tempHandCamRecArray临时相机信息存储列表、动画跳至globalAniTime并开始播放
                if (!lastFrameRTriggerDown)
                {
                    tempHandCamRecArray = new List<HandCamRecord>();
                    aniGoto(globalAniTime);
                    aniPlay();
                }
                //记录当前相机信息
                tempHandCamRecArray.Add(new HandCamRecord(
                    globalAniTime, RHandCam.transform.localPosition, RHandCam.transform.localRotation,
                    HandCamFunction.ApertureLevel, HandCamFunction.ISOLevel, HandCamFunction.ShutterLevel, HandCamFunction.FocalLength, HandCamFunction.FocusDistance));
                //若当前时间大于系统内记录的总时间，则更新总时间
                if (globalAniTime > RecordLength) RecordLength = globalAniTime;
                //将“是否已经合并了临时相机信息列表”的Flag设为否
                lastMerged = false;
            }
        }
        //松开录像按钮后
        else
        {
            inRecMode = false;
            foreach (var currObj in recModeObjs) currObj.SetActive(false);
            aniPause();
            if (lastFrameRTriggerDown && !lastMerged)
            {
                mergeHandCamRec();
                lastMerged = true;
            }

            MoveTimeSlideInShoot();
        }

        //切换到Playback模式
        if (modeChangeBKFade.done && Input.GetAxis("LTrigger") != 1 && lastFrameLTriggerDown && RecordLength > 0)
        {
            inRecMode = false;

            foreach (var currObj in recModeObjs) currObj.SetActive(false);

            foreach (var currObj in tripodModeObjs) if (currObj.GetComponent<RawImage>() != null) currObj.GetComponent<RawImage>().enabled = false;
            foreach (var currObj in lockModeObjs) if (currObj.GetComponent<RawImage>() != null) currObj.GetComponent<RawImage>().enabled = false;

            aniPause();

            inPlaybackMode = true;

            StartCoroutine("TargetAlphaChange", new ImageAlphaFadePara(BK_BKGD, 0.9f, 0f, 0.3f, fadeCurv, modeChangeBKFade));
            UI_cameraView.GetComponent<Animator>().SetBool("InPlaybackMode", true);

            resetAllButtonStats();
            currentButton = 3;

            PosX = new AnimationCurve();
            PosY = new AnimationCurve();
            PosZ = new AnimationCurve();
            RotX = new AnimationCurve();
            RotY = new AnimationCurve();
            RotZ = new AnimationCurve();
            RotW = new AnimationCurve();
            foreach (CameraPara p in CameraPara.GetValues(typeof(CameraPara)))
                CameraParaCurve[p] = new AnimationCurve();

            foreach (var currData in HandCamRecArray)
            {
                PosX.AddKey(currData.time, currData.HandCamPos.x);
                PosY.AddKey(currData.time, currData.HandCamPos.y);
                PosZ.AddKey(currData.time, currData.HandCamPos.z);
                RotX.AddKey(currData.time, currData.HandCamRot.x);
                RotY.AddKey(currData.time, currData.HandCamRot.y);
                RotZ.AddKey(currData.time, currData.HandCamRot.z);
                RotW.AddKey(currData.time, currData.HandCamRot.w);
                CameraParaCurve[CameraPara.Aperture].AddKey(currData.time, currData.ApertureLevel);
                CameraParaCurve[CameraPara.ShutterSpeed].AddKey(currData.time, currData.ShutterSpeedLevel);
                CameraParaCurve[CameraPara.ISO].AddKey(currData.time, currData.ISOLevel);
                CameraParaCurve[CameraPara.FocalLength].AddKey(currData.time, currData.FocalLength);
                CameraParaCurve[CameraPara.FocusDistance].AddKey(currData.time, currData.FocusDistance);
            }

            if (RecordLength == totalTimelineLength && MainProgress == 302) allowFinishing = true;
            playbackTime = globalAniTime;
            updatePlaybackTime();
        }

        //更新已记录长度与进度条
        recordLengthSpan.transform.localScale = new Vector3(RecordLength / totalTimelineLength, 1, 1);
        timeSlider.value = globalAniTime;

        //呼叫帮助界面
        if (Input.GetAxisRaw("LGrip") == 1)
        {
            HelpMenu.SetActive(true);
        }
        else
        {
            HelpMenu.SetActive(false);
        }
    }

    /// 在回放方式（非拍摄方式）下执行的所有指令
    void Playback()
    {
        timeSlider.value = playbackTime;

        if (playBackModePlaying)
        {
            if (playbackTime + Time.deltaTime <= RecordLength)
                playbackTime += Time.deltaTime;
            else
            {
                playbackTime = RecordLength;
                playBackModePlaying = false;
                playBackMode_Indicator.GetComponent<Animator>().runtimeAnimatorController = pauseIndicator;
                buttons[3].GetComponent<SpriteRenderer>().sprite = playButtSprite;
                movieSound.Stop();
            }

            updatePlaybackTime();
        }


        //切换到普通模式
        if (modeChangeBKFade.done && Input.GetAxis("LTrigger") != 1 && lastFrameLTriggerDown && !ShowFinishConfWin)
        {
            switchToShootingMode();
        }

        //直到录满才允许结束
        resetAllButtonStats();

        if (ShowFinishConfWin || ShowTrashConfWin)
        {
            if (ShowFinishConfWin)
            {
                if (finishingConfirm)
                    FinishingHintDot.transform.localPosition = new Vector3(-90.55f, -22.2f, 0);
                else
                    FinishingHintDot.transform.localPosition = new Vector3(-20.7f, -22.2f, 0);
                if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Right))
                    finishingConfirm = false;
                else if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Left))
                    finishingConfirm = true;
                if (Input.GetAxis("RTrigger") != 1 && lastFrameRTriggerDown && finishingConfirm)
                {
                    MainProgress = 400;
                }
                else if (Input.GetAxis("RTrigger") != 1 && lastFrameRTriggerDown && !finishingConfirm)
                {
                    FinishingHintBKGD.SetActive(false);
                    ShowFinishConfWin = false;
                }
            }
            if (ShowTrashConfWin)
            {
                if (trashConfirm)
                    TrashHintDot.transform.localPosition = new Vector3(-43.1f, -22.2f, 0);
                else
                    TrashHintDot.transform.localPosition = new Vector3(26.7f, -22.2f, 0);
                if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Right))
                    trashConfirm = false;
                else if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Left))
                    trashConfirm = true;
                if (Input.GetAxis("RTrigger") != 1 && lastFrameRTriggerDown && trashConfirm)
                {
                    TrashHintBKGD.SetActive(false);
                    ShowTrashConfWin = false;
                    allowFinishing = true;

                    {
                        RecordLength = 0;   //总已拍摄长度
                        timeSlider.value = 0;   //时间滑块进度
                        recordLengthSpan.transform.localScale = new Vector3(0, 1, 1);  //总已拍摄长度的进度条长度
                        HandCamRecArray = new List<HandCamRecord>();    //建立新的相机信息列表
                        movieSound.Stop();  //停止播放声音
                        globalAniTime = 0;  //全局时间归零
                        aniPause(); //停止播放动画
                    }

                    switchToShootingMode();
                }
                else if (Input.GetAxis("RTrigger") != 1 && lastFrameRTriggerDown && !trashConfirm)
                {
                    TrashHintBKGD.SetActive(false);
                    ShowTrashConfWin = false;
                }
            }
        }
        else
        {
            //按钮切换
            if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Right))
            {
                currentButton = currentButton == 8 ? 0 : currentButton + 1;
            }
            else if (joystickButton.GetKeyDown(HandRole.RightHand, ButtonDirection.Left))
            {
                currentButton = currentButton == 0 ? 8 : currentButton - 1;
            }
            //按钮功能
            buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
            buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);

            switch (currentButton)
            {
                case 0: //到头
                    if (Input.GetAxis("RTrigger") == 1)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.1f;
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                        if (lastFrameRTriggerDown)
                        {
                            playbackTime = 0;
                            updatePlaybackTime();
                        }
                    }
                    break;
                case 1: //快退
                    if (Input.GetAxis("RTrigger") > 0)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color(1, 1 - Input.GetAxis("RTrigger"), 1 - Input.GetAxis("RTrigger"), 1);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * Mathf.Lerp(1.2f, 1.2f, Input.GetAxis("RTrigger"));
                        playbackTime = playbackTime - 0.5f * Input.GetAxis("RTrigger") < 0 ? 0 : playbackTime - 0.5f * Input.GetAxis("RTrigger");
                        updatePlaybackTime();
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                    }
                    break;
                case 2: //帧退
                    if (Input.GetAxis("RTrigger") == 1)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.1f;
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                        if (lastFrameRTriggerDown)
                        {
                            playbackTime = playbackTime - 0.0333333333333333f * 5 < 0 ? 0 : playbackTime - 0.0333333333333333f * 5;
                            updatePlaybackTime();
                        }
                    }
                    break;
                case 3: //播放暂停
                    if (Input.GetAxis("RTrigger") != 1 && lastFrameRTriggerDown)
                    {
                        if (playBackModePlaying)
                        {
                            playBackModePlaying = false;
                            playBackMode_Indicator.GetComponent<Animator>().runtimeAnimatorController = pauseIndicator;
                            buttons[currentButton].GetComponent<SpriteRenderer>().sprite = playButtSprite;
                            movieSound.Pause();
                        }
                        else
                        {
                            playBackModePlaying = true;
                            playBackMode_Indicator.GetComponent<Animator>().runtimeAnimatorController = playIndicator;
                            buttons[currentButton].GetComponent<SpriteRenderer>().sprite = pauseButtSprite;
                            movieSound.time = playbackTime;
                            movieSound.Play();
                        }
                    }
                    if (Input.GetAxis("RTrigger") == 1)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.1f;
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                    }
                    break;
                case 4: //帧进
                    if (Input.GetAxis("RTrigger") == 1)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.1f;
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                        if (lastFrameRTriggerDown)
                        {
                            playbackTime = playbackTime + 0.0333333333333333f * 5 > RecordLength ? RecordLength : playbackTime + 0.0333333333333333f * 5;
                            updatePlaybackTime();
                        }
                    }
                    break;
                case 5: //快进
                    if (Input.GetAxis("RTrigger") > 0)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color(1, 1 - Input.GetAxis("RTrigger"), 1 - Input.GetAxis("RTrigger"), 1);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * Mathf.Lerp(1.2f, 1.2f, Input.GetAxis("RTrigger"));
                        playbackTime = playbackTime + 0.5f * Input.GetAxis("RTrigger") > RecordLength ? RecordLength : playbackTime + 0.5f * Input.GetAxis("RTrigger");
                        updatePlaybackTime();
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                    }
                    break;
                case 6: //到尾
                    if (Input.GetAxis("RTrigger") == 1)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.1f;
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                        if (lastFrameRTriggerDown)
                        {
                            playbackTime = RecordLength;
                            updatePlaybackTime();
                        }
                    }
                    break;
                case 7: //清空
                    if (Input.GetAxis("RTrigger") == 1)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.1f;
                        if (playBackModePlaying)
                        {
                            playBackModePlaying = false;
                            playBackMode_Indicator.GetComponent<Animator>().runtimeAnimatorController = pauseIndicator;
                            buttons[3].GetComponent<SpriteRenderer>().sprite = playButtSprite;
                            movieSound.Pause();
                        }
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                    }
                    if (Input.GetAxis("RTrigger") != 1 && lastFrameRTriggerDown)
                    {
                        TrashHintBKGD.SetActive(true);
                        ShowTrashConfWin = true;
                    }
                    break;
                case 8: //完成
                    if (playBackModePlaying)
                    {
                        playBackModePlaying = false;
                        playBackMode_Indicator.GetComponent<Animator>().runtimeAnimatorController = pauseIndicator;
                        buttons[3].GetComponent<SpriteRenderer>().sprite = playButtSprite;
                        movieSound.Pause();
                    }
                    if (Input.GetAxis("RTrigger") == 1)
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.1f;
                        if (playBackModePlaying)
                        {
                            playBackModePlaying = false;
                            playBackMode_Indicator.GetComponent<Animator>().runtimeAnimatorController = pauseIndicator;
                            buttons[3].GetComponent<SpriteRenderer>().sprite = playButtSprite;
                            movieSound.Pause();
                        }
                    }
                    else
                    {
                        buttons[currentButton].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
                        buttons[currentButton].transform.localScale = originalScales[currentButton] * 1.2f;
                    }
                    if (Input.GetAxis("RTrigger") != 1 && lastFrameRTriggerDown)
                    {
                        FinishingHintBKGD.SetActive(true);
                        ShowFinishConfWin = true;
                        finishingConfirm = false;
                    }
                    break;
                default:
                    break;
            }
        }

    }

    private void MoveTimeSlideInShoot()
    {
        if (joystickButton.GetKeyDown(HandRole.LeftHand, ButtonDirection.Left))
        {
            playbackTime = playbackTime - 0.0333333333333333f * 5 < 0 ? 0 : playbackTime - 0.0333333333333333f * 5;
            aniGoto(playbackTime);
        }
        if (joystickButton.GetKeyDown(HandRole.LeftHand, ButtonDirection.Right))
        {
            playbackTime = playbackTime + 0.0333333333333333f * 5 > RecordLength ? RecordLength : playbackTime + 0.0333333333333333f * 5;
            aniGoto(playbackTime);
        }
    }

    /// 从回放模式切换到拍摄模式
    void switchToShootingMode()
    {
        inPlaybackMode = false;
        playBackModePlaying = false;
        playBackMode_Indicator.GetComponent<Animator>().runtimeAnimatorController = pauseIndicator;
        buttons[3].GetComponent<SpriteRenderer>().sprite = playButtSprite;

        StartCoroutine("TargetAlphaChange", new ImageAlphaFadePara(BK_BKGD, 0.0f, 0f, 0.3f, fadeCurv, modeChangeBKFade));
        UI_cameraView.GetComponent<Animator>().SetBool("InPlaybackMode", false);
        foreach (var currObj in tripodModeObjs) if (currObj.GetComponent<RawImage>() != null) currObj.GetComponent<RawImage>().enabled = true;
        foreach (var currObj in lockModeObjs) if (currObj.GetComponent<RawImage>() != null) currObj.GetComponent<RawImage>().enabled = true;
    }

    void mergeHandCamRec()
    {

        if (HandCamRecArray.Count == 0 || tempHandCamRecArray[0].time > HandCamRecArray[HandCamRecArray.Count - 1].time)
        {
            HandCamRecArray.AddRange(tempHandCamRecArray);
            return;
        }

        float minNew = 0, maxNew = 0;
        int minOldInd = 0, maxOldInd = 0;
        bool minFound = false, maxFound = false;
        minNew = tempHandCamRecArray[0].time;
        maxNew = tempHandCamRecArray[tempHandCamRecArray.Count - 1].time;
        for (int i = 0; i < HandCamRecArray.Count; i++)
        {
            if (!minFound && HandCamRecArray[i].time >= minNew)
            {
                minOldInd = Mathf.Max(i - 1, 0);
                minFound = true;
            }

            if (!maxFound && HandCamRecArray[i].time > maxNew)
            {
                maxOldInd = i;
                maxFound = true;
                break;
            }
        }

        if (!maxFound && HandCamRecArray.Count > 0) maxOldInd = HandCamRecArray.Count - 1;

        if (HandCamRecArray.Count > 0) HandCamRecArray.RemoveRange(minOldInd, maxOldInd - minOldInd + 1);
        HandCamRecArray.InsertRange(minOldInd, tempHandCamRecArray);
    }

    void updatePlaybackTime()
    {
        RHandCam.transform.localPosition = new Vector3(PosX.Evaluate(playbackTime), PosY.Evaluate(playbackTime), PosZ.Evaluate(playbackTime));
        RHandCam.transform.localRotation = new Quaternion(RotX.Evaluate(playbackTime), RotY.Evaluate(playbackTime), RotZ.Evaluate(playbackTime), RotW.Evaluate(playbackTime));

        HandCamFunction.ApertureLevel = (int)CameraParaCurve[CameraPara.Aperture].Evaluate(playbackTime);
        HandCamFunction.ShutterLevel = (int)CameraParaCurve[CameraPara.ShutterSpeed].Evaluate(playbackTime);
        HandCamFunction.ISOLevel = (int)CameraParaCurve[CameraPara.ISO].Evaluate(playbackTime);
        HandCamFunction.FocalLength = CameraParaCurve[CameraPara.FocalLength].Evaluate(playbackTime);
        HandCamFunction.FocusDistance = CameraParaCurve[CameraPara.FocusDistance].Evaluate(playbackTime);

        aniGoto(playbackTime);
    }

    /// 将所有播放界面上按钮恢复到初始状态
    void resetAllButtonStats()
    {
        for (int i = 0; i < 9; i++)
        {
            buttons[i].transform.localScale = originalScales[i];
            buttons[i].GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 150);
        }
        //根据是否已经拍完了来决定最后一个“完成”按钮的显示颜色
        buttons[8].GetComponent<SpriteRenderer>().color = allowFinishing ? new Color32(255, 255, 255, 150) : new Color32(255, 255, 255, 25);
    }

    void aniPlay()
    {
        animationPlayback = true;
        if (!movieSound.isPlaying && globalAniTime < totalTimelineLength) movieSound.Play();
    }

    /// 暂定动画
    void aniPause()
    {
        animationPlayback = false;
        if (movieSound.isPlaying) movieSound.Pause();
    }

    void aniUpdate()
    {
        if (animationPlayback)
        {
            globalAniTime = globalAniTime + Time.deltaTime < totalTimelineLength ? globalAniTime + Time.deltaTime : totalTimelineLength;
        }
    }

    /// 将动画跳转到指定时间
    private void aniGoto(float targetTime)
    {
        if (targetTime < 0)
            globalAniTime = movieSound.time = 0;
        else if (targetTime > totalTimelineLength)
            globalAniTime = movieSound.time = totalTimelineLength;
        else
            globalAniTime = movieSound.time = targetTime;
        if (!movieSound.isPlaying && animationPlayback && globalAniTime < totalTimelineLength) movieSound.Play();
    }

    /// 用于在指定的时间后修改MainProgress值的协程
    IEnumerator editMainProgress(editMainProgressPara inPara)
    {
        yield return new WaitForSeconds(inPara.waitSec);
        MainProgress = inPara.targetProgress;
    }

    /// 用于改变UI元素透明度渐变的的协程
    IEnumerator TargetAlphaChange(ImageAlphaFadePara inPara)
    {
        if (inPara._EndFlagTraced) inPara._DoneFlag.done = false;
        float currAlpha, originalAlpha;
        currAlpha = originalAlpha = inPara._Img.color.a;
        float passedTime = inPara._DelayTime;
        do
        {
            currAlpha = Mathf.Lerp(originalAlpha, inPara._TargetAlpha, passedTime / inPara._TotalTime);
            inPara._Img.color = new Color(inPara._Img.color.r, inPara._Img.color.g, inPara._Img.color.b, inPara._Curv.Evaluate(currAlpha));
            passedTime += Time.deltaTime;
            yield return null;
        }
        while (Mathf.Abs(currAlpha - inPara._TargetAlpha) > 0.001f);
        if (inPara._EndFlagTraced) inPara._DoneFlag.done = true;
    }
}