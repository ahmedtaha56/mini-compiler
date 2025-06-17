using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        Console.WriteLine("Enter usernames (separated by commas):");
        string input = Console.ReadLine();

        List<string> usernames = input.Split(',').Select(u => u.Trim()).ToList();
        Dictionary<string, string> results = new();
        List<string> invalidUsernames = new();

        foreach (string username in usernames)
        {
            if (ValidateUsername(username, out string reason))
            {
                Console.WriteLine($"{username} - Valid");
                string details = AnalyzeUsername(username);
                Console.WriteLine(details);

                string password = GeneratePassword();
                string strength = EvaluatePasswordStrength(password);
                Console.WriteLine($"Generated Password: {password} (Strength: {strength})\n");

                results[username] = $"Valid\n{details}\nGenerated Password: {password} (Strength: {strength})\n";
            }
            else
            {
                Console.WriteLine($"{username} - Invalid ({reason})\n");
                results[username] = $"Invalid ({reason})";
                invalidUsernames.Add(username);
            }
        }

        SaveResultsToFile(results);

        Console.WriteLine($"Summary:\n- Total Usernames: {usernames.Count}\n- Valid Usernames: {results.Count(r => r.Value.Contains(\"Valid\"))}\n- Invalid Usernames: {invalidUsernames.Count}\n");

        if (invalidUsernames.Count > 0)
        {
            Console.WriteLine($"Invalid Usernames: {string.Join(", ", invalidUsernames)}\nDo you want to retry invalid usernames? (y/n):");
            string retry = Console.ReadLine()?.Trim().ToLower();

            if (retry == "y")
            {
                Console.WriteLine("Enter invalid usernames:");
                string retryInput = Console.ReadLine();
                List<string> retryUsernames = retryInput.Split(',').Select(u => u.Trim()).ToList();

                foreach (string username in retryUsernames)
                {
                    if (ValidateUsername(username, out string reason))
                    {
                        Console.WriteLine($"{username} - Valid");
                        string details = AnalyzeUsername(username);
                        Console.WriteLine(details);

                        string password = GeneratePassword();
                        string strength = EvaluatePasswordStrength(password);
                        Console.WriteLine($"Generated Password: {password} (Strength: {strength})\n");

                        results[username] = $"Valid\n{details}\nGenerated Password: {password} (Strength: {strength})\n";
                    }
                    else
                    {
                        Console.WriteLine($"{username} - Invalid ({reason})\n");
                    }
                }

                SaveResultsToFile(results);
            }
        }
    }

    static bool ValidateUsername(string username, out string reason)
    {
        if (!Regex.IsMatch(username, "^[a-zA-Z]") )
        {
            reason = "Username must start with a letter.";
            return false;
        }

        if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]{5,15}$"))
        {
            reason = "Username length must be between 5 and 15 characters and only contain letters, digits, or underscores.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    static string AnalyzeUsername(string username)
    {
        int uppercaseCount = Regex.Matches(username, "[A-Z]").Count;
        int lowercaseCount = Regex.Matches(username, "[a-z]").Count;
        int digitCount = Regex.Matches(username, "[0-9]").Count;
        int underscoreCount = Regex.Matches(username, "[_]").Count;

        return $"Letters: {uppercaseCount + lowercaseCount} (Uppercase: {uppercaseCount}, Lowercase: {lowercaseCount}), Digits: {digitCount}, Underscores: {underscoreCount}";
    }

    static string GeneratePassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*";

        Random random = new Random();

        string password = string.Concat(
            RandomChars(uppercase, 2, random),
            RandomChars(lowercase, 2, random),
            RandomChars(digits, 2, random),
            RandomChars(specialChars, 2, random),
            RandomChars(uppercase + lowercase + digits + specialChars, 4, random));

        return new string(password.OrderBy(c => random.Next()).ToArray());
    }

    static string RandomChars(string source, int count, Random random)
    {
        return new string(Enumerable.Range(0, count).Select(_ => source[random.Next(source.Length)]).ToArray());
    }

    static string EvaluatePasswordStrength(string password)
    {
        int lengthScore = password.Length >= 12 ? 1 : 0;
        int typeScore = new[] {"[A-Z]", "[a-z]", "[0-9]", "[!@#$%^&*]"}.Count(pattern => Regex.IsMatch(password, pattern));

        if (lengthScore + typeScore >= 4)
            return "Strong";
        if (lengthScore + typeScore >= 3)
            return "Medium";
        return "Weak";
    }

    static void SaveResultsToFile(Dictionary<string, string> results)
    {
        using StreamWriter writer = new StreamWriter("UserDetails.txt");
        writer.WriteLine("Validation Results:");

        foreach (var result in results)
        {
            writer.WriteLine($"{result.Key} - {result.Value}\n");
        }

        int total = results.Count;
        int valid = results.Count(r => r.Value.Contains("Valid"));
        int invalid = total - valid;

        writer.WriteLine($"Summary:\n- Total Usernames: {total}\n- Valid Usernames: {valid}\n- Invalid Usernames: {invalid}\n");
    }
}
