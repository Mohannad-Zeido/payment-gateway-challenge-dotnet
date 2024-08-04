

namespace PaymentGateway.Application.Enums;

public enum Currency
{
    GBP,
    USD,
    EUR
}

public static class CurrencyExtension
{
    public static string[] SupportedCurrencies()
    {
        return Enum.GetNames(typeof(Currency));
    }
}