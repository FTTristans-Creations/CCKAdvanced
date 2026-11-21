// #define CVR_CLIENT

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CVR.CCK;
using UnityEngine;
using Random = UnityEngine.Random;

#if CVR_CLIENT
using ABI_RC.Core.Player;
#endif

namespace ABI.CCK.Components
{
    [HelpURL(WebLinks.CCKDocsStateBehavioursUrl + "animator-driver")]
    public class AnimatorDriver : StateMachineBehaviour
    {
        public List<AnimatorDriverTask> EnterTasks = new();
        public List<AnimatorDriverTask> ExitTasks = new();
        
        public bool localOnly;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
#if CVR_CLIENT
            if (!_initialized) Initialize(animator);
#endif
            foreach (AnimatorDriverTask task in EnterTasks) task.Execute(animator, this);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
#if CVR_CLIENT
            if (!_initialized) Initialize(animator);
#endif
            foreach (AnimatorDriverTask task in ExitTasks) task.Execute(animator, this);
        }
        
        #if CVR_CLIENT
        
        public enum AnimatorType
        {
            LocalAvatar,
            RemoteAvatar,
            Spawnable,
            MiscAnimator
        }

        public AnimatorType animatorType = AnimatorType.MiscAnimator;
        internal PlayerSetup playerSetup;
        internal CVRSpawnable spawnable;
        internal PuppetMaster puppetMaster;
        
        private bool _initialized;
        
        private void Initialize(Animator animator)
        {
            _initialized = true;

            playerSetup = animator.gameObject.GetComponentInParent<PlayerSetup>();
            if (playerSetup != null && playerSetup._animator == animator)
            {
                animatorType = AnimatorType.LocalAvatar;
                return;
            }

            puppetMaster = animator.gameObject.GetComponentInParent<PuppetMaster>();
            if (puppetMaster != null && puppetMaster._animator == animator)
            {
                animatorType = AnimatorType.RemoteAvatar;
                return;
            }
            
            spawnable = animator.gameObject.GetComponentInParent<CVRSpawnable>();
            if (spawnable != null)
            {
                animatorType = AnimatorType.Spawnable;
                return;
            }
            
            animatorType = AnimatorType.MiscAnimator;
        }
        
