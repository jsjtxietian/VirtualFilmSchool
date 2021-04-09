using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Playables;

public class Afanty : MonoBehaviour
{
    public GameObject ground;
    public PlayableDirector timeline;
    public AudioSource movieSound;

    [SerializeField]
    private ParticleSystem oilPS;
    [SerializeField]
    private ParticleSystem jarPS;
    private VideoPlayer _albedoPlayer;
    private float currentTime = 0f;
    private bool isAlone = false;


    public void setRootPos(Vector3 pos)
    {
        GameObject rootObj = GameObject.Find("RootObject");
        rootObj.transform.position = pos;
    }

    // Use this for initialization
    void Start()
    {
        isAlone = GameObject.Find("Scripts") == null ? true : false;

        _albedoPlayer = ground.GetComponent<VideoPlayer>();
        _albedoPlayer.url = Application.streamingAssetsPath + "/Afanty" + "/albedo.mp4";
        _albedoPlayer.Prepare();
        _albedoPlayer.Play();

        if (isAlone)
        {
            timeline.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlone)
        {
            currentTime += Time.deltaTime;
            SetAniTime(currentTime);
        }
    }

    public void SetAniTime(float time)
    {
        timeline.time = currentTime = time;
        PlayParticle(currentTime);
        PlayOilVideo(currentTime);
    }

    private void PlayOilVideo(float animationTime)
    {
        float time = Mathf.Clamp(animationTime - 61.4f, 0, (float)(timeline.duration));
        _albedoPlayer.time = time;
    }
    private void PlayParticle(float animationTime)
    {
        if (floatEqual(animationTime, 61) || floatEqual(animationTime, 64) 
            || floatEqual(animationTime, 73) || floatEqual(animationTime, 89)
            || animationTime < 60 || animationTime > 89)
        {
            oilPS.Clear();
        }
        if (animationTime > 60 && animationTime < 61)
        {
            oilPS.Simulate(animationTime - 60f);
        }
        else if (animationTime > 63 && animationTime < 64)
        {
            oilPS.Simulate(animationTime - 63f);
        }
        else if (animationTime > 70 && animationTime < 73)
        {
            oilPS.Simulate(animationTime - 70f);
        }
        else if (animationTime > 77 && animationTime < 89)
        {
            oilPS.Simulate(animationTime - 77f);
        }

        if (floatEqual(animationTime, 109) || animationTime < 100 || animationTime > 109)
        {
            jarPS.Clear();
        }
        if (animationTime > 100 && animationTime < 109)
        {
            jarPS.Simulate(animationTime - 100f);
        }
    }

    private bool floatEqual(float f1, float f2)
    {
        if (Mathf.Abs(f1 - f2) < Time.deltaTime * 1.5)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}
