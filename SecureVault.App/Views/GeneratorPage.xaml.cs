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

    private void OnLengthChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (LengthValueText != null)
        {
            LengthValueText.Text = $"{(int)e.NewValue} caratteri";
        }
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
            GeneratedPasswordText.Text = $"Errore: {ex.Message}";
        }
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        if (GeneratedPasswordText.Text != "Clicca Genera per creare una password" && 
            !GeneratedPasswordText.Text.StartsWith("Errore:"))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(GeneratedPasswordText.Text);
            Clipboard.SetContent(dataPackage);
            
            // Show feedback
            GeneratedPasswordText.Text = "✅ Copiato negli appunti!";
            Task.Delay(1500).ContinueWith(_ => 
            {
                DispatcherQueue.TryEnqueue(() => OnGenerateClick(null, null));
            });
        }
    }
}
