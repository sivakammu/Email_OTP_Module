using System;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Timer = System.Timers.Timer;

class Program
{
    static async Task Main(string[] args)
    {
        Email_OTP_Module otpModule = new Email_OTP_Module();
        otpModule.Start();

        Console.Write("Enter your email address: ");
        string email = Console.ReadLine();
        int status = otpModule.Generate_OTP_Email(email);

        if (status == Email_OTP_Module.STATUS_EMAIL_OK)
        {
            Console.WriteLine("OTP has been sent successfully");
        }
        else if (status == Email_OTP_Module.STATUS_EMAIL_INVALID)
        {
            Console.WriteLine("Email address is invalid");
        }
        else if (status == Email_OTP_Module.STATUS_EMAIL_FAIL)
        {
            Console.WriteLine("Email address does not exist or sending to the email has failed");
        }
        if (status == Email_OTP_Module.STATUS_EMAIL_OK)
        {
            Console.WriteLine("Please enter the OTP sent to your email:");
            int otpStatus = await otpModule.CheckOtpAsync(new ConsoleIOStream());

            switch (otpStatus)
            {
                case Email_OTP_Module.STATUS_OTP_OK:
                    Console.WriteLine("OTP is valid and checked");
                    break;
                case Email_OTP_Module.STATUS_OTP_FAIL:
                    Console.WriteLine("OTP is wrong after 10 tries");
                    break;
                case Email_OTP_Module.STATUS_OTP_TIMEOUT:
                    Console.WriteLine("OTP expired due to timeout.");
                    break;
            }
        }

        otpModule.Close();
    }
}

public class Email_OTP_Module
{
    public const int STATUS_EMAIL_OK = 1;
    public const int STATUS_EMAIL_FAIL = 2;
    public const int STATUS_EMAIL_INVALID = 3;
    public const int STATUS_OTP_OK = 4;
    public const int STATUS_OTP_FAIL = 5;
    public const int STATUS_OTP_TIMEOUT = 6;

    private string currentOtp;
    private string email;
    private int retries;

    public Email_OTP_Module()
    {
        currentOtp = null;
        email = null;
        retries = 0;
    }

    public void Start()
    {
        
    }

    public void Close()
    {
        
    }

    public int Generate_OTP_Email(string userEmail)
    {
        if (!IsValidEmail(userEmail))
        {
            return Email_OTP_Module.STATUS_EMAIL_INVALID;
        }

        currentOtp = GenerateOtp();
        Console.WriteLine($"Printed for testing {currentOtp}"); // for testing purpose 
        email = userEmail;

        string emailBody = $"Your OTP Code is {currentOtp}. The code is valid for 1 minute";
        if (SendEmail(userEmail, emailBody))
        {
            StartTimer();
            return Email_OTP_Module.STATUS_EMAIL_OK;
        }
        else
        {
            return Email_OTP_Module.STATUS_EMAIL_FAIL;
        }
    }

    private bool IsValidEmail(string email)
    {
        // Only allow emails from the ".dso.org.sg" domain
        string pattern = @"^[a-zA-Z0-9._%+-]+.dso.org.sg";
        return Regex.IsMatch(email, pattern);
    }

    private string GenerateOtp()
    {
        // Generate a 6-digit random OTP
        Random random = new Random();
        return random.Next(0, 1000000).ToString("D6");
    }

    private bool SendEmail(string emailAddress, string emailBody)
    {
        try
        {
            // Email sending logic
            //MailMessage mail = new MailMessage(".dso.org.sg", emailAddress);
            //SmtpClient client = new SmtpClient("smtp.dso.com");

            //mail.Subject = "Your OTP Code";
            //mail.Body = emailBody;

            //client.Send(mail);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
    }

    private void StartTimer()
    {
        Timer timer = new Timer(60000);
        timer.Elapsed += (sender, e) => InvalidateOtp();
        timer.AutoReset = false;
        timer.Start();
    }

    private void InvalidateOtp()
    {
        currentOtp = null;
    }

    public async Task<int> CheckOtpAsync(IOStream input)
    {
        for (int i = 0; i < 10; i++)
        {
            if (currentOtp == null)
            {
                return Email_OTP_Module.STATUS_OTP_TIMEOUT;
            }

            string userInput = await ReadOtpFromInputAsync(input, 60000); // 1 minute timeout
            if (userInput == currentOtp)
            {
                InvalidateOtp();
                return Email_OTP_Module.STATUS_OTP_OK;
            }
            retries++;
        }
        return Email_OTP_Module.STATUS_OTP_FAIL;
    }

    private async Task<string> ReadOtpFromInputAsync(IOStream input, int timeout)
    {
        return await input.ReadOtpAsync(timeout);
    }
}

public interface IOStream
{
    Task<string> ReadOtpAsync(int timeout);
}

public class ConsoleIOStream : IOStream
{
    public async Task<string> ReadOtpAsync(int timeout)
    {
        string otp = null;
        CancellationTokenSource cts = new CancellationTokenSource(timeout);

        try
        {
            otp = await Task.Run(() => Console.ReadLine(), cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("OTP input timed out.");
        }

        return otp;
    }
}
