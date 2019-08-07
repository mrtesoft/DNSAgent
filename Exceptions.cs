using System;
using MrTe.Net.Dns;

namespace DNSAgent
{
    ///// <summary>
    /////     Timeout reached when query a remote name server.
    ///// </summary>
    //internal class NameServerTimeoutException : Exception {}

    /// <summary>
    ///     When a query is redirected to this name server itself, causing infinite loop.
    /// </summary>
    internal class InfiniteForwardingException : Exception
    {
        public InfiniteForwardingException(DnsQuestion question)
        {
            Question = question;
        }
        DnsQuestion _Question;
        public DnsQuestion Question { get { return _Question; } set { _Question = value; } }
    }

    internal class ParsingException : Exception {}
}