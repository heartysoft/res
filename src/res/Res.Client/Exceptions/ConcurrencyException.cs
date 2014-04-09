using System;
using System.CodeDom;

namespace Res.Client.Exceptions
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message) : base(message)
        {
        } 
    }
}