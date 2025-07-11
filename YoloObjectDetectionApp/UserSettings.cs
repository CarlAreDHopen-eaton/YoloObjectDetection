using System;

namespace YoloObjectDetectionApp
{
    public class UserSettings
    {
        public string ConnectionUrl { get; set; } = "rtsp://SomeUser:SomePassword@10.0.0.166:554/live/camera3/stream1";
        public bool ShowVideoStats { get; set; } = true;
        public bool EnableInference { get; set; } = true;
    }
}
