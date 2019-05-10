using System.Collections.Generic;

namespace Moriyama.AzureSearch.Umbraco.Application.Models.Indexing
{
    public class NestedContent
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string NcContentTypeAlias { get; set; }
        public List<UrlPickerLink> UrlPicker { get; set; }
    }
}
