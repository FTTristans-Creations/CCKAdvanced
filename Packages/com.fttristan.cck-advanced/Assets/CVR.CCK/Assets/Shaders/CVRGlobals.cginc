#ifndef CVR_GLOBALS_CGINC
#define CVR_GLOBALS_CGINC

struct CVRRenderingData
{
    // 0=UGC, 1=Player, 2=Portable, 3=Mirror, 4=Capture
    int cameraType;

    // 0/1, true if camera output is flipped (mirror, flipped portal camera)
    int isMirroredView;

    // Layer culling mask of rendering camera
    int cameraCullingMask;

    // World-space rendering camera position.
    // This is correct for all camera types, including mirrors.
    float3 cameraPos;

    // World-space player camera position
    float3 playerCameraPos;

    // World-space player camera rotation quaternion
    float4 playerCameraRot;

    float3 padding;
};

StructuredBuffer<CVRRenderingData> _CVRRenderingBuffer;
#define CVR_RENDERING _CVRRenderingBuffer[0]

// Camera type enum (0=UGC, 1=Player, 2=Portable, 3=Mirror, 4=Capture)
int CVR_CameraType() { return CVR_RENDERING.cameraType; }

// 0/1, mirrored camera output
int CVR_IsMirroredView() { return CVR_RENDERING.isMirroredView; }

// Layer mask of active rendering camera
int CVR_CameraCullingMask() { return CVR_RENDERING.cameraCullingMask; }

// World-space position of rendering camera.
// This is correct for all camera types, including mirrors.
float3 CVR_CameraPos() { return CVR_RENDERING.cameraPos; }

// World-space position of player camera
float3 CVR_PlayerCameraPos() { return CVR_RENDERING.playerCameraPos; }

// World-space rotation of player camera
float4 CVR_PlayerCameraRot() { return CVR_RENDERING.playerCameraRot; }

struct CVRGlobalShaderData
{
    // Local milliseconds since midnight
    int timeOfDayMs;

    // UTC milliseconds since midnight
    int timeOfDayMsUtc;

    // Day of year (1-365/366)
    int dayOfYear;

    // 0/1, true if leap year
    int isLeapYear;

    // 0/1, true if in VR
    int isUsingVr;

    // Local player's up direction vector
    float3 localPlayerUp;

    // Main menu fade (0 closed, 1 open, ~1s transition)
    float mainMenuFade;

    // Quick menu fade (0 closed, 1 open, ~1s transition)
    float quickMenuFade;

    // RTT to game server in ms
    int networkPing;

    // Connected player count (including self, min 1)
    int playerCount;

    // 0/1, full-body tracking calibrated
    float trackingFullBodyActive;

    // Battery percentages (0-100, 0 if not found)
    float trackingLeftHandBattery;
    float trackingRightHandBattery;
    float trackingLeftElbowBattery;
    float trackingRightElbowBattery;
    float trackingLeftFootBattery;
    float trackingRightFootBattery;
    float trackingLeftKneeBattery;
    float trackingRightKneeBattery;
    float trackingHipsBattery;

    float3 padding;
};

StructuredBuffer<CVRGlobalShaderData> _CVRGlobalBuffer;
#define CVR_GLOBALS   _CVRGlobalBuffer[0]

// Local milliseconds since midnight
int CVR_TimeOfDayMs() { return CVR_GLOBALS.timeOfDayMs; }

// UTC milliseconds since midnight
int CVR_TimeOfDayMsUtc() { return CVR_GLOBALS.timeOfDayMsUtc; }

// Day of year (1-365/366)
int CVR_DayOfYear() { return CVR_GLOBALS.dayOfYear; }

// 0/1, true if leap year
int CVR_IsLeapYear() { return CVR_GLOBALS.isLeapYear; }

// 0/1, true if in VR
int CVR_IsUsingVr() { return CVR_GLOBALS.isUsingVr; }

// Local player's up direction vector
float3 CVR_LocalPlayerUp() { return CVR_GLOBALS.localPlayerUp; }

// Main menu fade (0 closed, 1 open)
float  CVR_MainMenuFade() { return CVR_GLOBALS.mainMenuFade; }

// Quick menu fade (0 closed, 1 open)
float  CVR_QuickMenuFade() { return CVR_GLOBALS.quickMenuFade; }

// RTT to game server in ms
int CVR_NetworkPing() { return CVR_GLOBALS.networkPing; }

// Connected player count
int CVR_PlayerCount() { return CVR_GLOBALS.playerCount; }

// 0/1, full-body tracking calibrated
float  CVR_TrackingFullBodyActive() { return CVR_GLOBALS.trackingFullBodyActive; }

// Battery % for left hand tracker
float  CVR_TrackingLeftHandBattery() { return CVR_GLOBALS.trackingLeftHandBattery; }

// Battery % for right hand tracker
float  CVR_TrackingRightHandBattery() { return CVR_GLOBALS.trackingRightHandBattery; }

// Battery % for left elbow tracker
float  CVR_TrackingLeftElbowBattery() { return CVR_GLOBALS.trackingLeftElbowBattery; }

// Battery % for right elbow tracker
float  CVR_TrackingRightElbowBattery() { return CVR_GLOBALS.trackingRightElbowBattery; }

// Battery % for left foot tracker
float  CVR_TrackingLeftFootBattery() { return CVR_GLOBALS.trackingLeftFootBattery; }

// Battery % for right foot tracker
float  CVR_TrackingRightFootBattery() { return CVR_GLOBALS.trackingRightFootBattery; }

// Battery % for left knee tracker
float  CVR_TrackingLeftKneeBattery() { return CVR_GLOBALS.trackingLeftKneeBattery; }

// Battery % for right knee tracker
float  CVR_TrackingRightKneeBattery() { return CVR_GLOBALS.trackingRightKneeBattery; }

// Battery % for hips tracker
float  CVR_TrackingHipsBattery() { return CVR_GLOBALS.trackingHipsBattery; }

#endif // CVR_GLOBALS_CGINC