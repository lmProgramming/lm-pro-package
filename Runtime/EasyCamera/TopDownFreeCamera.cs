using UnityEngine;
using UnityEngine.UI;

namespace LM.EasyCamera
{
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownFreeCamera : MonoBehaviour, ITopDownFreeCamera
    {
        // generic - drag one finger to move the screen, boundaries - finger near bounds moves the screen
        public enum DraggingType
        {
            Generic,
            Boundaries,
            None
        }

        public static TopDownFreeCamera Instance;

        public bool followingObject;
        public GameObject objectToFollow;

        public bool forcedToFollowCurrentObject;

        public float dragSpeed = 1f;
        public float keySpeed = 1f;

        public bool fakingMobilePlatformAndTestingMobileInput;
        public Transform sq1;
        public Transform sq2;

        public bool isZooming;

        public DraggingType curDraggingType = DraggingType.Generic;

        public float screenPercentageDistanceToBoundToStartDragging = 0.1f;
        public float boundDraggingSpeed = 1;

        public float scrollSpeed = 1f;
        public float mobileScrollSpeed = 1f;

        public float maxZoom;
        public float minZoom;

        public bool updateCamera = true;

        public RenderTexture pausedMenuGameTexture;
        public RawImage backgroundBlurImage;

        public int nextFramesToRender;

        [SerializeField] private AnimationCurve zoomCurve;
        [SerializeField] private AnimationCurve moveCurve;

        public Camera uiCamera;

        private Camera _cameraMain;
        private float _maxXPos;
        private float _maxYPos;

        private float _minXPos;
        private float _minYPos;

        private Vector2 _previousMousePosition = Vector2.zero;

        private void Awake()
        {
            _cameraMain = GetComponent<Camera>();
            Instance = this;
        }

        private void Start()
        {
            pausedMenuGameTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            backgroundBlurImage.texture = pausedMenuGameTexture;
        }

        private void Update()
        {
            if (ShouldUpdateCamera())
            {
                if (followingObject && objectToFollow != null)
                    // camera centered around some object
                    transform.position = new Vector3(objectToFollow.transform.position.x,
                        objectToFollow.transform.position.y, transform.position.z);
                else
                    // camera free to move, can move by dragging finger or mouse
                    ProcessDrag();

                // zoom the camera
                ProcessZoom();

                if (!(Application.platform == RuntimePlatform.Android ||
                      Application.platform == RuntimePlatform.IPhonePlayer))
                    _previousMousePosition = Input.mousePosition;
            }
        }

        public float Zoom
        {
            get => _cameraMain.orthographicSize;
            set => _cameraMain.orthographicSize = value;
        }

        public void LateUpdate()
        {
            if (--nextFramesToRender == 0) DisableCamera();
        }

        public void StartFollowingObject(GameObject newObjectToFollow)
        {
            followingObject = true;
            objectToFollow = newObjectToFollow;
        }

        public void StartFollowingObjectIfNotFollowingItAlready(GameObject newObjectToFollow)
        {
            if (newObjectToFollow == objectToFollow)
            {
                StopFollowingObject();
                return;
            }

            followingObject = true;
            objectToFollow = newObjectToFollow;
        }

        public void ForceStartFollowingObject(GameObject newObjectToFollow)
        {
            followingObject = true;
            objectToFollow = newObjectToFollow;
            forcedToFollowCurrentObject = true;
        }

        public void StopFollowingObject()
        {
            if (!forcedToFollowCurrentObject)
            {
                followingObject = false;
                objectToFollow = null;
            }
        }

        public void ForceStopFollowingObject()
        {
            followingObject = false;
            objectToFollow = null;
            forcedToFollowCurrentObject = false;
        }

        public void SetBoundaries(float minXPos, float maxXPos, float minYPos, float maxYPos)
        {
            _minXPos = minXPos;
            _maxXPos = maxXPos;
            _minYPos = minYPos;
            _maxYPos = maxYPos;

            minZoom = (maxXPos + maxYPos) / 1.5f;
        }

        public void TurnOnCameraRenderingToTexture()
        {
            _cameraMain.targetTexture = pausedMenuGameTexture;
            backgroundBlurImage.SetNativeSize();

            nextFramesToRender = 2;
        }

        public void TurnOffCameraRenderingToTexture()
        {
            _cameraMain.targetTexture = null;

            EnableCamera();
        }

        public void EnableCamera()
        {
            _cameraMain.enabled = true;
            uiCamera.clearFlags = CameraClearFlags.Nothing;
        }

        public void EnableCameraForOneFrame()
        {
            _cameraMain.enabled = true;

            nextFramesToRender = 2;
        }

        public void DisableCamera()
        {
            _cameraMain.enabled = false;
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private bool IsMobilePlatform()
        {
            return Application.isMobilePlatform || fakingMobilePlatformAndTestingMobileInput;
        }

        private bool ShouldUpdateCamera()
        {
            return updateCamera && (followingObject || GameInput.AmountOfHeldUIElements == 0);
        }

        private void ProcessDrag()
        {
            if (curDraggingType == DraggingType.None) return;

            bool nowDown;

            //check if input is activated (mouse down or finger touching screen)
            if (IsMobilePlatform())
                nowDown = Input.touchCount > 0;
            else
                nowDown = (Input.GetMouseButton(0) && !GameInput.IsPointerOverUI) || Input.GetAxis("Horizontal") != 0 ||
                          Input.GetAxis("Vertical") != 0;

            //if mouse currently holding or finger touching the screen
            if (nowDown)
            {
                var incrementDrag = Vector2.zero;

                if (curDraggingType == DraggingType.Generic)
                    incrementDrag = ProcessGenericDrag(incrementDrag);
                else if (curDraggingType == DraggingType.Boundaries)
                    incrementDrag = ProcessBoundariesDrag(incrementDrag);

                transform.Translate(dragSpeed * zoomCurve.Evaluate(_cameraMain.orthographicSize / minZoom) *
                                    incrementDrag);

                // clamp the position
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, _minXPos, _maxXPos),
                    Mathf.Clamp(transform.position.y, _minYPos, _maxYPos), transform.position.z);
            }
        }

