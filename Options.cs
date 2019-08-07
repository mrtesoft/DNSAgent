using System.Collections.Generic;

namespace DNSAgent
{
    internal class Options
    {
        /// <summary>
        ///     Set to true to automatically hide the window on start.
        /// </summary>
       bool _HideOnStart=false;
        public bool HideOnStart { get{return _HideOnStart;} set{_HideOnStart=value;} } 

        /// <summary>
        ///     IP and port that DNSAgent will listen on. 0.0.0.0:53 for all interfaces and 127.0.0.1:53 for localhost. Of course
        ///     you can use other ports.
        /// </summary>
        string _ListenOn="127.0.0.1";
        public string ListenOn { get{return _ListenOn;} set{_ListenOn=value;} }

        /// <summary>
        ///     Querys that don't match any rules will be send to this server.
        /// </summary>
        
        string _DefaultNameServer="8.8.8.8";
       
        public string DefaultNameServer { get{return _DefaultNameServer;} set{_DefaultNameServer=value;} }

        /// <summary>
        ///     Whether to use DNSPod HttpDNS protocol to query the name server for A record.
        /// </summary>
        bool _UseHttpQuery=false;
        public bool UseHttpQuery { get{return _UseHttpQuery;} set{_UseHttpQuery=value;} }

        /// <summary>
        ///     Timeout for a query, in milliseconds. This may be overridden by rules.cfg for a specific domain name.
        /// </summary>
        int _QueryTimeout=4000;
        public int QueryTimeout { get{return _QueryTimeout;} set{_QueryTimeout=value;} }

        /// <summary>
        ///     Whether to enable compression pointer mutation to query the default name servers. This may avoid MITM attack in
        ///     some network environments.
        /// </summary>
        bool _CompressionMutation=false;
        public bool CompressionMutation { get{return _CompressionMutation;} set{_CompressionMutation=value;} }

        /// <summary>
        ///     Whether to enable caching of responses.
        /// </summary>
        bool _CacheResponse=true;
        public bool CacheResponse { get{return _CacheResponse;} set{_CacheResponse=value;}}

        /// <summary>
        ///     How long will the cached response live. If a DNS record's TTL is longer than this value, it will be used instead of
        ///     this. Set to 0 to use the original TTL.
        /// </summary>
        int _CacheAge=0;
        public int CacheAge { get{return _CacheAge;} set{_CacheAge=value;} }

        /// <summary>
        ///     Source network whitelist. Only IPs from these network are accepted. Set to null to accept all IP (disable
        ///     whitelist), empty to deny all IP.
        /// </summary>
        List<string> _NetworkWhitelist=null;
        public List<string> NetworkWhitelist { get{
        return _NetworkWhitelist;
        } set{_NetworkWhitelist=value;}} 
    }
}