using System.Windows;
using System.Windows.Controls;

namespace IndustrialMonitor.Helper;

/// <summary>
/// 让 PasswordBox.Password 可以与 ViewModel 双向绑定。
/// </summary>
public static class PwdHelper
{
    public static readonly DependencyProperty CustPwdProperty =
        DependencyProperty.RegisterAttached(
            "CustPwd",
            typeof(string),
            typeof(PwdHelper),
            new PropertyMetadata(string.Empty, PasswordPropertyChanged));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.RegisterAttached(
            "IsOpen",
            typeof(bool),
            typeof(PwdHelper),
            new PropertyMetadata(false, BindingEnabledChanged));

    public static string GetCustPwd(DependencyObject obj) =>
        (string?)obj.GetValue(CustPwdProperty) ?? string.Empty;

    public static void SetCustPwd(DependencyObject obj, string value) =>
        obj.SetValue(CustPwdProperty, value);

    public static bool GetIsOpen(DependencyObject obj) =>
        (bool)obj.GetValue(IsOpenProperty);

    public static void SetIsOpen(DependencyObject obj, bool value) =>
        obj.SetValue(IsOpenProperty, value);

    private static void PasswordPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is PasswordBox passwordBox)
        {
            string newPassword = eventArgs.NewValue as string ?? string.Empty;
            if (passwordBox.Password != newPassword)
            {
                passwordBox.Password = newPassword;
            }
        }
    }

    private static void BindingEnabledChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= PasswordChanged;
        if (eventArgs.NewValue is true)
        {
            passwordBox.PasswordChanged += PasswordChanged;
        }
    }

    private static void PasswordChanged(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is PasswordBox passwordBox)
        {
            SetCustPwd(passwordBox, passwordBox.Password);
        }
    }
}