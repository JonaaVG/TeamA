namespace GentleHealersApi.Exceptions
{
    public class EmailDeliveryException : Exception
    {
        public EmailDeliveryException(string message, Exception inner) : base(message, inner) { }
    }
}