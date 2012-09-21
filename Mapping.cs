﻿/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 9.9.2012
 * Time: 14:20
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using AlbLib.Imaging;
using AlbLib.XLD;

namespace AlbLib
{
	namespace Mapping
	{
		/// <summary>
		/// Class representing game map.
		/// </summary>
		public class Map
		{
			/// <summary>
			/// Id of map.
			/// </summary>
			public readonly int Id;
			
			/// <summary>
			/// Switch for wait/rest, light-environment, NPC converge range, possibly more.
			/// </summary>
			public byte Flags{get;set;}
			
			/// <summary>
			/// Amount of NPCs and monsters.
			/// </summary>
			public byte NumNPC{get;set;}
			
			/// <summary>
			/// Map type.
			/// </summary>
			public MapType Type{get;set;}
			
			/// <summary>
			/// Used sound. (?)
			/// </summary>
			public byte Sound{get;set;}
			
			/// <summary>
			/// Width in tiles.
			/// </summary>
			public byte Width{
				get{
					return (byte)Data.GetLength(0);
				}
			}
			
			/// <summary>
			/// Height in tiles.
			/// </summary>
			public byte Height{
				get{
					return (byte)Data.GetLength(1);
				}
			}
			
			/// <summary>
			/// One-based tileset ID.
			/// </summary>
			public byte Tileset{get;set;}
			
			/// <summary>
			/// Combat background graphics.
			/// </summary>
			public byte ComGFX{get;set;}
			
			/// <summary>
			/// One-based palette ID.
			/// </summary>
			public byte Palette{get;set;}
			
			/// <summary>
			/// Frequency of animations.
			/// </summary>
			public byte AnimRate{get;set;}
			
			private Tile[,] data;
			
			/// <summary>
			/// Array of map tiles.
			/// </summary>
			public Tile[,] Data{
				get{
					return data;
				}
				set{
					if(value.GetLength(0) > Byte.MaxValue || value.GetLength(1) > Byte.MaxValue)throw new ArgumentException(null,"value");
					data = value;
				}
			}
			
			/// <summary>
			/// Loads map from stream.
			/// </summary>
			/// <param name="stream">
			/// Source stream.
			/// </param>
			public Map(Stream stream) : this(-1, stream)
			{
				
			}
			
			/// <summary>
			/// Loads map from stream and assigns an id.
			/// </summary>
			/// <param name="id">
			/// Id of map.
			/// </param>
			/// <param name="stream">
			/// Source stream.
			/// </param>
			public Map(int id, Stream stream)
			{
				Id = id;
				
				BinaryReader reader = new BinaryReader(stream);
				Flags = reader.ReadByte();
				NumNPC = reader.ReadByte();
				Type = (MapType)reader.ReadByte();
				Sound = reader.ReadByte();
				byte width = reader.ReadByte();
				byte height = reader.ReadByte();
				Tileset = reader.ReadByte();
				ComGFX = reader.ReadByte();
				Palette = reader.ReadByte();
				AnimRate = reader.ReadByte();
				if(NumNPC == 0)
				{
					reader.ReadBytes(320);
				}else if(NumNPC == 0x40)
				{
					reader.ReadBytes(960);
				}else{
					reader.ReadBytes(NumNPC*10);
				}
				data = new Tile[width,height];
				for(byte y = 0; y < height; y++)
				for(byte x = 0; x < width; x++)
				{
					data[x,y] = new Tile(x,y,stream);
				}
			}
			
			/// <summary>
			/// Loads existing map using in-game ID. (Alt+F2)
			/// </summary>
			public static Map Load(int id)
			{
				int fid = id/100;
				int sid = id%100;
				using(FileStream stream = new FileStream(Paths.MapDataN.Format(fid), FileMode.Open))
				{
					XLDFile.ReadToIndex(stream, sid);
					return new Map(id, stream);
				}
			}
			
			public GraphicPlane Combine()
			{
				GraphicPlane plane = new GraphicPlane(this.Width*16, this.Height*16);
				plane.Palette = ImagePalette.GetFullPalette(this.Palette);
				foreach(Tile t in this.Data)
				{
					RawImage ul = IconGraphics.GetTileUnderlay(this.Tileset, t);
					RawImage ol = IconGraphics.GetTileOverlay(this.Tileset, t);
					Point loc = new Point(t.X*16, t.Y*16);
					plane.Objects.Add(new GraphicObject(ul, loc));
					plane.Objects.Add(new GraphicObject(ol, loc));
				}
				return plane;
			}
		}
		
