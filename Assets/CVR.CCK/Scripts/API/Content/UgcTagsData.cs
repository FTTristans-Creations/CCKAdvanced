using JetBrains.Annotations;

namespace CVR.CCKEditor.API
{
    [PublicAPI]
    public struct UgcTagsData
    {
        public bool Gore;
        public bool Horror;
        public bool Jumpscare;
        public bool Nudity;             // Deprecated, replaced by Explicit
        public bool Suggestive;
        public bool Violence;
        public bool ContainsMusic;      // Deprecated
        public bool ExtremelyBright;    // Deprecated, replaced by Flashing Effects
        public bool ExtremelyHuge;      // Deprecated, to be handled on client as Removable
        public bool ExtremelySmall;     // Deprecated, to be handled on client as Removable
        public bool FlashingColors;     // Deprecated, replaced by Flashing Effects
        public bool FlashingLights;     // Deprecated, replaced by Flashing Effects
        public bool LoudAudio;          // Auto-tagged, an audioclip is above -8db
        public bool ParticleSystems;    // Deprecated, always was part of the removables system
        public bool ScreenEffects;
        public bool SpawnAudio;         // Deprecated, literally never did jack shit
        public bool LongRangeAudio;     // Auto-tagged, an audiosource is 2D or above 15m distance
        
        // TEMPORARY FIX
        public bool FlashingEffects
        {
            get => FlashingColors || FlashingLights || ExtremelyBright;
            set => FlashingColors = FlashingLights = ExtremelyBright = value;
        }
        public bool Explicit
        {
            get => Nudity;
            set => Nudity = value;
        }
        
        public static UgcTagsData operator |(UgcTagsData a, UgcTagsData b)
        {
            return new UgcTagsData
            {
                Gore = a.Gore || b.Gore,
                Horror = a.Horror || b.Horror,
                Jumpscare = a.Jumpscare || b.Jumpscare,
                Nudity = a.Nudity || b.Nudity,
                Suggestive = a.Suggestive || b.Suggestive,
                Violence = a.Violence || b.Violence,
                ContainsMusic = a.ContainsMusic || b.ContainsMusic,
                ExtremelyBright = a.ExtremelyBright || b.ExtremelyBright,
                ExtremelyHuge = a.ExtremelyHuge || b.ExtremelyHuge,
                ExtremelySmall = a.ExtremelySmall || b.ExtremelySmall,
                FlashingColors = a.FlashingColors || b.FlashingColors,
                FlashingLights = a.FlashingLights || b.FlashingLights,
                LoudAudio = a.LoudAudio || b.LoudAudio,
                ParticleSystems = a.ParticleSystems || b.ParticleSystems,
                ScreenEffects = a.ScreenEffects || b.ScreenEffects,
                SpawnAudio = a.SpawnAudio || b.SpawnAudio,
                LongRangeAudio = a.LongRangeAudio || b.LongRangeAudio
            };
        }
    }
}