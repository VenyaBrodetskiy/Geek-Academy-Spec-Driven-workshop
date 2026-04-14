using System.Text.Json.Serialization;

namespace SupportOpsMcp.Models;

public sealed record CustomerLookupResult(
    bool Found,
    string CustomerEmail,
    string? CustomerName,
    string? Plan,
    DateOnly? SignupDate,
    decimal? LastChargeAmount,
    DateOnly? LastChargeDate,
    [property: JsonPropertyName("refunds_last_12_months")]
    int? RefundsLast12Months,
    [property: JsonPropertyName("contacts_last_30_days")]
    int? ContactsLast30Days,
    string? AccountStatus
)
{
    public static CustomerLookupResult FromProfile(CustomerProfile profile)
    {
        return new CustomerLookupResult(
            Found: true,
            CustomerEmail: profile.CustomerEmail,
            CustomerName: profile.CustomerName,
            Plan: profile.Plan,
            SignupDate: profile.SignupDate,
            LastChargeAmount: profile.LastChargeAmount,
            LastChargeDate: profile.LastChargeDate,
            RefundsLast12Months: profile.RefundsLast12Months,
            ContactsLast30Days: profile.ContactsLast30Days,
            AccountStatus: profile.AccountStatus);
    }

    public static CustomerLookupResult NotFound(string customerEmail)
    {
        return new CustomerLookupResult(
            Found: false,
            CustomerEmail: customerEmail,
            CustomerName: null,
            Plan: null,
            SignupDate: null,
            LastChargeAmount: null,
            LastChargeDate: null,
            RefundsLast12Months: null,
            ContactsLast30Days: null,
            AccountStatus: null);
    }
}
