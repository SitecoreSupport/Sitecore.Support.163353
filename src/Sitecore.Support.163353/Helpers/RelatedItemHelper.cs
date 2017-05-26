using Sitecore.Data.Items;
using Sitecore.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace Sitecore.Support.Helpers
{
  public static class RelatedItemHelper
  {
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
    this IEnumerable<TSource> source,
    Func<TSource, TKey> keySelector)
    {
      var knownKeys = new HashSet<TKey>();
      return source.Where(element => knownKeys.Add(keySelector(element)));
    }
    public static void AppendChildItems(List<Item> itemList, Item item)
    {
      foreach (Item childItem in item.Children)
      {
        itemList.Add(childItem);
        AppendChildItems(itemList, childItem);
      }
    }
    public static void UnlockRelatedItems(Item contentItem)
    {
      List<Item> listOfItemsToUnlock = GetDataSourceItems(contentItem);
      AppendChildItems(listOfItemsToUnlock, contentItem);
      var itemsToUnlock  = listOfItemsToUnlock.DistinctBy(x => x.Name);

      foreach (Item datasourceItem in itemsToUnlock)
      {
        datasourceItem.Locking.Unlock();
      }
    }

    public static List<Item> GetDataSourceItems(Item item)
    {
      string str;
      List<string> dsItemsPath = new List<string>();
      List<Item> itemList = new List<Item>();
      str = GetLayoutField(item);
      XmlDocument layoutXml = LoadData(str);
      dsItemsPath = FindDataSource(layoutXml);
      foreach (string itemPath in dsItemsPath)
      {
        if (Context.Database.GetItem(itemPath) != null)
        {
          itemList.Add(Context.Database.GetItem(itemPath));
        }
      }
      return itemList;
    }

    public static string GetLayoutField(Item item)
    {
      if (item != null)
      {
        return item[FieldIDs.LayoutField];
      }
      return String.Empty;
    }

    private static XmlDocument LoadData(string str)
    {
      if (!string.IsNullOrEmpty(str))
      {
        return XmlUtil.LoadXml(str);
      }
      return XmlUtil.LoadXml("<r/>");
    }

    private static List<string> FindDataSource(XmlDocument doc)
    {
      List<string> dsList = new List<string>();
      if (doc != null)
      {
        foreach (XmlNode child in doc.ChildNodes)
        {
          Recursively(child, dsList);
        }
        return dsList;
      }
      return null;
    }

    private static void Recursively(XmlNode node, List<string> dsList)
    {
      string tmp;
      tmp = XmlUtil.GetAttribute("ds", node);
      if (String.IsNullOrEmpty(tmp))
      {
        tmp = XmlUtil.GetAttribute("s:ds", node);
      }
      if (!String.IsNullOrEmpty(tmp))
      {
        dsList.Add(tmp);
      }
      foreach (XmlNode child in node.ChildNodes)
      {
        Recursively(child, dsList);
      }
    }
  }
}