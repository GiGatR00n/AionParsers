﻿namespace Jamie.Trade
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Xml.Schema;
	using System.Xml.Serialization;

	[Serializable]
	public partial class GoodsList
	{
		[XmlElement("salestime", Form = XmlSchemaForm.Unqualified)]
		[DefaultValue(null)]
		public string salestime;

		[XmlElement("item", Form = XmlSchemaForm.Unqualified)]
		public List<GoodsListItem> Items;

		[XmlAttribute]
		public int id;
	}

	[Serializable]
	[XmlType(AnonymousType = true)]
	public partial class GoodsListItem
	{
		[XmlAttribute]
		public int id;

		[XmlAttribute]
		[DefaultValue(0)]
		public int buy_limit;

		[XmlAttribute]
		[DefaultValue(0)]
		public int sell_limit;
	}

	[Serializable]
	[XmlType(AnonymousType = true)]
	[XmlRoot(ElementName = "goodslists", Namespace = "", IsNullable = false)]
	public partial class GoodsLists
	{
		[XmlElement("list", Form = XmlSchemaForm.Unqualified)]
		public List<GoodsList> list;
	}
}
