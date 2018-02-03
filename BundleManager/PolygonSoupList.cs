﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BundleFormat;
using BundleUtilities;
using DebugHelper;
using MathLib;
using ModelViewer.SceneData;
using OpenTK;
using StandardExtension;

namespace BundleManager
{
    public class PolygonSoupProperty
    {
        public uint UnknownProperty;
        public byte[] Indices;
		public byte[] IndicesIndices;

		public PolygonSoupProperty()
        {
            Indices = new byte[4];
			IndicesIndices = new byte[4];
		}

        public static PolygonSoupProperty Read(BinaryReader br)
        {
            PolygonSoupProperty result = new PolygonSoupProperty();

            result.UnknownProperty = br.ReadUInt32();

            for (int i = 0; i < result.Indices.Length; i++)
            {
                result.Indices[i] = br.ReadByte();
			}

			for (int i = 0; i < result.IndicesIndices.Length; i++)
			{
				result.IndicesIndices[i] = br.ReadByte();
			}

            return result;
        }

        public void Write(BinaryWriter bw)
        {
            // Cap to 0x9D64 for PC
            ushort unknownProperty1 = (ushort)(UnknownProperty & 0xFFFF);
            /*byte unk1 = (byte) (unknownProperty1 & 0xFF);
            byte unk2 = (byte) (unknownProperty1 >> 8);

            if (unk2 > 0x9D && unk2 != 0xFF)
                unk2 = 0x9D;

            if (unk1 > 0x64 && unk1 != 0xFF)
                unk1 = 0x64;*/

            //unknownProperty1 = (ushort)((unk2 << 8) | unk1);

            if (unknownProperty1 > 0x9D64 && unknownProperty1 != 0xFFFF)
            {
                unknownProperty1 = 0x8316; //0x9531; // 0x9007 //0x8511 //0x8316;
            }


            //if (unknownProperty1 > 0x9D64)
            //    unknownProperty1 = 0xFFFF;
            ushort unknownProperty2 = (ushort) ((UnknownProperty >> 16) & 0xFFFF);
            uint unknownProperty = (uint)((unknownProperty2 << 16) | unknownProperty1);

            bw.Write(unknownProperty);

            for (int i = 0; i < Indices.Length; i++)
            {
                bw.Write(Indices[i]);
            }

            for (int i = 0; i < IndicesIndices.Length; i++)
            {
                bw.Write(IndicesIndices[i]);
            }
        }

        public override string ToString()
        {
            return "Prop: " + UnknownProperty.ToString("X8");
        }
    }

    public class PolygonSoupBoundingBox
    {
        public BoxF Box;
        public int Unknown;

        public override string ToString()
        {
            return Box.ToString() + ", " + Unknown;
        }
    }

    public class PolygonSoupChunk
    {
        public Vector3I Position;
        public float Scale;
        public uint PropertyListStart;
        public uint PointListStart;
        public short Unknown7;
        public byte Unknown8;
        public byte Unknown9;
        public byte PointCount;
        public byte Unknown10;
        public short Unknown11;

        public List<Vector3S> PointList;
        public List<PolygonSoupProperty> PropertyList;

        public PolygonSoupChunk()
        {
            PointList = new List<Vector3S>();
            PropertyList = new List<PolygonSoupProperty>();
        }

        public static PolygonSoupChunk Read(BinaryReader br)
        {
            PolygonSoupChunk result = new PolygonSoupChunk();

            result.Position = br.ReadVector3I();
            result.Scale = br.ReadSingle();
            result.PropertyListStart = br.ReadUInt32();
            result.PointListStart = br.ReadUInt32();
            result.Unknown7 = br.ReadInt16();
            result.Unknown8 = br.ReadByte();
            result.Unknown9 = br.ReadByte();
            result.PointCount = br.ReadByte();
            result.Unknown10 = br.ReadByte();
            result.Unknown11 = br.ReadInt16();

            br.BaseStream.Position = result.PointListStart;
            for (int i = 0; i < result.PointCount; i++)
            {
                result.PointList.Add(br.ReadVector3S());
            }

            br.BaseStream.Position = result.PropertyListStart;

            int count = (result.Unknown9 >> 1) * 2 +
                        (result.Unknown9 - (result.Unknown9 >> 1) * 2) +
                        (((result.Unknown8 - result.Unknown9) >> 2) * 4) +
                        ((result.Unknown8 - result.Unknown9) - ((result.Unknown8 - result.Unknown9) >> 2) * 4);

            for (int i = 0; i < count; i++)
            {
                result.PropertyList.Add(PolygonSoupProperty.Read(br));
            }

            return result;
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Position);
            bw.Write(Scale);
            bw.Write(PropertyListStart);
            bw.Write(PointListStart);
            bw.Write(Unknown7);
            bw.Write(Unknown8);
            bw.Write(Unknown9);
            bw.Write(PointCount);
            bw.Write(Unknown10);
            bw.Write(Unknown11);

