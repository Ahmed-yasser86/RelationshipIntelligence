namespace ServiceContracts.DTOs
{
    /// <summary>
    /// One channel membership: the shared channel <see cref="Name"/> plus this
    /// contact's handle on it (<see cref="Value"/> — a number, username, or URL).
    /// </summary>
    public class ContactChannelRequest
    {
        public string? Name { get; set; }

        public string? Value { get; set; }
    }
}