        #endif
    }

    [System.Serializable]
    public class AnimatorDriverTask
    {
        #region Enums
        
        public enum Operator
        {
            Set,
            Addition,
            Subtraction,
            Multiplication,
            Division,
            Modulo,
            Power,
            Log,
            Equal,
            NotEqual,
            LessThan,
            LessEqual,
            MoreThan,
            MoreEqual,
            IPart,
            FPart,
            LogicalAnd,
            LogicalOr,
            BitwiseAnd,
            BitwiseOr,
            BitwiseXor,
            LeftShift,
            RightShift,
            RotateLeft,
            RotateRight,
            Conditional
        }

        public enum SourceType
        {
            Static,
            Parameter,
            Random
        }

        public enum ParameterType
        {
            None,
            Float,
            Int,
            Bool,
            Trigger
        }
        
        #endregion Enums

        #region Fields
        
        public ParameterType targetType = ParameterType.None;
        public string targetName = "";

        public Operator op = Operator.Set;

        public SourceType aType = SourceType.Static;
        public float aValue;
        public float aMax = 1f;
        public ParameterType aParamType;
        public string aName = "";
        
        public SourceType bType = SourceType.Static;
        public float bValue;
        public float bMax = 1f;
        public ParameterType bParamType;
        public string bName = "";

        public SourceType cType = SourceType.Static;
        public float cValue;
        public float cMax = 1f;
        public ParameterType cParamType;
        public string cName = "";
        
        #endregion Fields

        #region Execution
        
        public void Execute(Animator animator, AnimatorDriver driver)
        {
#if CVR_CLIENT
            if (driver.animatorType == AnimatorDriver.AnimatorType.RemoteAvatar && driver.localOnly) return;
            if (driver.animatorType == AnimatorDriver.AnimatorType.Spawnable && !driver.spawnable.IsMine() && driver.localOnly) return;
#endif
            
            float valA = GetSourceValue(animator, aType, aValue, aMax, aParamType, aName);

            // Operations that only need A
            switch (op)
            {
                case Operator.Set:
                    ApplyResult(animator, driver, valA);
                    return;
                case Operator.IPart:
                    ApplyResult(animator, driver, Mathf.Floor(valA));
                    return;
                case Operator.FPart:
                    ApplyResult(animator, driver, valA - Mathf.Floor(valA));
                    return;
            }

            // Operations that need B
            float valB = GetSourceValue(animator, bType, bValue, bMax, bParamType, bName);
            
            // Conditional operation needs C
            float valC = 0f;
            if (op == Operator.Conditional)
                valC = GetSourceValue(animator, cType, cValue, cMax, cParamType, cName);
            
            float res = PerformOperation(valA, valB, valC);
            ApplyResult(animator, driver, res);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetSourceValue(Animator animator, SourceType sourceType, float value, float max, ParameterType paramType, string name)
        {
            return sourceType switch
            {
                SourceType.Static => value,
                SourceType.Random => Random.Range(value, max),
                SourceType.Parameter => paramType switch
                {
                    ParameterType.Bool => animator.GetBool(name) ? 1f : 0f,
                    ParameterType.Float => animator.GetFloat(name),
                    ParameterType.Int => animator.GetInteger(name),
                    _ => 0f
                },
                _ => 0f
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float PerformOperation(float valA, float valB, float valC)
        {
            switch (op)
            {
                case Operator.Addition: return valA + valB;
                case Operator.Subtraction: return valA - valB;
                case Operator.Multiplication: return valA * valB;
                case Operator.Division: return valA / valB;
                case Operator.Modulo: return valA % valB;
                case Operator.Power: return Mathf.Pow(valA, valB);
                case Operator.Log: return Mathf.Log(valA, valB);
                case Operator.Equal: return Mathf.Approximately(valA, valB) ? 1f : 0f;
                case Operator.NotEqual: return !Mathf.Approximately(valA, valB) ? 1f : 0f;
                case Operator.LessThan: return valA < valB ? 1f : 0f;
                case Operator.LessEqual: return valA <= valB ? 1f : 0f;
                case Operator.MoreThan: return valA > valB ? 1f : 0f;
                case Operator.MoreEqual: return valA >= valB ? 1f : 0f;
                case Operator.LogicalAnd: return (valA >= 0.5f && valB >= 0.5f) ? 1f : 0f;
                case Operator.LogicalOr: return (valA >= 0.5f || valB >= 0.5f) ? 1f : 0f;
                case Operator.BitwiseAnd: return (int)valA & (int)valB;
                case Operator.BitwiseOr: return (int)valA | (int)valB;
                case Operator.BitwiseXor: return (int)valA ^ (int)valB;
                case Operator.LeftShift: return (int)valA << (int)valB;
                case Operator.RightShift: return (int)valA >> (int)valB;
                case Operator.RotateLeft:
                    {
                        int value = (int)valA;
                        int shift = (int)valB & 0x1F; // Fast mod 32 for bit operations
                        return (value << shift) | (value >> (32 - shift));
                    }
                case Operator.RotateRight:
                    {
                        int value = (int)valA;
                        int shift = (int)valB & 0x1F; // Fast mod 32 for bit operations
                        return (value >> shift) | (value << (32 - shift));
                    }
                case Operator.Conditional: return valA >= 0.5f ? valB : valC;
                
                // Handled already, defining so Rider shuts it
                case Operator.Set:
                case Operator.IPart:
                case Operator.FPart:
                default: break;
            }
            
            return 0f;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyResult(Animator animator, AnimatorDriver driver, float res)
        {
#if CVR_CLIENT
            switch (driver.animatorType)
            {
                case AnimatorDriver.AnimatorType.LocalAvatar:
                    PlayerSetup.Instance.ChangeAnimatorParam(targetName, res);
                    break;
                case AnimatorDriver.AnimatorType.RemoteAvatar:
                    switch (targetType)
                    {
                        case ParameterType.Bool:
                        case ParameterType.Trigger:
                            driver.puppetMaster.animatorManager.SetParameter(targetName, res >= 0.5f);
                            break;
                        case ParameterType.Float:
                            driver.puppetMaster.animatorManager.SetParameter(targetName, res);
                            break;
                        case ParameterType.Int:
                            driver.puppetMaster.animatorManager.SetParameter(targetName, Mathf.RoundToInt(res));
                            break;
                    }
                    break;
                case AnimatorDriver.AnimatorType.Spawnable:
                    if (!driver.spawnable.UpdateValueIfNecessary(targetName, res))
                    {
                        switch (targetType)
                        {
                            case ParameterType.Bool:
                                animator.SetBool(targetName, res >= 0.5f);
                                break;
                            case ParameterType.Trigger:
                                if (res >= 0.5f) animator.SetTrigger(targetName);
                                break;
                            case ParameterType.Float:
                                animator.SetFloat(targetName, res);
                                break;
                            case ParameterType.Int:
                                animator.SetInteger(targetName, Mathf.RoundToInt(res));
                                break;
                        }
                    }
                    break;
                
                
                case AnimatorDriver.AnimatorType.MiscAnimator:
                default:
#endif 
                    switch (targetType)
                    {
                        case ParameterType.Bool:
                            animator.SetBool(targetName, res >= 0.5f);
                            break;
                        case ParameterType.Trigger:
                            if (res >= 0.5f) animator.SetTrigger(targetName);
                            break;
                        case ParameterType.Float:
                            animator.SetFloat(targetName, res);
                            break;
                        case ParameterType.Int:
                            animator.SetInteger(targetName, Mathf.RoundToInt(res));
                            break;
                        // Handled already, defining so Rider shuts it
                        case ParameterType.None:
                        default:
                            break;
                    }
#if CVR_CLIENT
                    break;
            }
#endif
        }
        
        #endregion Execution
    }
}