namespace MrTe.Core.Json
{
    using System;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Collections;

    public interface IJsonObject
    {
        
        string Value { get; }
    }

    public class JsonArray : ArrayList, IJsonObject
    {
        public static implicit operator JsonArray(string str)
        {
            return (JsonArray)(new JSONParser().GetJSONObject(str));
        }
        public string Value
        {
            get
            {
                throw new NotImplementedException();
            }
       }

        public override string ToString()
        {
             System.Text.StringBuilder sb = new System.Text.StringBuilder();
             (new JSONParser()).toJSON(sb,this,null);
             return sb.ToString();
        }
        public string ToString(string formart)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            (new JSONParser()).toJSON(sb, this, formart);
            return sb.ToString();
        }
      
        
    }

    public class JsonBoolean : IJsonObject
    {
        private bool _value;

        public JsonBoolean()
        {
            this._value = false;
        }

        public JsonBoolean(bool value)
        {
            this._value = false;
            this._value = value;
        }

        public static implicit operator bool(JsonBoolean o)
        {
            return bool.Parse(o.Value);
        }
        public override string ToString()
        {
            return Value;
        }
        public string Value
        {
            get
            {
                return this._value.ToString();
            }
        }
    }

    public class JsonNumber : IJsonObject
    {
        private string _value = string.Empty;
        public JsonNumber()
        {

        }
        private JsonNumber(char c)
        {
            this._value = c.ToString();
        }

        internal void Append(char c)
        {
            this._value = this._value + c;
        }

        internal void Append(string s)
        {
            this._value = this._value + s;
        }

        internal int IndexOf(string s)
        {
            return this._value.IndexOf(s);
        }

        public static JsonNumber operator +(JsonNumber a, char c)
        {
            a.Append(c);
            return a;
        }

        public static JsonNumber operator +(JsonNumber a, string s)
        {
            a.Append(s);
            return a;
        }

        public static implicit operator JsonNumber(char c)
        {

            return new JsonNumber(c);
        }


        public static implicit operator string(JsonNumber o)
        {
            return o.Value;
        }


        public override string ToString()
        {
            return this.Value;
        }

        public string Value
        {
            get
            {
                return this._value;
            }
        }
    }
    public class JsonObject : IJsonObject
    {
        

        public static implicit operator JsonObject(string str)
        {
            return (JsonObject)(new JSONParser()).GetJSONObject(str);
            
        }
       

    
        private StringCollection keys = new StringCollection();
        private HybridDictionary list = new HybridDictionary();

        public void Add(string key, object value)
        {
            this.list.Add(key, value);
            this.keys.Add(key);
        }
        public void Remove(string key)
        {
            this.list.Remove(key);
            this.keys.Remove(key);
        }

        public bool Contains(string key)
        {
            return this.list.Contains(key);
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public object this[string key]
        {
            get
            {
                return this.list[key];
            }
            set
            {
                if (!this.Contains(key)) { this.Add(key, value); }
                else { this.list[key] = value; }
            }
        }

        public JsonObject getObject(string key) 
        {
            return (JsonObject)this[key];
        }

        public JsonArray getArray(string key)
        {
            return (JsonArray)this[key];
        }

        public JsonDate getDate(string key)
        {
            return (JsonDate)this[key];
        }
        public int getInt(string key)
        {
            return int.Parse(this[key]+"");
        }
        public double getDouble(string key)
        {
            return double.Parse(this[key] + "");
        }

        public string[] Keys
        {
            get
            {
                string[] array = new string[this.keys.Count];
                this.keys.CopyTo(array, 0);
                return array;
            }
        }
       
        public string Value
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            (new JSONParser()).toJSON(sb, this, null);
            return sb.ToString();
        }
        public  string ToString(string formart)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            (new JSONParser()).toJSON(sb, this, formart);
            return sb.ToString();
        }

    }

    public class JsonString : IJsonObject
    {

        private string _value = string.Empty;
        public JsonString()
        {

        }
        private JsonString(char c)
        {
            this._value = c.ToString();
        }
        internal void Append(char c)
        {
            this._value = this._value + c;
        }

        internal void Append(string s)
        {
            this._value = this._value + s;
        }


        public static JsonString operator +(JsonString a, char c)
        {
            a.Append(c);
            return a;
        }

        public static JsonString operator +(JsonString a, string s)
        {
            a.Append(s);
            return a;
        }


         public static implicit operator JsonString(string str)
         {
             JsonString temp = new JsonString();
             temp.Append(str);
             return temp;
        }
        public static implicit operator JsonString(char c)
        {


            return new JsonString(c);
        }



        public static implicit operator string(JsonString o)
        {
            return o.Value;
        }

        public override string ToString()
        {
            return this.Value;
        }

        public string Value
        {
            get
            {
                return this._value;
            }
        }
    }
    public class JsonDate : IJsonObject
    {
      
        private DateTime _value=DateTime.Now;
        public JsonDate()
        { 
        }
        public JsonDate(string d)
        {
            _value = DateTime.Parse(d);
        }
        public DateTime getDate()
        {
            return _value;
         }
        public void setDate(DateTime D)
        {
            _value = D;
        }
        public static implicit operator DateTime(JsonDate o)
        {

            return o.getDate();
        }
        public static implicit operator JsonDate(DateTime o)
        {
           JsonDate D=new JsonDate();
            D.setDate(o);

            return D;
        }
        public string Value
        {
            get
            {
                return this._value.ToString();
            }
        }
        public string ToString()
        {
            return _value.ToString();
        }
        public string ToString(string format)
        {
            return _value.ToString(format);
        }
    }
    public sealed class JSONParser
    {
        private char _ch = ' ';
        private int _idx = 0;
        private string _json = null;
        public const char END_OF_STRING = '\0';
        public const char JSON_ARRAY_BEGIN = '[';
        public const char JSON_ARRAY_END = ']';
        public const char JSON_DECIMAL_SEPARATOR = '.';
        public const char JSON_ITEMS_SEPARATOR = ',';
        public const char JSON_OBJECT_BEGIN = '{';
        public const char JSON_OBJECT_END = '}';
        public const char JSON_PROPERTY_SEPARATOR = ':';
        public const char JSON_STRING_DOUBLE_QUOTE = '"';
        public const char JSON_STRING_SINGLE_QUOTE = '\'';
        public const char NEW_LINE = '\n';
        public const char RETURN = '\r';

        internal bool CompareNext(string s)
        {
            if ((this._idx + s.Length) > this._json.Length)
            {
                return false;
            }
            return (this._json.Substring(this._idx, s.Length) == s);
        }

        public IJsonObject GetJSONObject(string json)
        {
            this._json = json;
            this._idx = 0;
            this._ch = ' ';
            return this.GetObject();
        }

        internal IJsonObject GetObject()
        {
            if (this._json == null)
            {
                throw new Exception("Missing json string.");
            }
            this.ReadWhiteSpaces();
            switch (this._ch)
            {
                //添加修改 '
                case '\'':
                    return this.ReadString();
                case '"':
                    return this.ReadString();

                case '-':
                    return this.ReadNumber();

                case '[':
                    return this.ReadArray();

                case '{':
                    return this.ReadObject();
            }
            if ((this._ch >= '0') && (this._ch <= '9'))
            {
                return this.ReadNumber();
            }
            return this.ReadWord();
        }

        internal JsonArray ReadArray()
        {
            JsonArray array = new JsonArray();
            if (this._ch != '[')
            {
                throw new NotSupportedException("Array could not be read.");
            }
            this.ReadNext();
            this.ReadWhiteSpaces();
            if (this._ch != ']')
            {
                while (this._ch != '\0')
                {
                    array.Add(this.GetObject());
                    this.ReadWhiteSpaces();
                    if (this._ch == ']')
                    {
                        this.ReadNext();
                        return array;
                    }
                    if (this._ch != ',')
                    {
                        return array;
                    }
                    this.ReadNext();
                    this.ReadWhiteSpaces();
                }
                return array;
            }
            this.ReadNext();
            return array;
        }

        internal JsonString ReadJsonObject()
        {
            JsonString str = new JsonString();
            int num = 0;
            bool flag = false;
            while (this._ch != '\0')
            {
                if (this._ch == '(')
                {
                    num++;
                    flag = true;
                }
                else if (this._ch == ')')
                {
                    num--;
                }
                str += (JsonString)this._ch;
                this.ReadNext();
                if (flag && (num == 0))
                {
                    return str;
                }
            }
            return str;
        }

        internal bool ReadNext()
        {
            if (this._idx >= this._json.Length)
            {
                this._ch = '\0';
                return false;
            }
            this._ch = this._json[this._idx];
            this._idx++;
            return true;
        }

        internal JsonNumber ReadNumber()
        {
            JsonNumber number = new JsonNumber();
            if (this._ch == '-')
            {
                number += "-";
                this.ReadNext();
            }
            while (((this._ch >= '0') && (this._ch <= '9')) && (this._ch != '\0'))
            {
                number += (JsonNumber)this._ch;
                this.ReadNext();
            }
            if (this._ch == '.')
            {
                number += Convert.ToChar(0x2e);
                this.ReadNext();
                while (((this._ch >= '0') && (this._ch <= '9')) && (this._ch != '\0'))
                {
                    number += (JsonNumber)this._ch;
                    this.ReadNext();
                }
            }
            if ((this._ch == 'e') || (this._ch == 'E'))
            {
                number += Convert.ToChar(0x65);
                this.ReadNext();
                if ((this._ch == '-') || (this._ch == '+'))
                {
                    number += (JsonNumber)this._ch;
                    this.ReadNext();
                }
                while (((this._ch >= '0') && (this._ch <= '9')) && (this._ch != '\0'))
                {
                    number += (JsonNumber)this._ch;
                    this.ReadNext();
                }
            }
            return number;
        }

        internal JsonObject ReadObject()
        {
            JsonObject obj2 = new JsonObject();
            if (this._ch == '{')
            {
                this.ReadNext();
                this.ReadWhiteSpaces();
                if (this._ch != '}')
                {
                    while (this._ch != '\0')
                    {
                        string key = "";
                        //key = (string)this.ReadString();
                        //修改Key读取
                        if (this._ch == '"' || this._ch == '\'')
                        {
                            key = (string)this.ReadString();
                        }
                        else {
                            this.ReadPrev();
                            JsonString tkey = new JsonString();
               while (this.ReadNext() && this._ch != ':'){ tkey += this._ch;}
                            key = (string)tkey;
                         }
                     
                        this.ReadWhiteSpaces();
                        if (this._ch != ':')
                        {
                            break;
                        }
                        this.ReadNext();
                        obj2.Add(key, this.GetObject());
                        this.ReadWhiteSpaces();
                        if (this._ch == '}')
                        {
                            this.ReadNext();
                            return obj2;
                        }
                        if (this._ch != ',')
                        {
                            break;
                        }
                        this.ReadNext();
                        this.ReadWhiteSpaces();
                    }
                }
                else
                {
                    this.ReadNext();
                    return obj2;
                }
            }
            throw new NotSupportedException("obj");
        }

        internal bool ReadPrev()
        {
            if (this._idx <= 0)
            {
                return false;
            }
            this._idx--;
            this._ch = this._json[this._idx];
            return true;
        }

        internal string ReadUnicode()
        {

            string c = "";
             c="";
             for(int i=0;i<4;i++)
             {
             this._ch =this._json[this._idx];
             c+=this._ch;
             this._idx++; 
             }
            //this._ch="";

                byte[] BAry = new byte[2];
                BAry[1] = System.Convert.ToByte(c.Substring(0, 2), 16); 
                BAry[0] = System.Convert.ToByte(c.Substring(2, 2), 16);
                c = System.Text.Encoding.Unicode.GetString(BAry);

            /*
             //$val = hexdec ($c); 
            
            uint val = 0x0050;
            byte[] s;
            if (val < 0x7f)
            {
                s = new byte[1];
                s[0] = (byte)val;

            }
            else if (val < 0x800)
            {
                s = new byte[3];
                s[1] = (byte)(0xc0 | (val >> 6));
                s[2] = (byte)(0x80 | (val & 0x3f));

            }
            else
            {
                s = new byte[4];

                s[1] = (byte)(0xe0 | (val >> 12));


                s[2] = (byte)(0x80 | ((val >> 6) & 0x3f));


                s[3] = (byte)(0x80 | (val & 0x3f));

            }

            c = System.Text.Encoding.UTF8.GetString(s);
*/
            return c;
        }

        internal JsonString ReadString()
        {
            JsonString str = new JsonString();
            //修改String读取  if (this._ch != '"')添加schar
            char schar = '"'; if (this._ch == '\'' && schar == '"') { schar = '\''; }

            if (this._ch != schar)
            {
                throw new NotSupportedException("The string could not be read.");
            }

            while (this.ReadNext())
            {
                //修改String读取  if (this._ch != '"')
                if (this._ch == schar)
                {
                    this.ReadNext();
                   
                    return str;
                }
                if (this._ch == '\\')
                {
                    this.ReadNext();
                    switch (this._ch)
                    {
                        case 'r':
                            {
                                str += Convert.ToChar(13);
                                continue;
                            }
                        case 't':
                            {
                                str += Convert.ToChar(9);
                                continue;
                            }
                        case 'n':
                            {
                                str += Convert.ToChar(10);
                                continue;
                            }
                        case 'f':
                            {
                                str += Convert.ToChar(12);
                                continue;
                            }
                        case '\\':
                            {
                                str += Convert.ToChar(0x5c);
                                continue;
                            }
                        case 'b':
                            {
                                str += Convert.ToChar(8);
                                continue;
                            }
                        case 'u':
                            {
                                str += ReadUnicode();
                                continue;
                            }
                    }
                    str += (JsonString)this._ch;
                    continue;
                }
                str += (JsonString)this._ch;
            }
            return str;
        }

        internal void ReadWhiteSpaces()
        {
            while ((this._ch != '\0') && (this._ch <= ' '))
            {
                this.ReadNext();
            }
        }

        internal IJsonObject ReadWord()
        {
            char ch = this._ch;
            switch (ch)
            {
                case 'f':
                    if (!this.CompareNext("alse"))
                    {
                        break;
                    }
                    this.ReadNext();
                    this.ReadNext();
                    this.ReadNext();
                    this.ReadNext();
                    this.ReadNext();
                    return new JsonBoolean(false);

                case 'n':
                    if (this.CompareNext("ull"))
                    {
                        this.ReadNext();
                        this.ReadNext();
                        this.ReadNext();
                        this.ReadNext();
                        return null;
                    }
                    if (this.CompareNext("ew "))
                    {
                       
                        
                        JsonString S = this.ReadJsonObject();
                        string R = System.Text.RegularExpressions.Regex.Replace(S, @"\s*new\s*Date\(\s*["",'](.*?)["",']\s*\)", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (R != S) {
                            return new JsonDate(R);
                        }
                        
                        return S;
                       // return this.ReadJsonObject();
                    }
                    break;

                default:
                    if ((ch == 't') && this.CompareNext("rue"))
                    {
                        this.ReadNext();
                        this.ReadNext();
                        this.ReadNext();
                        this.ReadNext();
                        return new JsonBoolean(true);
                    }
                    break;
            }
            throw new NotSupportedException("word " + this._ch);
        }

        #region toJSON

        public string toJSON(object Obj)
        {

            System.Text.StringBuilder SB = new System.Text.StringBuilder();
            toJSON(SB, Obj,null);
            return SB.ToString();

        }

        public void toJSON(System.Text.StringBuilder sb, object obj,string format)
        {
            if (obj == null) { sb.Append("null"); return; }

            #region Base

            if (obj == null || obj == System.DBNull.Value)
            {
                sb.Append("null");
                return;
            }
            if (obj is double || obj is float || obj is long || obj is int || obj is short || obj is byte || obj is decimal)
            {
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture.NumberFormat, "{0}", obj);
                return;
            }
            if (obj.GetType().IsEnum)
            {
                sb.Append((int)obj);
                return;
            }
            if (obj is bool)
            {
                sb.Append(obj.ToString().ToLower());
                return;
            }
            if (obj is string || obj is Guid)
            {
                System.Text.StringBuilder sbtemp = new System.Text.StringBuilder();
                if (obj is Guid && obj != null) { obj = obj.ToString(); }
                sbtemp.Append("\"");
                string s = obj as string;
                if (s == null)
                {
                    sbtemp.Append("\"");
                    sb.Append(sbtemp.ToString());
                    return;
                }
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '\"':
                            sbtemp.Append("\\\"");
                            break;
                        case '\\':
                            sbtemp.Append("\\\\");
                            break;
                        case '\b':
                            sbtemp.Append("\\b");
                            break;
                        case '\f':
                            sbtemp.Append("\\f");
                            break;
                        case '\n':
                            sbtemp.Append("\\n");
                            break;
                        case '\r':
                            sbtemp.Append("\\r");
                            break;
                        case '\t':
                            sbtemp.Append("\\t");
                            break;
                        default:
                            int i = (int)c;
                            if (i < 32 || i > 127)
                            {
                                sbtemp.Append(c);
                                // sbtemp.AppendFormat("\\u{0:X04}", i);
                            }
                            else
                            {
                                sbtemp.Append(c);
                            }
                            break;
                    }
                }
                sbtemp.Append("\"");
                sb.Append(sbtemp.ToString());
                return;
            }
            if (obj is DateTime)
            {
                sb.Append("\"");
                sb.Append(((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss"));
                sb.Append("\"");

                /*
                //en-US
                SB.Append("new Date(\"");
                DateTime TDateTime = (DateTime)Obj;

                TDateTime = TDateTime.ToLocalTime();

                SB.Append(TDateTime.ToString("MMMM, d yyyy HH:mm:ss", new System.Globalization.CultureInfo("en-US", false).DateTimeFormat));
                SB.Append("\")");
                 */

                return;
            }





            #endregion Base

            #region JsonBase

            if (obj is JsonNumber)
            {
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture.NumberFormat, "{0}", obj);
                return;
            }
            if (obj is JsonBoolean)
            {
                sb.Append(((JsonBoolean)obj).Value.ToLower());
                return;
            }
            if (obj is JsonString)
            {
                toJSON(sb, obj + "",format);
                return;
            }
            if (obj is JsonDate)
            {
                toJSON(sb, ((JsonDate)obj).getDate(), format);
                return;
            }


            #endregion JsonBase

            #region System.Data
            if (obj is System.Data.DataSet)
            {
                System.Data.DataSet DS = obj as System.Data.DataSet;
                sb.Append("{\"Tables\":{");
                foreach (System.Data.DataTable DT in DS.Tables)
                {
                    sb.AppendFormat("\"{0}\":", DT.TableName);
                    toJSON(sb, DT, format);
                    sb.Append(",");
                }
                // Remove the trailing comma.
                if (DS.Tables.Count > 0)
                {
                    --sb.Length;
                }
                sb.Append("}}");
                return;
            }
            if (obj is System.Data.DataTable)
            {
                sb.Append("{\"Rows\":[");
                System.Data.DataTable DT = obj as System.Data.DataTable;
                foreach (System.Data.DataRow DR in DT.Rows)
                {
                    toJSON(sb, DR, format);
                    sb.Append(",");
                }
                // Remove the trailing comma.
                if (DT.Rows.Count > 0)
                {
                    --sb.Length;
                }
                sb.Append("]}");
                return;
            }
            if (obj is System.Data.DataRow)
            {
                sb.Append("{");
                System.Data.DataRow DR = obj as System.Data.DataRow;
                foreach (System.Data.DataColumn Column in DR.Table.Columns)
                {
                    sb.AppendFormat("\"{0}\":", Column.ColumnName);
                    toJSON(sb, DR[Column], format);
                    sb.Append(",");
                }
                // Remove the trailing comma.
                if (DR.Table.Columns.Count > 0)
                {
                    --sb.Length;
                }
                sb.Append("}");
                return;
            }
            #endregion System.Data

            #region Item Check

            System.Reflection.PropertyInfo PI = obj.GetType().GetProperty("Item");

            #region Object
            if (PI != null && obj.GetType().GetProperty("Keys") != null)
            {
                System.Reflection.PropertyInfo PIK = obj.GetType().GetProperty("Keys");
                object okeys = PIK.GetValue(obj, null);
                string[] skeys = null;
                if (okeys is string[]) { skeys = (string[])okeys; }

                if (skeys == null)
                {

                    System.Reflection.MethodInfo MH = okeys.GetType().GetMethod("CopyTo");
                    System.Reflection.PropertyInfo PIC = okeys.GetType().GetProperty("Count");
                    int count = 0;
                    if (PIC != null) count = (int)PIC.GetValue(okeys, null);


                    if (count > 0 && MH != null && MH.GetParameters().Length > 0 && MH.GetParameters()[0].ParameterType == typeof(System.Array))
                    {

                        System.Array akeys = System.Array.CreateInstance(typeof(object), count);
                        MH.Invoke(okeys, new object[] { akeys, 0 });
                        skeys = new string[count];
                        int i = 0;
                        foreach (object key in akeys)
                        {
                            if (!(key is string)) continue;
                            if (key + "" == "") continue;
                            skeys[i] = key + "";
                            i++;
                        }


                    }
                    else if (count > 0 && MH != null && MH.GetParameters().Length > 0 && MH.GetParameters()[0].ParameterType == typeof(string[]))
                    {
                        skeys = new string[count];
                        MH.Invoke(okeys, new object[] { skeys, 0 });

                    }
                }
                //if (skeys != null && skeys.Length > 0){
                    sb.Append("{");
                    int c = 0;
                    for (int i = 0;skeys != null && i < skeys.Length; i++)
                    {
                        string key = skeys[i]; if (key == null) continue;

                        if (c != 0) sb.Append(",");
                        sb.Append("\"" + key + "\":");
                        toJSON(sb, PI.GetValue(obj, new object[] { key }), format);

                        c++;



                    }
                    sb.Append("}");
                //}

                return;



            }
            #endregion Object

            else if (PI != null && (obj.GetType().GetProperty("Count") != null || obj.GetType().GetProperty("Length") != null) && PI.GetIndexParameters().Length > 0 && PI.GetIndexParameters()[0].ParameterType == typeof(int))
            {
                int c = 0;
                if (c == 0 && obj.GetType().GetProperty("Count").GetValue(obj, null) != null)
                    c = (int)obj.GetType().GetProperty("Count").GetValue(obj, null);
                else if (c == 0 && obj.GetType().GetProperty("Length").GetValue(obj, null) != null)
                    c = (int)obj.GetType().GetProperty("Length").GetValue(obj, null);
                //if (c > 0){
                    sb.Append("[");
                    for (int i = 0; i < c; i++)
                    {
                        if (i != 0) sb.Append(",");

                        toJSON(sb, PI.GetValue(obj, new object[] { i }),format);

                    }

                    sb.Append("]");

               // }

                return;
            }
            #endregion Item Check

            #region Array
            if (obj.GetType().GetProperty("Length") != null && obj.GetType().GetMethod("GetValue", new Type[] { typeof(int) }) != null)
            {

                int l = (int)obj.GetType().GetProperty("Length").GetValue(obj, null);
                //if (l > 0){
                    sb.Append("[");
                    for (int i = 0; i < l; i++)
                    {
                        if (i != 0) sb.Append(",");
                        toJSON(sb, obj.GetType().GetMethod("GetValue", new Type[] { typeof(int) }).Invoke(obj, new object[] { i }), format);

                    }
                    sb.Append("]");
               // }
                return;
            }
            #endregion Array



            #region other
            if (obj is System.Collections.IEnumerable)
            {

                bool hasItems = false;
                System.Collections.IEnumerable e = obj as System.Collections.IEnumerable;

                sb.Append("[");
                foreach (object val in e)
                {
                    toJSON(sb, val,format);
                    sb.Append(",");
                    hasItems = true;
                }
                // Remove the trailing comma.
                if (hasItems)
                {
                    --sb.Length;
                }
                sb.Append("]");
                return;
            }
            #endregion other

           
            sb.Append("\""+obj.GetType().FullName+"\"");



        }
        
       
        

        #endregion toJSON


    }


}

