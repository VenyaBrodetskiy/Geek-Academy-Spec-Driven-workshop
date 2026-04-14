using System.Text.Json;
using SupportOpsMcp.Models;

namespace SupportOpsMcp;

public sealed class SupportOpsDataStore
{
    private readonly Lazy<IReadOnlyDictionary<string, CustomerProfile>> _customers = new(LoadCustomers);

    public CustomerProfile? FindCustomer(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return _customers.Value.TryGetValue(NormalizeEmail(email), out var customer)
            ? customer
            : null;
    }

    private static IReadOnlyDictionary<string, CustomerProfile> LoadCustomers()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "mock_customers.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Could not find Data/mock_customers.json in the MCP server output directory.", path);
        }

        var json = File.ReadAllText(path);
        var customers = JsonSerializer.Deserialize<List<CustomerProfile>>(json, SupportOpsJson.Options)
            ?? throw new InvalidOperationException("Data/mock_customers.json did not contain a valid customer list.");

        return customers.ToDictionary(
            customer => NormalizeEmail(customer.CustomerEmail),
            customer => customer,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
