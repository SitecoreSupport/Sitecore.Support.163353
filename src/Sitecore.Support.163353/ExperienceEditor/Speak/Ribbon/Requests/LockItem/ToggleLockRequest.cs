using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using System;
using Sitecore.Data.Items;
using System.Collections.Generic;
using System.Xml;
using Sitecore.Xml;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.LockItem
{
  public class ToggleLockRequest : PipelineProcessorRequest<ItemContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      base.RequestContext.ValidateContextItem();
      Sitecore.Data.Items.Item item = this.SwitchLock(base.RequestContext.Item);
      this.HandleVersionCreating(item);
      return new PipelineProcessorResponseValue
      {
        Value = new
        {
          Locked = item.Locking.IsLocked(),
          Version = item.Version.Number
        }
      };
    }

    protected Sitecore.Data.Items.Item SwitchLock(Sitecore.Data.Items.Item item)
    {
      if (item.Locking.IsLocked())
      {
        item.Locking.Unlock();
        #region fix
        //if it is an EXM message, looping through related datasource items and unlocking them
        if (item.Paths.Path.Contains("Email Campaign/Messages"))
        {
          this.UnlockDatasourceItems(item);
        }
        #endregion
        return item;
      }
      if (Sitecore.Context.User.IsAdministrator)
      {
        item.Locking.Lock();
        return item;
      }
      return Sitecore.Context.Workflow.StartEditing(item);
    }

    private void HandleVersionCreating(Sitecore.Data.Items.Item finalItem)
    {
      if (base.RequestContext.Item.Version.Number != finalItem.Version.Number)
      {
        Web.WebUtil.SetCookieValue(base.RequestContext.Site.GetCookieKey("sc_date"), string.Empty, DateTime.MinValue);
      }
    }

    #region patch_helper_methods
    protected void UnlockDatasourceItems(Item contentItem)
    {
      foreach (Item datasourceItem in GetDataSourceItems(contentItem))
      {
        datasourceItem.Locking.Unlock();
      }
    }
    public List<Item> GetDataSourceItems(Item item)
    {
      string str;
      List<string> dsItemsPath = new List<string>();
      List<Item> itemList = new List<Item>();
      str = this.GetLayoutField(item);
      XmlDocument layoutXml = this.LoadData(str);
      dsItemsPath = this.FindDataSource(layoutXml);
      foreach (string itemPath in dsItemsPath)
      {
        if (Context.Database.GetItem(itemPath) != null)
        {
          itemList.Add(Context.Database.GetItem(itemPath));
        }
      }
      return itemList;
    }

    protected string GetLayoutField(Item item)
    {
      if (item != null)
      {
        return item[FieldIDs.LayoutField];
      }
      return String.Empty;
    }

    private XmlDocument LoadData(string str)
    {
      if (!string.IsNullOrEmpty(str))
      {
        return XmlUtil.LoadXml(str);
      }
      return XmlUtil.LoadXml("<r/>");
    }

    private List<string> FindDataSource(XmlDocument doc)
    {
      List<string> dsList = new List<string>();
      if (doc != null)
      {
        foreach (XmlNode child in doc.ChildNodes)
        {
          this.Recursively(child, dsList);
        }
        return dsList;
      }
      return null;
    }

    private void Recursively(XmlNode node, List<string> dsList)
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
        this.Recursively(child, dsList);
      }
    }
    #endregion
  }
}