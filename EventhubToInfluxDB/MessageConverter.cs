using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("EventhubToInfluxDB.Tests")]
namespace EventhubToInfluxDB
{
    
    public class MessageConverterOptions
    {
        public string Name { get; set; }
        public string Timestamp { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Fields { get; set; }
    }

    internal class MessageConverter
    {

        string _measurementName;
        string _timestampName;
        List<string> _tagnames;
        List<string> _fieldnames;

        //public MessageConverter(string measurementName, string timestampName, List<string> tagnames, List<string> fieldnames)
        public MessageConverter(IOptionsMonitor<MessageConverterOptions> optionsMonitor)
        {
            var options = optionsMonitor.CurrentValue;
            _measurementName = options.Name ?? throw new ArgumentNullException();

            _timestampName = options.Timestamp;
            _tagnames = options.Tags ?? throw new ArgumentNullException();
            _fieldnames = options.Fields ?? throw new ArgumentNullException();
        }

        // converts json message to InfluxDB line format
        public string Convert(string data, IDictionary<string, object> properties, IDictionary<string, object> systemproperties )
        {
            JObject jdata = JObject.Parse(data);

            // timestamp
            string timestampStr = (string)jdata.SelectToken(_timestampName);
            
            DateTime dateTime;
            if (timestampStr == null || !DateTime.TryParse(timestampStr, out dateTime)) {
                dateTime = DateTime.UtcNow;
            }

            List<string> _tags = new List<string>();

            var myTest2 = jdata.Descendants();
            // process any tag names.
            foreach (var item in _tagnames)
            {
                IEnumerable<JToken> tags = SearchFields(myTest2, item);
                //IEnumerable<JToken> tags = jdata.SelectTokens(item);
                foreach (var tag in tags)
                {

                    JProperty parentProp = (JProperty)tag.Parent;

                    _tags.Add(EscapeTag(parentProp.Name)+"="+EscapeTag(tag.Value<string>()));
                }
            }


            List<string> _fields = new List<string>();
                        
            // process any tag names.
            foreach (var item in _fieldnames)
            {
                // get all fields

                IEnumerable<JToken> fields = SearchFields(myTest2, item);
                                                
                //IEnumerable<JToken> fields = jdata.SelectTokens(item);
                foreach (var field in fields)
                {

                    if (field.Type == JTokenType.Property)
                    {
                        if (field.HasValues)
                        {
                            // do not decend into deeper. Only first level
                            foreach (var value in field)
                            {
                                JProperty parentProp = (JProperty)value.Parent;
                                switch (value.Type)
                                {
                                    case JTokenType.String:
                                        _fields.Add(EscapeTag(parentProp.Name) + "=" + value.Value<string>().Replace("\"", "\\\""));
                                        break;
                                    case JTokenType.Float:
                                    case JTokenType.Boolean:                                    
                                    case JTokenType.Integer:
                                    case JTokenType.Date:
                                    case JTokenType.Guid:
                                        _fields.Add(EscapeTag(parentProp.Name) + "=" + value.Value<string>());
                                        break;
                                    default:
                                        break;

                                }
                            }
                        }

                    } else
                    {
                        JProperty parentProp = (JProperty)field.Parent;
                        switch (field.Type)
                        {
                            case JTokenType.String:

                                
                                _fields.Add(EscapeTag(parentProp.Name) + "=" + field.Value<string>().Replace("\"", "\\\""));
                                break;
                            case JTokenType.Float:
                            case JTokenType.Boolean:
                            case JTokenType.Integer:
                            case JTokenType.Date:
                            case JTokenType.Guid:                                

                                _fields.Add(EscapeTag(parentProp.Name) + "=" + field.Value<string>());
                                break;
                            default:
                                break;

                        }
                    }                                        
                }
            }

            if (_fields.Count == 0) return null;

            // if no timestamp, use system.
            StringBuilder result = new StringBuilder();

            result.Append(EscapeMeasurement(_measurementName));
            if (_tags.Count > 0)
            {
                result.Append(","+String.Join(",", _tags.ToArray()));
            }
            result.Append(" ");

            result.Append(String.Join(",", _fields.ToArray()));

            result.Append(" ");

            result.Append(new DateTimeOffset(dateTime).ToUnixTimeMilliseconds());

            return result.ToString();
        }

        private IEnumerable<JToken> SearchFields(IEnumerable<JToken> tokens, string item)
        {

            IEnumerable<JToken> result = tokens
            .Where(t =>
            {
                if (t.Type == JTokenType.Property)
                {
                    string newpath = "";
                    if (t.Parent != null && t.Parent.Type == JTokenType.Object)
                    {
                        var po = (JObject)(t.Parent);
                        if (po.Parent != null)
                        {
                            var pp = (JProperty)po.Parent;
                            newpath = pp.Name + ".";
                        }
                    }
                    newpath += "." + ((JProperty)t).Name;

                    if (Regex.IsMatch(newpath, item))
                    {
                        return true;
                    }
                }
                return false;
            })
            .Select(p => ((JProperty)p).Value);
            return result;
        }

        private string EscapeTag(string v)
        {
            return v.Replace(",", "\\,").Replace("=", "\\=").Replace(" ", "\\ ");
        }
        private string EscapeMeasurement(string v)
        {
            return v.Replace(",", "\\,").Replace(" ", "\\ ");
        }
    }
}
