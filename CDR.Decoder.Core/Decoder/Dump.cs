﻿using System;
using System.IO;
using CDR.Decoder.BER;

namespace CDR.Decoder
{
    public enum DumpType
    {
        TLV_TXT = 0,
        TLV_XML = 1,
        ELEMENT_TXT = 2,
        ELEMENT_XML = 3
    }

    public partial class CdrDecoder : BerDecoder
    {
        public int Dump(Stream asnStream)
        {
            return this.Dump(asnStream, System.Console.Out, DumpType.ELEMENT_TXT, byte.MaxValue);
        }

        public int Dump(Stream asnStream, DumpType dumpType)
        {
            return this.Dump(asnStream, System.Console.Out, dumpType, byte.MaxValue);
        }

        public int Dump(Stream asnStream, TextWriter dumpWriter, DumpType dumpType, byte maxLevel)
        {
            long offset = asnStream.Position;

            BerDecoderResult pr = BerDecoderResult.Finished;
            TlvObject tlv;
            CdrElement record;
            int cnt = 0;

            for (; ; )//(int n = 11; n > 0; n--)
            {
                pr = DecodeTlv(asnStream, out tlv, ref offset, 0, maxLevel);

                if (pr != BerDecoderResult.Finished)
                    break;

                switch (dumpType)
                {
                    case DumpType.TLV_TXT:
                        tlv.DumpToTxt(dumpWriter, String.Format("{0,8} > {1}", tlv.Offset, ++cnt));
                        dumpWriter.WriteLine();
                        break;
                    case DumpType.TLV_XML:
                        tlv.DumpToXml(dumpWriter, 0);
                        break;
                    case DumpType.ELEMENT_TXT:
                        record = _tlvParser.ParseTlvObject(tlv);
                        record.DumpToTxt(dumpWriter, String.Format("{0,8} > {1}", record.Offset, ++cnt));
                        dumpWriter.WriteLine();
                        break;
                    case DumpType.ELEMENT_XML:
                        record = _tlvParser.ParseTlvObject(tlv);
                        record.DumpToXml(dumpWriter, 0);
                        break;
                }
            }

            dumpWriter.Flush();

            return (int)(pr);
        }
    }
}