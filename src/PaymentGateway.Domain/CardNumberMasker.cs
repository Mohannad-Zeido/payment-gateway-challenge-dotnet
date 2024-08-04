namespace PaymentGateway.Domain;

public static class CardNumberMasker
{
    public static string GetLastFourDigits(long cardNumber)
    {
        var cardNumberString = cardNumber.ToString();
        return cardNumberString.Substring(cardNumberString.Length - 4);
    }
}