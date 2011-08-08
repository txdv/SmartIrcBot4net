using System;
using System.Reflection;

using Meebey.SmartIrc4net;

namespace SmartIrcBot4net
{

  class JoinTrigger : Trigger
  {
    MethodInfo Method { get; set; }

    public JoinTrigger(IrcBotPlugin plugin, MethodInfo method)
      : base(plugin)
    {
      Method = method;
    }

    public bool Handle(JoinEventArgs args)
    {
      Invoke(Method, GetValues(Method.GetParameters(), (info) => {
        return Process(info, args);
      }));
      return true;
    }
  }
}

