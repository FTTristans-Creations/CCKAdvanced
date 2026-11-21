namespace CVR.CCKEditor.ContentBuilder
{
    public class LegalAssurance
    {
        public LegalAssurance(bool hasRightsToTheContentBeingUploaded, bool contentIsTaggedAppropriately)
        {
            HasRightsToTheContentBeingUploaded = hasRightsToTheContentBeingUploaded;
            ContentIsTaggedAppropriately = contentIsTaggedAppropriately;
        }

        public bool IsBothAccepted => HasRightsToTheContentBeingUploaded && ContentIsTaggedAppropriately;
        public bool HasRightsToTheContentBeingUploaded { get; }
        public bool ContentIsTaggedAppropriately { get; }
    }
}