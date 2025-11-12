using System.Management;

namespace Watchdog.Interfaces;

internal interface IManagementObjectSearcherFactory
{
    IManagementObjectSearcher Create(string query);
}

internal interface IManagementObjectSearcher
{
    IEnumerable<IManagementObject> Get();
}

internal interface IManagementObject
{
    object? this[string propertyName] { get; }
}
