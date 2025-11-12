using System.Diagnostics.CodeAnalysis;
using System.Management;
using Watchdog.Interfaces;

namespace Watchdog.Services;

internal class ManagementObjectSearcherFactory : IManagementObjectSearcherFactory
{
    public IManagementObjectSearcher Create(string query) => new ManagementObjectSearcherAdapter(query);
}

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal class ManagementObjectSearcherAdapter : IManagementObjectSearcher
{
    private readonly ManagementObjectSearcher _searcher;

    public ManagementObjectSearcherAdapter(string query)
    {
        _searcher = new ManagementObjectSearcher(query);
    }

    public IEnumerable<IManagementObject> Get()
    {
        foreach (ManagementObject obj in _searcher.Get())
        {
            yield return new ManagementObjectAdapter(obj);
        }
    }
}

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal class ManagementObjectAdapter : IManagementObject
{
    private readonly ManagementObject _obj;

    public ManagementObjectAdapter(ManagementObject obj)
    {
        _obj = obj;
    }

    public object? this[string propertyName] => _obj[propertyName];
}
