﻿#region Copyright and License
// Copyright 2010..2014 Alexander Reinert
// 
// This file is part of the MrTe.Net - C# DNS client/server and SPF Library (http://arsofttoolsnet.codeplex.com/)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MrTe.Net.Dns
{
	/// <summary>
	///   <para>L64</para>
	///   <para>
	///     Defined in
	///     <see cref="!:http://tools.ietf.org/html/rfc6742">RFC 6742</see>
	///   </para>
	/// </summary>
	public class L64Record : DnsRecordBase
	{
		/// <summary>
		///   The preference
		/// </summary>
		public ushort Preference { get; private set; }

		/// <summary>
		///   The Locator
		/// </summary>
		public ulong Locator64 { get; private set; }

		internal L64Record() {}

		/// <summary>
		///   Creates a new instance of the L64Record class
		/// </summary>
		/// <param name="name"> Domain name of the host </param>
		/// <param name="timeToLive"> Seconds the record should be cached at most </param>
		/// <param name="preference"> The preference </param>
		/// <param name="locator64"> The Locator </param>
		public L64Record(string name, int timeToLive, ushort preference, ulong locator64)
			: base(name, RecordType.L64, RecordClass.INet, timeToLive)
		{
			Preference = preference;
			Locator64 = locator64;
		}

		internal override void ParseRecordData(byte[] resultData, int startPosition, int length)
		{
			Preference = DnsMessageBase.ParseUShort(resultData, ref startPosition);
			Locator64 = DnsMessageBase.ParseULong(resultData, ref startPosition);
		}

		internal override string RecordDataToString()
		{
			string locator = Locator64.ToString("x16");
			return Preference + " " + locator.Substring(0, 4) + ":" + locator.Substring(4, 4) + ":" + locator.Substring(8, 4) + ":" + locator.Substring(12);
		}

		protected internal override int MaximumRecordDataLength
		{
			get { return 10; }
		}

		protected internal override void EncodeRecordData(byte[] messageData, int offset, ref int currentPosition, Dictionary<string, ushort> domainNames)
		{
			DnsMessageBase.EncodeUShort(messageData, ref currentPosition, Preference);
			DnsMessageBase.EncodeULong(messageData, ref currentPosition, Locator64);
		}
	}
}