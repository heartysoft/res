using System;
using System.Collections.Generic;

namespace Res.Client
{
    public class TypeRegistry : TypeTagResolver
    {
        readonly Dictionary<Type, string> _registry = new Dictionary<Type, string>();
        readonly Dictionary<string, Type> _reverseRegistry = new Dictionary<string, Type>(); 

        public TypeRegistry Register<T>(string typeTag)
        {
            var type = typeof(T);
            _registry[type] = typeTag;
            _reverseRegistry[typeTag] = type;

            return this;
        }

        public string GetTagFor(object message)
        {
            Type type = message.GetType();
            if (_registry.ContainsKey(type))
                return _registry[type];

            throw new TypeNotRegisteredException(type);
        }

        public Type GetTypeFor(string tag)
        {
            if (_reverseRegistry.ContainsKey(tag) == false)
                throw new TagNotRegisteredException(tag);

            return _reverseRegistry[tag];
        }

        public class TagNotRegisteredException : Exception
        {
            public TagNotRegisteredException(string tag)
                : base(string.Format("The tag '{0}' has not been registered with this registry.", tag))
            {
            }
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