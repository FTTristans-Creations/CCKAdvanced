using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CVR.Newtonsoft.Json;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form
{
    [JsonConverter(typeof(ContentTagsConverter))]
    public struct ContentTags
    {
        public bool Gore { get; set; }
        public bool Horror { get; set; }
        public bool Jumpscare { get; set; }
        
        /// <summary>
        /// New, added on CCK 4 new build pannel
        /// </summary>
        public bool Explicit { get; set; }
        public bool Suggestive { get; set; }
        public bool Violence { get; set; }
        
        /// <summary>
        /// New, added on CCK 4 new build pannel
        /// </summary>
        public bool FlashingEffects { get; set; }
        
        /// <summary>
        /// CCK auto-tags this: audio-clip is above -8db
        /// </summary>
        public bool LoudAudio { get; set; }
        public bool ScreenEffects { get; set; }
        
        /// <summary>
        /// CCK auto-tags this: an audio source is 2D or above 15m distance
        /// Only available in Props and Avatars
        /// </summary>
        public bool LongRangeAudio { get; set; }
        
        /// <summary>
        /// Whether the file is tagged as mature content or not
        /// </summary>
        public bool IsMatureContent => Explicit || Gore;
        
        private static readonly PropertyInfo[] TagDataProperties = typeof(ContentTags).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        /// <summary>
        /// Get a UgcTagsData object from a string array
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static ContentTags FromStringArray(IEnumerable<string> tags)
        {
            object tmpThis = new ContentTags();
            foreach (var tag in tags)
            {
                var field = TagDataProperties.FirstOrDefault(x => x.Name.Equals(tag, StringComparison.InvariantCultureIgnoreCase));
                if (field == null) continue;
                field.SetValue(tmpThis, true);
            }
            return (ContentTags)tmpThis;
        }
        /// <summary>
        /// Build a string array from a UgcTagsData object
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ToStringArray()
        {
            var tmpThis = (object)this; // c# is the worst language on this fucking planet.
            return from field in TagDataProperties where field.GetValue(tmpThis) is true select field.Name.ToLowerInvariant();
        }
        
        public static ContentTags operator |(ContentTags a, ContentTags b)
        {
            return new ContentTags
            {
                Gore = a.Gore || b.Gore,
                Horror = a.Horror || b.Horror,
                Jumpscare = a.Jumpscare || b.Jumpscare,
                Explicit = a.Explicit || b.Explicit,
                Suggestive = a.Suggestive || b.Suggestive,
                Violence = a.Violence || b.Violence,
                FlashingEffects = a.FlashingEffects || b.FlashingEffects,
                LoudAudio = a.LoudAudio || b.LoudAudio,
                ScreenEffects = a.ScreenEffects || b.ScreenEffects,
                LongRangeAudio = a.LongRangeAudio || b.LongRangeAudio
            };
        }
    }
}