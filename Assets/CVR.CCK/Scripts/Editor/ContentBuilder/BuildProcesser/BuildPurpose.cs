namespace CVR.CCKEditor.ContentBuilder
{
    /// <summary>
    /// Purpose of the current build operation.
    /// Used to determine how content should be built (e.g. don't lock shaders for local testing).
    /// </summary>
    public enum BuildPurpose
    {
        /// <summary>
        /// Content will be published online.
        /// </summary>
        OnlinePublish,
        
        /// <summary>
        /// Content is being built for local testing.
        /// </summary>
        LocalTest,
        
        /// <summary>
        /// Content is building on entering playmode.
        /// This is not being fully built into a bundle.
        /// </summary>
        PlayMode
    }
}