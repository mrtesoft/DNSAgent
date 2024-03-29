﻿using System;
/*using System.Collections.Concurrent;*/
using System.Linq;
using MrTe.Net.Dns;

namespace DNSAgent
{
    internal class DnsCacheMessageEntry
    {
         public bool IsExpired;
        public DnsCacheMessageEntry(DnsMessage message, int timeToLive)
        {
            IsExpired=DateTime.Now > ExpireTime ? true : false;
            
            Message = message;
            var records = message.AnswerRecords.Concat(message.AuthorityRecords).ToList();
            if (records.Any())
                timeToLive = Math.Max(records.Min(record => record.TimeToLive), timeToLive);
            ExpireTime = DateTime.Now.AddSeconds(timeToLive);
        }

        DnsMessage _Message;
        public DnsMessage Message { get{return _Message;} set{_Message=value;} }
        public DateTime ExpireTime { get; set; }

       
    }
    /*
    internal class DnsMessageCache :
        ConcurrentDictionary<string, ConcurrentDictionary<RecordType, DnsCacheMessageEntry>>
    {
         
        public void Update(DnsQuestion question, DnsMessage message, int timeToLive)
        {
            if (!ContainsKey(question.Name))
                this[question.Name] = new ConcurrentDictionary<RecordType, DnsCacheMessageEntry>();

            this[question.Name][question.RecordType] = new DnsCacheMessageEntry(message, timeToLive);
        }
    }*/

    internal class DnsMessageCache {


       //System.Collections.Specialized.StringCollection keys = new System.Collections.Specialized.StringCollection();
       private System.Collections.Specialized.HybridDictionary list = new System.Collections.Specialized.HybridDictionary();
        
        public System.Collections.Generic.Dictionary<RecordType, DnsCacheMessageEntry> this[string key]{
            get {
                if (!list.Contains(key)) return null;

                return (System.Collections.Generic.Dictionary<RecordType, DnsCacheMessageEntry>)list[key];
              }
            set {
                if (!list.Contains(key))
                { 
                    list.Add(key,value);
                }
                else
                    list[key] = value;
             }
        
        }

        public void Update(DnsQuestion question, DnsMessage message, int timeToLive)
        {
            if (!ContainsKey(question.Name))
                this[question.Name] = new System.Collections.Generic.Dictionary<RecordType, DnsCacheMessageEntry>();

            this[question.Name][question.RecordType] = new DnsCacheMessageEntry(message, timeToLive);
        }
        public bool ContainsKey(string key) {
            if (list.Contains(key)) return true;
            return false;
        }
        public void Clear() {
            
            foreach (string key in list.Keys)
            {
                this[key].Clear();
            }
            list.Clear();

        }
    }

}