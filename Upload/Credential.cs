using System.Text;

namespace HpScan.Upload;

public record Credential(string UserName, string Password)
{
    public static Credential WithPasswordFromConsole(string userName)
    {
        Console.WriteLine("password for ftp paperless access: ");
        var pwd = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo i = Console.ReadKey(true);
            if (i.Key == ConsoleKey.Enter)
            {
                break;
            }
            else if (i.Key == ConsoleKey.Backspace)
            {
                if (pwd.Length > 0)
                {
                    pwd.Remove(pwd.Length - 1, 1);
                    Console.Write("\b \b");
                }
            }
            else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
            {
                pwd.Append(i.KeyChar);
                Console.Write("*");
            }
        }
        Console.WriteLine();
        return new Credential(userName, pwd.ToString());
    }
}