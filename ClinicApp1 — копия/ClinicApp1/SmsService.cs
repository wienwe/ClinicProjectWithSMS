using Twilio;
using Twilio.Rest.Api.V2010.Account;
using System.Threading.Tasks;

namespace PolyclinicApp
{
    public class SmsService
    {
        private readonly string accountSid;
        private readonly string authToken;
        private readonly string fromPhoneNumber;

        public SmsService(string accountSid, string authToken, string fromPhoneNumber)
        {
            this.accountSid = accountSid;
            this.authToken = authToken;
            this.fromPhoneNumber = fromPhoneNumber;
            TwilioClient.Init(accountSid, authToken);
        }

        public async Task SendSmsAsync(string toPhoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(toPhoneNumber)) return;

            try
            {
                var formattedPhone = FormatPhoneNumber(toPhoneNumber);

                var sms = await MessageResource.CreateAsync(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(fromPhoneNumber),
                    to: new Twilio.Types.PhoneNumber(formattedPhone)
                );

                Console.WriteLine($"SMS sent to {formattedPhone}, SID: {sms.Sid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке SMS: {ex.Message}");
            }
        }

        private static string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;

            phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            if (phone.StartsWith("8"))
                return "+7" + phone.Substring(1);
            if (phone.StartsWith("7"))
                return "+" + phone;

            return phone;
        }
    }
}