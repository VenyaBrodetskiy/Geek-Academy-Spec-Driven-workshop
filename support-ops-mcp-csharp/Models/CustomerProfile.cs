using System.Text.Json.Serialization;

namespace SupportOpsMcp.Models;

public sealed record CustomerProfile(
    string CustomerEmail,
    string CustomerName,
    string Plan,
    DateOnly SignupDate,
    decimal LastChargeAmount,
    DateOnly LastChargeDate,
    [property: JsonPropertyName("refunds_last_12_months")]
    int RefundsLast12Months,
    [property: JsonPropertyName("contacts_last_30_days")]
    int ContactsLast30Days,
    string AccountStatus
);
