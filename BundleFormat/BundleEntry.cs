﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BundleUtilities;
using Microsoft.SqlServer.Server;

namespace BundleFormat
{
	public class EntryBlock
	{
		public bool Compressed;
		public uint CompressedSize;
		public uint UncompressedSize;
		public uint UncompressedAlignment; // default depending on file type
		public byte[] RawData;
		public byte[] Data
		{
			get
			{
				if (RawData == null)
					return null;

				if (Compressed)
					return RawData.Decompress((int)UncompressedSize);

				return RawData;
			}
			set
			{
				if (Compressed)
					RawData = value.Compress();
				else
					RawData = value;

				UncompressedSize = (uint)value.Length;
				CompressedSize = (uint)RawData.Length;
			}
		}
	}

    public class EntryInfo
    {
        public uint ID;
        public EntryType Type;
        public string Path;
		public DebugInfo DebugInfo;

        public EntryInfo(uint id, EntryType type, string path, DebugInfo debugInfo)
        {
            ID = id;
            Type = type;
            Path = path;
			DebugInfo = debugInfo;
        }
    }

	public struct Dependency
	{
		public ulong ID;
		public uint EntryPointerOffset;
	}

	public struct DebugInfo
	{
		public string Name;
		public string TypeName;
	}

	/*public struct BundleReference
	{
		public string Path;
		public uint EntryCount;
	}*/

    public class BundleEntry
    {
		public BundleArchive Archive;
		//public BundleReference Archive;

		public int Index;

        public ulong ID;
        public ulong References;
		public int DependenciesListOffset;
        public short DependencyCount;
		public List<Dependency> Dependencies;

		public DebugInfo DebugInfo;

		public EntryBlock[] EntryBlocks;

		public bool HasHeader => HasSection(0);
        public bool HasBody => HasSection(1);
		public bool HasThird => HasSection(2);

		public EntryType Type;

        public BundlePlatform Platform;
        public bool Console => Platform == BundlePlatform.X360 || Platform == BundlePlatform.PS3;

        public bool Dirty;

        public BundleEntry(BundleArchive archive)
        {
			Archive = archive;
			/*Archive = new BundleReference();
			Archive.Path = archive.Path;
			Archive.EntryCount = (uint)archive.Entries.Count;*/
			Dependencies = new List<Dependency>();
        }

		public bool HasSection(int section)
		{
			return EntryBlocks != null &&
				   section < EntryBlocks.Length &&
				   section >= 0 &&
				   EntryBlocks[section] != null &&
				   EntryBlocks[section].Data != null &&
				   EntryBlocks[section].Data.Length > 0;
		}

        public MemoryStream MakeStream(bool body = false)
        {
			if (EntryBlocks == null)
				return null;

            if (body)
                return new MemoryStream(EntryBlocks[1].Data);
            return new MemoryStream(EntryBlocks[0].Data);
        }

        public List<BundleDependency> GetDependencies()
        {
            List<BundleDependency> result = new List<BundleDependency>();

			if (Dependencies.Count > 0)
			{
				for (int i = 0; i < Dependencies.Count; i++)
				{
					BundleDependency dependency = new BundleDependency();

					dependency.EntryID = Dependencies[i].ID;
					dependency.EntryPointerOffset = (int)Dependencies[i].EntryPointerOffset;

					BundleEntry entry = null;

					/*string file = BundleCache.GetFileByEntryID(dependency.EntryID);
					if (!string.IsNullOrEmpty(file))
					{
						BundleArchive archive = BundleArchive.Read(file, dependency.EntryID);
						entry = archive.GetEntryByID(dependency.EntryID);
					}*/
					//}

					// TODO
					for (int j = 0; j < Archive.Entries.Count; j++)
					{
						if (Archive.Entries[j].ID != dependency.EntryID)
							continue;

						dependency.EntryIndex = j;
						entry = Archive.Entries[j];
					}

					dependency.Entry = entry;

					result.Add(dependency);
				}
				return result;
			}

            MemoryStream ms = MakeStream();
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = Console;

            br.BaseStream.Position = DependenciesListOffset;

            for (int i = 0; i < DependencyCount; i++)
            {
                BundleDependency bundleDependency = new BundleDependency();

                bundleDependency.EntryID = br.ReadUInt64();
                bundleDependency.EntryPointerOffset = br.ReadInt32();
                bundleDependency.Unknown2 = br.ReadInt32();

                BundleEntry entry = null;

				/*string file = BundleCache.GetFileByEntryID(bundleDependency.EntryID);
				if (!string.IsNullOrEmpty(file))
				{
					BundleArchive archive = BundleArchive.Read(file, bundleDependency.EntryID);
					entry = archive.GetEntryByID(bundleDependency.EntryID);
				}*/

				// TODO
				for (int j = 0; j < Archive.Entries.Count; j++)
                {
                    if (Archive.Entries[j].ID != bundleDependency.EntryID)
                        continue;

                    bundleDependency.EntryIndex = j;
                    entry = Archive.Entries[j];
                }

                bundleDependency.Entry = entry;

                result.Add(bundleDependency);
			}

            br.Close();
            ms.Close();

            return result;
        }

