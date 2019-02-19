/*
 * Author: Stefan Dieckmann
 * Date: 11 November 2018
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARFoundation;

using Utilities;

namespace AR
{
    public enum ARState
    {
        None,
        Searching,
        Placing,
        Finalizing,
        Done
    }

    /// <summary>
    /// Controls all AR functionality
    /// </summary>
    public class ARManager : PersistentSingleton<ARManager>
    {
        public ARState CurrentARState = ARState.None;

        // Callbacks for when an event happens
        public event Action OnARReady;
        public event Action OnAROff;
        public event Action OnSearching;
        public event Action OnPlaneFound;
        public event Action OnPlaced;
        public event Action OnFinalized;

        public bool ARSupported = false;

        public bool AROn
        {
            get { return _AROn; }
        }
        private bool _AROn = false;

        private Camera _MainCamera;
        private Camera _ARCamera;

        private ARSession _ARSession;
        private ARSessionOrigin _ARSessionOrigin;

        private ARPointCloudManager _ARPointCloudManager;
        private ARPlaneManager _ARPlaneManager;

        [SerializeField]
        [Tooltip("A transform which should be made to appear to be at the touch point.")]
        Transform _Center;

        /// <summary>
        /// A transform which should be made to appear to be at the touch point.
        /// </summary>
        public Transform Center
        {
            get { return _Center; }
            set { _Center = value; }
        }

        [SerializeField]
        [Tooltip("The rotation the content should appear to have.")]
        Quaternion _Rotation;

        /// <summary>
        /// The rotation the content should appear to have.
        /// </summary>
        public Quaternion Rotation
        {
            get { return _Rotation; }
            set
            {
                _Rotation = value;
                if (_ARSessionOrigin != null)
                    _ARSessionOrigin.MakeContentAppearAt(Center, Center.transform.position, _Rotation);
            }
        }

        [SerializeField]
        [Tooltip("The rotation the content should appear to have.")]
        float _Scale;

        /// <summary>
        /// The scale the content should appear to have.
        /// </summary>
        public float Scale
        {
            get { return _Scale; }
            set
            {
                _Scale = value;
                if (_ARSessionOrigin != null)
                    _ARSessionOrigin.transform.localScale = Vector3.one * _Scale;
            }
        }

        private static List<ARRaycastHit> ARRaycastHits = new List<ARRaycastHit>();

        private static List<ARPlane> _Planes = new List<ARPlane>();

        private static List<ARPointCloud> _ARPointClouds = new List<ARPointCloud>();

        private int _PlaneCount = 0;

        private int _TapCount = 0;
        private float _DoubleTapTimer = 0;

        #region Unity Methods

        private void OnEnable()
        {
            ARSubsystemManager.systemStateChanged += OnARSubsystemManager_SystemStateChanged;

            _MainCamera = Camera.main;

            _ARSession = GetComponentInChildren<ARSession>();
            _ARSessionOrigin = GetComponentInChildren<ARSessionOrigin>();

            _ARCamera = _ARSessionOrigin.transform.GetChild(0).GetComponent<Camera>();

            _ARPointCloudManager = _ARSessionOrigin.GetComponent<ARPointCloudManager>();

            _ARPointCloudManager.pointCloudUpdated += OnPointCloudUpdated;
            _ARPointCloudManager.enabled = false;

            _ARPlaneManager = _ARSessionOrigin.GetComponent<ARPlaneManager>();
            _ARPlaneManager.enabled = false;
        }

        private void OnDisable()
        {
            ARSubsystemManager.systemStateChanged -= OnARSubsystemManager_SystemStateChanged;

            _ARPointCloudManager.pointCloudUpdated -= OnPointCloudUpdated;
        }

        private void Update()
        {
            UpdateARState();
        }

        #endregion // Unity Methods

        #region Events

        /// <summary>
        /// Called when the AR system state has changed. Used for setting ARSupported.
        /// </summary>
        /// <param name="obj">Contains the current state</param>
        private void OnARSubsystemManager_SystemStateChanged(ARSystemStateChangedEventArgs obj)
        {
            if (!ARSupported)
            {
                if ((int)obj.state == (int)ARSystemState.Ready)
                {
                    ARSupported = true;
                    _ARCamera.enabled = false;

                    if (OnARReady != null)
                        OnARReady();
                }
            }
        }

        /// <summary>
        /// Called when new point clouds are added. Used to destroy all point clouds.
        /// </summary>
        /// <param name="obj">Contains the new point cloud</param>
        private void OnPointCloudUpdated(ARPointCloudUpdatedEventArgs obj)
        {
            _ARPointClouds.Add(obj.pointCloud);
        }

        #endregion // Events

        #region // Methods

        /// <summary>
        /// Sets AR on or off
        /// </summary>
        /// <param name="state">If positive then AR is on. Negative is off</param>
        public void ChangeARState(bool state)
        {
            _AROn = !_AROn;

            if (_AROn)
            {
                SwitchAROn();
            }
            else
            {
                SwitchAROff();
            }
        }

        /// <summary>
        /// AR On method.
        /// </summary>
        private void SwitchAROn()
        {
            // Early exit if not supported
            if (!ARSupported)
            {
                return;
            }

            _ARCamera.enabled = true;
            _MainCamera.enabled = false;

            ARSubsystemManager.DestroySubsystems();
            ARSubsystemManager.CreateSubsystems();
            ARSubsystemManager.StopSubsystems();
            ARSubsystemManager.StartSubsystems();

            _ARPointCloudManager.enabled = true;
            _ARPlaneManager.enabled = true;

            _PlaneCount = 0;

            Scale = 10;

            _ARSessionOrigin.MakeContentAppearAt(Center, new Vector3(99999, 99999, 99999), Rotation);

            CurrentARState = ARState.Searching;

            if (OnSearching != null)
                OnSearching();

            SetAllPlanesActive(true);
        }

        /// <summary>
        /// AR Off method.
        /// </summary>
        private void SwitchAROff()
        {
            _ARCamera.enabled = false;
            _MainCamera.enabled = true;

            _ARPointCloudManager.enabled = false;
            _ARPlaneManager.enabled = false;

            CurrentARState = ARState.None;

            if (OnAROff != null)
                OnAROff();

            SetAllPlanesActive(false);
        }

        /// <summary>
        /// Sets all AR planes active or inactive
        /// </summary>
        /// <param name="value">Controls if AR planes are on or off</param>
        private void SetAllPlanesActive(bool value)
        {
            _ARPlaneManager.GetAllPlanes(_Planes);
            foreach (var plane in _Planes)
                plane.gameObject.SetActive(value);
        }

        /// <summary>
        /// Sets the new AR state.
        /// </summary>
        /// <param name="newARState">New state to set</param>
        /// <param name="ignoreCurrentState">Used to set to the same state</param>
        private void SetARState(ARState newARState, bool ignoreCurrentState = false)
        {
            if (CurrentARState == newARState && !ignoreCurrentState)
            {
                // Already in state
                return;
            }

            switch (CurrentARState)
            {
                case ARState.Searching:

                    break;

                case ARState.Placing:

                    break;

                case ARState.Finalizing:

                    break;
            }
        }

        /// <summary>
        /// Called from Update. Calls the correct method based on CurrentARState.
        /// </summary>
        private void UpdateARState()
        {
            switch(CurrentARState)
            {
                case ARState.Searching:
                    UpdateSearching();
                    break;

                case ARState.Placing:
                    UpdatePlacing();
                    break;

                case ARState.Finalizing:
                    UpdateFinalizing();
                    break;
            }
        }

        /// <summary>
        /// Searching state
        /// </summary>
        private void UpdateSearching()
        {
            if (_ARPlaneManager.planeCount != _PlaneCount)
            {
                if (OnPlaneFound != null)
                {
                    OnPlaneFound();
                }

               CurrentARState = ARState.Placing;
            }
        }

        /// <summary>
        /// Placing state. This is when the user can tap to place.
        /// </summary>
        private void UpdatePlacing()
        {
            if (Input.touchCount == 0)
                return;

            var touch = Input.GetTouch(0);

            if (_ARSessionOrigin.Raycast(touch.position, ARRaycastHits, TrackableType.PlaneWithinPolygon))
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.
                var hitPose = ARRaycastHits[0].pose;

                // This does not move the content; instead, it moves and orients the ARSessionOrigin
                // such that the content appears to be at the raycast hit position.
                _ARSessionOrigin.MakeContentAppearAt(Center, hitPose.position, Rotation);

                if (OnPlaced != null)
                {
                    OnPlaced();
                }

                // AR placed so change to Finalizing
                CurrentARState = ARState.Finalizing;
            }
        }

        /// <summary>
        /// Finalizing state. In this state the user can rotate and scale. Double tap to finish.
        /// </summary>
        private void UpdateFinalizing()
        {
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                _TapCount++;
            }
            if (_TapCount > 0)
            {
                _DoubleTapTimer += Time.deltaTime;
            }
            if (_TapCount >= 2)
            {
                //DOuble tap detected
                _DoubleTapTimer = 0.0f;
                _TapCount = 0;

                if (OnFinalized != null)
                {
                    OnFinalized();
                }

                Done();
            }
            if (_DoubleTapTimer > 0.5f)
            {
                _DoubleTapTimer = 0f;
                _TapCount = 0;
            }
        }

        /// <summary>
        /// Done state. Disables all AR point clouds and planes.
        /// </summary>
        private void Done()
        {
            _ARPlaneManager.enabled = false;
            _ARPointCloudManager.enabled = false;

            foreach (ARPointCloud arPC in _ARPointClouds)
            {
                if (arPC != null)
                    Destroy(arPC.gameObject);
            }

            _ARPointClouds.Clear();

            SetAllPlanesActive(false);

            CurrentARState = ARState.Done;
        }
        #endregion
    }
}
