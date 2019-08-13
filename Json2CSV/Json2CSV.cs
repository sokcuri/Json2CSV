using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Collections;
using System.Windows.Forms;

namespace Json2CSV
{
    public static class Json2CSV
    {
        static void ResolveTypeDictionary(JToken Token, string ParentName, Stack<JTokenType> TokenStack, ref OrderedDictionary Dictionary)
        {
            if (Token.Type == JTokenType.Array)
            {
                foreach (var item in Token.Children())
                {
                    var ParentProcName = ((ParentName.Length > 0) ? $"{ParentName}[]" : "");
                    TokenStack.Push(Token.Type);
                    ResolveTypeDictionary(item, ParentProcName, TokenStack, ref Dictionary);
                    TokenStack.Pop();
                }
            }
            else if (Token.Type == JTokenType.Object)
            {
                var Properties = Token.Children<JProperty>();
                foreach (var prop in Properties)
                {
                    TokenStack.Push(Token.Type);
                    var ParentProcName = ((ParentName.Length > 0) ? $"{ParentName}." : "") + prop.Name;
                    ResolveTypeDictionary(prop.Value, ParentProcName, TokenStack, ref Dictionary);
                    TokenStack.Pop();
                }
            }
            else
            {
                var arr = new JTokenType[TokenStack.Count];
                TokenStack.CopyTo(arr, 0);
                Array.Reverse(arr);
                var TempTokenStack = new Stack<JTokenType>(arr);

                if (TempTokenStack.Count > 2 &&
                    Token.Type == JTokenType.Boolean &&
                    TempTokenStack.Pop() == JTokenType.Object &&
                    TempTokenStack.Pop() == JTokenType.Object)
                {
                    {
                        var ParentProcName = ParentName.Substring(0, ParentName.LastIndexOf(".")) + "[]" + "/" + "True";
                        if (!Dictionary.Contains(ParentProcName))
                            Dictionary.Add(ParentProcName, JTokenType.String.ToString());
                    }

                    {
                        var ParentProcName = ParentName.Substring(0, ParentName.LastIndexOf(".")) + "[]" + "/" + "False";
                        if (!Dictionary.Contains(ParentProcName))
                            Dictionary.Add(ParentProcName, JTokenType.String.ToString());
                    }
                }
                else
                {
                    if (!Dictionary.Contains(ParentName))
                        Dictionary.Add(ParentName, Token.Type.ToString());
                }
            }
        }


        static void ConvertJsonToTbl(JToken Token, string ParentName, Stack<JTokenType> TokenStack, ref OrderedDictionary Dictionary)
        {
            if (Token.Type == JTokenType.Array)
            {
                foreach (var item in Token.Children())
                {
                    OrderedDictionary TempDictionary = new OrderedDictionary();
                    foreach (DictionaryEntry d in Dictionary)
                    {
                        TempDictionary.Add(d.Key, new List<string> { });
                    }

                    var ParentProcName = ((ParentName.Length > 0) ? $"{ParentName}[]" : "");
                    TokenStack.Push(Token.Type);
                    ConvertJsonToTbl(item, ParentProcName, TokenStack, ref TempDictionary);
                    TokenStack.Pop();

                    foreach (DictionaryEntry d in TempDictionary)
                    {
                        var Result = "";
                        foreach (var s in d.Value as List<string>)
                        {
                            if (s == "")
                                continue;

                            if (Result != "")
                            {
                                Result += "|";
                            }
                            Result += s;
                        }
                        (Dictionary[d.Key] as List<string>).Add(Result);
                    }
                }
            }
            else if (Token.Type == JTokenType.Object)
            {
                var Properties = Token.Children<JProperty>();
                foreach (var prop in Properties)
                {
                    TokenStack.Push(Token.Type);
                    var ParentProcName = ((ParentName.Length > 0) ? $"{ParentName}." : "") + prop.Name;
                    ConvertJsonToTbl(prop.Value, ParentProcName, TokenStack, ref Dictionary);
                    TokenStack.Pop();
                }
            }
            else
            {
                var arr = new JTokenType[TokenStack.Count];
                TokenStack.CopyTo(arr, 0);
                Array.Reverse(arr);
                var TempTokenStack = new Stack<JTokenType>(arr);

                if (TempTokenStack.Count > 2 &&
                    Token.Type == JTokenType.Boolean &&
                    TempTokenStack.Pop() == JTokenType.Object &&
                    TempTokenStack.Pop() == JTokenType.Object)
                {
                    var BooleanType = Token.Value<Boolean>();
                    var BooleanString = BooleanType ? "True" : "False";
                    var ParentProcName = ParentName.Substring(0, ParentName.LastIndexOf(".")) + "[]" + "/" + BooleanString;
                    var ValueList = Dictionary[ParentProcName] as List<string>;
                    var FillValue = ParentName.Substring(ParentName.LastIndexOf(".") + 1);
                    ValueList.Add(FillValue);
                }
                else if (TempTokenStack.Count > 2 &&
                    Token.Type == JTokenType.String &&
                    TempTokenStack.Pop() == JTokenType.Array &&
                    TempTokenStack.Pop() == JTokenType.Object)
                {
                    var ValueList = Dictionary[ParentName] as List<string>;
                    ValueList.Add(Token.ToString());
                }
                else if (Token.Type == JTokenType.Boolean)
                {
                    var ValueList = Dictionary[ParentName] as List<string>;
                    ValueList.Add(Token.ToString() == "True" ? "true" : "false");
                }
                else
                {
                    var ValueList = Dictionary[ParentName] as List<string>;
                    ValueList.Add(Token.ToString());
                }
            }
        }

        static bool IsWholeEmptyField(string FieldName, OrderedDictionary Dictionary)
        {
            foreach (string s in Dictionary[FieldName] as List<string>)
            {
                if (s.Length > 0)
                    return false;
            }
            return true;
        }

        public static bool Convert(string filename)
        {
            string text = File.ReadAllText(filename);
            var array = JToken.Parse(text);
            OrderedDictionary TypeDictionary = new OrderedDictionary();
            ResolveTypeDictionary(array, "", new Stack<JTokenType>(), ref TypeDictionary);

            OrderedDictionary TableDictionary = new OrderedDictionary();
            foreach (DictionaryEntry d in TypeDictionary)
            {
                TableDictionary.Add(d.Key, new List<string> { });
            }

            ConvertJsonToTbl(array, "", new Stack<JTokenType>(), ref TableDictionary);

            var FieldNames = "";
            var LineCount = 0;
            foreach (DictionaryEntry d in TableDictionary)
            {
                if (IsWholeEmptyField(d.Key.ToString(), TableDictionary))
                    continue;

                if (LineCount == 0)
                {
                    LineCount = (d.Value as List<string>).Count;
                }
                if (FieldNames.Length > 0)
                {
                    FieldNames += ",";
                }
                FieldNames += d.Key;
            }

            var Contents = FieldNames + "\n";
            for (int i = 0; i < LineCount; i++)
            {
                var Lines = "";
                foreach (DictionaryEntry d in TableDictionary)
                {
                    if (IsWholeEmptyField(d.Key.ToString(), TableDictionary))
                        continue;

                    if (Lines.Length > 0)
                        Lines += ",";
                    Lines += (TableDictionary[d.Key] as List<string>)[i];
                }
                Contents += Lines + "\n";
            }

            string csvFileName = filename.Substring(0, filename.LastIndexOf("."));
            csvFileName += ".csv";

            try
            {
                File.WriteAllText(csvFileName, Contents);
            }
            catch (IOException exception)
            {
                MessageBox.Show(exception.Message, "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
    }
}