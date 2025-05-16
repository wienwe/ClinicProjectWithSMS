using System;
using System.Windows.Forms;

namespace PolyclinicApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var dbHelper = new DatabaseHelper("localhost", "polyclinic", "postgres", "7994821Kk.");
                dbHelper.InitializeDatabase();

                var smsService = new SmsService(
                    "ACXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                    "your_auth_token",
                    "+1234567890");

                Application.Run(new AuthForm(dbHelper, smsService));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске приложения: {ex.Message}");
            }
        }
    }
}