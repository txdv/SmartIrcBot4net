using System;
using System.Collections.Generic;

using Manos.IO;
using Meebey.SmartIrc4net;
using SmartIrcBot4net;

namespace Test
{
  public class AdminPlugin : IrcBotPlugin
  {
    List<string> admins = new List<string>();

    public AdminPlugin(string admin)
    {
      admins.Add(admin);
    }

    public bool IsAdmin(string nick)
    {
      return admins.Contains(nick);
    }

    public bool Add(string nick)
    {
      if (IsAdmin(nick)) {
        return false;
      } else {
        admins.Add(nick);
        return true;
      }
    }

    public bool Delete(string nick)
    {
      if (!IsAdmin(nick)) {
        return false;
      } else {
        admins.Remove(nick);
        return true;
      }
    }

    [OnCommand("admin add (?<admin>(.+))")]
    public void AdminAdd(Channel channel, string nick, string admin)
    {
      if (!IsAdmin(nick)) {
        return;
      }

      if (Add(admin)) {
        Bot.SendMessage(SendType.Message, channel.Name, string.Format("added {0} to admins", admin));
      } else {
        Bot.SendMessage(SendType.Message, channel.Name, string.Format("{0} is already an admin", admin));
      }
    }

    [OnCommand("admin del (?<admin>(.+))")]
    public void AdminDel(Channel channel, string nick, string admin)
    {
      if (!IsAdmin(nick)) {
        return;
      }

      if (Delete(admin)) {
        Bot.SendMessage(SendType.Message, channel.Name, string.Format("removed {0} from the admin list", admin));
      } else {
        Bot.SendMessage(SendType.Message, channel.Name, string.Format("no such admin {0}", admin));
      }
    }

    [OnCommand("admin list$")]
    public void AdminList(Channel channel, string nick, string admin)
    {
      if (!IsAdmin(nick)) {
        return;
      }

      Bot.SendMessage(SendType.Message, channel.Name, admins.Count.ToString());
    }
  }

  public class TestPlugin : IrcBotPlugin
  {
    public AdminPlugin AdminPlugin { get; protected set; }

    Dictionary<string, string> db = new Dictionary<string, string>();

    public TestPlugin(AdminPlugin plugin)
    {
      AdminPlugin = plugin;
      On = true;
    }

    [PreCommand]
    public bool AdminCheck(string nick)
    {
      return AdminPlugin.IsAdmin(nick);
    }

    [OnCommand("db (?<On>(on|off))$")]
    [PreCommand]
    public bool On { get; set; }

    [OnCommand("db$")]
    public void Check(string target)
    {
      Bot.SendMessage(SendType.Message, target, "service is " + (On ? "on" : "off"));
    }

    [OnCommand(@"db set (?<key>(\w+)) (?<value>(.+))")]
    public void Set(string key, string value)
    {
      db[key] = value;
    }

    [OnCommand(@"db get (?<key>(\w+))")]
    public void Get(string target, string key)
    {
      string val;
      if (!db.TryGetValue(key, out val)) {
        Bot.SendMessage(SendType.Message, target, "no such key");
      } else {
        Bot.SendMessage(SendType.Message, target, string.Format("{0}:{1}", key, db[key]));
      }
    }

    [OnCommand(@"db (remove|rm|del|delete) (?<key>(\w+))")]
    public void Delete(string target, string key)
    {
      if (db.ContainsKey(key)) {
        Bot.SendMessage(SendType.Message, target, "deleted key " + key);
        db.Remove(key);
      } else {
        Bot.SendMessage(SendType.Message, target, "no such key");
      }
    }
  }

  public class Greeter : IrcBotPlugin
  {
    [OnJoin]
    public void Greet(JoinEventArgs args)
    {
      if (args.Who != Bot.Nickname) {
        Bot.SendMessage(SendType.Message, args.Channel, string.Format("Hello {0}", args.Who));
      }
    }
  }

  public class FloodPlugin : IrcBotPlugin
  {
    [OnCommand("join (?<channel>(.+))")]
    public void JoinCommand(string channel)
    {
      Bot.RfcJoin(channel);
    }

    [OnCommand(@"flood (?<count>(\d+))")]
    public void Flood(string channel, int count)
    {
      DateTime now = DateTime.Now;
      for (int i = 0; i < count; i++) {
        Bot.SendMessage(SendType.Message, channel, "test string");
      }
      Console.WriteLine(DateTime.Now - now);
    }
  }

  class MainClass
  {
    public static void Main(string[] args)
    {
      var context = Context.Create();

      IrcBot bot = new IrcBot(context);

      bot.SendDelay = 0;

      var adminPlugin = new AdminPlugin("txdv");

      bot.Plugin(adminPlugin);
      bot.Plugin(new TestPlugin(adminPlugin));
      bot.Plugin(new FloodPlugin());

      //bot.SendDelay = 0;

      bot.Connect(new string[] { "127.0.0.1" }, 6667, delegate {
        bot.ActiveChannelSyncing = true;
        bot.Login("bot", "test bot");
        bot.RfcJoin("#six");
      });

      context.Start();
    }
  }
}
