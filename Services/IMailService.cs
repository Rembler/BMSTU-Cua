using System.Threading.Tasks;

namespace Cua.Services
{
    public interface IMailService
    {
        Task SendEmailAsync(string address, string subject, string message);
    }
}