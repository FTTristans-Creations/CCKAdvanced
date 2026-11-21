using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ABI.CCK.Components;
using CVR.CCKEditor.Validations.Context;
using CVR.CCKEditor.Validations.Steps;

namespace CVR.CCKEditor.Validations
{
    public static class Validator
    {
        // First func array is the root steps, which run on the RootComponent at the beginning of the validation pipeline.
        // Second func array is the object steps, which run against each object found while processing the content.
        private static readonly Dictionary<CVRAssetInfo.AssetType, (Func<IValidationStep>[] rootSteps, Func<IValidationStep>[] objectSteps)> _validationSteps = new()
        {
            [CVRAssetInfo.AssetType.Avatar] = (
                new Func<IValidationStep>[]
                {
                    () => new ProjectSetupStep(false),
                    () => new AvatarHeightRootStep(),
                    () => new MissingOrGenericAnimatorRootStep()
                },
                new Func<IValidationStep>[]
                {
                    () => new TotalTriangleCountStep(),
                    () => new TotalMaterialCountStep(),
                    () => new CameraMissingRTStep(),
                    () => new DepthLightDetectionValidationStep(),
                    () => new AudioLongRangeStep(),
                    () => new LoudAudioValidationStep(),
                    () => new AudioImportSettingsStep(true),
                    () => new ParticlePrewarmStep(true),
                    () => new NetworkedObjectStep(),
                    () => new ShaderErrorValidationStep(),
                    () => new ShaderStereoSupportStep(),
                    () => new TextureValidationStep(),
                    () => new LegacyBlendShapeNormalsStep(),
                    () => new MissingScriptsStep(),
                    () => new BrokenMonoBehaviourStep(),
                }
            ),
            [CVRAssetInfo.AssetType.Spawnable] = (
                new Func<IValidationStep>[]
                {
                    () => new ProjectSetupStep(false),
                },
                new Func<IValidationStep>[]
                {
                    () => new TotalTriangleCountStep(),
                    () => new TotalMaterialCountStep(),
                    () => new CameraMissingRTStep(),
                    () => new DepthLightDetectionValidationStep(),
                    () => new AudioLongRangeStep(),
                    () => new LoudAudioValidationStep(),
                    () => new AudioImportSettingsStep(true),
                    () => new ParticlePrewarmStep(true),
                    () => new NetworkedObjectStep(),
                    () => new ShaderErrorValidationStep(),
                    () => new ShaderStereoSupportStep(),
                    () => new TextureValidationStep(),
                    () => new LegacyBlendShapeNormalsStep(),
                    () => new MissingScriptsStep(),
                    () => new BrokenMonoBehaviourStep(),
                }
            ),
            [CVRAssetInfo.AssetType.World] = (
                new Func<IValidationStep>[]
                {
                    () => new ProjectSetupStep(true),
                    () => new SpawnpointOOBRootStep(),
                },
                new Func<IValidationStep>[]
                {
                    // () => new CameraMissingRTStep(), // TODO: Reverify this for Worlds
                    () => new DepthLightDetectionValidationStep(),
                    // () => new LoudAudioValidationStep(),
                    () => new AudioImportSettingsStep(false),
                    () => new ParticlePrewarmStep(false),
                    () => new NetworkedObjectStep(true), // Check for shitty world spawnables
                    () => new ShaderErrorValidationStep(),
                    () => new ShaderStereoSupportStep(),
                    () => new TextureValidationStep(false, false, false), // Do not enforce texture priority or readable
                    () => new LegacyBlendShapeNormalsStep(),
                    () => new MissingScriptsStep(),
                    () => new BrokenMonoBehaviourStep(),
                }
            )
        };

        public static async Task<ValidationPipelineResult> Validate(
            CVRAssetInfo assetInfo,
            Action<float> onProgress = null)
        {
            if (!_validationSteps.TryGetValue(assetInfo.type, out var config))
                throw new InvalidOperationException();

            ValidationPipeline pipeline = new();
            
            foreach (var stepFactory in config.rootSteps) pipeline.AddRootStep(stepFactory());
            foreach (var stepFactory in config.objectSteps) pipeline.AddPipelineStep(stepFactory());

            BaseValidationContext context = assetInfo.type switch
            {
                CVRAssetInfo.AssetType.Avatar => new AvatarValidationContext(assetInfo.gameObject),
                CVRAssetInfo.AssetType.Spawnable => new SpawnableValidationContext(assetInfo.gameObject),
                CVRAssetInfo.AssetType.World => new WorldValidationContext(assetInfo.gameObject),
                _ => throw new InvalidOperationException()
            };

            return await pipeline.ExecuteAsync(context, onProgress);
        }
    }
}