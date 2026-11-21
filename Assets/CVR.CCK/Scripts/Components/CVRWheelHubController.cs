using System.Collections;
using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Wheel Hub Controller")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-wheel-hub-controller")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRWheelHubController : MonoBehaviour
    {
        [Header("Setup")]
        public Rigidbody rigidBody;
        public List<CVRWheelController> wheels = new List<CVRWheelController>();
        
        [Header("Config")]
        public float motorTorque = 200;
        public float brakeTorque = 200;
        [Tooltip("The theoretical maximum forward speed in m/s")]
        public float maxSpeedForward = 20;
        [Tooltip("The theoretical maximum backwards speed in m/s")]
        public float maxSpeedReverse = 20;
        public float steeringRange = 30;
        public float steeringRangeAtMaxSpeed = 10;
        [Tooltip("Flips the forward direction regarding control and speed calculations indicated by the white arrow")]
        public bool reverseForward = false;
        public AnimationCurve torqueCurve = new AnimationCurve(new[]{new Keyframe(0f, 0.25f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f)});
        
        public bool guiGearboxFoldout;
        public bool enableGearbox = false;
        public List<CVRWheelHubControllerGear> gears = new List<CVRWheelHubControllerGear>();
        public float shiftTime = 0.1f;
        public bool mirrorGearsToReverse = false;
        public bool automaticShifting = true;
        public int desiredGear = 0;

        [Header("Input")]
        public float inputAccelerate = 0f;
        public float inputBrake = 0f;
        public float inputSteering = 0f;
        [Space]
        [Tooltip("disables the steering of wheels and instead turns by driving the wheels at different speeds/directions")]
        public bool useSkidSteering = false;
        public float lowSkidSteeringSpeed = 5f;
        [Tooltip("Applies half your brake torque when trying to turn while driving forward while using skid steering")]
        public bool useBrakeTurning = false;
        [Space]
        [Tooltip("Applies brakes when moving forward and inputting backwards and vice versa")]
        public bool counterInputBrake = false;
        [Tooltip("if enabled checks if the player is in the first control seat of the prop. If so it picks up the players inputs using the default car control scheme")]
        public bool useDefaultCarControls = false;
        [Space]
        [Tooltip("Gradually applies passive braking when the car has no control inputs.")]
        public bool usePassiveBraking = false;
        public float passiveBrakingTorque = 200;

        [Header("Debug")]
        [Tooltip("Used for debugging the car in playmode in your editor. Has no effect in-game")]
        public bool debugWASDSteering = false;
        
        //internals
        private float acceleration;
        private float forwardSpeed;
        private float speedFactor;
        private float absoluteMaxSpeedForward;
        private float absoluteSpeedFactor;
        private float currentMotorTorque;
        private float currentSteerRange;
        private bool isAccelerating;
        private bool isBraking;

        private int currentGear = 0;
        private float currentShiftTime;
        private int shiftDirection = 0;


        private void Start()
        {
            absoluteMaxSpeedForward = maxSpeedForward;
            if (enableGearbox)
            {
                foreach (var gear in gears)
                {
                    absoluteMaxSpeedForward = Mathf.Max(absoluteMaxSpeedForward, gear.maxSpeed);
                }
            }
        }
        
        private void Update()
        {
            if (debugWASDSteering)
            {
                inputAccelerate = (Input.GetKey(KeyCode.W)?1f:0f) + (Input.GetKey(KeyCode.S)?-1f:0f);
                inputSteering = (Input.GetKey(KeyCode.D)?1f:0f) + (Input.GetKey(KeyCode.A)?-1f:0f);
                inputBrake = (Input.GetKey(KeyCode.Space)?1f:0f);
            }
        }
        
        private void FixedUpdate()
        {
            if (reverseForward)
            {
                forwardSpeed = Vector3.Dot(transform.forward * -1f, rigidBody.velocity);
                acceleration = inputAccelerate * -1f;
            }
            else
            {
                forwardSpeed = Vector3.Dot(transform.forward, rigidBody.velocity);
                acceleration = inputAccelerate;
            }
            
            if (enableGearbox)
            {
                CalculateGearBox();
            }

            if (forwardSpeed > 0f)
            {
                speedFactor = Mathf.InverseLerp(0, maxSpeedForward, Mathf.Abs(forwardSpeed));
            }
            else
            {
                speedFactor = Mathf.InverseLerp(0, maxSpeedReverse, Mathf.Abs(forwardSpeed));
            }

            absoluteSpeedFactor = Mathf.InverseLerp(0, absoluteMaxSpeedForward, Mathf.Abs(forwardSpeed));

            currentMotorTorque = torqueCurve.Evaluate(speedFactor) * motorTorque;
            
            currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, absoluteSpeedFactor);

            isAccelerating = Mathf.Abs(inputAccelerate) > 0f;
            isBraking = inputBrake > 0f;

            if (counterInputBrake && isAccelerating && ((inputAccelerate > 0f && forwardSpeed < -0.1f) || (inputAccelerate < 0f && forwardSpeed > 0.1f)))
            {
                isAccelerating = false;
                isBraking = true;
                inputBrake = Mathf.Abs(inputAccelerate);
            }

            if (useSkidSteering)
            {
                //TODO: Variables will be reworked once it is known to work
                float skidRatio = Mathf.Clamp01(Mathf.InverseLerp(0, lowSkidSteeringSpeed, Mathf.Abs(forwardSpeed)));

                float leftTorque = 0f;
                float leftBrake = 0f;
                float rightTorque = 0f;
                float rightBrake = 0f;

                float steerFactorLeft = inputSteering * 0.5f + acceleration * 0.5f;
                float steerFactorRight = inputSteering * -0.5f + acceleration * 0.5f;

                leftTorque = steerFactorLeft * currentMotorTorque;
                rightTorque = steerFactorRight * currentMotorTorque;

                float relativeInputAccelerate = reverseForward ? -inputAccelerate : inputAccelerate;
                float relativeInputSteering = reverseForward ? -inputSteering : inputSteering;
                
                if (useBrakeTurning && relativeInputAccelerate > 0.01 && Mathf.Abs(inputSteering) > 0.01)
                {
                    leftBrake = Mathf.Clamp01(steerFactorRight - 0.5f) * brakeTorque * skidRatio;
                    rightBrake = Mathf.Clamp01(steerFactorLeft - 0.5f) * brakeTorque * skidRatio;
                }

                if (useBrakeTurning && relativeInputAccelerate < -0.01 && Mathf.Abs(inputSteering) > 0.01)
                {
                    leftBrake = Mathf.Clamp01((inputSteering * 0.5f + Mathf.Abs(acceleration * 0.5f)) - 0.5f) * brakeTorque * skidRatio;
                    rightBrake = Mathf.Clamp01((inputSteering * -0.5f + Mathf.Abs(acceleration * 0.5f)) - 0.5f) * brakeTorque * skidRatio;
                }

                if (!useBrakeTurning && relativeInputAccelerate > 0.01 && Mathf.Abs(inputSteering) > 0.01)
                {
                    leftTorque = (steerFactorLeft - 0.5f) * currentMotorTorque;
                    rightTorque = (steerFactorRight - 0.5f) * currentMotorTorque;

                    if (leftTorque < 0)
                    {
                        leftTorque /= (1 + skidRatio);
                    }

                    if (rightTorque < 0)
                    {
                        rightTorque /= (1 + skidRatio);
                    }
                }

                if (!useBrakeTurning && relativeInputAccelerate < -0.01 && Mathf.Abs(inputSteering) > 0.01)
                {
                    leftTorque = ((relativeInputSteering * 0.5f + Mathf.Abs(acceleration * 0.5f)) - 0.5f) * currentMotorTorque;
                    rightTorque = ((relativeInputSteering * -0.5f + Mathf.Abs(acceleration * 0.5f)) - 0.5f) * currentMotorTorque;

                    if (leftTorque > 0)
                    {
                        leftTorque /= (1 + skidRatio);
                    }

                    if (rightTorque > 0)
                    {
                        rightTorque /= (1 + skidRatio);
                    }
                }

                if (!reverseForward)
                {
                    if (leftTorque > 0 && Mathf.Abs(inputAccelerate) < 0.01 && Mathf.Abs(inputSteering) > 0.01f)
                    {
                        leftTorque /= (1 + skidRatio);
                    }

                    if (rightTorque > 0 && Mathf.Abs(inputAccelerate) < 0.01 && Mathf.Abs(inputSteering) > 0.01f)
                    {
                        rightTorque /= (1 + skidRatio);
                    }
                }
                else
                {
                    if (leftTorque < 0 && Mathf.Abs(inputAccelerate) < 0.01 && Mathf.Abs(inputSteering) > 0.01f)
                    {
                        leftTorque /= (1 + skidRatio);
                    }

                    if (rightTorque < 0 && Mathf.Abs(inputAccelerate) < 0.01 && Mathf.Abs(inputSteering) > 0.01f)
                    {
                        rightTorque /= (1 + skidRatio);
                    }
                }

                if (usePassiveBraking && Mathf.Abs(inputAccelerate) < 0.01 && Mathf.Abs(inputSteering) < 0.01 && inputBrake < 0.01)
                {
                    leftBrake = passiveBrakingTorque * Mathf.Clamp(1 - speedFactor, 0.01f, 1f);
                    rightBrake = passiveBrakingTorque * Mathf.Clamp(1 - speedFactor, 0.01f, 1f);
                }

                foreach (var wheel in wheels)
                {
                    if (wheel.isOnLeftSide)
                    {
                        wheel.motorTorque = leftTorque;
                        wheel.brakeTorque = leftBrake;
                    }
                    else
                    {
                        wheel.motorTorque = rightTorque;
                        wheel.brakeTorque = rightBrake;
                    }

                    if (wheel.isSteerable) wheel.steerAngle = 0f;

                    wheel.sidewaysFrictionStiffness = Mathf.Lerp(wheel.skidSteerSidewaysFriction, wheel.sidewaysFriction, skidRatio);

                    if (isBraking)
                    {
                        wheel.motorTorque = 0f;
                        wheel.brakeTorque = brakeTorque;
                    }
                }
            }
            else
            {
                foreach (var wheel in wheels)
                {
                    if (wheel.isSteerable)
                    {
                        wheel.steerAngle = inputSteering * currentSteerRange * wheel.steerMultiplier;
                    }

                    if (isAccelerating && !isBraking)
                    {
                        if (wheel.isMotorized)
                        {
                            wheel.motorTorque = acceleration * currentMotorTorque;
                        }
                        wheel.brakeTorque = 0;
                    }
                    else if (isAccelerating && isBraking)
                    {
                        if (wheel.isMotorized)
                        {
                            wheel.motorTorque = acceleration * currentMotorTorque;
                        }
                        if (wheel.isBraking)
                        {
                            wheel.brakeTorque = Mathf.Abs(inputBrake) * brakeTorque;
                        }
                    }
                    else if (isBraking)
                    {
                        if (wheel.isBraking)
                        {
                            wheel.brakeTorque = Mathf.Abs(inputBrake) * brakeTorque;
                        }
                        wheel.motorTorque = 0;
                    }
                    else
                    {
                        wheel.motorTorque = 0;
                        wheel.brakeTorque = 0;
                    }
                }

                if (usePassiveBraking && Mathf.Abs(inputAccelerate) < 0.01 && Mathf.Abs(inputSteering) < 0.01 && inputBrake < 0.01)
                {
                    foreach (var wheel in wheels)
                    {
                        if (wheel.isBraking)
                        {
                            wheel.brakeTorque = passiveBrakingTorque * Mathf.Clamp(1 - speedFactor,0.01f,1f);
                        }
                    }
                }
            }
        }

        private void CalculateGearBox()
        {
            //Shift Up and Down
            if (speedFactor > gears[currentGear].shiftUpRatio && gears.Count > currentGear + 1 && shiftDirection == 0 && (automaticShifting || desiredGear > currentGear) && (mirrorGearsToReverse || forwardSpeed > 0f))
            {
                shiftDirection = 1;
                currentShiftTime = Time.time;
            }

            if (speedFactor < gears[currentGear].shiftDownRatio && currentGear > 0 && shiftDirection == 0 && (automaticShifting || desiredGear < currentGear))
            {
                shiftDirection = -1;
                currentShiftTime = Time.time;
            }
            
            //Handle Shifting
            if (shiftDirection != 0)
            {
                motorTorque = 0;
                if (Time.time - currentShiftTime > shiftTime)
                {
                    currentGear += shiftDirection;
                    shiftDirection = 0;
                }
                else
                {
                    return;
                }
            }
            
            //Apply values from gear
            motorTorque = gears[currentGear].torque;
            maxSpeedForward = gears[currentGear].maxSpeed;
            if (mirrorGearsToReverse)
            {
                maxSpeedReverse = gears[currentGear].maxSpeed;
            }
            else
            {
                maxSpeedReverse = gears[0].maxSpeed;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            if (reverseForward)
                DrawArrow(transform.position, transform.forward * -1f, 1);
            else
                DrawArrow(transform.position, transform.forward, 1);
        }
        
        private void DrawArrow(Vector3 position, Vector3 direction, float size)
        {
            var a1 = RotatePointAroundPivot(position + new Vector3(0, 0.1f * size, 0), position, direction);
            var a2 = RotatePointAroundPivot(position + new Vector3(0.1f * size, 0, 0), position, direction);
            var a3 = RotatePointAroundPivot(position + new Vector3(0, -0.1f * size, 0), position, direction);
            var a4 = RotatePointAroundPivot(position + new Vector3(-0.1f * size, 0, 0), position, direction);
            
            var b1 = RotatePointAroundPivot(position + new Vector3(0, 0.1f * size, 0.3f * size), position, direction);
            var b2 = RotatePointAroundPivot(position + new Vector3(0.1f * size, 0, 0.3f * size), position, direction);
            var b3 = RotatePointAroundPivot(position + new Vector3(0, -0.1f * size, 0.3f * size), position, direction);
            var b4 = RotatePointAroundPivot(position + new Vector3(-0.1f * size, 0, 0.3f * size), position, direction);
            
            var c1 = RotatePointAroundPivot(position + new Vector3(0, 0.2f * size, 0.3f * size), position, direction);
            var c2 = RotatePointAroundPivot(position + new Vector3(0.2f * size, 0, 0.3f * size), position, direction);
            var c3 = RotatePointAroundPivot(position + new Vector3(0, -0.2f * size, 0.3f * size), position, direction);
            var c4 = RotatePointAroundPivot(position + new Vector3(-0.2f * size, 0, 0.3f * size), position, direction);
            
            var d = RotatePointAroundPivot(position + new Vector3(0, 0, 0.5f * size), position, direction);
            
            Gizmos.DrawLine(position, a1);
            Gizmos.DrawLine(position, a2);
            Gizmos.DrawLine(position, a3);
            Gizmos.DrawLine(position, a4);
            
            Gizmos.DrawLine(a1, b1);
            Gizmos.DrawLine(a2, b2);
            Gizmos.DrawLine(a3, b3);
            Gizmos.DrawLine(a4, b4);
            
            Gizmos.DrawLine(b1, c1);
            Gizmos.DrawLine(b2, c2);
            Gizmos.DrawLine(b3, c3);
            Gizmos.DrawLine(b4, c4);
            
            Gizmos.DrawLine(c1, d);
            Gizmos.DrawLine(c2, d);
            Gizmos.DrawLine(c3, d);
            Gizmos.DrawLine(c4, d);
        }
            
        private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 direction)
        {
            var dir = point - pivot; // get point direction relative to pivot
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            dir = rotation * dir; // rotate it
            point = dir + pivot; // calculate rotated point
            return point; // return it
        }
    }

    [System.Serializable]
    public class CVRWheelHubControllerGear
    {
        public float maxSpeed;
        public float torque;
        public float shiftUpRatio = 0.8f;
        public float shiftDownRatio = 0.2f;
    }
}