            bw.BaseStream.Position = PointListStart;
            for (int i = 0; i < PointCount; i++)
            {
                bw.Write(PointList[i]);
            }

            bw.BaseStream.Position = PropertyListStart;

            int count = (Unknown9 >> 1) * 2 +
                        (Unknown9 - (Unknown9 >> 1) * 2) +
                        (((Unknown8 - Unknown9) >> 2) * 4) +
                        ((Unknown8 - Unknown9) - ((Unknown8 - Unknown9) >> 2) * 4);

            for (int i = 0; i < count; i++)
            {
                PropertyList[i].Write(bw);
            }
        }

        //public static uint Upper = 0;

        public Mesh BuildMesh(Vector3I pos, float scale)
        {
            Mesh mesh = new Mesh();
            mesh.Materials = new Dictionary<uint, Material>();

			for (int i = 0; i < PropertyList.Count; i++)
            {
                PolygonSoupProperty property = PropertyList[i];
                mesh.Indices.Add(property.Indices[0]);
                mesh.Indices.Add(property.Indices[1]);
                mesh.Indices.Add(property.Indices[2]);
                if (property.Indices[3] != 0xFF)
                {
                    mesh.Indices.Add(property.Indices[3]);
                    mesh.Indices.Add(property.Indices[2]);
                    mesh.Indices.Add(property.Indices[1]);
                }

                ushort unknownProperty1 = (ushort)(property.UnknownProperty & 0xFFFF);
                ushort unknownProperty2 = (ushort)((property.UnknownProperty >> 16));// & 0xFFFF);

                //ushort banana = (ushort)(unknownProperty1 & 0xFF);
                //ushort id = (ushort) (unknownProperty1 & 0x7FFF);

                //if (unknownProperty1 != 0xFFFF)
                // Upper = Math.Max(unknownProperty1, Upper);

                // To Reverse
                //uint unknownProperty = (uint)((unknownProperty2 << 16) | unknownProperty1);

                /*if (unknownProperty1 == 0x9DA2)
                {
                    Material mat = new Material(unknownProperty1.ToString("X4"), Color.Orange);
                    mesh.Materials[property.Indices[0]] = mat;
                    mesh.Materials[property.Indices[1]] = mat;
                    mesh.Materials[property.Indices[2]] = mat;
                    if (property.Indices[3] != 0xFF)
                        mesh.Materials[property.Indices[3]] = mat;
                } else if (unknownProperty1 > 0x9D64 && unknownProperty1 != 0xFFFF)
                {
                    Material mat = new Material(unknownProperty1.ToString("X4"), Color.Red);
                    mesh.Materials[property.Indices[0]] = mat;
                    mesh.Materials[property.Indices[1]] = mat;
                    mesh.Materials[property.Indices[2]] = mat;
                    if (property.Indices[3] != 0xFF)
                        mesh.Materials[property.Indices[3]] = mat;
                }
                else
                {
                    Material mat = new Material(unknownProperty1.ToString("X4"), Color.White);
                    mesh.Materials[property.Indices[0]] = mat;
                    mesh.Materials[property.Indices[1]] = mat;
                    mesh.Materials[property.Indices[2]] = mat;
                    if (property.Indices[3] != 0xFF)
                        mesh.Materials[property.Indices[3]] = mat;
                }*/

                /*int red = banana * 10 % 255;
                int green = banana * 5 % 255;
                int blue = banana * 12 % 255;

                Color color = Color.FromArgb(red, green, blue);
                Material mat = new Material(banana.ToString("X2"), color);
                    //Color.FromArgb(banana & 0xFF, 0, (banana >> 8) & 0xFF));
                mesh.Materials[property.Indices[0]] = mat;
                mesh.Materials[property.Indices[1]] = mat;
                mesh.Materials[property.Indices[2]] = mat;
                if (property.Indices[3] != 0xFF)
                    mesh.Materials[property.Indices[3]] = mat;*/

                //if (unknownProperty1 > 0x9D64 && unknownProperty1 != 0xFFFF)
                if (unknownProperty1 > 0x9D64 && unknownProperty1 != 0xFFFF)
                {
                    string bla = property.IndicesIndices[0].ToString("X2") + "_" +
                                 property.IndicesIndices[1].ToString("X2") + "_" +
                                 property.IndicesIndices[2].ToString("X2") + "_" +
                                 property.IndicesIndices[3].ToString("X2");
                    //Material mat = new Material(unknownProperty1.ToString("X4"),
                    Material mat = new Material(unknownProperty1.ToString("X4") + "_" + unknownProperty2.ToString("X4") + "_" + bla,
                        Color.FromArgb(unknownProperty1 & 0xFF, 0, (unknownProperty1 >> 8) & 0xFF));
                    mesh.Materials[property.Indices[0]] = mat;
                    mesh.Materials[property.Indices[1]] = mat;
                    mesh.Materials[property.Indices[2]] = mat;
                    if (property.Indices[3] != 0xFF)
                        mesh.Materials[property.Indices[3]] = mat;
                }
                else
                {
                    string bla = property.IndicesIndices[0].ToString("X2") + "_" +
                                 property.IndicesIndices[1].ToString("X2") + "_" +
                                 property.IndicesIndices[2].ToString("X2") + "_" +
                                 property.IndicesIndices[3].ToString("X2");
                    //Material mat = new Material(unknownProperty1.ToString("X4"),
                    Material mat = new Material(unknownProperty1.ToString("X4") + "_" + unknownProperty2.ToString("X4") + "_" + bla, Color.White);
                    mesh.Materials[property.Indices[0]] = mat;
                    mesh.Materials[property.Indices[1]] = mat;
                    mesh.Materials[property.Indices[2]] = mat;
                    if (property.Indices[3] != 0xFF)
                        mesh.Materials[property.Indices[3]] = mat;
                }
            }
			

			List<Vector3S> points = PointList;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3S vert = points[i];
                mesh.Vertices.Add(new Vector3((vert.X + pos.X) * scale, (vert.Y + pos.Y) * scale, (vert.Z + pos.Z) * scale));
            }

