using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SetPokeToFingerAttachPoint : MonoBehaviour
{
    public Transform PokeAttachPoint;

    private XRPokeInteractor _xrPokeInteractor;
    private void Start()
    {
        _xrPokeInteractor = transform.parent.parent.GetComponentInChildren<XRPokeInteractor>();
        SetPokeAttachPoint();
    }
    void SetPokeAttachPoint()
    {
        if (PokeAttachPoint == null) { Debug.Log("Poke attach point is null"); return; }
        if (_xrPokeInteractor == null) { Debug.Log("XR Poke interactor is null"); return; }
        _xrPokeInteractor.attachTransform= PokeAttachPoint;
    }
}
