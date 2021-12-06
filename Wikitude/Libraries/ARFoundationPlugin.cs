using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Wikitude
{
    public class ARFoundationPlugin : MonoBehaviour
    {
        public GameObject ARCamera;

        private Plugin _arFoundationPlugin;
        private ARCameraManager _arCameraManager;

        private Color32[] _colorData;
        private int _frameIndex;

        private IntrinsicsCalibration? _calibration;

        private void Awake() {
            _arFoundationPlugin = gameObject.AddComponent<Plugin>();
            _arFoundationPlugin.Identifier = "AR Foundation Plugin";
            _arFoundationPlugin.HasInputModule = true;

            _arFoundationPlugin.OnPluginError.AddListener(error => {
                Debug.Log($"OnPluginError Code: {error.Code}, Domain: {error.Domain}, Message: {error.Message}");
            });
        }

        private void Start() {
            _arCameraManager = FindObjectOfType<ARCameraManager>();
            if (_arCameraManager == null) {
                Debug.LogError("No object of type ARCameraManager found in scene");
            }
        }

        private void Update() {
            if (_arCameraManager.TryGetLatestImage(out var cameraImage)) {
                var metadata = new ColorCameraFrameMetadata {
                    Width = cameraImage.width,
                    Height = cameraImage.height,
                    CameraPosition = CaptureDevicePosition.Back,
                    ColorSpace = Application.platform == RuntimePlatform.Android ? FrameColorSpace.YUV_420_888 : FrameColorSpace.YUV_420_NV21,
                    TimestampScale = 1
                };

                var planes = new List<CameraFramePlane>();
                unsafe {
                    for (int planeIndex = 0; planeIndex < cameraImage.planeCount; planeIndex++) {
                        var unityPlane = cameraImage.GetPlane(planeIndex);
                        var wikitudePlane = new CameraFramePlane {
                            Data = (IntPtr)unityPlane.data.GetUnsafePtr(),
                            DataSize = (uint)unityPlane.data.Length,
                            PixelStride = unityPlane.pixelStride,
                            RowStride = unityPlane.rowStride
                        };

                        planes.Add(wikitudePlane);
                    }
                }

                if (_calibration == null) {
                    if (_arCameraManager.TryGetIntrinsics(out var intrinsics)) {
                        _calibration = new IntrinsicsCalibration {
                            PrincipalPoint = new Point {
                                X = intrinsics.principalPoint.x,
                                Y = intrinsics.principalPoint.y
                            },
                            FocalLength = new Point {
                                X = intrinsics.focalLength.x,
                                Y = intrinsics.focalLength.y
                            },
                        };
                    }
                }

                if (_calibration != null) {
                    metadata.HasIntrinsicsCalibration = true;
                    metadata.IntrinsicsCalibration = _calibration.Value;
                }

                var flipZAxis = Matrix4x4.Scale(new Vector3(1, 1, -1));
                var flipYAxis = Matrix4x4.Scale(new Vector3(1, -1, 1));
                var rotate90AroundX = Matrix4x4.Rotate(Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f)));

                float cameraToSurfaceAngle;
                switch (Screen.orientation) {
                    case ScreenOrientation.Portrait:
                        cameraToSurfaceAngle = 90.0f;
                        break;
                    case ScreenOrientation.PortraitUpsideDown:
                        cameraToSurfaceAngle = -90.0f;
                        break;
                    case ScreenOrientation.LandscapeLeft:
                        cameraToSurfaceAngle = 0.0f;
                        break;
                    case ScreenOrientation.LandscapeRight:
                        cameraToSurfaceAngle = 180.0f;
                        break;
                    default:
                        cameraToSurfaceAngle = 0.0f;
                        break;
                }

                if (SystemInfo.deviceName.Contains("Nexus 5X")) {
                    /* The Nexus 5X camera is flipped upside-down, so the cameraToSurfaceAngle also needs to be flipped.
                     * Since there is no other API to detect this, we have to rely on the device name.
                     */
                    cameraToSurfaceAngle += 180.0f;
                }

                var cameraToSurfaceRotation = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, cameraToSurfaceAngle));

                var arPose = Matrix4x4.TRS(ARCamera.transform.localPosition, ARCamera.transform.localRotation, ARCamera.transform.localScale);
                var pose = rotate90AroundX * flipZAxis * arPose * flipYAxis * cameraToSurfaceRotation;

                var cameraFrame = new CameraFrame(++_frameIndex, 0, metadata, planes, pose);
                _arFoundationPlugin.NotifyNewCameraFrame(cameraFrame);

                cameraImage.Dispose();
            }
        }
    }
}