		/// <summary>
		/// BLKLIST.
		/// </summary>
		public class Block2D
		{
			/// <summary>
			/// Width in tiles.
			/// </summary>
			public byte Width{
				get{
					return (byte)Data.GetLength(0);
				}
			}
			
			/// <summary>
			/// Height in tiles.
			/// </summary>
			public byte Height{
				get{
					return (byte)Data.GetLength(1);
				}
			}
			
			/// <param name="stream">
			/// Source stream.
			/// </param>
			public Block2D(Stream stream)
			{
				byte width = (byte)stream.ReadByte();
				byte height = (byte)stream.ReadByte();
				data = new Tile[width,height];
				for(byte y = 0; y < height; y++)
				for(byte x = 0; x < width; x++)
				{
					data[x,y] = new Tile(x, y, stream);
				}
			}
			
			private Tile[,] data;
			
			/// <summary>
			/// Block of tiles.
			/// </summary>
			public Tile[,] Data{
				get{
					return data;
				}
				set{
					if(value.GetLength(0) > Byte.MaxValue || value.GetLength(1) > Byte.MaxValue)throw new ArgumentException(null,"value");
					data = value;
				}
			}
		}
		
		/// <summary>
		/// Used for loading tile images.
		/// </summary>
		public static class IconGraphics
		{
			private static readonly RawImage[][] tilesets = new RawImage[short.MaxValue][];
			private static readonly RawImage[][] tilesetssorted = new RawImage[short.MaxValue][];
			
			/// <summary>
			/// Loads tileset as an array of RawImages.
			/// </summary>
			/// <param name="index">
			/// Zero-based tileset index.
			/// </param>
			/// <returns>
			/// Array representing the tileset.
			/// </returns>
			public static RawImage[] GetTileset(int index)
			{
				index -= 1;
				if(tilesetssorted[index] == null)
				{
					if(tilesets[index] == null)
					{
						int fx = index/100;
						int tx = index%100;
						using(FileStream stream = new FileStream(Paths.IconGraphicsN.Format(fx), FileMode.Open))
						{
							int len = XLDFile.ReadToIndex(stream, tx);
							tilesets[index] = new RawImage[len/256];
							for(int i = 0; i < len/256; i++)
							{
								tilesets[index][i] = new RawImage(stream, 16, 16);
							}
						}
					}
					TileData[] data = IconData.GetTileset(index);
					tilesetssorted[index] = new RawImage[data.Length];
					foreach(TileData td in data)
					{
						tilesetssorted[index][td.Id] = tilesets[index][td.GrID];
					}
				}
				return tilesetssorted[index];
			}
			
			/// <summary>
			/// Gets tile graphics using tile index.
			/// </summary>
			/// <param name="tileset">
			/// Zero-based tileset index.
			/// </param>
			/// <param name="index">
			/// Overlay or underlay.
			/// </param>
			public static RawImage GetTile(int tileset, int index)
			{
				if(index <= 1)return null;
				return GetTileset(tileset)[index-2];
			}
			
			/// <summary>
			/// Returns image representing underlay portion of tile.
			/// </summary>
			/// <param name="tileset">
			/// Zero-based tileset index.
			/// </param>
			/// <param name="tile">
			/// Tile.
			/// </param>
			/// <returns>
			/// Underlay image.
			/// </returns>
			public static RawImage GetTileUnderlay(int tileset, Tile tile)
			{
				if(tile.Underlay <= 1)return null;
				return GetTileset(tileset)[tile.Underlay-2];
			}
			
			/// <summary>
			/// Returns image representing overlay portion of tile.
			/// </summary>
			/// <param name="tileset">
			/// Zero-based tileset index.
			/// </param>
			/// <param name="tile">
			/// Tile.
			/// </param>
			/// <returns>
			/// Overlay image.
			/// </returns>
			public static RawImage GetTileOverlay(int tileset, Tile tile)
			{
				if(tile.Overlay <= 1)return null;
				return GetTileset(tileset)[tile.Overlay-2];
			}
		}
		
		/// <summary>
		/// This class represents static tile data.
		/// </summary>
		public static class IconData
		{
			private static readonly TileData[][] tilesets = new TileData[4096][];
			
