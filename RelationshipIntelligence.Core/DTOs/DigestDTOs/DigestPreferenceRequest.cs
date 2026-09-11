namespace ServiceContracts.DTOs
{
    public class DigestPreferenceRequest
    {
        public bool Enabled { get; set; } = true;
        public double Threshold { get; set; } = 50;
        public int Count { get; set; } = 5;
    }
}
