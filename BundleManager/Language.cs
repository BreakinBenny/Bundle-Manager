﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BundleFormat;
using BundleUtilities;

namespace BundleManager
{
    public class Language
    {
        public int Unknown1;
        public Dictionary<uint, string> Data;
        public int Unknown2;

        public Language()
        {
            Data = new Dictionary<uint, string>();
        }

        public static Language Read(BundleEntry entry)
        {
            Language result = new Language();

            MemoryStream ms = entry.MakeStream();
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = entry.Console;

            result.Unknown1 = br.ReadInt32();
            int count = br.ReadInt32();
            result.Unknown2 = br.ReadInt32();

            for (int i = 0; i < count - 1; i++)
            {
                uint id = br.ReadUInt32();
                string txt = br.ReadCStringPtr();
                result.Data.Add(id, txt);
            }

            br.Close();
            ms.Close();

            //result.Write(entry);

            return result;
        }

        public void Write(BundleEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(Unknown1);
            bw.Write(Data.Count + 1);
            bw.Write(Unknown2);

            long headPos = bw.BaseStream.Position;
            
            foreach (uint id in Data.Keys)
            {
                bw.Write(id);
                bw.Write((uint) 0); // offset
            }

            bw.Write((uint)0);
            bw.Write((uint)0); // offset

            int index = 0;
            foreach (uint id in Data.Keys)
            {
                long pos = bw.BaseStream.Position;

                // go back and write offset
                bw.BaseStream.Position = headPos + (index * 8) + 4;
                bw.Write((uint)pos);

                bw.BaseStream.Position = pos;
                if (Data[id] == null)
                    bw.WriteCStr("");
                else
                    bw.WriteCStr(Data[id]);
                index++;
            }

            long pos2 = bw.BaseStream.Position;

            // go back and write offset
            bw.BaseStream.Position = headPos + (index * 8) + 4;
            bw.Write((uint)pos2);

            bw.BaseStream.Position = pos2;

            int paddingCount = (int)(entry.Header.Length - bw.BaseStream.Position);

            string padding = "";
            for (int i = 0; i < paddingCount; i++)
                padding += "A";
            bw.WriteCStr(padding);

            bw.Flush();
            byte[] data = ms.ToArray();
            bw.Close();
            ms.Close();

            entry.Header = data;
            entry.Dirty = true;
        }

        public static uint HashID(string id)
        {
            /*uint result = 0xFFFFFFFF;
            for (int i = 0; i < id.Length; i++)
            {
                byte b = (byte)id[i];
                //if (b == 0)
                //    break;
                byte newByte = (byte)((b - 0x41) & 0xFF);
                if (newByte <= 0x19)
                    b += 0x20;
                byte lowByte = (byte)(result & 0xFF);
                lowByte = (byte)((lowByte ^ b) & 0xFF);
                result = (result >> 8) & 0x00FFFFFF;
                result = (result ^ Crc32.Crc32BTable[lowByte]) & 0xFFFFFFFF;
            }
            result = (~result) & 0xFFFFFFFF;

            return result;*/

            byte[] message = Encoding.ASCII.GetBytes(id);
            UInt32 hash = UInt32.MaxValue;
            for (UInt32 i = 0; i < message.Length; i++)
            {
                UInt32 index = message[i];
                index ^= (hash & 0x000000FF);
                hash = Crc32.Crc32BTable[index] ^ (hash >> 8);
            }

            return hash;
        }
    }
}