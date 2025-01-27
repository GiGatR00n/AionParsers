﻿namespace Jamie.Npcs {
	using System.Xml.Serialization;
	using System;
	using System.ComponentModel;
	using System.Xml.Schema;
	using System.Xml;
	using System.IO;
	using System.Xml.XPath;
	using System.Collections.Generic;
	using Jamie.ParserBase;

	[Serializable]
	[XmlRoot(ElementName = "spawn", Namespace = "", IsNullable = false)]
	public partial class SpawnGroup {
		[XmlElement("spot", Form = XmlSchemaForm.Unqualified)]
		public SpawnGroupObject[] @object;

		[XmlAttribute]
		[DefaultValue(SpawnTime.ALL_TIME)]
		public SpawnTime time;

		[XmlAttribute]
		public string anchor;

		[XmlAttribute]
		[DefaultValue(SpawnHandlerType.NONE)]
		public SpawnHandlerType handler;

		/* Moved to SpawnMap
			   [XmlAttribute]
			   public int map;
		*/
		[XmlAttribute]
		public int npc_id;

		[XmlAttribute]
		public int respawn_time;

		[XmlAttribute]
		[DefaultValue(0)]
		public int pool;

		[XmlAttribute]
		[DefaultValue(0)]
		public int rw;

		[XmlAttribute]
		[DefaultValue(0)]
		public int npcid_dr;

		[XmlAttribute]
		[DefaultValue(0)]
		public int npcid_da;

		[XmlAttribute]
		[DefaultValue(0)]
		public int npcid_li;

		[XmlAttribute]
		[DefaultValue(false)]
		public bool boss;

		[XmlIgnore]
		public string name;

		[XmlIgnore]
		public decimal upper;

		[XmlIgnore]
		public SourceType type;

		[XmlIgnore]
		public int level;

		public SpawnGroup() {
			this.pool = 0;
			this.npcid_dr = 0;
			this.npcid_da = 0;
			this.npcid_li = 0;
			this.boss = false;
		}
	}

	[Serializable]
	[XmlType(AnonymousType = true)]
	public partial class SpawnGroupObject {

		[XmlAttribute]
		public decimal x;

		[XmlAttribute]
		public decimal y;

		[XmlAttribute]
		public decimal z;

		[XmlAttribute]
		public sbyte h;

		[XmlAttribute]
		[DefaultValue(0)]
		public int w;

		[XmlAttribute]
		[DefaultValue(0)]
		public int rw;

		[XmlAttribute]
		[DefaultValue(0)]
		public int static_id;

		[XmlAttribute]
		[DefaultValue(0)]
		public int fly;
	}

	[Serializable]
	public enum SpawnTime {
		ALL_TIME = 0,
		DAY,
		NIGHT,
	}

	[Serializable]
	public enum SpawnHandlerType {
		NONE = 0,
		RIFT,
		STATIC,
	}

	[Serializable]
	[XmlRoot(ElementName = "spawn_map", Namespace = "", IsNullable = false)]
	public partial class SpawnMap {
		[XmlAttribute(Form = XmlSchemaForm.Unqualified)]
		public int map_id;

		[XmlElement("spawn", Form = XmlSchemaForm.Unqualified)]
		public SpawnGroup[] Spawns;
	}

	[Serializable]
	[XmlRoot(ElementName = "spawns", Namespace = "", IsNullable = false)]
	public partial class SpawnsFile : IXmlSerializable {
		[XmlElement("spawn_map", Form = XmlSchemaForm.Unqualified)]
		public SpawnMap spawn_map;

		#region IXmlSerializable Members

		public XmlSchema GetSchema() {
			return null;
		}

		public void ReadXml(XmlReader reader) {
			var settings = new XmlReaderSettings() {
				ConformanceLevel = ConformanceLevel.Auto,
				ValidationFlags = XmlSchemaValidationFlags.None,
				ValidationType = ValidationType.None,
				CloseInput = false,
				ProhibitDtd = true
			};

			reader.MoveToContent();
			reader.Read();
			reader.Skip(); //Skip the first book.

			List<SpawnGroup> spawns = new List<SpawnGroup>();
			while (reader.Read()) {
				switch (reader.NodeType) {
					case XmlNodeType.Comment:
						break;
					case XmlNodeType.Element:
						XmlSerializer ser = new XmlSerializer(typeof(SpawnGroup));
						XmlReader fragReader = XmlReader.Create(reader, settings);
						var spawnGroup = (SpawnGroup)ser.Deserialize(fragReader);
						
						spawns.Add(spawnGroup);
						break;
				}
			}
			this.spawn_map = new SpawnMap();
			this.spawn_map.Spawns = new SpawnGroup[spawns.Count];
			this.spawn_map.Spawns = spawns.ToArray();
		}

		public void WriteXml(XmlWriter writer) {
			// Lets give some meaningful names to npcs
			Utility.LoadStrings(AppDomain.CurrentDomain.SetupInformation.ApplicationBase);

			writer.WriteStartElement("spawn_map");
			writer.WriteStartAttribute("map_id");
			writer.WriteRaw(this.spawn_map.map_id.ToString());
			writer.WriteEndAttribute();

			for (int i = 0; i < spawn_map.Spawns.Length; i++) {
				SpawnGroup sp = spawn_map.Spawns[i];

				if (i > 0)
					writer.WriteWhitespace("\n\t\t");
				if (sp.level != 0)
					writer.WriteComment(" " + Utility.StringIndex.GetString(sp.name) + " " + sp.level + "p ");
				else
					writer.WriteComment(" " + Utility.StringIndex.GetString(sp.name) + " ");
				writer.WriteWhitespace("\n");

				using (var ms = new MemoryStream()) {
					var ser = new XmlSerializer(typeof(SpawnGroup));

					var settings = writer.Settings.Clone();
					var xFragWriter = XmlWriter.Create(ms, settings);
					ser.Serialize(ms, sp);
					ms.Position = 0;

					var fragment = new XPathDocument(ms);
					var navigator = fragment.CreateNavigator();
					navigator.MoveToFirst();
					string xml = navigator.InnerXml
					    .Replace(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"",
					    	   String.Empty);

					string[] lines = xml.Split('\n');

					// stupid writer indents with spaces anyway; fix it
					for (int line = 0; line < lines.Length; line++) {
						lines[line] = lines[line].Replace("  ", "\t\t").Insert(0, "\t\t");
					}
					writer.WriteRaw(String.Join("\n", lines));
				}
			}
			writer.WriteWhitespace("\n");
			writer.WriteRaw("\t");
			writer.WriteEndElement();
		}

		#endregion
	}
}
