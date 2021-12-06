using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARFoundationController : SampleController
{
    public GameObject Instructions;
    public GameObject UnsupportedDeviceText;


    private IEnumerator CheckARFoundationSupport() {
        void ShowUnsupportedDeviceMessage() {
            Instructions.SetActive(false);
            UnsupportedDeviceText.SetActive(true);
        }

        if (Application.platform == RuntimePlatform.Android) {
            /* On Android, first check if the Android version could support ARFoundation, because otherwise the API would not work */
            using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
                int versionNumber = version.GetStatic<int>("SDK_INT");
                if (versionNumber < 24) {
                    ShowUnsupportedDeviceMessage();
                }
            }
        }

        bool arFoundationStateDetermined = false;
        while (!arFoundationStateDetermined) {
            Debug.Log($"Checking ARFoundation support {ARSession.state}.");
            switch (ARSession.state) {
                case ARSessionState.CheckingAvailability:
                case ARSessionState.Installing:
                case ARSessionState.SessionInitializing:
                    yield return new WaitForSeconds(0.1f);
                    break;
                case ARSessionState.None:
                case ARSessionState.Unsupported:
                    ShowUnsupportedDeviceMessage();
                    arFoundationStateDetermined = true;
                    break;
                default:
                    arFoundationStateDetermined = true;
                    break;
            }
        }
    }

    protected override void Awake() {
        base.Awake();
        StartCoroutine(CheckARFoundationSupport());
    }
}
