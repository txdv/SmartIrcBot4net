using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using SmartIrcBot4net.Extensions;
using Meebey.SmartIrc4net;

namespace SmartIrcBot4net
{
  abstract class PreCommandTrigger : Trigger
  {
    public PreCommandAttribute Attribute { get; set; }

    public PreCommandTrigger(IrcBotPlugin plugin, PreCommandAttribute attribute)
      : base(plugin)
    {
      Attribute = attribute;
    }

    public abstract bool Handle(MessageType type, IrcEventArgs args);
  }

  class MethodPreCommandTrigger : PreCommandTrigger
  {
    MethodInfo Method { get; set; }

    public MethodPreCommandTrigger(IrcBotPlugin plugin, PreCommandAttribute attribute, MethodInfo method)
      : base(plugin, attribute)
    {
      Method = method;
    }

    public override bool Handle(MessageType type, IrcEventArgs args)
    {
      return (bool)Invoke(Method, GetValues(Method.GetParameters(), (info) => {
        return Process(info, args);
      }));
    }
  }

  class PropertyPreCommandTrigger : PreCommandTrigger
  {
    PropertyInfo Property { get; set; }

    public PropertyPreCommandTrigger(IrcBotPlugin plugin, PreCommandAttribute attribute, PropertyInfo property)
      : base(plugin, attribute)
    {
      Property = property;
    }

    public override bool Handle(MessageType type, IrcEventArgs args)
    {
      if (Property.PropertyType == typeof(string)) {
        string val = Property.GetValue(Plugin, null) as string;
        var b = GetBool(val);
        if (b.HasValue) {
          return b.Value;
        } else {
          throw new Exception(string.Format("string property value not supported: {0}", val));
        }
      } else if (Property.PropertyType == typeof(bool)) {
        return (bool)Property.GetValue(Plugin, null);
      } else {
        throw new Exception("Property type not supported");
      }
    }
  }

  abstract class CommandTrigger : Trigger
  {
    public OnCommandAttribute Attribute { get; set; }

    public string Prefix {
      get {
        return Attribute.Prefix ?? Plugin.DefaultPrefix ?? Plugin.Bot.DefaultPrefix;
      }
    }

    public CommandTrigger(IrcBotPlugin plugin, OnCommandAttribute attribute)
      : base(plugin)
    {
      Attribute = attribute;
    }

    public abstract bool Handle(MessageType type, IrcEventArgs args);

    protected Match GetMatch(MessageType type, IrcEventArgs args)
    {

      string msg = args.Data.Message;

      if (!msg.StartsWith(Prefix)) {
        return null;
      }

      msg = msg.Substring(Prefix.Length);

      var match = Attribute.Regex.Match(msg);

      if (Attribute.Type != MessageType.All && Attribute.Type != type) {
        return null;
      }

      if (!args.Data.Message.StartsWith(Prefix)) {
        return null;
      }

      if (!match.Success) {
        return null;
      }

      return match;
    }
  }

  class MethodCommandTrigger : CommandTrigger
  {

    public MethodInfo Method { get; set; }

    public MethodCommandTrigger(IrcBotPlugin plugin, OnCommandAttribute attribute, MethodInfo method)
      : base(plugin, attribute)
    {
      Method = method;
    }

    public override bool Handle(MessageType type, IrcEventArgs args)
    {
      var match = GetMatch(type, args);
      if (match == null) {
        return false;
      }

      Invoke(Method, GetValues(Method.GetParameters(), (info) => {
        return Process(info, match) ?? Process(info, args);
      }));

      return true;
    }

  }

  class PropertyCommandTrigger : CommandTrigger
  {

    static Type[] genericTypes = new Type[] { typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };

    static bool IsGeneric(Type type)
    {
      foreach (var gtype in genericTypes) {
        if (type == gtype) {
          return true;
        }
      }
      return false;
    }

    public PropertyInfo Property  { get; set; }

    public PropertyCommandTrigger(IrcBotPlugin plugin, OnCommandAttribute attribute, PropertyInfo property)
      : base(plugin, attribute)
    {
      Property = property;
    }

    public bool GenericSetValue(string text)
    {
      var type = Property.PropertyType;

      MethodInfo tryParse = null;
      var methods = type.GetMethods();
      foreach (var method in methods) {
        if ((method.Name == "TryParse") && (method.GetParameters().Length == 2)) {
          tryParse = method;
        }
      }

      object o = null;

      var args = new object[] { text, o };
      bool res = (bool)tryParse.Invoke(null, args);

      if (!res) {
        return false;
      }

      return SetValue(args[1]);
    }

    public override bool Handle(MessageType type, IrcEventArgs args)
    {
      var match = GetMatch(type, args);
      if (match == null) {
        return false;
      }

      string stringValue = match.Groups.Get(Property.Name);

      if (stringValue == null) {
        return false;
      }

      var t = Property.PropertyType;
      if (t == typeof(bool)) {
        bool? value = GetBool(stringValue);
        if (!value.HasValue) {
          return false;
        }

        return SetValue(value.Value);
      } else if (t == typeof(int)) {
        int value = 0;
        if (!int.TryParse(stringValue, out value)) {
          return false;
        }

        return SetValue(value);
      } else if (IsGeneric(t)) {
        return GenericSetValue(stringValue);
      } else {
        return false;
      }
    }

    bool SetValue(object value)
    {
      if (!Property.CanWrite) {
        return false;
      }

      Property.SetValue(Plugin as object, value, null);
      return true;
    }
  }
}

