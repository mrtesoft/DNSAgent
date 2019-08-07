using System;
/*using System.Collections.Concurrent;*/
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
/*using System.Threading.Tasks;*/
using MrTe.Net;
using MrTe.Net.Dns;
using MrTe.Threading.Tasks;
using DNSAgent;

namespace DNSAgent
{
    internal class DnsAgent
    {
        /*
         private Task _forwardingTask;
        private Task _listeningTask;
         */
       
        private List<KeyValuePair<IPAddress, int>> _networkWhitelist; // Key for address, value for mask

        private MrTe.Threading.Tasks.CancellationTokenSource _stopTokenSource;
        private System.Collections.Generic.Dictionary<ushort, IPEndPoint> _transactionClients;
        private System.Collections.Generic.Dictionary<ushort, MrTe.Threading.Tasks.CancellationTokenSource> _transactionTimeoutCancellationTokenSources;
        private UdpClient _UdpForwarder=null;
        private UdpClient _UdpListener=null;
        private readonly string _listenOn;
        
        public DnsAgent(Options options, Rules rules, string listenOn, DnsMessageCache cache)
        {
           
            if(options==null)options=new Options();;
            Options = options;
            if(rules==null)rules=new Rules();
            Rules = rules;
            _listenOn = listenOn;
            if(cache==null)cache=new DnsMessageCache();
            Cache = cache ;
        }

        private Options _options;
        public Options Options
        {
            get { return _options; }
            set
            {
                _options = value;
                if (_options.NetworkWhitelist == null)
                    _networkWhitelist = null;
                else
                {
                    _networkWhitelist = _options.NetworkWhitelist.Select(s =>
                    {
                        var pieces = s.Split('/');
                        var ip = IPAddress.Parse(pieces[0]);
                        var mask = int.Parse(pieces[1]);
                        return new KeyValuePair<IPAddress, int>(ip, mask);
                    }).ToList();
                }
            }
        }

        Rules _Rules;
        public Rules Rules { get{return _Rules;} set{_Rules=value;} }

        DnsMessageCache _Cache;
        public DnsMessageCache Cache { get{return _Cache;} set{_Cache=value;} }
        public event Action Started;
        public event Action Stopped;
        public IPEndPoint _IPEndPoint;

        System.Threading.Thread _ListeningTask;
        System.Threading.Thread _ForwardingTask;
        

        
        void ListeningTask_Run()
        {

            while (!_stopTokenSource.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint receivepoint = new IPEndPoint(IPAddress.Broadcast, _IPEndPoint.Port);
                    byte[] buf = _UdpListener.Receive(ref receivepoint);

                    ProcessMessage(receivepoint, buf);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.ConnectionReset)
                        Logger.Error("[Listener.Receive] Unexpected socket error:\n{0}", e);
                }
                /*catch (AggregateException e)
                {
                    var socketException = e.InnerException as SocketException;
                    if (socketException != null)
                    {
                        if (socketException.SocketErrorCode != SocketError.ConnectionReset)
                            Logger.Error("[Listener.Receive] Unexpected socket error:\n{0}", e);
                    }
                    else
                        Logger.Error("[Listener] Unexpected exception:\n{0}", e);
                }*/

