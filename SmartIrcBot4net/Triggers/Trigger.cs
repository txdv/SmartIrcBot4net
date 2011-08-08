using System;
using System.Reflection;
using System.Text.RegularExpressions;

using Meebey.SmartIrc4net;
using SmartIrcBot4net.Extensions;

namespace SmartIrcBot4net
{
  class Trigger
  {
    protected IrcBotPlugin Plugin { get; set; }

    public Trigger(IrcBotPlugin plugin)
    {
      Plugin = plugin;
    }

    protected object Process(ParameterInfo info, IrcEventArgs args)
    {
      if (info.ParameterType == typeof(IrcEventArgs)) {
        return args;
      } else {
        return Process(info, args.Data);
      }
    }

    protected object Process(ParameterInfo info, IrcMessageData data)
    {
      if (info.ParameterType == typeof(IrcMessageData)) {
        return data;
      } else if (info.ParameterType == typeof(ReplyCode)) {
        return data.Type;
      } else if (info.ParameterType == typeof(Channel)) {
        return Plugin.Bot.GetChannel(data.Channel);
      } else if (info.ParameterType == typeof(string)) {
        string name = info.Name.ToLower();
        switch (name) {
        case "channel":
        case "chan":
          return data.Channel;
        case "nickname":
        case "nick":
        case "name":
          return data.Nick;
        case "target":
          return data.Channel ?? data.Nick;
        case "from":
          return data.From;
        case "host":
          return data.Host;
        case "ident":
        case "id":
          return data.Ident;
        case "message":
        case "msg":
          return data.Message;
        case "rawmessage":
        case "rawmsg":
        case "raw":
          return data.RawMessage;
        default:
          return null;
        }
      } else {
        return null;
      }
    }

    protected object Process(ParameterInfo info, JoinEventArgs args)
    {
      if (info.ParameterType == typeof(JoinEventArgs)) {
        return args;
      } else if (info.ParameterType == typeof(string)) {
        string name = info.Name.ToLower();
        switch (name) {
        case "who":
        case "nick":
          return args.Who;
        case "channel":
        default:
          return args.Channel;
        }
      } else {
        return Process(info, args.Data);
      }
    }

    protected object Process(ParameterInfo info, Match match)
    {
      if (info.ParameterType == typeof(Match)) {
        return match;
      } else {
        return Process(info, match.Groups);
      }
    }

    protected object Process(ParameterInfo info, GroupCollection groups)
    {
      if (info.ParameterType == typeof(GroupCollection)) {
        return groups;
      } else if (info.ParameterType == typeof(string)) {
        return groups.Get(info.Name);
      } else {
        return null;
      }
    }

    protected object[] GetValues(ParameterInfo[] parameters, Func<ParameterInfo, object> callback)
    {
      object[] values = new object[parameters.Length];

      for (int i = 0; i < parameters.Length; i++) {
        object o = callback(parameters[i]);

        if (o != null) {
          values[i] = o;
        } else if (parameters[i].ParameterType == typeof(string)) {
          Console.WriteLine ("nulllizable");
        }

        if (values[i] == null) {
          values[i] = null;
        }
      }
      return values;
    }

    protected void Debug(object[] parameters)
    {
      for (int i = 0; i < parameters.Length; i++) {
        object o = parameters[i];
        Console.WriteLine("{0}:{1}:{1}", i, parameters[i].GetType(), (o == null ? "(null)" : o));
      }
    }

    protected object Invoke(MethodInfo method, object[] parameters)
    {
      return method.Invoke(Plugin, parameters);
    }

    protected bool? GetBool(string text)
    {
      switch (text) {
      case "1":
      case "on":
      case "true":
        return true;
      case "0":
      case "off":
      case "false":
        return false;
      case null:
        return null;
      default:
        return null;
      }
    }
  }
}

