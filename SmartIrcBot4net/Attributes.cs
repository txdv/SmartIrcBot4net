using System;
using System.Text.RegularExpressions;

namespace SmartIrcBot4net
{
  public enum MessageType {
    All,
    Query,
    Channel,
  }

  public abstract class IrcBotAttribute : System.Attribute
  {
  }

  public class OnMessageAttribute : IrcBotAttribute
  {
    public OnMessageAttribute(MessageType type)
      : this(type, null)
    {
    }

    public OnMessageAttribute(string target)
      : this(MessageType.All, target)
    {
    }

    public OnMessageAttribute()
      : this(MessageType.All, null)
    {
    }

    public OnMessageAttribute(MessageType type, string target)
    {
      Type = type;
      Target = target;
    }

    public MessageType Type { get; set; }
    public string Target { get; set; }
  }

  public class OnCommandAttribute : OnMessageAttribute
  {
    public OnCommandAttribute(string prefix, string command)
    {
      Prefix = prefix;
      Command = command;
    }

    public OnCommandAttribute(string command)
    : this(null, command)
    {
    }

    public string Prefix { get; set; }
    string command;
    public string Command {
      get {
        return command;
      }
      set {
        command = value;
        Regex = new Regex(command, RegexOptions.Compiled);
      }
    }

    internal Regex Regex { get; set; }
  }

  public class PreCommandAttribute : IrcBotAttribute
  {
  }

  public class OnJoinAttribute : IrcBotAttribute
  {
    public OnJoinAttribute()
    {
    }

    public OnJoinAttribute(string channel)
    {
      Channel = channel;
    }

    public string Channel { get; set; }
  }

  public class OnPartAttribute : IrcBotAttribute
  {
    public OnPartAttribute()
    {
    }

    public OnPartAttribute(string channel)
    {
      Channel = channel;
    }

    public string Channel { get; set; }
  }
}

