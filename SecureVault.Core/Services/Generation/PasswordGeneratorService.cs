using System.Security.Cryptography;
using System.Text;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;

namespace SecureVault.Core.Services.Generation;

/// <summary>
/// Professional password generator with cryptographically secure random generation
/// </summary>
public class PasswordGeneratorService : IPasswordGeneratorService
{
    private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowercaseChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SpecialChars = "!@#$%^&*-_=+";
    private const string AmbiguousChars = "0O1lI"; // Characters to avoid if AvoidAmbiguous is true

    // Common words for passphrase generation (BIP39-inspired but simplified)
    private static readonly string[] PassphraseWords = new[]
    {
        "ability", "able", "about", "above", "absent", "absorb", "abstract", "absurd", "abuse", "access",
        "accident", "account", "accuse", "achieve", "acid", "acoustic", "acquire", "across", "act", "action",
        "actor", "actress", "actual", "adapt", "add", "addict", "address", "adjust", "admit", "adult",
        "advance", "advice", "aerobic", "afford", "afraid", "again", "age", "agent", "agree", "ahead",
        "aim", "air", "airport", "aisle", "alarm", "album", "alcohol", "alert", "alien", "all",
        "alley", "allow", "almost", "alone", "alpha", "already", "also", "alter", "always", "amateur",
        "amazing", "among", "amount", "amused", "analyst", "anchor", "ancient", "anger", "angle", "angry",
        "animal", "ankle", "announce", "annual", "another", "answer", "antenna", "antique", "anxiety", "any",
        "apart", "apology", "appear", "apple", "approve", "april", "arch", "arctic", "area", "arena",
        "argue", "arm", "armed", "armor", "army", "around", "arrange", "arrest", "arrive", "arrow",
        "art", "artefact", "artist", "artwork", "ask", "aspect", "assault", "asset", "assist", "assume",
        "asthma", "athlete", "atom", "attack", "attend", "attitude", "attract", "auction", "audit", "august",
        "aunt", "author", "auto", "autumn", "average", "avocado", "avoid", "awake", "aware", "away",
        "awesome", "awful", "awkward", "axis", "baby", "bachelor", "bacon", "badge", "bag", "balance",
        "balcony", "ball", "bamboo", "banana", "banner", "bar", "barely", "bargain", "barrel", "base",
        "basic", "basket", "battle", "beach", "bean", "beauty", "become", "beef", "before", "begin",
        "behave", "behind", "believe", "below", "belt", "bench", "benefit", "best", "betray", "better",
        "between", "beyond", "bicycle", "bid", "bike", "bind", "biology", "bird", "birth", "bitter",
        "black", "blade", "blame", "blanket", "blast", "bleak", "bless", "blind", "blood", "blossom",
        "blouse", "blue", "blur", "blush", "board", "boat", "body", "boil", "bomb", "bone",
        "bonus", "book", "boost", "border", "boring", "borrow", "boss", "bottom", "bounce", "box",
        "boy", "bracket", "brain", "brand", "brass", "brave", "bread", "breeze", "brick", "bridge",
        "brief", "bright", "bring", "brisk", "broccoli", "broken", "bronze", "broom", "brother", "brown",
        "brush", "bubble", "buddy", "budget", "buffalo", "build", "bulb", "bulk", "bullet", "bundle",
        "bunker", "burden", "burger", "burst", "bus", "business", "busy", "butter", "buyer", "buzz"
    };

    public string GeneratePassword(PasswordGeneratorOptions options)
    {
        if (options.IsPassphrase)
        {
            return GeneratePassphrase(options);
        }

        var charPool = BuildCharacterPool(options);
        
        if (string.IsNullOrEmpty(charPool))
        {
            throw new InvalidOperationException("Character pool is empty. At least one character type must be selected.");
        }

        string password;
        int attempts = 0;
        const int maxAttempts = 100;

        do
        {
            password = GenerateRandomString(charPool, options.Length);
            attempts++;

            if (attempts > maxAttempts)
            {
                throw new InvalidOperationException("Failed to generate a valid password after maximum attempts.");
            }

        } while (!ValidatePassword(password, options));

        return password;
    }

    public string GeneratePassphrase(PasswordGeneratorOptions options)
    {
        var words = new List<string>();
        
        for (int i = 0; i < options.WordCount; i++)
        {
            var word = PassphraseWords[GetSecureRandomNumber(0, PassphraseWords.Length)];
            
            if (options.CapitalizeWords)
            {
                word = char.ToUpperInvariant(word[0]) + word.Substring(1);
            }
            
            words.Add(word);
        }

        var passphrase = string.Join(options.Separator, words);

        if (options.IncludeNumber)
        {
            var number = GetSecureRandomNumber(0, 9999);
            passphrase += options.Separator + number.ToString("D4");
        }

        return passphrase;
    }

    public List<string> GeneratePasswordSuggestions(PasswordGeneratorOptions options, int count = 5)
    {
        var suggestions = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            try
            {
                var password = GeneratePassword(options);
                suggestions.Add(password);
            }
            catch
            {
                // Skip if generation fails
            }
        }

        return suggestions;
    }

    public bool ValidatePassword(string password, PasswordGeneratorOptions options)
    {
        if (string.IsNullOrEmpty(password) || password.Length < options.Length)
            return false;

        if (!options.RequireFromEachCategory)
            return true;

        bool hasUpper = !options.UseUppercase || password.Any(char.IsUpper);
        bool hasLower = !options.UseLowercase || password.Any(char.IsLower);
        bool hasDigit = !options.UseDigits || password.Any(char.IsDigit);
        bool hasSpecial = !options.UseSpecialChars || password.Any(c => SpecialChars.Contains(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    // Private helper methods

    private string BuildCharacterPool(PasswordGeneratorOptions options)
    {
        var pool = new StringBuilder();

        if (options.UseUppercase)
        {
            pool.Append(options.AvoidAmbiguous ? 
                UppercaseChars.Where(c => !AmbiguousChars.Contains(c)).ToArray() : 
                UppercaseChars);
        }

        if (options.UseLowercase)
        {
            pool.Append(options.AvoidAmbiguous ? 
                LowercaseChars.Where(c => !AmbiguousChars.Contains(c)).ToArray() : 
                LowercaseChars);
        }

        if (options.UseDigits)
        {
            pool.Append(options.AvoidAmbiguous ? 
                DigitChars.Where(c => !AmbiguousChars.Contains(c)).ToArray() : 
                DigitChars + "01");
        }

        if (options.UseSpecialChars)
        {
            pool.Append(SpecialChars);
        }

        if (!string.IsNullOrEmpty(options.CustomCharacters))
        {
            pool.Append(options.CustomCharacters);
        }

        return pool.ToString();
    }

    private string GenerateRandomString(string charPool, int length)
    {
        var result = new char[length];
        
        for (int i = 0; i < length; i++)
        {
            result[i] = charPool[GetSecureRandomNumber(0, charPool.Length)];
        }

        return new string(result);
    }

    private int GetSecureRandomNumber(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
            throw new ArgumentException("minValue must be less than maxValue");

        long range = (long)maxValue - minValue;
        byte[] randomBytes = new byte[8];
        
        RandomNumberGenerator.Fill(randomBytes);
        
        ulong randomValue = BitConverter.ToUInt64(randomBytes, 0);
        return (int)(minValue + (randomValue % (ulong)range));
    }
}
