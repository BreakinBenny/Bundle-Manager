﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BundleUtilities
{
    public class BinaryReader2 : BinaryReader
    {
        public bool BigEndian { get; set; }

        public BinaryReader2(Stream input) : base(input)
        {
            BigEndian = false;
        }

        public BinaryReader2(Stream input, Encoding encoding) : base(input, encoding)
        {
            BigEndian = false;
        }

        public BinaryReader2(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
            BigEndian = false;
        }

        private bool ShouldFlip()
        {
            return (BigEndian && BitConverter.IsLittleEndian) || (!BigEndian && !BitConverter.IsLittleEndian);
        }

        public override short ReadInt16()
        {
            var data = base.ReadBytes(2);
            if (data.Length < 2)
                throw new EndOfStreamException();
            if (ShouldFlip())
                Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public override int ReadInt32()
        {
            var data = base.ReadBytes(4);
            if (data.Length < 4)
                throw new EndOfStreamException();
            if (ShouldFlip())
                Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public override long ReadInt64()
        {
            var data = base.ReadBytes(8);
            if (data.Length < 8)
                throw new EndOfStreamException();
            if (ShouldFlip())
                Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public override ushort ReadUInt16()
        {
            var data = base.ReadBytes(2);
            if (data.Length < 2)
                throw new EndOfStreamException();
            if (ShouldFlip())
                Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public override uint ReadUInt32()
        {
            var data = base.ReadBytes(4);
            if (data.Length < 4)
                throw new EndOfStreamException();
            if (ShouldFlip())
                Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public override ulong ReadUInt64()
        {
            var data = base.ReadBytes(8);
            if (data.Length < 8)
                throw new EndOfStreamException();
            if (ShouldFlip())
                Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public override float ReadSingle()
        {
            var data = base.ReadBytes(4);
            if (data.Length < 4)
                throw new EndOfStreamException();
            if (ShouldFlip())
                Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        public override double ReadDouble()
        {
            var data = base.ReadBytes(8);
            if (data.Length < 8)
                throw new EndOfStreamException();
            if (ShouldFlip())
                Array.Reverse(data);
            return BitConverter.ToDouble(data, 0);
        }
    }
}
