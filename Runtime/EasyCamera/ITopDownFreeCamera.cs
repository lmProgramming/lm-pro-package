using UnityEngine;

namespace LM.EasyCamera
{
    public interface ITopDownFreeCamera
    {
        float Zoom { get; set; }
        void LateUpdate();
        void StartFollowingObject(GameObject newObjectToFollow);
        void StartFollowingObjectIfNotFollowingItAlready(GameObject newObjectToFollow);
        void ForceStartFollowingObject(GameObject newObjectToFollow);
        void StopFollowingObject();
        void ForceStopFollowingObject();
        void SetBoundaries(float minXPos, float maxXPos, float minYPos, float maxYPos);
        void TurnOnCameraRenderingToTexture();
        void TurnOffCameraRenderingToTexture();
        void EnableCamera();
        void EnableCameraForOneFrame();
        void DisableCamera();
    }
}