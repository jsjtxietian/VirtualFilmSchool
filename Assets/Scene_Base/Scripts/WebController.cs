using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExtensionMethods;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public enum ShootingState
{
    Pause,
    Shooting,
    Playback,
}

public class WebController : MonoBehaviour
{
    private Afanty Afanty;
    [SerializeField]
    private WebCamController cameraController;
    private float _currentAniTime = 0;
    private float CurrentAniTime
    {
        get => _currentAniTime;
        set
        {
            _currentAniTime = Mathf.Clamp(value, 0, totalTime);

            int minute = (int)(_currentAniTime / 60);
            int second = (int)(_currentAniTime - minute * 60);
            int millisecond = (int)((_currentAniTime - (int)_currentAniTime) * 100);
            timeSliderText.text = string.Format("{0:D2}:{1:D2}.{2:D2}", minute, second, millisecond);
        }
    }

    private bool IsInited = false;

    private ShootingState state = ShootingState.Pause;
    private float currentMaxTime = 0;
    private float totalTime;
    [SerializeField]
    private Sprite[] buttonSprite;
    //UI
    private Slider timeSlider;
    private Text timeSliderText;
    private Button playbackButton;


    void Awake()
    {
        StartCoroutine(LoadAdditive());
    }

    IEnumerator LoadAdditive()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Afanty", LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Init();
    }

    private void Init()
    {
        Afanty = GameObject.Find("RootObject").GetComponent<Afanty>();
        totalTime = (float)Afanty.timeline.duration;
        Afanty.timeline.Play();

        cameraController.Init();

        ExtensionClass.BindEventToUIButton("TimeLineControl/Shoot", Shoot);
        timeSlider = GameObject.Find("UI/TimeLineControl/TimeSlider").GetComponent<Slider>();
        timeSlider.maxValue = totalTime;
        timeSlider.onValueChanged.AddListener(OnTimelineValueChange);
        timeSliderText = GameObject.Find("UI/TimeLineControl/TimeText").GetComponent<Text>();

        playbackButton = GameObject.Find("UI/TimeLineControl/Playback").GetComponent<Button>();
        playbackButton.onClick.AddListener(OnPlaybackButtonClick);

        IsInited = true;
    }

    void Update()
    {
        if (!IsInited)
            return;


        if (state.Equals(ShootingState.Shooting))
        {
            CurrentAniTime += Time.deltaTime;
            currentMaxTime = CurrentAniTime > currentMaxTime ? CurrentAniTime : currentMaxTime;
            cameraController.Record(CurrentAniTime);
            cameraController.ChangeCamParaInWeb();
        }
        else if (state.Equals(ShootingState.Playback))
        {
            if (CurrentAniTime + Time.deltaTime >= currentMaxTime)
            {
                CurrentAniTime = currentMaxTime;
            }
            else
            {
                CurrentAniTime += Time.deltaTime;
                cameraController.ReplayCam(CurrentAniTime);
            }
        }
        else
        {
            cameraController.ChangeCamParaInWeb();
        }

        Afanty.SetAniTime(CurrentAniTime);
        timeSlider.value = CurrentAniTime;

       
    }


    private void Shoot()
    {
        if (!state.Equals(ShootingState.Shooting))
        {
            state = ShootingState.Shooting;
        }
        else
        {
            state = ShootingState.Pause;
        }
    }

    private void OnTimelineValueChange(float value)
    {
        if (state.Equals(ShootingState.Shooting) || state.Equals(ShootingState.Playback))
            return;
        if (value > currentMaxTime)
        {
            timeSlider.value = currentMaxTime;
            return;
        }
        CurrentAniTime = value;
    }

    private void OnPlaybackButtonClick()
    {
        if (state.Equals(ShootingState.Pause))
        {
            state = ShootingState.Playback;
            playbackButton.transform.GetChild(0).GetComponent<Image>().sprite = buttonSprite[1];
        }
        else if (state.Equals(ShootingState.Playback))
        {
            state = ShootingState.Pause;
            playbackButton.transform.GetChild(0).GetComponent<Image>().sprite = buttonSprite[0];
        }

    }

}
