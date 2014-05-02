using System;
using System.Collections.Generic;

namespace Res.Client
{
    public class TypeRegistry : TypeTagResolver
    {
        readonly Dictionary<Type, string> _registry = new Dictionary<Type, string>();

        public TypeRegistry Register<T>(string typeTag)
        {
            _registry[typeof(T)] = typeTag;

            return this;
        }

        public string GetTagFor(object message)
        {
            Type type = message.GetType();
            if (_registry.ContainsKey(type))
                return _registry[type];

            throw new TypeNotRegisteredException(type);
        }

        public class TypeNotRegisteredException : Exception
        {
            public TypeNotRegisteredException(Type type)
                : base(string.Format("The type '{0}' has not been registered with this registry.", type))
            {
            }
        }
    }
}