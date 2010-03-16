﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CDR.Decoder.BER
{
    using TlvObjectList = List<TlvObject>;

    public enum AsnTagClass
    {
        Universal = 0x00,
        Application = 0x40,
        ContextSpecific = 0x80,
        Private = 0xC0
    }

    public class TlvObject   // Type-Length-Value object
    {
        private int _tag;
        private AsnTagClass _tagClass;
        private bool _tagConstructed;
        private object _tagValue;
        private string _tagPath;
        private long _tagOffset;

        public TlvObject(int tag, AsnTagClass tagClass, bool tagConstructed, long tagOffset)
        {
            _tag = tag;
            _tagClass = tagClass;
            _tagConstructed = tagConstructed;
            _tagPath = tag.ToString();
            _tagOffset = tagOffset;
        }

        public int Tag { get { return _tag; } }
        public AsnTagClass Class { get { return _tagClass; } }
        public bool IsConstructed { get { return _tagConstructed; } }
        public object Value { get { return _tagValue; } }
        public string Path { get { return _tagPath; } }
        public long Offset { get { return _tagOffset; } }
        public bool IsEmpty
        {
            get
            {
                if (_tagValue == null)
                {
                    return true;
                }
                else if (_tagConstructed)
                {
                    return (_tagValue as TlvObjectList).Count > 0 ? false : true;
                }
                else
                {
                    return (_tagValue as byte[]).Length > 0 ? false : true;
                }
            }
        }

        public int ReadValue(Stream asnStream, int bytesToRead)
        {
            // TODO: Исключить чтение для типов Constructed, поставить проверку на null
            _tagValue = new byte[bytesToRead];
            return asnStream.Read((_tagValue as byte[]), 0, bytesToRead);
        }

        private void UpdateAllPaths()
        {
            if (this.IsConstructed && !this.IsEmpty)
            {
                foreach (TlvObject _tlv in (this.Value as TlvObjectList))
                {
                    _tlv._tagPath = String.Concat(this._tagPath, ".", _tlv._tag.ToString());
                    _tlv.UpdateAllPaths();
                }
            }
        }

        public void AddTlv(TlvObject tlvChild)
        {
            if (_tagValue == null)
            {
                _tagValue = new TlvObjectList(1);
            }
            tlvChild._tagPath = String.Concat(this._tagPath, ".", tlvChild._tagPath);
            tlvChild.UpdateAllPaths();
            (_tagValue as TlvObjectList).Add(tlvChild);
        }

        public void DumpToXml(TextWriter dumpWriter, int ident)
        {
            for (int i = ident; i > 0; i--) { dumpWriter.Write("\t"); }
            dumpWriter.Write("<{0} Offset=\"{1}\" Tag=\"{2}({3})\">"
                , this.IsConstructed ? "Constructed" : "Primitive"
                , this.Offset
                , this.Class.ToString()
                , this.Tag);
            if (this.IsEmpty)
            {
                dumpWriter.WriteLine("</{0}>", this.IsConstructed ? "Constructed" : "Primitive");
            }
            else if (this.IsConstructed)
            {
                dumpWriter.WriteLine();
                foreach (TlvObject tlv in (this.Value as TlvObjectList)) { tlv.DumpToXml(dumpWriter, ident + 1); };
                for (int i = ident; i > 0; i--) { dumpWriter.Write("\t"); }
                dumpWriter.WriteLine("</Constructed>");
            }
            else
            {
                dumpWriter.Write(BitConverter.ToString(this.Value as byte[]).Replace("-", String.Empty));
                dumpWriter.WriteLine("</Primitive>");
            }
        }

        public override string ToString()
        {
            StringBuilder record = new StringBuilder(String.Format("{0}=", this.Path));
            if (this.IsConstructed)
            {
                record.Append('[');
                if (!this.IsEmpty)
                {
                    for (int i = 0; i < (this.Value as TlvObjectList).Count; i++)
                    {
                        if (i > 0) record.Append(' ');
                        record.Append((this.Value as TlvObjectList)[i].ToString());
                    }
                }
                record.Append(']');
            }
            else
            {
                if (!this.IsEmpty)
                {
                    record.AppendFormat("\"{0}\"", BitConverter.ToString(this.Value as byte[]).Replace("-", String.Empty));
                }
            }

            return record.ToString();
        }

        public void DumpToTxt(TextWriter dumpWriter, string leftHeader)
        {
            dumpWriter.Write("{0} {1}", leftHeader, this.ToString());
        }
    }
}