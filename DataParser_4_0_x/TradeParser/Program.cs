﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Diagnostics;
using Jamie.ParserBase;
using Jamie.Quests;

namespace Jamie.Trade {
	class Program {
		static string root = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

		static void Main(string[] args) {

			Utility.WriteExeDetails();
			Console.WriteLine("Loading strings...");
			Utility.LoadStrings(root);

			Console.WriteLine("Loading items...");
			Utility.LoadItems(root);

			Console.WriteLine("Loading NPCs...");
			Utility.LoadClientNpcs(root);

			Console.WriteLine("Loading client goods lists...");
			Utility.LoadNpcGoodLists(root);

			Console.WriteLine("Loading client trades lists...");
			Utility.LoadNpcTradeLists(root);

			Console.WriteLine("Loading quests...");
			Utility.LoadQuestFile(root);

			Console.WriteLine("Loading NPC dialogs...");
			Utility.LoadHtmlDialogs(root);

			Console.WriteLine("Loading original tradelist...");
			Utility.LoadTradeListTemplates(root);

			var merchants = Utility.ClientNpcIndex.NpcList.Where(n => n.cursor_type == CursorType.trade ||
														   n.disk_type == DiskType.merchant ||
																		n.npc_function_type == NpcFunction.Merchant ||
														   n.TradeInfo != null ||
														   n.AbyssTradeInfo != null ||
														   n.CouponTradeInfo != null ||
														   n.ExtraCurrencyTradeInfo != null);
			foreach (var merchant in merchants) {
				if (merchant.use_script) {
					Debug.Print("Scripted: {0}", merchant.id);
				}
				var template = new TradelistTemplate();
				template.npc_id = merchant.id;
				template.name = Utility.StringIndex.GetString(merchant.name).ToLower();
				if (merchant.AbyssTradeInfo != null) {
					template.npc_type = TradeListType.ABYSS;
					template.tradelist = new List<Tradelist>();
					TabList tabList = merchant.AbyssTradeInfo.TabList[0];
					foreach (var tabData in tabList.Data) {
						Tradelist list = new Tradelist();
						list.id = Utility.GoodListIndex.GoodLists.Where(g => g.name == tabData.atab).First().id;
						template.tradelist.Add(list);
					}
				}
				if (merchant.TradeInfo != null) {
					template.npc_type = TradeListType.KINAH;
					template.tradelist = new List<Tradelist>();
					if (merchant.TradeInfo.buy_price_rate == 0)
						template.buy_price_rate = 20;
					else
						template.buy_price_rate = (decimal)merchant.TradeInfo.buy_price_rate / 10;
					if (merchant.TradeInfo.sell_price_rate == 0)
						template.sell_price_rate = 0; // Gameserver defaults it to 100, use default value to omit from server
					else
						template.sell_price_rate = (decimal)merchant.TradeInfo.sell_price_rate / 10;
					template.rate = template.sell_price_rate * template.buy_price_rate;

					TabList tabList = merchant.TradeInfo.TabList[0];
					foreach (var tabData in tabList.Data) {
						Tradelist list = new Tradelist();
						list.id = Utility.GoodListIndex.GoodLists.Where(g => g.name == tabData.tab).First().id;
						template.tradelist.Add(list);
					}
				}
				if (merchant.CouponTradeInfo != null) {
					//template.npc_type = TradeListType.COUPON;
					template.npc_type = TradeListType.REWARD;
					template.tradelist = new List<Tradelist>();
					TabList tabList = merchant.CouponTradeInfo.TabList[0];
					foreach (var tabData in tabList.Data) {
						Tradelist list = new Tradelist();
						list.id = Utility.GoodListIndex.GoodLists.Where(g => g.name == tabData.ctab).First().id;
						template.tradelist.Add(list);
					}
				}
				if (merchant.ExtraCurrencyTradeInfo != null) {
					template.npc_type = TradeListType.REWARD;
					template.tradelist = new List<Tradelist>();
					TabList tabList = merchant.ExtraCurrencyTradeInfo.TabList[0];
					foreach (var tabData in tabList.Data) {
						Tradelist list = new Tradelist();
						var matched = Utility.GoodListIndex.GoodLists.Where(g => g.name == tabData.etab).FirstOrDefault();
						if (matched == null) {
							Debug.Print("Missing tab data for NPC: {0}", merchant.id);
							break;
						}
						else {
							list.id = matched.id;
							template.tradelist.Add(list);
						}
					}
				}
				if (merchant.TradeInTradeInfo != null) {
					template.npc_type = TradeListType.NORMAL;
					template.tradelist = new List<Tradelist>();
					TabList tabList = merchant.TradeInTradeInfo.TabList[0];
					foreach (var tabData in tabList.Data) {
						Tradelist list = new Tradelist();
						var matched = Utility.TradeInGoodsListIndex.GoodLists.Where(g => g.name == tabData.ttab).FirstOrDefault();
						if (matched == null) {
							Debug.Print("Missing tab data for NPC: {0}", merchant.id);
							break;
						}
						else {
							list.id = matched.id;
							template.tradelist.Add(list);
						}
					}
				}
				var dialogs = Utility.DialogFiles.Where(p => p.Key.StartsWith(merchant.name.ToLower() + '|'));
				if (dialogs.Any()) {
					foreach (var dialog in dialogs) {
						var pg = dialog.Value.HtmlPages.Where(p => p.npcfuncs != null).FirstOrDefault();
						if (pg != null) {
							var sellbuy = pg.npcfuncs.Where(f => f.GetType().Equals(typeof(trade_buy)) ||
														  f.GetType().Equals(typeof(trade_sell)) ||
														  f.GetType().Equals(typeof(exchange_coin)) ||
														  f.GetType().Equals(typeof(Pet_adopt)));
							if (sellbuy.Any()) {
								template.npc_funcs = null;
								foreach (var func in pg.npcfuncs)
									template.npc_funcs += func.Function + ";";
								template.npc_funcs = template.npc_funcs.TrimEnd(';');
								break;
							}
							if (pg.npcfuncs != null && String.IsNullOrEmpty(template.npc_funcs)) {
								// not found, add what exists
								foreach (var func in pg.npcfuncs)
									template.npc_funcs += func.Function + ";";
								template.npc_funcs = template.npc_funcs.TrimEnd(';');
							}
						}
					}
				}
				if (template.tradelist != null && template.tradelist.Count > 0) {
					template.count = template.tradelist.Count;
					var oldEntry = Utility.OriginalTradeList.TradeLists
									  .Where(t => t.npc_id == template.npc_id)
									  .FirstOrDefault();
					if (oldEntry != null) {
						Utility.OriginalTradeList.TradeLists.Remove(oldEntry);
					}
					Utility.OriginalTradeList.TradeLists.Add(template);
				}
			}

			Utility.OriginalTradeList.TradeLists =
			    Utility.OriginalTradeList.TradeLists.OrderBy(t => t.npc_id).ToList();

			string outputPath = Path.Combine(root, @".\output");
			if (!Directory.Exists(outputPath))
				Directory.CreateDirectory(outputPath);

			//var outputFile = new TradeListFile();

			var settings = new XmlWriterSettings() {
				CheckCharacters = false,
				CloseOutput = false,
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\n",
				Encoding = new UTF8Encoding(false)
			};

			try {
				using (var fs = new FileStream(Path.Combine(outputPath, "npc_trade_list.xml"),
										 FileMode.Create, FileAccess.Write))
				using (var writer = XmlWriter.Create(fs, settings)) {
					XmlSerializer ser = new XmlSerializer(typeof(TradeListFile));
					ser.Serialize(writer, Utility.OriginalTradeList);
				}
			}
			catch (Exception ex) {
				Debug.Print(ex.ToString());
			}

			var outputGoodlist = new GoodsLists();
			outputGoodlist.list = new List<GoodsList>();
			foreach (var gl in Utility.GoodListIndex.GoodLists) {
				var goodlist = new GoodsList();
				goodlist.id = gl.id;
				goodlist.Items = new List<GoodsListItem>();
				foreach (var it in gl.data) {
					var gItem = new GoodsListItem();

					// Data element with no item data in 3.0?
					if (it.item == null) {
						Debug.Print("Null Value on goodlist item!");
						continue;
					}


					gItem.id = Utility.ItemIndex.GetItem(it.item).id;
					if (it.sell_limitSpecified)
						gItem.sell_limit = it.sell_limit;
					if (it.buy_limitSpecified)
						// Need to Fix Sales time based on client data
						goodlist.salestime = "0 0 0/1 * * ?";
					gItem.buy_limit = it.buy_limit;
					goodlist.Items.Add(gItem);
				}
				outputGoodlist.list.Add(goodlist);
			}

			try {
				using (var fs = new FileStream(Path.Combine(outputPath, "goodslists.xml"),
										 FileMode.Create, FileAccess.Write))
				using (var writer = XmlWriter.Create(fs, settings)) {
					XmlSerializer ser = new XmlSerializer(typeof(GoodsLists));
					ser.Serialize(writer, outputGoodlist);
				}
			}
			catch (Exception ex) {
				Debug.Print(ex.ToString());
			}

			var outputTradeInlist = new TradeInGoodsLists();
			outputTradeInlist.list = new List<TradeInGoodsList>();
			foreach (var gl in Utility.TradeInGoodsListIndex.GoodLists) {
				var tradeingoodlist = new TradeInGoodsList();
				tradeingoodlist.id = gl.id;
				tradeingoodlist.Items = new List<TradeInGoodsListItem>();
				foreach (var it in gl.data) {
					var gItem = new TradeInGoodsListItem();
					gItem.id = Utility.ItemIndex.GetItem(it.item).id;
					tradeingoodlist.Items.Add(gItem);
				}
				outputTradeInlist.list.Add(tradeingoodlist);
			}

			try {
				using (var fs = new FileStream(Path.Combine(outputPath, "tradeingoodslists.xml"),
										 FileMode.Create, FileAccess.Write))
				using (var writer = XmlWriter.Create(fs, settings)) {
					XmlSerializer ser = new XmlSerializer(typeof(TradeInGoodsLists));
					ser.Serialize(writer, outputTradeInlist);
				}
			}
			catch (Exception ex) {
				Debug.Print(ex.ToString());
			}
		}
	}
}
