using Hunter.Domain.Prospecting;

namespace Hunter.Application.Campaigning;

public static class TemplateRenderer
{
    public static string Render(string content, Prospect prospect, string? senderName = null)
    {
        return content
            .Replace("{{business_name}}", prospect.BusinessName)
            .Replace("{{city}}", prospect.City ?? string.Empty)
            .Replace("{{category}}", prospect.Category.ToString())
            .Replace("{{sender_name}}", senderName ?? string.Empty);
    }
}
