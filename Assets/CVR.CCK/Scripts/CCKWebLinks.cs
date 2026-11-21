namespace CVR.CCK
{
    public static class WebLinks
    {
        #region ChilloutVR URLs
        
        public const string CVRHubUrl = "https://hub.chilloutvr.net/";
        public const string CVRHubLoginUrl = CVRHubUrl + "login";
        public const string CVRHubSignupUrl = CVRHubUrl + "login?page=signup";
        
        public const string CVRFeedbackUrl = "https://feedback.chilloutvr.net/";
        public const string CVRDocsUrl = "https://docs.chilloutvr.net/";
        
        #endregion ChilloutVR URLs

        #region CCK Documentation URLs
        
        public const string CCKDocsUrl = CVRDocsUrl + "cck/";
        //public const string CCKDocsUrl = "http://127.0.0.1:8000/"; // Local testing URL
        
        public const string CCKDocsComponentsUrl = CCKDocsUrl + "components/";
        public const string CCKDocsStateBehavioursUrl = CCKDocsUrl + "state-behaviours/";
        public const string CCKDocsValidationsUrl = CCKDocsUrl + "validations/";
        public const string CCKDocsContentTagsUrl = CCKDocsUrl + "content-tags/";
        
        #endregion CCK Documentation URLs
    }
}