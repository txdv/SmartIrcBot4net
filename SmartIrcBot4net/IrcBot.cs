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
    internal List<PreCommandTrigger> PreCommands { get; set; }
    internal List<CommandTrigger>    Commands    { get; set; }
    internal List<JoinTrigger>       Joins       { get; set; }

    public IrcBotPlugin()
    {
      Commands    = new List<CommandTrigger>();
      Joins       = new List<JoinTrigger>();
      PreCommands = new List<PreCommandTrigger>();
    }

    internal void Register()
    {
      var type = GetType();

      foreach (var member in type.GetMembers()) {
        foreach (object attribute in member.GetCustomAttributes(true)) {
          if (attribute is OnCommandAttribute) {
            if (member is MethodInfo) {
              Commands.Add(new MethodCommandTrigger(this, attribute as OnCommandAttribute, member as MethodInfo));
            } else if (member is PropertyInfo) {
              Commands.Add(new PropertyCommandTrigger(this, attribute as OnCommandAttribute, member as PropertyInfo));
            }
          } else if (attribute is OnJoinAttribute) {
            if (member is MethodInfo) {
              Joins.Add(new JoinTrigger(this, member as MethodInfo));
            }
          } else if (attribute is PreCommandAttribute) {
            if (member is MethodInfo) {
              PreCommands.Add(new MethodPreCommandTrigger(this, attribute as PreCommandAttribute, member as MethodInfo));
            } else if (member is PropertyInfo) {
              PreCommands.Add(new PropertyPreCommandTrigger(this, attribute as PreCommandAttribute, member as PropertyInfo));
            }
          }
        }
      }
    }

    internal void HandleOnJoin(object sender, JoinEventArgs args)
    {
      foreach (var join in Joins) {
        join.Handle(args);
      }
    }

    internal void HandleOnMessage(MessageType type, IrcEventArgs args)
    {
      foreach (var pre in PreCommands) {
        if (!pre.Handle(type, args)) {
          return;
        }
      }

      foreach (var command in Commands) {
        if (command.Handle(type, args)) {
          return;
        }
      }
    }

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

    private List<IrcBotPlugin> plugins = new List<IrcBotPlugin>();

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
      foreach (var plugin in plugins) {
        plugin.HandleOnJoin(sender, e);
      }
    }

    void HandleOnMessage(MessageType type, IrcEventArgs args)
    {
      foreach (var plugin in plugins) {
        plugin.HandleOnMessage(type, args);
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

      plugin.Register();

      plugins.Add(plugin);

      return true;
    }

    public void Plugin(IEnumerable plugins)
    {
      foreach (var plugin in plugins) {
        Plugin(plugin as IrcBotPlugin);
      }
    }

    public void LoadTryParse(Assembly assembly)
    {
      Trigger.Load(assembly);
    }

    public bool LoadTryParse(Type type)
    {
      return Trigger.Load(type);
    }
  }
}

