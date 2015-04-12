using System;

namespace Res.Client
{
    public interface TypeTagResolver
    {
        string GetTagFor(object o);
        Type GetTypeFor(string tag);
        TypeRegistryEntry[] GetRegisteredEvents();
    }
}