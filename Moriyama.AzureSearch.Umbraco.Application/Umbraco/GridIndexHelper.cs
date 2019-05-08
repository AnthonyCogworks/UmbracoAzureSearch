using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moriyama.AzureSearch.Umbraco.Application.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace Moriyama.AzureSearch.Umbraco.Application.Umbraco
{
    /// <summary>
    /// Parses the grid value into indexable values
    /// </summary>
    public class GridIndexHelper
    {
        private readonly log4net.ILog _logger;

        public GridIndexHelper(log4net.ILog logger)
        {
            _logger = logger;
        }

        public IEnumerable<KeyValuePair<string, string>> GetIndexValues(string value, string propertyAlias)
        {
            var result = new List<KeyValuePair<string, string>>();

            //if there is a value, it's a string and it's detected as json
            if (!value.DetectIsJson())
            {
                return result;
            }

            var sb = new StringBuilder();

            try
            {
                //get all values and put them into a single field (using JsonPath)
                var gridVal = JsonConvert.DeserializeObject<GridValue>(value);
                var rows = gridVal.Sections.SelectMany(x => x.Rows);

                foreach (var row in rows)
                {
                    var controls = row.Areas.SelectMany(x => x.Controls);

                    foreach (var control in controls)
                    {
                        var controlValue = control.Value;

                        if (controlValue.Type == JTokenType.String)
                        {
                            AppendValue(propertyAlias, row.Name, controlValue.Value<string>(), sb, result);
                        }
                        else if (controlValue.Type == JTokenType.Object)
                        {
                            AppendJsonObjectData(controlValue, propertyAlias, row.Name, sb, result);
                        }
                    }
                }

                if (sb.Length > 0)
                {
                    //First save the raw value to a raw field
                    //result.Add(new KeyValuePair<string, IEnumerable<object>>($"{UmbracoExamineIndex.RawFieldPrefix}{propertyAlias}", new[] { rawVal }));

                    //index the property with the combined/cleaned value
                    //result.Add(new KeyValuePair<string, string>(propertyAlias, sb.ToString()));
                }
            }
            catch (InvalidCastException ex)
            {
                //swallow...on purpose, there's a chance that this isn't the json format we are looking for and we don't want that to affect the website.
                _logger.Error(ex);
            }
            catch (JsonException ex)
            {
                //swallow...on purpose, there's a chance that this isn't json and we don't want that to affect the website.
                _logger.Error(ex);
            }
            catch (ArgumentException ex)
            {
                //swallow on purpose to prevent this error: Can not add Newtonsoft.Json.Linq.JValue to Newtonsoft.Json.Linq.JObject.
                _logger.Error(ex);
            }

            return result;
        }

        private void AppendJsonObjectData(JToken controlVal, string propertyAlias, string rowName, StringBuilder sb, List<KeyValuePair<string, string>> result)
        {
            // convert to dictionary so we can inspect values
            var controlItems = GetControlItems(controlVal);

            foreach (var item in controlItems)
            {
                if (item.Value is string stringValue)
                {
                    if (!string.IsNullOrWhiteSpace(stringValue))
                    {
                        if (stringValue.StartsWith("umb://")) { continue; } // do not index pickers

                        AppendValue(propertyAlias, rowName, stringValue, sb, result);
                    }
                }
                else if (item.Value is JArray jArray) 
                {
                    // value might be a url picker, so let's try to convert it
                    var urlPickers = GetUrlPickers(jArray);

                    foreach (var picker in urlPickers)
                    {
                        AppendValue(propertyAlias, rowName, picker.Name, sb, result);
                    }
                }
                else
                {
                    // do nothing because we are not expecting other types of data
                    _logger.Warn($"Azure Indexer: Unexpected data type {item.Key}");
                }
            }
        }


        private IEnumerable<UrlPickerLink> GetUrlPickers(JArray jArray)
        {
            try
            {
                var urlPickers = jArray.ToObject<IEnumerable<UrlPickerLink>>()
                                        .Where(x => !string.IsNullOrWhiteSpace(x.Url));

                return urlPickers;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                return new List<UrlPickerLink>();
            }
        }

        /// <summary>
        /// Converts JToken into a dictionary. This is pretty nasty, but it works.
        /// </summary>
        private IEnumerable<KeyValuePair<string, object>> GetControlItems(JToken controlVal)
        {
            try
            {
                var dictionary = controlVal.ToObject<Dictionary<string, object>>();
                var moduleItems = (dictionary["value"] as JObject)
                                    .ToObject<Dictionary<string, object>>()
                                    .Where(x => x.Key != "name");

                return moduleItems;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return new List<KeyValuePair<string, object>>();
            }
        }


        private void AppendValue(string propertyAlias, string rowName, string itemValue, StringBuilder sb, List<KeyValuePair<string, string>> result)
        {
            itemValue = itemValue.StripHtml().Trim();
            sb.Append(itemValue);
            sb.Append(" ");

            //add the row name as an individual field
            result.Add(new KeyValuePair<string, string>($"{propertyAlias}.{rowName}", itemValue));
        }
    }
}
