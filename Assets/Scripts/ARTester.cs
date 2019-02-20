﻿/*
 * Author: Stefan Dieckmann
 * Date: 11 November 2018
 */

using UnityEngine;
using UnityEngine.UI;

using AR;

/// <summary>
/// Used for testing ARManager.
/// </summary>
public class ARTester : MonoBehaviour
{
    public GameObject ARButton;

    public Text ARButtonText, ARStatusText;

    public GameObject FinalizingPanel;

    private float _RotationValue = 0;
    private int _RotationDir = 0;

    private float _ScaleValue = 10;
    private int _ScaleDir = 0;

    // Start is called before the first frame update
    void Start()
    {
        ARManager.Instance.OnARReadyEvent += Instance_OnARReady;
        ARManager.Instance.OnOffEvent += Instance_OnOff;
        ARManager.Instance.OnSearchingEvent += Instance_OnSearching;
        ARManager.Instance.OnPlaneFoundEvent += Instance_OnPlaneFound;
        ARManager.Instance.OnFinalizedEvent += Instance_OnFinalized;
        ARManager.Instance.OnDoneEvent += Instance_OnDone;

        ARButton.SetActive(false);
        ARButtonText.text = ARManager.Instance.AROn == true ? "AR ON" : "AR OFF";
    }

    private void OnDisable()
    {
        ARManager.Instance.OnARReadyEvent -= Instance_OnARReady;
        ARManager.Instance.OnOffEvent -= Instance_OnOff;
        ARManager.Instance.OnSearchingEvent -= Instance_OnSearching;
        ARManager.Instance.OnPlaneFoundEvent -= Instance_OnPlaneFound;
        ARManager.Instance.OnFinalizedEvent -= Instance_OnFinalized;
        ARManager.Instance.OnDoneEvent -= Instance_OnDone;
    }

    void Instance_OnARReady()
    {
        ARButton.SetActive(true);
    }

    void Instance_OnOff()
    {
        FinalizingPanel.SetActive(false);

        ARStatusText.text = "";
    }

    void Instance_OnSearching()
    {
        _ScaleValue = ARManager.Instance.Scale;

        FinalizingPanel.SetActive(false);

        ARStatusText.text = "Searching";
    }

    void Instance_OnPlaneFound()
    {
        ARStatusText.text = "Tap To Place";
    }

    void Instance_OnFinalized()
    {
        FinalizingPanel.SetActive(true);

        ARStatusText.text = "Double Tap To Finish";
    }

    void Instance_OnDone()
    {
        FinalizingPanel.SetActive(false);

        ARStatusText.text = "";
    }

    public void SwitchAR()
    {
        ARManager.Instance.SwitchAR();

        ARButtonText.text = ARManager.Instance.AROn == true ? "AR ON" : "AR OFF";
    
        if (!ARManager.Instance.AROn)
        {
            FinalizingPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (_RotationDir != 0)
        {
            _RotationValue += 30 * _RotationDir * Time.deltaTime;

            if (_RotationValue < 0)
            {
                _RotationValue = 360;
            }
            else if (_RotationValue > 360)
            {
                _RotationValue = 0;
            }

            ARManager.Instance.Rotation = Quaternion.AngleAxis(_RotationValue, Vector3.up);

        }

        if (_ScaleDir != 0)
        {
            _ScaleValue += 10 * _ScaleDir * Time.deltaTime;

            _ScaleValue = Mathf.Clamp(_ScaleValue, 0.1f, 20);

            ARManager.Instance.Scale = _ScaleValue;
        }
    }

    public void Rotate(int dir)
    {
        _RotationDir = dir;
    }

    public void Scale(int dir)
    {
        _ScaleDir = dir;
    }
}