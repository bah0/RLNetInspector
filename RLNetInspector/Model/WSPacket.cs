using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace RLNetInspector.Model
{
    public class WSPacket : INotifyPropertyChanged
    {
        private string psyNetID;
        private string psyService;
        private string responseData;
        private string requestData;
        private string requestBody;
        private string responseBody;
        private Dictionary<string, string> requestHeader;
        private Dictionary<string, string> responseHeader;

        static Regex headerRegex = new Regex(@"^(?:([A-Za-z\-0-9]*):([: \w\d].*)*)$", RegexOptions.Multiline | RegexOptions.Compiled);
        static Regex contentRegex = new Regex(@"^{.*}$", RegexOptions.Multiline | RegexOptions.Compiled);

        [JsonIgnore]
        public Dictionary<string, string> RequestHeader { get => requestHeader; set { requestHeader = value; RaisePropertyChanged(nameof(RequestHeader));} }

        [JsonIgnore]
        public Dictionary<string, string> ResponseHeader { get => responseHeader; set { responseHeader = value; RaisePropertyChanged(nameof(responseHeader));} }

        [JsonIgnore]
        public string RequestBody
        {
            get { return requestBody; }
            set { requestBody = value; }
        }

        [JsonIgnore]
        public string ResponseBody
        {
            get { return responseBody; }
            set { responseBody = value; }
        }

        public string PsyNetID
        {
            get { return psyNetID; }
            set { psyNetID = value; RaisePropertyChanged(nameof(PsyNetID)); }
        }

        public string RequestData
        {
            get { return requestData; }
            set { requestData = value; RaisePropertyChanged(nameof(RequestData)); RaisePropertyChanged(nameof(RequestReceived)); parseRequest(); }
        }

        public string ResponseData
        {
            get { return responseData; }
            set { responseData = value; RaisePropertyChanged(nameof(ResponseData)); RaisePropertyChanged(nameof(ResponseReceived)); parseResponse(); }
        }

        public string PsyService
        {
            get { return psyService; }
            set { psyService = value; RaisePropertyChanged(nameof(PsyService)); }
        }


        private void parseRequest()
        {
            if (requestData == null)
                return;

            MatchCollection headerMatches = headerRegex.Matches(RequestData);
            MatchCollection contentMatches = contentRegex.Matches(RequestData);

            foreach (Match m in headerMatches)
            {
                GroupCollection groups = m.Groups;
                string key, val;

                switch (groups.Count)
                {
                    case 2:
                        key = groups[1].Value.Trim();
                        val = "";
                        break;
                    case 3:
                        key = groups[1].Value.Trim();
                        val = groups[2].Value.Trim();
                        break;
                    default:
                        key = "";
                        val = "";
                        break;
                }
                if (RequestHeader.ContainsKey(key))
                {
                    Console.WriteLine("RequestHeader contains key "+key);
                }
                else
                    RequestHeader.Add(key, val);
            }
            
            if (RequestHeader.ContainsKey("PsyService"))
                PsyService = requestHeader["PsyService"];
            else if(RequestHeader.ContainsKey("PsyPing"))
                PsyService = requestHeader["PsyPing"];

            foreach (Match m in contentMatches)
            {
                RequestBody = m.Value;
            }
        }
        private void parseResponse()
        {
            if (responseData == null)
                return;

            MatchCollection headerMatches = headerRegex.Matches(ResponseData);
            MatchCollection contentMatches = contentRegex.Matches(ResponseData);

            foreach (Match m in headerMatches)
            {
                GroupCollection groups = m.Groups;
                string key, val;

                switch (groups.Count)
                {
                    case 2:
                        key = groups[1].Value.Trim();
                        val = "";
                        break;
                    case 3:
                        key = groups[1].Value.Trim();
                        val = groups[2].Value.Trim();
                        break;
                    default:
                        key = "";
                        val = "";
                        break;
                }
                if (ResponseHeader.ContainsKey(key))
                {
                    Console.WriteLine("ResponseHeader contains key " + key);
                }
                else
                    ResponseHeader.Add(key, val);
            }


            foreach (Match m in contentMatches)
            {
                ResponseBody = m.Value;
            }
        }

        public WSPacket()
        {
            RequestHeader = new Dictionary<string, string>();
            ResponseHeader = new Dictionary<string, string>();
        }

        [JsonIgnore]
        public string RequestReceived { get => (!String.IsNullOrEmpty(requestData)).ToString(); }

        [JsonIgnore]
        public string ResponseReceived { get => (!String.IsNullOrEmpty(responseData)).ToString(); }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static string PrettyJson(string json)
        {
            if (json == null)
                return "";

            var parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

    }
}
