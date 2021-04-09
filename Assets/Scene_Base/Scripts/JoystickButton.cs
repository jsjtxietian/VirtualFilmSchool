using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HTC.UnityPlugin.Vive;

public enum ButtonDirection
{
    Up,
    Down,
    Left,
    Right
}

//NOTE: 游戏是60帧,连续按duration帧并且加起来为threashold认为是一个事件
public class JoystickButton : MonoBehaviour
{
    [SerializeField]
    private int duration = 24;
    [SerializeField]
    private float threashold = 12;
    private Dictionary<string, Queue<float>> AxisQueue;
    private readonly string X = "X";
    private readonly string Y = "Y";

    void Awake()
    {
        AxisQueue = new Dictionary<string, Queue<float>>();
        AxisQueue.Add(HandRole.LeftHand.ToString() + X, new Queue<float>());
        AxisQueue.Add(HandRole.LeftHand.ToString() + Y, new Queue<float>());
        AxisQueue.Add(HandRole.RightHand.ToString() + X, new Queue<float>());
        AxisQueue.Add(HandRole.RightHand.ToString() + Y, new Queue<float>());
    }

    public bool GetKeyDown(HandRole role, ButtonDirection direction)
    {

        if (role == HandRole.RightHand)
        {
            switch (direction)
            {
                case ButtonDirection.Up:
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                        return true;
                    break;
                case ButtonDirection.Down:
                    if (Input.GetKeyDown(KeyCode.DownArrow))
                        return true;
                    break;
                case ButtonDirection.Left:
                    if (Input.GetKeyDown(KeyCode.LeftArrow))
                        return true;
                    break;
                case ButtonDirection.Right:
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                        return true;
                    break;
            }
        }


        Queue<float> queue = GetQueue(role, direction);
        if (queue.Count < duration - 1)
            return false;

        bool isPositive = direction.Equals(ButtonDirection.Up) || direction.Equals(ButtonDirection.Right);
        bool result = false;
        float sum = 0;
        foreach (var item in queue.ToArray())
        {
            sum += item;
        }

        if (isPositive && sum > threashold)
        {
            result = true;
        }
        else if (!isPositive && sum < -threashold)
        {
            result = true;
        }

        if (result)
            queue.Clear();

        return result;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateOneHand(HandRole.LeftHand, X);
        UpdateOneHand(HandRole.LeftHand, Y);
        UpdateOneHand(HandRole.RightHand, X);
        UpdateOneHand(HandRole.RightHand, Y);
    }

    private void UpdateOneHand(HandRole role, string direction)
    {
        string name = role.ToString() + direction;
        if (AxisQueue[name].Count > duration)
        {
            AxisQueue[name].Dequeue();
        }

        if (direction.Equals(X))
        {
            AxisQueue[name].Enqueue(ViveInput.GetAxisEx(role, ControllerAxis.JoystickX));
        }
        else
        {
            AxisQueue[name].Enqueue(ViveInput.GetAxisEx(role, ControllerAxis.JoystickY));
        }
    }

    private Queue<float> GetQueue(HandRole role, ButtonDirection direction)
    {
        string axis = null;

        switch (direction)
        {
            case ButtonDirection.Down:
            case ButtonDirection.Up:
                axis = Y;
                break;
            case ButtonDirection.Left:
            case ButtonDirection.Right:
                axis = X;
                break;
        }

        return AxisQueue[role.ToString() + axis];
    }
}
