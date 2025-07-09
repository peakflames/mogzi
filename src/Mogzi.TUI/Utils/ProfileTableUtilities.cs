namespace Mogzi.TUI.Utils;

/// <summary>
/// Utilities for creating profile tables that can be reused across the application.
/// </summary>
public static class ProfileTableUtilities
{
    /// <summary>
    /// Creates a Spectre.Console table showing available profiles from the configuration.
    /// </summary>
    /// <param name="title">Optional title for the table. If null, uses "Available Profiles".</param>
    /// <returns>A configured Table object, or null if no profiles are found or an error occurs.</returns>
    public static Table? CreateProfilesTable(string? title = null)
    {
        try
        {
            var configPath = Mogzi.Utils.ConfigurationLocator.FindConfigPath();
            if (configPath is null)
            {
                return null;
            }

            var jsonContent = File.ReadAllText(configPath);
            var configRoot = JsonSerializer.Deserialize(jsonContent, ApplicationConfigurationContext.Default.ApplicationConfigurationRoot);

            var config = configRoot?.RootConfig;
            if (config?.Profiles == null || !config.Profiles.Any())
            {
                return null;
            }

            var table = new Table()
                .Title(title ?? "Available Profiles")
                .Border(TableBorder.Rounded)
                .AddColumn("Name")
                .AddColumn("Model")
                .AddColumn("Provider")
                .AddColumn("Default");

            foreach (var profile in config.Profiles)
            {
                _ = table.AddRow(
                    Markup.Escape(profile.Name ?? "-"),
                    Markup.Escape(profile.ModelId ?? "-"),
                    Markup.Escape(profile.ApiProvider ?? "-"),
                    profile.Default ? ":check_mark_button:" : ""
                );
            }

            return table;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a simple markup string showing profile information for text-based output.
    /// </summary>
    /// <returns>A markup string with profile information, or null if no profiles are found.</returns>
    public static string? CreateProfilesMarkup()
    {
        try
        {
            var configPath = Mogzi.Utils.ConfigurationLocator.FindConfigPath();
            if (configPath is null)
            {
                return null;
            }

            var jsonContent = File.ReadAllText(configPath);
            var configRoot = JsonSerializer.Deserialize(jsonContent, ApplicationConfigurationContext.Default.ApplicationConfigurationRoot);

            var config = configRoot?.RootConfig;
            if (config?.Profiles == null || !config.Profiles.Any())
            {
                return null;
            }

            var markup = new StringBuilder();
            _ = markup.AppendLine("[bold]Available Profiles:[/]");

            foreach (var profile in config.Profiles)
            {
                var defaultIndicator = profile.Default ? " [green](default)[/]" : "";
                _ = markup.AppendLine($"• [cyan]{Markup.Escape(profile.Name ?? "-")}[/] - {Markup.Escape(profile.ModelId ?? "-")} via {Markup.Escape(profile.ApiProvider ?? "-")}{defaultIndicator}");
            }

            return markup.ToString();
        }
        catch
        {
            return null;
        }
    }
}
