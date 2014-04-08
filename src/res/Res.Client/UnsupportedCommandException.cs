using System;

namespace Res.Client
{
    public class UnsupportedCommandException : Exception
    {
        public UnsupportedCommandException(string command)
            : base(string.Format("The command {0} is not supported here.", command))
        {
            
        }
    }
}