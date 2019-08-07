using System.Collections.Generic;
/*using Newtonsoft.Json;*/

namespace DNSAgent
{
    internal class Rules : List<Rule> {}

    internal class Rule
    {
        /// <summary>
        ///     Regex pattern to match the domain name.
        /// </summary>
     
        string _Pattern="$^";
        public string Pattern { get{return _Pattern;} set{_Pattern=value;} }

        /// <summary>
        ///     IP Address for this domain name. IPv4 address will be returned as A record and IPv6 address as AAAA record.
        /// </summary>
       
        string _Address=null;
        public string Address { get{return _Address;} set{_Address=value;} }

        /// <summary>
        ///     The name server used to query about this domain name. If "Address" is set, this will be ignored.
        /// </summary>
        string _NameServer=null;
        public string NameServer { get{return _NameServer;} set{_NameServer=value;} }

        /// <summary>
        ///     Whether to use DNSPod HttpDNS protocol to query the name server for this domain name.
        /// </summary>
        
        bool? _UseHttpQuery=null;
        public bool? UseHttpQuery { get{return _UseHttpQuery;} set{UseHttpQuery=value;} }

        /// <summary>
        ///     Timeout for the query, in milliseconds. This overrides options.cfg. If "Address" is set, this will be ignored.
        /// </summary>
        
        int? _QueryTimeout;
        public int? QueryTimeout { get{return _QueryTimeout;} set{_QueryTimeout=value;} }

        /// <summary>
        ///     Whether to transform request to AAAA type.
        /// </summary>
       
        bool? _ForceAAAA=null;
        public bool? ForceAAAA { get{return _ForceAAAA;} set{_ForceAAAA=value;} }

        /// <summary>
        ///     Whether to enable compression pointer mutation to query this name server. If "Address" is set, this will be
        ///     ignored.
        /// </summary>
        bool? _CompressionMutation=null;
        public bool? CompressionMutation { get{return _CompressionMutation;} set{_CompressionMutation=value;} }
    }
}