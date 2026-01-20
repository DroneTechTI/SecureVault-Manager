using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;
using Windows.ApplicationModel.DataTransfer;

namespace SecureVault.App.Views;

public sealed partial class GeneratorPage : Page
{
    private readonly IPasswordGeneratorService _generatorService;

    public GeneratorPage()
    {
        this.InitializeComponent();
        _generatorService = App.Services.GetRequiredService<IPasswordGeneratorService>();
    }

    private void OnGenerateClick(object sender, RoutedEventArgs e)
    {
        var options = new PasswordGeneratorOptions
        {
            Length = (int)LengthSlider.Value,
            UseUppercase = UppercaseToggle.IsOn,
            UseLowercase = LowercaseToggle.IsOn,
            UseDigits = DigitsToggle.IsOn,
            UseSpecialChars = SpecialToggle.IsOn,
            AvoidAmbiguous = AvoidAmbiguousToggle.IsOn
        };

        try
        {
            var password = _generatorService.GeneratePassword(options);
            GeneratedPasswordText.Text = password;
        }
        catch (Exception ex)
        {
            GeneratedPasswordText.Text = $"Error: {ex.Message}";
        }
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        if (GeneratedPasswordText.Text != "Click Generate to create a password" && 
            !GeneratedPasswordText.Text.StartsWith("Error:"))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(GeneratedPasswordText.Text);
            Clipboard.SetContent(dataPackage);
        }
    }
}
