using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Infrastructure.Email;

namespace LittleEcommerce.UnitTests.Outbox;

public class WelcomeEmailTemplateTests
{
    [Fact]
    public void Render_ShouldGreetByFirstNameAndLinkToStore()
    {
        var email = WelcomeEmailTemplate.Render(new WelcomeEmailPayload(Guid.NewGuid(), "ana@loja.com", "Ana Maria Souza"), "https://loja.exemplo", "id@x");

        Assert.Equal("ana@loja.com", email.ToAddress);
        Assert.Equal("Ana Maria Souza", email.ToName);
        Assert.Equal("Boas-vindas à LittleEcommerce, Ana!", email.Subject);
        Assert.Contains("Olá, Ana!", email.Html);
        Assert.Contains("href=\"https://loja.exemplo\"", email.Html);
        Assert.Contains("https://loja.exemplo", email.Text);
        Assert.Equal("id@x", email.MessageId);
    }

    [Fact]
    public void Render_ShouldEncodeUserInputInHtml()
    {
        var email = WelcomeEmailTemplate.Render(new WelcomeEmailPayload(Guid.NewGuid(), "x@loja.com", "<script>alert(1)</script>"), "https://loja.exemplo");

        Assert.DoesNotContain("<script>", email.Html);
        Assert.Contains("&lt;script&gt;", email.Html);
    }
}
