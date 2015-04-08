using System;

namespace Res.Client
{
    public class TypeRegistryEntry
    {
        public string TypeTag { get; private set; }
        public Type CurrentClrType { get; private set; }

        public TypeRegistryEntry(string typeTag, Type currentClrType)
        {
            TypeTag = typeTag;
            CurrentClrType = currentClrType;
        }
    }
}