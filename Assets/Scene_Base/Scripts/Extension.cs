using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

namespace ExtensionMethods
{
    public static class ExtensionClass
    {
        public static GameObject GetChildByName(this GameObject go, string name)
        {
            return go.transform.Find(name).gameObject;
        }

        public static T CircleNext<T>(this T e) where T : Enum
        {
            T[] all = (T[])Enum.GetValues(typeof(T));
            int i = Array.IndexOf(all, e);
            if (i == all.Length - 1)
                return (T)all[0];
            return (T)(all[i + 1]);
        }

        public static int GetIndex<T>(this T e) where T : Enum
        {
            T[] all = (T[])Enum.GetValues(typeof(T));
            int i = Array.IndexOf(all, e);
            return i;
        }

        public static T CirclePrevious<T>(this T e) where T : Enum
        {
            T[] all = (T[])Enum.GetValues(typeof(T));
            int i = Array.IndexOf(all, e);
            if (i == 0)
                return (T)all[all.Length - 1];
            return (T)(all[i - 1]);
        }

        public static void AddEventTriggerListener(EventTrigger trigger,
                                       EventTriggerType eventType,
                                       System.Action<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = eventType;
            entry.callback = new EventTrigger.TriggerEvent();
            entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(callback));
            trigger.triggers.Add(entry);
        }


        public static void BindEventToUIButton(string name, UnityAction call)
        {
            Transform t = GameObject.Find("UI").transform.Find(name);
            if (t == null)
                Debug.LogError("Can not find gameobject: " + name);
            t.GetComponent<Button>().onClick.AddListener(call);
        }
    }
}
