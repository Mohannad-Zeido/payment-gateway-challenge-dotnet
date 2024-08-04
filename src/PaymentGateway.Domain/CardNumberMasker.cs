namespace PaymentGateway.Domain;

public static class CardNumberMasker
{
    public static int GetLastFourDigits(long cardNumber)
    {
        var cardNumberString = cardNumber.ToString();
        var lastFour = cardNumberString.Substring(cardNumberString.Length - 4);
        return int.Parse(lastFour);
    }
}