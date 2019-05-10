using System.Collections.Generic;

namespace Moriyama.AzureSearch.Umbraco.Application.Models.Indexing
{
    public class NestedContent
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string NcContentTypeAlias { get; set; }

        public string Description { get; set; }

        // UrlPicker is the alias of the property in Umbraco. 
        // The reason this was done like this was due to time related to inspecting the JArray.
        // It was the only solution possible within the timeframe.
        // Example json:
        /*
         {[
              {
                "key": "39b9bf9c-2b7a-4321-9365-a514e6f1a3c6",
                "name": "Item 1",
                "ncContentTypeAlias": "complexUrlPicker",
                "urlPicker": [
                  {
                    "name": "Google",
                    "target": "_blank",
                    "url": "Google.com",
                    "published": true,
                    "icon": "icon-link"
                  }
                ],
                "description": "This is a description"
              }
        }]
             */
        public List<UrlPickerLink> UrlPicker { get; set; }


    }
}
