using UnityEngine;
using UnityEngine.UI;

/// 用于记录手持相机运动轨迹与参数的自定义类


/// 用于判断透明度变化是否已经结束的自定义类
class FadeFinFlag
{
    public bool done { get; set; }
}


/// 用于透明度变化协程的自定义类
class ImageAlphaFadePara
{

    /// 目标图片

    public Image _Img;

    /// 目标不透明度

    public float _TargetAlpha;

    /// 延迟时间，以秒为单位

    public float _DelayTime;

    /// 变化总持续时间

    public float _TotalTime;

    /// 变化曲线，可以调整以产生缓入缓出等效果

    public AnimationCurve _Curv;

    /// 用于监视变化是否结束的外部实例，需使用FadeFinFlag自定义类，如无需监视则在构造函数中可省略

    public FadeFinFlag _DoneFlag;

    /// 是否要监视结束状态

    public bool _EndFlagTraced = false;

    public ImageAlphaFadePara(Image _inImg, float _inTargetAlpha, float _inDelayTime, float _inTotalTime, AnimationCurve _inCurv)
    {
        _Img = _inImg;
        _TargetAlpha = _inTargetAlpha;
        _DelayTime = -_inDelayTime;
        _TotalTime = _inTotalTime;
        _Curv = _inCurv;
    }

    public ImageAlphaFadePara(Image _inImg, float _inTargetAlpha, float _inDelayTime, float _inTotalTime, AnimationCurve _inCurv, FadeFinFlag _inDoneFlag)
    {
        _Img = _inImg;
        _TargetAlpha = _inTargetAlpha;
        _DelayTime = -_inDelayTime;
        _TotalTime = _inTotalTime;
        _Curv = _inCurv;
        _DoneFlag = _inDoneFlag;
        _DoneFlag.done = false;
        _EndFlagTraced = true;
    }
}


/// 用于在指定时间显示指定图文提示的类

class TutHint
{

    /// 当MainProgress等于几时显示

    public int _Progress;

    /// 显示提示的开始与结束时间，当抵达目标Progress值后开始计时

    public float _OnTime, _OffTime;

    /// 提示信息的GameObject，即需被显隐的对象

    public GameObject _Target;

    public TutHint(int _inProgress, float _inOnTime, float _inOffTime, GameObject _inTarget)
    {
        _Progress = _inProgress;
        _OnTime = _inOnTime;
        _OffTime = _inOffTime;
        _Target = _inTarget;
    }
}


/// 用作editMainProgress协程参数的类

class editMainProgressPara
{

    /// 等待多久后改变进度

    public float waitSec;

    /// 改变后的目标进度

    public int targetProgress;

    public editMainProgressPara(float inWaitSec, int inTargetProgress)
    {
        waitSec = inWaitSec;
        targetProgress = inTargetProgress;
    }
}