            return mesh;
        }

        public override string ToString()
        {
            return "Pos: " + Position + ", Scale: " + Scale + ", Unk7: " + Unknown7 + ", PointCount: " + PointCount;
        }
    }

    public class PolygonSoupList
    {
        public Vector3 Min;
        public int Unknown4;
        public Vector3 Max;
        public int Unknown8;
        public uint ChunkPointerStart;
        public uint BoxListStart;
        public int ChunkCount;
        public uint FileSize;

        public List<uint> ChunkPointers;
        public List<PolygonSoupBoundingBox> BoundingBoxes;
        public List<PolygonSoupChunk> Chunks;

        public PolygonSoupList()
        {
            ChunkPointers = new List<uint>();
            BoundingBoxes = new List<PolygonSoupBoundingBox>();
            Chunks = new List<PolygonSoupChunk>();
        }

        public static PolygonSoupList Read(BundleEntry entry)
        {
            PolygonSoupList result = new PolygonSoupList();

            MemoryStream ms = entry.MakeStream();
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = entry.Console;

            result.Min = br.ReadVector3F();
            result.Unknown4 = br.ReadInt32();
            result.Max = br.ReadVector3F();
            result.Unknown8 = br.ReadInt32();
            result.ChunkPointerStart = br.ReadUInt32();
            result.BoxListStart = br.ReadUInt32();
            result.ChunkCount = br.ReadInt32();
            result.FileSize = br.ReadUInt32();

            // No Data
            if (result.ChunkCount == 0)
            {
                br.Close();
                ms.Close();
                return result;
            }

            br.BaseStream.Position = result.ChunkPointerStart;

            for (int i = 0; i < result.ChunkCount; i++)
            {
                result.ChunkPointers.Add(br.ReadUInt32());
            }

            //br.BaseStream.Position += (16 - br.BaseStream.Position % 16);
            //br.BaseStream.Position = result.BoxListStart;

            for (int i = 0; i < result.ChunkCount; i++)
            {
                // Read Vertically

                long pos = result.BoxListStart + 0x70 * (i / 4) + 4 * (i % 4);

                PolygonSoupBoundingBox box = new PolygonSoupBoundingBox();

                BoxF boundingBox = new BoxF();
                br.BaseStream.Position = pos;
                float minX = br.ReadSingle();
                br.BaseStream.Position += 12;
                float minY = br.ReadSingle();
                br.BaseStream.Position += 12;
                float minZ = br.ReadSingle();

                boundingBox.Min = new Vector3(minX, minY, minZ);

                br.BaseStream.Position += 12;
                float maxX = br.ReadSingle();
                br.BaseStream.Position += 12;
                float maxY = br.ReadSingle();
                br.BaseStream.Position += 12;
                float maxZ = br.ReadSingle();

                boundingBox.Max = new Vector3(maxX, maxY, maxZ);

                box.Box = boundingBox;

                br.BaseStream.Position += 12;
                box.Unknown = br.ReadInt32();

                result.BoundingBoxes.Add(box);
            }

            for (int i = 0; i < result.ChunkPointers.Count; i++)
            {
                br.BaseStream.Position = result.ChunkPointers[i];

                result.Chunks.Add(PolygonSoupChunk.Read(br));
            }

            br.Close();
            ms.Close();

            return result;
        }

        public void Write(BundleEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(Min);
            bw.Write(Unknown4);
            bw.Write(Max);
            bw.Write(Unknown8);
            bw.Write(ChunkPointerStart);
            bw.Write(BoxListStart);
            bw.Write(ChunkCount);
            bw.Write(FileSize);

            // No Data
            if (ChunkCount == 0)
            {
                bw.Flush();
                byte[] data2 = ms.ToArray();
                bw.Close();
                ms.Close();

                entry.Header = data2;
                entry.Dirty = true;
                return;
            }

            bw.BaseStream.Position = ChunkPointerStart;

            for (int i = 0; i < ChunkCount; i++)
            {
                bw.Write(ChunkPointers[i]);
            }

            //br.BaseStream.Position += (16 - br.BaseStream.Position % 16);
            //br.BaseStream.Position = BoxListStart;

            for (int i = 0; i < ChunkCount; i++)
            {
                // Write Vertically

                long pos = BoxListStart + 0x70 * (i / 4) + 4 * (i % 4);

                PolygonSoupBoundingBox box = BoundingBoxes[i];

                bw.BaseStream.Position = pos;
                bw.Write(box.Box.Min.X);
                bw.BaseStream.Position += 12;
                bw.Write(box.Box.Min.Y);
                bw.BaseStream.Position += 12;
                bw.Write(box.Box.Min.Z);

                bw.BaseStream.Position += 12;
                bw.Write(box.Box.Max.X);
                bw.BaseStream.Position += 12;
                bw.Write(box.Box.Max.Y);
                bw.BaseStream.Position += 12;
                bw.Write(box.Box.Max.Z);

                bw.BaseStream.Position += 12;
                bw.Write(box.Unknown);
            }

            for (int i = 0; i < ChunkPointers.Count; i++)
            {
                bw.BaseStream.Position = ChunkPointers[i];

                Chunks[i].Write(bw);
            }

            bw.Flush();
            byte[] data = ms.ToArray();
            bw.Close();
            ms.Close();

            entry.Header = data;
            entry.Dirty = true;
        }

        public Scene MakeScene(ILoader loader = null)
        {
            Scene scene = new Scene();

            int index = 0;
            foreach (PolygonSoupChunk chunk in Chunks)
            {
                string id = index.ToString();

                Vector3I pos = chunk.Position;
                float scale = chunk.Scale;
                /*List<Mesh> meshes = chunk.BuildMesh(pos, scale);
                for (int i = 0; i < meshes.Count; i++)
                {
                    Mesh mesh = meshes[i];
                    Model model = new Model(mesh);
                    SceneObject sceneObject = new SceneObject(id + "_" + i, model);
                    //sceneObject.ID = id;
                    //sceneObject.Transform = Matrix4.CreateScale(scale) *
                    //                        Matrix4.CreateTranslation(new Vector3(pos.X, pos.Y, pos.Z));
                    scene.AddObject(sceneObject);
                }*/
                Model model = new Model(chunk.BuildMesh(pos, scale));
                SceneObject sceneObject = new SceneObject(id, model);
                //sceneObject.ID = id;
                //sceneObject.Transform = Matrix4.CreateScale(scale) *
                //                        Matrix4.CreateTranslation(new Vector3(pos.X, pos.Y, pos.Z));
                scene.AddObject(sceneObject);
                index++;

                // TODO: TEMP
                //break;
            }

            return scene;
        }

        public void ImportObj(string path)
        {
            Stream s = File.Open(path, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(s);

            string currentMesh = "";
            PolygonSoupProperty currentProperty;

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line == null || line.Trim().StartsWith("#"))
                    continue;

                line = line.Trim();

                if (line.StartsWith("v"))
                {
                    string[] options = line.Split(' ');
                    if (options.Length < 4)
                        throw new ReadFailedError("Invalid Vertex Line: " + line);

                    if (!float.TryParse(options[1], NumberStyles.None, CultureInfo.CurrentCulture,
                        out var v1))
                    {
                        throw new ReadFailedError("Invalid Coord: " + options[1]);
                    }

                    if (!float.TryParse(options[1], NumberStyles.None, CultureInfo.CurrentCulture,
                        out var v2))
                    {
                        throw new ReadFailedError("Invalid Coord: " + options[2]);
                    }

                    if (!float.TryParse(options[1], NumberStyles.None, CultureInfo.CurrentCulture,
                        out var v3))
                    {
                        throw new ReadFailedError("Invalid Coord: " + options[3]);
                    }

                    // TODO: Use Vertices

                } else if (line.StartsWith("f"))
                {
                    string[] options = line.Split(' ');
                    if (options.Length < 4)
                        throw new ReadFailedError("Invalid Face Line: " + line);

                    if (!byte.TryParse(options[1], NumberStyles.None, CultureInfo.CurrentCulture,
                        out var f1))
                    {
                        throw new ReadFailedError("Invalid Index: " + options[1]);
                    }

                    if (!byte.TryParse(options[2], NumberStyles.None, CultureInfo.CurrentCulture,
                        out var f2))
                    {
                        throw new ReadFailedError("Invalid Index: " + options[2]);
                    }

                    if (!byte.TryParse(options[3], NumberStyles.None, CultureInfo.CurrentCulture,
                        out var f3))
                    {
                        throw new ReadFailedError("Invalid Index: " + options[3]);
                    }
                    
                    // TODO: Use Indices and make quads maybe?
                } else if (line.StartsWith("usemtl"))
                {
                    string[] options = line.Split(' ');
                    if (options.Length < 2)
                        throw new ReadFailedError("Invalid Material Line: " + line);
                    string material = options[1];
                    string[] matStrings = material.Split('_');
                    if (matStrings.Length < 7)
                        throw new ReadFailedError("Invalid Material: " + material);
                    string property1 = matStrings[1];
                    string property2 = matStrings[2];
                    string newByte1 = matStrings[3];
                    string newByte2 = matStrings[4];
                    string newByte3 = matStrings[5];
                    string newByte4 = matStrings[6];

                    if (!ushort.TryParse(property1, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture,
                        out var prop1))
                    {
                        throw new ReadFailedError("Invalid Property1: " + property1);
                    }

                    if (!ushort.TryParse(property2, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture,
                        out var prop2))
                    {
                        throw new ReadFailedError("Invalid Property2: " + property1);
                    }

                    if (!byte.TryParse(newByte1, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture,
                        out var nByte1))
                    {
                        throw new ReadFailedError("Invalid NByte1: " + property1);
                    }

                    if (!byte.TryParse(newByte2, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture,
                        out var nByte2))
                    {
                        throw new ReadFailedError("Invalid NByte2: " + property1);
                    }

                    if (!byte.TryParse(newByte3, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture,
                        out var nByte3))
                    {
                        throw new ReadFailedError("Invalid NByte3: " + property1);
                    }

                    if (!byte.TryParse(newByte4, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture,
                        out var nByte4))
                    {
                        throw new ReadFailedError("Invalid NByte4: " + property1);
                    }

                    currentProperty = new PolygonSoupProperty();
                    currentProperty.UnknownProperty = (uint) ((prop2 << 16) | prop1);
                    //currentProperty.Indices = ???; // TODO
                    currentProperty.IndicesIndices[0] = nByte1;
                    currentProperty.IndicesIndices[1] = nByte2;
                    currentProperty.IndicesIndices[2] = nByte3;
                    currentProperty.IndicesIndices[3] = nByte4;

                } else if (line.StartsWith("g"))
                {
                    string[] options = line.Split(' ');
                    if (options.Length < 2)
                        throw new ReadFailedError("Invalid Group: <none>");
                    currentMesh = options[1];
                }
            }

            sr.Close();
            s.Close();

            // TODO: Replace PolygonSoupList contents with parsed obj data
        }
    }
}