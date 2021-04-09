using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoCapture
{
    static VideoCapture instance;

    public static VideoCapture Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new VideoCapture();
            }
            return instance;
        }
    }

    //AVProMovieCapture related implemention
    private GameObject VidCapCam;
    private AVProMovieCaptureFromCamera VidCap;

    public void Init()
    {
        VidCapCam = GameObject.Find("VidCapCam");
        VidCap = VidCapCam.GetComponent<AVProMovieCaptureFromCamera>();
        VidCapCam.SetActive(false);
    }

    public void StartCapture()
    {
        VidCapCam.SetActive(true);
        // Audio has been configured in Window/AV Movie Capture
        // AVProUnityAudioCapture a = mainCamera.AddComponent<AVProUnityAudioCapture>();
        // VidCap._audioCapture = a;
        // VidCap._forceFilename = System.DateTime.UtcNow.ToString();
        VidCap.StartCapture();
    }

    public void StopCapture()
    {
        VidCap.StopCapture();
    }


}