        private Vector2 ProcessGenericDrag(Vector2 incrementDrag)
        {
            if (IsMobilePlatform())
            {
                if (Input.touchCount > 0)
                {
                    if (fakingMobilePlatformAndTestingMobileInput)
                        sq1.transform.position = GameInput.WorldPointerPosition;

                    var viableTouches = 0;

                    for (var i = 0; i < Input.touches.Length; i++)
                        if (Input.GetTouch(i).phase != TouchPhase.Ended)
                        {
                            viableTouches += 1;

                            incrementDrag.x += -Input.GetTouch(i).deltaPosition.x;
                            incrementDrag.y += -Input.GetTouch(i).deltaPosition.y;
                        }

                    incrementDrag /= Mathf.Max(1, viableTouches);
                }
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    incrementDrag.x = _previousMousePosition.x - Input.mousePosition.x;
                    incrementDrag.y = _previousMousePosition.y - Input.mousePosition.y;
                }

                incrementDrag.x += Input.GetAxis("Horizontal") * keySpeed;
                incrementDrag.y += Input.GetAxis("Vertical") * keySpeed;
            }

            return incrementDrag;
        }

        private Vector2 ProcessBoundariesDrag(Vector2 incrementDrag)
        {
            if (IsMobilePlatform())
            {
                if (Input.touchCount > 0)
                    for (var i = 0; i < Input.touches.Length; i++)
                        if (Input.GetTouch(i).phase != TouchPhase.Ended)
                        {
                            var touchDistToLeftUpperCorner =
                                Input.GetTouch(i).position / new Vector2(Screen.width, Screen.height);

                            if (touchDistToLeftUpperCorner.x < screenPercentageDistanceToBoundToStartDragging)
                                incrementDrag.x -=
                                    (screenPercentageDistanceToBoundToStartDragging - touchDistToLeftUpperCorner.x) *
                                    boundDraggingSpeed;
                            else if (touchDistToLeftUpperCorner.x > 1 - screenPercentageDistanceToBoundToStartDragging)
                                incrementDrag.x +=
                                    (touchDistToLeftUpperCorner.x -
                                     (1 - screenPercentageDistanceToBoundToStartDragging)) *
                                    boundDraggingSpeed;

                            if (touchDistToLeftUpperCorner.y < screenPercentageDistanceToBoundToStartDragging)
                                incrementDrag.y -=
                                    (screenPercentageDistanceToBoundToStartDragging - touchDistToLeftUpperCorner.y) *
                                    boundDraggingSpeed;
                            else if (touchDistToLeftUpperCorner.y > 1 - screenPercentageDistanceToBoundToStartDragging)
                                incrementDrag.y +=
                                    (touchDistToLeftUpperCorner.y -
                                     (1 - screenPercentageDistanceToBoundToStartDragging)) *
                                    boundDraggingSpeed;
                        }
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    var touchDistToLeftUpperCorner = Input.mousePosition / new Vector2(Screen.width, Screen.height);

                    if (touchDistToLeftUpperCorner.x < screenPercentageDistanceToBoundToStartDragging)
                        incrementDrag.x -=
                            (screenPercentageDistanceToBoundToStartDragging - touchDistToLeftUpperCorner.x) *
                            boundDraggingSpeed;
                    else if (touchDistToLeftUpperCorner.x > 1 - screenPercentageDistanceToBoundToStartDragging)
                        incrementDrag.x +=
                            (touchDistToLeftUpperCorner.x - (1 - screenPercentageDistanceToBoundToStartDragging)) *
                            boundDraggingSpeed;

                    if (touchDistToLeftUpperCorner.y < screenPercentageDistanceToBoundToStartDragging)
                        incrementDrag.y -=
                            (screenPercentageDistanceToBoundToStartDragging - touchDistToLeftUpperCorner.y) *
                            boundDraggingSpeed;
                    else if (touchDistToLeftUpperCorner.y > 1 - screenPercentageDistanceToBoundToStartDragging)
                        incrementDrag.y +=
                            (touchDistToLeftUpperCorner.y - (1 - screenPercentageDistanceToBoundToStartDragging)) *
                            boundDraggingSpeed;
                }

                incrementDrag.x += Input.GetAxis("Horizontal") * keySpeed;
                incrementDrag.y += Input.GetAxis("Vertical") * keySpeed;
            }

            return incrementDrag;
        }

        private void ProcessZoom()
        {
            float increment = 0;

            if (!GameInput.IsPointerOverUI)
            {
                if (IsMobilePlatform())
                {
                    if (Input.touchCount == 2)
                    {
                        if (!isZooming) isZooming = true;

                        if (fakingMobilePlatformAndTestingMobileInput)
                        {
                            sq1.transform.position = GameInput.GetWorldPointerPosition();
                            sq2.transform.position = GameInput.GetWorldPointerPosition(1);
                        }

                        //two finger
                        var touchZero = Input.GetTouch(0);
                        var touchOne = Input.GetTouch(1);

                        // Find the position in the previous frame of each touch.
                        var touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                        var touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                        // Find the magnitude of the vector (the distance) between the touches in each frame.
                        var prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                        var touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                        // Find the difference in the distances between each frame.
                        var deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                        //scale the zoom increment value base on touchZoomSpeed
                        increment = deltaMagnitudeDiff * mobileScrollSpeed;
                    }
                    else
                    {
                        isZooming = false;
                    }
                }
                else
                {
                    //mice and keyboard
                    increment = -Input.GetAxis("Mouse ScrollWheel");
                }
            }

            //set new size
            _cameraMain.orthographicSize +=
                increment * scrollSpeed * zoomCurve.Evaluate(_cameraMain.orthographicSize / minZoom);

            // clamp the camera between max and min zoom
            _cameraMain.orthographicSize = Mathf.Clamp(_cameraMain.orthographicSize, maxZoom, minZoom);
        }
    }
}