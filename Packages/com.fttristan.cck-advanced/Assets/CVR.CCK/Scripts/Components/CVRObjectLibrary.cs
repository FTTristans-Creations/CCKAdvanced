using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-object-library")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRObjectLibrary : MonoBehaviour
    {
        public List<CVRObjectCatalogCategory> objectCatalogCategories = new List<CVRObjectCatalogCategory>();
        public List<CVRObjectCatalogEntry> objectCatalogEntries = new List<CVRObjectCatalogEntry>();
    }
}