			/// <summary>
			/// Returns data array for tileset.
			/// </summary>
			/// <param name="index">
			/// Zero-based tileset index
			/// </param>
			public static TileData[] GetTileset(int index)
			{
				if(tilesets[index] == null)
				{
					int fx = index/100;
					int tx = index%100;
					using(FileStream stream = new FileStream(Paths.IconDataN.Format(fx), FileMode.Open))
					{
						int len = XLDFile.ReadToIndex(stream, tx);
						tilesets[index] = new TileData[len/8];
						for(int i = 0; i < len/8; i++)
						{
							var data = new TileData(i, stream);
							tilesets[index][i] = data;
						}
					}
				}
				return tilesets[index];
			}
			
			/// <summary>
			/// Returns tile data from tileset.
			/// </summary>
			/// <param name="tileset">
			/// Zero-based tileset index
			/// </param>
			/// <param name="index">
			/// Tile index.
			/// </param>
			public static TileData GetTile(int tileset, int index)
			{
				return GetTileset(tileset)[index];
			}
		}
		
		/// <summary>
		/// Data about map tile.
		/// </summary>
		public struct TileData
		{
			/// <summary>
			/// Assigned id.
			/// </summary>
			public readonly int Id;
			
			/// <summary>
			/// TODO.
			/// </summary>
			public readonly byte Type;
			/// <summary>
			/// TODO.
			/// </summary>
			public readonly byte Collision;
			/// <summary>
			/// TODO.
			/// </summary>
			public readonly short Info;
			/// <summary>
			/// TODO.
			/// </summary>
			public readonly short GrID;
			/// <summary>
			/// TODO.
			/// </summary>
			public readonly byte FramesCount;
			readonly byte unknown1;
		
			/// <param name="id">
			/// Id to assign.
			/// </param>
			/// <param name="stream">
			/// Source stream.
			/// </param>
			public TileData(int id, Stream stream)
			{
				Id = id;
				
				BinaryReader reader = new BinaryReader(stream);
				Type = reader.ReadByte();
				Collision = reader.ReadByte();
				Info = reader.ReadInt16();
				GrID = reader.ReadInt16();
				FramesCount = reader.ReadByte();
				unknown1 = reader.ReadByte();
			}
		}
		
		/// <summary>
		/// Map tile.
		/// </summary>
		public struct Tile
		{
			/// <summary>
			/// Tile X position.
			/// </summary>
			public readonly byte X;
			
			/// <summary>
			/// Tile Y position.
			/// </summary>
			public readonly byte Y;
			
			/// <summary>
			/// Underlay tile id.
			/// </summary>
			public short Underlay{
				get;set;
			}
			
			/// <summary>
			/// Overlay tile id.
			/// </summary>
			public short Overlay{
				get;set;
			}
			
			/// <summary>
			/// Reads a map tile.
			/// </summary>
			public Tile(byte data1, byte data2, byte data3) : this()
			{
				Overlay = (short)((data1<<4)|((data2&0xF0)>>4));
				Underlay = (short)(data3|((data2&0x0F)<<8));
			}
			
			/// <summary>
			/// Reads a map tile.
			/// </summary>
			public Tile(Stream source) : this((byte)source.ReadByte(), (byte)source.ReadByte(), (byte)source.ReadByte())
			{
				
			}
			
			/// <summary>
			/// Reads a map tile.
			/// </summary>
			public Tile(byte x, byte y, byte data1, byte data2, byte data3) : this()
			{
				X = x;
				Y = y;
				Overlay = (short)((data1<<4)|((data2&0xF0)>>4));
				Underlay = (short)(data3|((data2&0x0F)<<8));
			}
			
			/// <summary>
			/// Reads a map tile.
			/// </summary>
			public Tile(byte x, byte y, Stream source) : this(x, y, (byte)source.ReadByte(), (byte)source.ReadByte(), (byte)source.ReadByte())
			{
				
			}
		}
		
		/// <summary>
		/// Type of map.
		/// </summary>
		public enum MapType : byte
		{
			/// <summary>
			/// Undefined type.
			/// </summary>
			Unknown = 0,
			/// <summary>
			/// 3D.
			/// </summary>
			Map3D = 1,
			/// <summary>
			/// 2D.
			/// </summary>
			Map2D = 2
		}
	}
}