        public string DetectName()
        {
			if (!string.IsNullOrWhiteSpace(DebugInfo.Name))
				return DebugInfo.Name;

            string theName = "worldvault";
            ulong theID = Crc32.HashCrc32B(theName);
            if (theID == ID)
                return theName;
            theName = "postfxvault";
            theID = Crc32.HashCrc32B(theName);
            if (theID == ID)
                return theName;
            theName = "cameravault";
            theID = Crc32.HashCrc32B(theName);
            if (theID == ID)
                return theName;

            string path = Path.GetFileNameWithoutExtension(Archive.Path);
            string file = null;
            if (path != null)
                file = path.ToUpper();

            if (file != null && file.StartsWith("TRK_UNIT") && file.EndsWith("_GR"))
            {
                string trackID = file.Substring(8).Replace("_GR", "").ToLower();
                string name = "trk_unit" + trackID + "_list";
                ulong newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "prp_inst_" + trackID;
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "prp_gl__" + trackID;
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "trk_unit" + trackID + "_passby";
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "trk_unit" + trackID + "_emitter";
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
            }

            if (file != null)
            {
                string aptName = file.ToLower() + ".swf";
                ulong aptID = Crc32.HashCrc32B(aptName);
                if (aptID == ID)
                    return aptName;
            }

            if (file != null && file.StartsWith("WHE_") && file.EndsWith("_GR"))
            {
                string wheelID = file.Substring(4).Replace("_GR", "").ToLower();
                string name = wheelID + "_graphics";
                ulong newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
            }

            if (file != null && file.StartsWith("VEH_"))
            {
                if (file.EndsWith("_AT"))
                {
                    string vehicleID = file.Substring(4).Replace("_AT", "").ToLower();
                    string name = vehicleID + "_attribsys";
                    ulong newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "deformationmodel";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "_bpr";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "_anim";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "_trafficstub";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "_vanm";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                } else if (file.EndsWith("_CD"))
                {
                    string vehicleID = file.Substring(4).Replace("_CD", "").ToLower();
                    string name = vehicleID;
                    ulong newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                } else if (file.EndsWith("_GR"))
                {
                    string vehicleID = file.Substring(4).Replace("_GR", "").ToLower();
                    string name = vehicleID + "_graphics";
                    ulong newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                }
            }

            // WorldCol Names
            for (int i = 0; i < Archive.EntryCount; i++)
            {
                string name = "trk_col_" + i;
                ulong newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "trk_clil" + i;
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
            }

            return "";
        }

