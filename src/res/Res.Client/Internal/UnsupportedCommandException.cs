using System;

namespace Res.Client.Internal
{
    public class UnsupportedCommandException : Exception
    {
        public UnsupportedCommandException(string command)
            : base(string.Format("The command {0} is not supported here.", command))
        {
            
        }
    }
}