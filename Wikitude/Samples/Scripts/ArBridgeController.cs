using System;
using Wikitude;
using UnityEngine.UI;

public class ArBridgeController : SampleController
{
    public WikitudeSDK WikitudeSDK;
    public Text ArBridgeAvailabilityText;
    protected override void Update() {
        base.Update();

        switch (WikitudeSDK.ArBridgeAvailability) {
            case ArBridgeAvailability.IndeterminateQueryFailed:
                ArBridgeAvailabilityText.text = "AR Bridge support couldn't be determined.";
                break;
            case ArBridgeAvailability.CheckingQueryOngoing:
                ArBridgeAvailabilityText.text = "AR Bridge support check ongoing.";
                break;
            case ArBridgeAvailability.Unsupported:
                ArBridgeAvailabilityText.text = "AR Bridge is not supported.";
                break;
            case ArBridgeAvailability.SupportedUpdateRequired:
                ArBridgeAvailabilityText.text = "AR Bridge is supported, but an update is available.";
                break;
            case ArBridgeAvailability.Supported:
                ArBridgeAvailabilityText.text = "AR Bridge is supported.";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
