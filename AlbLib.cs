﻿using System;
using System.IO;
using System.Collections.Generic;

namespace AlbLib
{
	/// <summary>
	/// Contains all paths used in this library.
	/// </summary>
	public static class Paths
	{
		/// <summary>
		/// Absolute path to any XLDLIBS/PALETTE{0}.XLD.
		/// </summary>
		public static PathInfo PaletteN;
		
		/// <summary>
		/// Absolute path to XLDLIBS/PALETTE.000.
		/// </summary>
		public static PathInfo GlobalPalette;
		
		/// <summary>
		/// Absolute path to any XLDLIBS/SCRIPT{0}.XLD.
		/// </summary>
		public static PathInfo ScriptsN;
		
		/// <summary>
		/// Absolute path to XLDLIBS/ITEMNAME.DAT.
		/// </summary>
		public static PathInfo ItemName;
		
		/// <summary>
		/// Absolute path to XLDLIBS/ITEMLIST.DAT.
		/// </summary>
		public static PathInfo ItemList;
		
		/// <summary>
		/// Sets all paths.
		/// </summary>
		/// <param name="path">Absolute path to XLDLIBS folder.</param>
		public static void SetXLDLIBS(string path)
		{
			PaletteN = Path.Combine(path, "PALETTE{0}.XLD");
			GlobalPalette = Path.Combine(path, "PALETTE.000");
			ScriptsN = Path.Combine(path, "SCRIPT{0}.XLD");
			ItemName =  Path.Combine(path, "ITEMNAME.DAT");
			ItemList =  Path.Combine(path, "ITEMLIST.DAT");
		}
	}
	
	/// <summary>
	/// Informations about path to Albion resource.
	/// </summary>
	public class PathInfo
	{
		/// <summary>
		/// Absolute file path.
		/// </summary>
		public string FileName{
			get;set;
		}
		
		/// <summary>
		/// Directory containing resource.
		/// </summary>
		public string Directory{
			get;set;
		}
		
		/// <summary>
		/// File name formatted with *.
		/// </summary>
		public string SearchPattern{
			get;set;
		}
		
		/// <summary></summary>
		public PathInfo(string path)
		{
			FileName = Path.GetFullPath(path);
			Directory = Path.GetDirectoryName(FileName);
			SearchPattern = String.Format(Path.GetFileName(path), '*');
		}
		
		/// <summary>
		/// Enumerates through all file sequences.
		/// </summary>
		public IEnumerable<string> EnumerateList()
		{
			int i = 0;
			string path;
			while(File.Exists(path = String.Format(Paths.PaletteN.FileName, i++)))
			{
				yield return path;
			}
		}
		
		/// <summary></summary>
		public static implicit operator PathInfo(string path)
		{
			return new PathInfo(path);
		}
		
		/// <summary></summary>
		public static implicit operator string(PathInfo info)
		{
			return info.ToString();
		}
		
		/// <summary></summary>
		public override string ToString()
		{
			return FileName;
		}
	}
}