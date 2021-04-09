# README

#### TODO：跨平台工作,安卓
* 重构代码,整理出流程，以及平台相关的代码
* 交互参考：https://www.youtube.com/watch?v=jDYLfCurgcI
* 目前随意拖动进度条会造成视频播放和粒子效果有问题,看下ParticleControlPlayable,以及视频相关
* 控制、内容、感官一致性、多模态？（以显示内容为基准）

#### Note:
* 老爷：object046(鼻子),Character002PalmBone003 1(右手),Character002PalmBone003(左手)，Character002Hub001(身体)
* 阿凡提：Base Human001Head,Base Human001PalmBone003 1,Base Human001PalmBone003,

#### 问题:
* camera里面找不到阴影(晃动)，干掉physical camera就可以解决
* 测试ViveInput中
  * 左右功能键和Y/B是同一个
  * bump按钮目前没有
* 录像的帧率是由游戏决定，capture似乎是24。没法决定帧率，所以没法决定快门速度，一般来说快门速度是帧速率的两倍才会有合适的运动模糊。
* 运动模糊、噪点
* camera UI的字体lit
* 两种输入viveinput和原来的，sensitivity不同
