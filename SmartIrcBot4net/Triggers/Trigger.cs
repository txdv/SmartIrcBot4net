using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using Meebey.SmartIrc4net;
using SmartIrcBot4net.Extensions;

using Manos.IO;

namespace SmartIrcBot4net
{
  class Trigger
  {
    private static Dictionary<Type, MethodInfo> tryParseMethods = new Dictionary<Type, MethodInfo>();

    static Trigger()
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
        Load(assembly);
      }
    }

    public static void Load(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes()) {
          Load(type);
        }
    }

    public static void Load(Type type)
    {
      foreach (var tuple in GetTryParseMethod(type)) {
        if (!tryParseMethods.ContainsKey(tuple.Item1)) {
          tryParseMethods[tuple.Item1] = tuple.Item2;
        }
      }
    }

    static IEnumerable<Tuple<Type, MethodInfo>> GetTryParseMethod(Type type)
    {
      foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
        var param = method.GetParameters();
        if ((method.ReturnType == typeof(bool)) && (method.Name == "TryParse") && (param.Length == 2)) {
          if (param[0].ParameterType == typeof(string) && param[1].IsOut) {
            yield return Tuple.Create<Type, MethodInfo>(param[1].ParameterType.GetElementType(), method);
          }
        }
      }
      yield break;
    }

    protected bool TryParse(Type type, string text, out object obj)
    {
      obj = null;
      var tryParse = GetTryParse(type);

      if (tryParse == null) {
        return false;
      }

      var args = new object[] { text, null };
      tryParse.Invoke(null, args);
      obj = args[1];
      return true;
    }


    protected bool HasTryParse(Type type)
    {
      return GetTryParseMethod(type) != null;
    }

    protected MethodInfo GetTryParse(Type type)
    {
      MethodInfo mi;
      if (tryParseMethods.TryGetValue(type, out mi)) {
        return mi;
      }
      return null;
    }

    public IrcBotPlugin Plugin { get; protected set; }
    public Context Context {
      get {
        return Plugin.Context;
      }
    }

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
        if (HasTryParse(info.ParameterType)) {
          string str = groups.Get(info.Name);
          if (str == null) {
            return null;
          } else {
            object o;
            if (TryParse(info.ParameterType, str, out o)) {
              return o;
            } else {
              return null;
            }
          }

        }
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

