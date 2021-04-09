using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ExtensionMethods;

public class PointEnterHighlight : MonoBehaviour
{
    private Material basematerial;
    [SerializeField]
    private Material yellowmaterial;

    //NOTE: triggered by Event trigger in Scene

    void OnEnable()
    {
        basematerial = GetComponent<MeshRenderer>().material;
    }

    public void OnEnter()
    {
        gameObject.GetComponent<MeshRenderer>().material = yellowmaterial;
    }

    public void OnExist()
    {
        gameObject.GetComponent<MeshRenderer>().material = basematerial;
    }
}
