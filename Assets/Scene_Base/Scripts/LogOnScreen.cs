using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LogOnScreen : MonoBehaviour
{
    private string myLog;
    private int num = 0;
    private Queue myLogQueue = new Queue();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        myLog = logString;
        string newString = "\n [" + num++ + "] : " + myLog;
        myLogQueue.Enqueue(newString);
        if(myLogQueue.Count > 10)
            myLogQueue.Dequeue();
        if (type == LogType.Exception)
        {
            newString = "\n" + stackTrace;
            myLogQueue.Enqueue(newString);
        }
        myLog = string.Empty;
        foreach (string q in myLogQueue)
        {
            myLog += q;
        }

        GetComponent<Text>().text = myLog;
    }
}