                catch (ObjectDisposedException)
                {
                } // Force closing _udpListener will cause this exception
                catch (Exception e)
                {
                    Logger.Error("[Listener] Unexpected exception:\n{0}", e);
                }
            }

        }

        void ForwardingTask_Run()
        {

            while (!_stopTokenSource.IsCancellationRequested)
            {
                try
                {

                    IPEndPoint receivepoint = new IPEndPoint(IPAddress.Broadcast, _IPEndPoint.Port);
                    byte[] buf = _UdpForwarder.Receive(ref receivepoint);
                    /*var query = await _udpForwarder.ReceiveAsync();*/
                    
                    DnsMessage message;
                    try
                    {
                        message = DnsMessage.Parse(buf);
                    }
                    catch (Exception)
                    {
                        throw new ParsingException();
                    }
                   
                    if (!_transactionClients.ContainsKey(message.TransactionID)) continue;
                    
                    IPEndPoint remoteEndPoint;
                    MrTe.Threading.Tasks.CancellationTokenSource ignore;

                    /*
                     _transactionClients.TryRemove(message.TransactionID, out remoteEndPoint);
                      _transactionTimeoutCancellationTokenSources.TryRemove(message.TransactionID, out ignore);
                       await _udpListener.SendAsync(query.Buffer, query.Buffer.Length, remoteEndPoint);
                     */

                    remoteEndPoint = _transactionClients[message.TransactionID];
                    ignore = _transactionTimeoutCancellationTokenSources[message.TransactionID];

                    _UdpListener.Send(buf, buf.Length, remoteEndPoint);



                    // Update cache
                    if (Options.CacheResponse) Cache.Update(message.Questions[0], message, Options.CacheAge);
                    _transactionClients.Remove(message.TransactionID);
                    _transactionTimeoutCancellationTokenSources.Remove(message.TransactionID);
                }
                catch (ParsingException)
                {
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.ConnectionReset)
                        Logger.Error("[Forwarder.Send] Name server unreachable.");
                    else
                        Logger.Error("[Forwarder.Receive] Unexpected socket error:\n{0}", e);
                }
                catch (ObjectDisposedException)
                {
                } // Force closing _udpListener will cause this exception
                catch (Exception e)
                {
                    Logger.Error("[Forwarder] Unexpected exception:\n{0}", e);
                }
            }

        }
         public bool Start()
        {
             _IPEndPoint = Utils.CreateIpEndPoint(_listenOn, 53);

             _stopTokenSource = new MrTe.Threading.Tasks.CancellationTokenSource();
            _transactionClients = new System.Collections.Generic.Dictionary<ushort, IPEndPoint>();
            _transactionTimeoutCancellationTokenSources = new System.Collections.Generic.Dictionary<ushort, MrTe.Threading.Tasks.CancellationTokenSource>();
            try
            {
                _UdpListener = new UdpClient(_IPEndPoint);
                _UdpForwarder = new UdpClient(0);
            }
            catch (SocketException e)
            {
                Logger.Error("[Listener] Failed to start DNSAgent:\n{0}", e);
                Stop();
                return false;
            }
          

            /*_listeningTask = Task.Run(async () =>
            {
               
            }, _stopTokenSource.Token);
             */

              _ListeningTask=new Thread(new ThreadStart(ListeningTask_Run));
              _ListeningTask.Start();
            /*
             _forwardingTask = Task.Run(async () =>
            {
                
            }, _stopTokenSource.Token);
             
             */
            _ForwardingTask = new Thread(new ThreadStart(ForwardingTask_Run));
            _ForwardingTask.Start();

            Logger.Info("Listening on {0}...", _IPEndPoint);
            OnStarted();
            return true;
        }

        public void Stop()
        {
            
            //_stopTokenSource?.Cancel();
            if(_stopTokenSource!=null)_stopTokenSource.Cancel();

            if(_UdpListener!=null)_UdpListener.Close();
             if(_UdpForwarder!=null)_UdpForwarder.Close();
            //_udpListener?.Close();
            //_udpForwarder?.Close();
          

            /*try
            {
             _listeningTask?.Wait();
             _forwardingTask?.Wait();*/

                 if(_ListeningTask!=null)_ListeningTask.Abort();
                 if (_ForwardingTask != null) _ForwardingTask.Abort();
              
           /* }
            catch (AggregateException)
            {
            }*/

            _stopTokenSource = null;
            _UdpListener = null;
            _UdpForwarder = null;
            _ListeningTask = null;
            _ForwardingTask = null;
            _transactionClients = null;
            _transactionTimeoutCancellationTokenSources = null;

            OnStopped();
        }
      
        private bool ProcessMessage(IPEndPoint receivepoint,byte[] buf)
        {
            if(buf==null)return false;
            if(buf.Length<=0)return false;
            
                try
                {
                    DnsMessage message;
                    DnsQuestion question;
                    bool respondedFromCache = false;

                    try
                    {
                        message = DnsMessage.Parse(buf);
                        question = message.Questions[0];
                    }
                    catch (Exception)
                    {
                        throw new ParsingException();
                    }

                    // Check for authorized subnet access
                    if (_networkWhitelist != null)
                    {
                        if (_networkWhitelist.All(pair =>
                            !pair.Key.GetNetworkAddress(pair.Value)
                                .Equals(receivepoint.Address.GetNetworkAddress(pair.Value))))
                        {
                            Logger.Info("-> {0} is not authorized, who requested {1}.",
                               receivepoint.Address,
                                question);
                            message.ReturnCode = ReturnCode.Refused;
                            message.IsQuery = false;
                        }
                    }
                    Logger.Info("-> {0} requested {1} (#{2}, {3}).", receivepoint.Address, question.Name,
                        message.TransactionID, question.RecordType);

                    // Query cache
                    if (Options.CacheResponse)
                    {
                        if (Cache.ContainsKey(question.Name) && Cache[question.Name].ContainsKey(question.RecordType))
                        {
                            DnsCacheMessageEntry entry = Cache[question.Name][question.RecordType];
                            if (!entry.IsExpired)
                            {
                                var cachedMessage = entry.Message;
                                Logger.Info("-> #{0} served from cache.", message.TransactionID,
                                    cachedMessage.TransactionID);
                                cachedMessage.TransactionID = message.TransactionID; // Update transaction ID
                                cachedMessage.TSigOptions = message.TSigOptions; // Update TSig options
                                message = cachedMessage;
                                respondedFromCache = true;
                            }
                        }
                    }

                    string targetNameServer = Options.DefaultNameServer;
                    bool useHttpQuery = Options.UseHttpQuery;
                    int queryTimeout = Options.QueryTimeout;
                    bool useCompressionMutation = Options.CompressionMutation;

                    // Match rules
                    if (message.IsQuery &&
                        (question.RecordType == RecordType.A || question.RecordType == RecordType.Aaaa))
                    {
                        for (var i = Rules.Count - 1; i >= 0; i--)
                        {
                            Match match = Regex.Match(question.Name, Rules[i].Pattern);
                            if (!match.Success) continue;
                         

                            // Domain name matched

                            RecordType recordType = question.RecordType;
                            if (Rules[i].ForceAAAA != null && Rules[i].ForceAAAA.Value) // RecordType override
                                recordType = RecordType.Aaaa;

                            if (Rules[i].NameServer != null) // Name server override
                                targetNameServer = Rules[i].NameServer;

                            if (Rules[i].UseHttpQuery != null) // HTTP query override
                                useHttpQuery = Rules[i].UseHttpQuery.Value;

                            if (Rules[i].QueryTimeout != null) // Query timeout override
                                queryTimeout = Rules[i].QueryTimeout.Value;

                            if (Rules[i].CompressionMutation != null) // Compression pointer mutation override
                                useCompressionMutation = Rules[i].CompressionMutation.Value;
                           
                            if (Rules[i].Address != null)
                            {
                                IPAddress ip;
                                IPAddress.TryParse(Rules[i].Address, out ip);
                                
                                if (ip == null) // Invalid IP, may be a domain name
                                {
                                    string address = string.Format(Rules[i].Address, match.Groups.Cast<object>().ToArray());

                                   
                                    if (recordType == RecordType.A && useHttpQuery)
                                    {
                                        ResolveWithHttp(targetNameServer, address, queryTimeout, message);
                                    }
                                    else
                                    {
                                        IPEndPoint serverEndpoint = Utils.CreateIpEndPoint(targetNameServer, 53);

                                      
                                        
                                        DnsClient dnsClient = new DnsClient(serverEndpoint.Address, queryTimeout,
                                            serverEndpoint.Port);
                                       DnsMessage response;
                                        /* response =
                                            await
                                                Task<DnsMessage>.Factory.FromAsync(dnsClient.BeginResolve,
                                                    dnsClient.EndResolve,
                                                    address, recordType, question.RecordClass, null);
                                        */

                                      

                                      // response = dnsClient.EndResolve(dnsClient.BeginResolve(address, recordType, question.RecordClass, null, null));
                                       response = dnsClient.Resolve(address, recordType, question.RecordClass);
                                      
                                        if (response == null)
                                        {
                                            Logger.Warning("Remote resolve failed for "+address+".");
                                            return false;
                                        }
                                        foreach (var answerRecord in response.AnswerRecords)
                                        {
                                            answerRecord.Name = question.Name;
                                            message.AnswerRecords.Add(answerRecord);
                                        }
                                        message.ReturnCode = response.ReturnCode;
                                        message.IsQuery = false;
                                    }
                                }
                                else
                                {
                                    if (recordType == RecordType.A &&
                                        ip.AddressFamily == AddressFamily.InterNetwork)
                                        message.AnswerRecords.Add(new ARecord(question.Name, 600, ip));
                                    else if (recordType == RecordType.Aaaa &&
                                             ip.AddressFamily == AddressFamily.InterNetworkV6)
                                        message.AnswerRecords.Add(new AaaaRecord(question.Name, 600, ip));
                                    else // Type mismatch
                                        continue;

                                    message.ReturnCode = ReturnCode.NoError;
                                    message.IsQuery = false;
                                }
                            }

                            break;
                        }
                    }

                    // TODO: Consider how to integrate System.Net.Dns with this project.
                    // Using System.Net.Dns to forward query if compression mutation is disabled
                    //if (message.IsQuery && !useCompressionMutation &&
                    //    (question.RecordType == RecordType.A || question.RecordType == RecordType.Aaaa))
                    //{
                    //    var dnsResponse = await Dns.GetHostAddressesAsync(question.Name);

                    //    if (question.RecordType == RecordType.A)
                    //    {
                    //        message.AnswerRecords.AddRange(dnsResponse.Where(
                    //            ip => ip.AddressFamily == AddressFamily.InterNetwork).Select(
                    //                ip => new ARecord(question.Name, 0, ip)));
                    //    else if (question.RecordType == RecordType.Aaaa)
                    //    {
                    //        message.AnswerRecords.AddRange(dnsResponse.Where(
                    //            ip => ip.AddressFamily == AddressFamily.InterNetworkV6).Select(
                    //                ip => new AaaaRecord(question.Name, 0, ip)));
                    //    }
                    //    message.ReturnCode = ReturnCode.NoError;
                    //    message.IsQuery = false;
                    //}

                    if (message.IsQuery && question.RecordType == RecordType.A && useHttpQuery)
                    {
                         ResolveWithHttp(targetNameServer, question.Name, queryTimeout, message);
                    }

                    if (message.IsQuery)
                    {
                        // Use internal forwarder to forward query to another name server

                        
                        IPEndPoint nendpoint = Utils.CreateIpEndPoint(targetNameServer, 53);
                     
                        ForwardMessage(message, receivepoint, buf, nendpoint,
                            queryTimeout, useCompressionMutation);


                    }
                    else
                    {
                        // Already answered, directly return to the client
                        byte[] responseBuffer;
                        message.Encode(false, out responseBuffer);
                        if (responseBuffer != null)
                        {
                            /*await
                                _udpListener.SendAsync(responseBuffer, responseBuffer.Length, buf.RemoteEndPoint);
                            */

                            _UdpListener.Send(responseBuffer,responseBuffer.Length,receivepoint);
                            // Update cache
                            if (Options.CacheResponse && !respondedFromCache)
                                Cache.Update(question, message, Options.CacheAge);
                        }
                    }
                }
                catch (ParsingException)
                {
                    return false;
                }
                catch (SocketException e)
                {
                    Logger.Error("[Listener.Send] Unexpected socket error:\n{0}", e);
                    return false;
                }
                catch (Exception e)
                {
                    Logger.Error("[Processor] Unexpected exception:\n{0}", e);
                    return false;
                }
           
            return true;
        }

        private bool ForwardMessage(DnsMessage message, IPEndPoint revicepoint,byte[] buf,
            IPEndPoint targetNameServer, int queryTimeout,
            bool useCompressionMutation)
        {
            DnsQuestion question = null;
            if (message.Questions.Count > 0)
                question = message.Questions[0];

            byte[] responseBuffer = null;
            try
            {
              
                if ((Equals(targetNameServer.Address, IPAddress.Loopback) ||
                     Equals(targetNameServer.Address, IPAddress.IPv6Loopback)) &&
                    targetNameServer.Port == ((IPEndPoint) _UdpListener.Client.LocalEndPoint).Port)
                    throw new InfiniteForwardingException(question);

                byte[] sendBuffer;
                if (useCompressionMutation)
                    message.Encode(false, out sendBuffer, true);
                else
                    sendBuffer = buf;
                
                _transactionClients[message.TransactionID] = revicepoint;
                 
                
                // Send to Forwarder
                /*await _udpForwarder.SendAsync(sendBuffer, sendBuffer.Length, targetNameServer);*/
                  _UdpForwarder.Send(sendBuffer,sendBuffer.Length,targetNameServer);

          
                


                if (_transactionTimeoutCancellationTokenSources.ContainsKey(message.TransactionID))
                    _transactionTimeoutCancellationTokenSources[message.TransactionID].Cancel();
                MrTe.Threading.Tasks.CancellationTokenSource cancellationTokenSource = new MrTe.Threading.Tasks.CancellationTokenSource();
               
                _transactionTimeoutCancellationTokenSources[message.TransactionID] = cancellationTokenSource;
               
                
                return true;
                // Timeout task to cancel the request
                /*try
                {*/
                    /*await Task.Delay(queryTimeout, cancellationTokenSource.Token);*/

                    Task.Delay(queryTimeout, cancellationTokenSource.Token);
                    
                    if (!_transactionClients.ContainsKey(message.TransactionID)) return false;
                    IPEndPoint ignoreEndPoint;
                    MrTe.Threading.Tasks.CancellationTokenSource ignoreTokenSource;
                   
                /*
                    _transactionClients.TryRemove(message.TransactionID, out ignoreEndPoint);
                    _transactionTimeoutCancellationTokenSources.TryRemove(message.TransactionID,
                        out ignoreTokenSource);
                 */
                    ignoreEndPoint = _transactionClients[message.TransactionID];
                    ignoreTokenSource=_transactionTimeoutCancellationTokenSources[message.TransactionID];


                
                    string warningText = message.Questions.Count > 0
                        ? message.Questions[0].Name+" (Type "+message.Questions[0].RecordType+")"
                        : "Transaction #"+message.TransactionID+"";
                    Logger.Warning("Query timeout for: {0}", warningText);
                    _transactionClients.Remove(message.TransactionID);
                    _transactionTimeoutCancellationTokenSources.Remove(message.TransactionID);
                /*
                 }
                catch (TaskCanceledException)
                {
                }
                */
            }
            catch (InfiniteForwardingException e)
            {
                Logger.Warning("[Forwarder.Send] Infinite forwarding detected for: {0} (Type {1})", e.Question.Name,
                    e.Question.RecordType);
                Utils.ReturnDnsMessageServerFailure(message, out responseBuffer);
               
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionReset) // Target name server port unreachable
                    Logger.Warning("[Forwarder.Send] Name server port unreachable: {0}", targetNameServer);
                else
                    Logger.Error("[Forwarder.Send] Unhandled socket error: {0}", e.Message);
                Utils.ReturnDnsMessageServerFailure(message, out responseBuffer);
                
            }
            catch (Exception e)
            {
                Logger.Error("[Forwarder] Unexpected exception:\n{0}", e);
                Utils.ReturnDnsMessageServerFailure(message, out responseBuffer);
                
            }

            // If we got some errors
          
            if (responseBuffer != null)
                /*await _udpListener.SendAsync(responseBuffer, responseBuffer.Length, originalUdpMessage.RemoteEndPoint);*/
                _UdpListener.Send(responseBuffer,responseBuffer.Length,revicepoint);
            return true;
        }

        private  bool ResolveWithHttp(string targetNameServer, string domainName, int timeout, DnsMessage message)
        {
            WebRequest request = WebRequest.Create("http://"+targetNameServer+"/d?dn="+domainName+"&ttl=1");
            request.Timeout = timeout;
           
            //var stream = (await request.GetResponseAsync()).GetResponseStream();
          Stream stream= request.GetResponse().GetResponseStream();
            if (stream == null)
                throw new Exception("Invalid HTTP response stream.");
            using (StreamReader reader = new StreamReader(stream))
            {
                
                //var result = await reader.ReadToEndAsync();
                string result = reader.ReadToEnd();
                if (string.IsNullOrEmpty(result))
                {
                    message.ReturnCode = ReturnCode.NxDomain;
                    message.IsQuery = false;
                }
                else
                {
                    var parts = result.Split(',');
                    var ips = parts[0].Split(';');
                    foreach (var ip in ips)
                    {
                        message.AnswerRecords.Add(new ARecord(domainName, int.Parse(parts[1]), IPAddress.Parse(ip)));
                    }
                    message.ReturnCode = ReturnCode.NoError;
                    message.IsQuery = false;
                }
            }
            return true;
        }

        #region Event Invokers

        protected virtual void OnStarted()
        {
            System.Action handler = Started;
            if (handler == null) return;
            handler.Invoke();
            //handler?.Invoke();
        }

        protected virtual void OnStopped()
        {
            System.Action handler = Stopped;
            if (handler == null) return;
            handler.Invoke();
           // handler?.Invoke();
        }

        #endregion
    }
}