        public Color GetColor()
        {
            switch (Type)
            {
                case EntryType.Texture:
                    return Color.Orange;
                case EntryType.Material:
                    return Color.DeepPink;
                case EntryType.TextFile:
                    break;
                case EntryType.VertexDescriptor:
                    break;
                case EntryType.Renderable:
                    return Color.Aquamarine;
                case EntryType.MaterialTechnique:
                    break;
                case EntryType.TextureState:
                    break;
                case EntryType.MaterialState:
                    break;
                case EntryType.ShaderProgramBuffer:
                    break;
                case EntryType.ShaderParameter:
                    break;
                case EntryType.Debug:
                    break;
                case EntryType.KdTree:
                    break;
                case EntryType.Snr:
                    break;
                case EntryType.AttribSysSchema:
                    break;
                case EntryType.AttribSysVault:
                    break;
                case EntryType.AptData:
                    break;
                case EntryType.Popup:
                    break;
                case EntryType.Font:
                    break;
                case EntryType.LuaCode:
                    break;
                case EntryType.InstanceList:
                    return Color.BlueViolet;
                case EntryType.EntryList:
                    return Color.Tomato;
                case EntryType.Language:
                    break;
                case EntryType.SatNavTile:
                    break;
                case EntryType.SatNavTileDirectory:
                    break;
                case EntryType.Model:
                    return Color.Blue;
                case EntryType.ColourCube:
                    break;
                case EntryType.HudMessage:
                    break;
                case EntryType.HudMessageList:
                    break;
                case EntryType.HudMessageSequence:
                    break;
                case EntryType.HudMessageSequenceDictionary:
                    break;
                case EntryType.WorldPainter2D:
                    break;
                case EntryType.PFXHookBundle:
                    break;
                case EntryType.ShaderTechnique:
                    break;
                case EntryType.ICETakeDictionary:
                    break;
                case EntryType.VideoData:
                    break;
                case EntryType.PolygonSoupList:
                    return Color.Goldenrod;
                case EntryType.CommsToolListDefinition:
                    break;
                case EntryType.CommsToolList:
                    break;
                case EntryType.AnimationCollection:
                    break;
                case EntryType.Registry:
                    break;
                case EntryType.GenericRwacWaveContent:
                    break;
                case EntryType.GinsuWaveContent:
                    break;
                case EntryType.AemsBank:
                    break;
                case EntryType.Csis:
                    break;
                case EntryType.Nicotine:
                    break;
                case EntryType.Splicer:
                    break;
                case EntryType.GenericRwacReverbIRContent:
                    break;
                case EntryType.SnapshotData:
                    break;
                case EntryType.ZoneList:
                    break;
                case EntryType.LoopModel:
                    break;
                case EntryType.AISections:
                    break;
                case EntryType.TrafficData:
                    break;
                case EntryType.TriggerData:
                    break;
                case EntryType.VehicleList:
                    break;
                case EntryType.GraphicsSpec:
                    return Color.SeaGreen;
                case EntryType.ParticleDescriptionCollection:
                    break;
                case EntryType.WheelList:
                    break;
                case EntryType.WheelGraphicsSpec:
                    break;
                case EntryType.TextureNameMap:
                    break;
                case EntryType.ProgressionData:
                    break;
                case EntryType.PropPhysics:
                    break;
                case EntryType.PropGraphicsList:
                    break;
                case EntryType.PropInstanceData:
                    break;
                case EntryType.BrnEnvironmentKeyframe:
                    break;
                case EntryType.BrnEnvironmentTimeLine:
                    break;
                case EntryType.BrnEnvironmentDictionary:
                    break;
                case EntryType.GraphicsStub:
                    break;
                case EntryType.StaticSoundMap:
                    break;
                case EntryType.StreetData:
                    break;
                case EntryType.VFXMeshCollection:
                    break;
                case EntryType.MassiveLookupTable:
                    break;
                case EntryType.VFXPropCollection:
                    break;
                case EntryType.StreamedDeformation:
                    break;
                case EntryType.ParticleDescription:
                    break;
                case EntryType.PlayerCarColours:
                    break;
                case EntryType.ChallengeList:
                    break;
                case EntryType.FlaptFile:
                    break;
                case EntryType.ProfileUpgrade:
                    break;
                case EntryType.VehicleAnimation:
                    break;
                case EntryType.BodypartRemapData:
                    break;
                case EntryType.LUAList:
                    break;
                case EntryType.LUAScript:
                    break;
            }
            return Color.Transparent;
        }
    }
}
