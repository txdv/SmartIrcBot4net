using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Manos.IO;
using Meebey.SmartIrc4net;

using SmartIrcBot4net.Extensions;

namespace SmartIrcBot4net
{
  public abstract class IrcBotPlugin
  {
    public IrcBot Bot { get; internal set; }
    public string DefaultPrefix { get; set; }
  }

  class ParameterHandler
  {
    public ParameterHandler(object[] parameters)
    {
    }
  }

  public class IrcBot : IrcClient
  {
    public string DefaultPrefix { get; set; }

    List<CommandTrigger> commands = new List<CommandTrigger>();
    List<JoinTrigger>    joins    = new List<JoinTrigger>();
    List<PreCommandTrigger> pre   = new List<PreCommandTrigger>();

    public IrcBot(Context context)
      : base(context)
    {
      DefaultPrefix = "!";

      OnChannelMessage += HandleOnChannelMessage;
      OnQueryMessage   += HandleOnQueryMessage;
      OnJoin += HandleOnJoin;
    }

    void HandleOnJoin(object sender, JoinEventArgs e)
    {
      foreach (var join in joins) {
        if (join.Handle(e)) {
          return;
        }
      }
    }

    void HandleOnMessage(MessageType type, IrcEventArgs args)
    {
      foreach (var precommand in pre) {
        if (!precommand.Handle(type, args)) {
          return;
        }
      }

      foreach (var command in commands) {
        if (command.Handle(type, args)) {
          return;
        }
      }
    }

    void HandleOnQueryMessage(object sender, IrcEventArgs e)
    {
      HandleOnMessage(MessageType.Query, e);
    }

    void HandleOnChannelMessage(object sender, IrcEventArgs e)
    {
      HandleOnMessage(MessageType.Channel, e);
    }

    public bool Plugin(IrcBotPlugin plugin)
    {
      if (!(plugin is IrcBotPlugin)) {
        return false;
      }

      plugin.Bot = this;

      var type = plugin.GetType();

      foreach (var member in type.GetMembers()) {
        foreach (object attribute in member.GetCustomAttributes(true)) {
          if (attribute is OnCommandAttribute) {
            if (member is MethodInfo) {
              commands.Add(new MethodCommandTrigger(plugin, attribute as OnCommandAttribute, member as MethodInfo));
            } else if (member is PropertyInfo) {
              commands.Add(new PropertyCommandTrigger(plugin, attribute as OnCommandAttribute, member as PropertyInfo));
            }
          } else if (attribute is OnJoinAttribute) {
            if (member is MethodInfo) {
              joins.Add(new JoinTrigger(plugin, member as MethodInfo));
            }
          } else if (attribute is PreCommandAttribute) {
            if (member is MethodInfo) {
              pre.Add(new MethodPreCommandTrigger(plugin, attribute as PreCommandAttribute, member as MethodInfo));
            } else if (member is PropertyInfo) {
              pre.Add(new PropertyPreCommandTrigger(plugin, attribute as PreCommandAttribute, member as PropertyInfo));
            }
          }
        }
      }
      return true;
    }

    public void Plugin(IEnumerable plugins)
    {
      foreach (var plugin in plugins) {
        Plugin(plugin as IrcBotPlugin);
      }
    }
  }
}

