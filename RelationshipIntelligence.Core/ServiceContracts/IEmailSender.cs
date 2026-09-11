using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string htmlBody, string textBody);
    }
}
