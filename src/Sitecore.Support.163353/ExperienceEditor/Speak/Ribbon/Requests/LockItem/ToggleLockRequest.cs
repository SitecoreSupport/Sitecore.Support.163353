using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using System;
using Sitecore.Data.Items;
using System.Collections.Generic;
using System.Xml;
using Sitecore.Xml;
using System.Linq.Expressions;
using System.Linq;
using Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.LockItem;
using Sitecore.Support.Helpers;

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
          RelatedItemHelper.UnlockRelatedItems(item);
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
    
    #endregion
  }
}