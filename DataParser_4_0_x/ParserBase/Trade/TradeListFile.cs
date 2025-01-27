﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.ComponentModel;

namespace Jamie.Trade {
	[Serializable]
	[XmlType(AnonymousType = true)]
	[XmlRoot(ElementName = "npc_trade_list", Namespace = "", IsNullable = false)]
	public partial class TradeListFile {
		[XmlElement("tradelist_template", Form = XmlSchemaForm.Unqualified)]
		public List<TradelistTemplate> TradeLists;
	}

	[Serializable]
	public partial class TradelistTemplate {
		[XmlElement("tradelist", Form = XmlSchemaForm.Unqualified)]
		public List<Tradelist> tradelist;

		[XmlAttribute]
		public int npc_id;

		[XmlIgnore]
		public string name;

		[XmlIgnore]
		public int count;

		[XmlAttribute]
		[DefaultValue(0)]
		public decimal sell_price_rate;

		[XmlIgnore]
		[DefaultValue(0)]
		public decimal buy_price_rate;

		[XmlIgnore]
		[DefaultValue(0)]
		public decimal rate;

		// [XmlAttribute]
		[XmlIgnore]
		public string npc_funcs;

		[XmlAttribute]
		[DefaultValue(TradeListType.KINAH)]
		public TradeListType npc_type;
	}

	[Serializable]
	[XmlType(AnonymousType = true)]
	public partial class Tradelist {
		[XmlAttribute]
		public int id;
	}

	[Serializable]
	public enum TradeListType {
		KINAH = 0,
		ABYSS,
		REWARD,
		NORMAL,
	}
}
