using System;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace NR.Cache.Remoting
{
    public class CachingProxy<T> : RealProxy
    {
        public override IMessage Invoke(IMessage msg)
        {
            throw new NotImplementedException();
        }
    }
}