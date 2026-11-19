using System.Net;
using LittleEcommerce.Source.Application.Abstractions;

namespace LittleEcommerce.Source.Infrastructure.Email;

public static class WelcomeEmailTemplate
{
    public static EmailMessage Render(WelcomeEmailPayload payload, string storeUrl, string? messageId = null)
    {
        var firstName = payload.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? payload.Name;
        var safeName = WebUtility.HtmlEncode(firstName);
        var safeUrl = WebUtility.HtmlEncode(storeUrl);

        var subject = $"Boas-vindas à LittleEcommerce, {firstName}!";

        var html = $"""
            <!doctype html>
            <html lang="pt-BR">
            <body style="margin:0;padding:24px;background:#f4f4f5;font-family:Arial,Helvetica,sans-serif;color:#18181b">
              <table role="presentation" width="100%" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:8px">
                <tr>
                  <td style="padding:32px">
                    <h1 style="margin:0 0 16px;font-size:22px">Olá, {safeName}!</h1>
                    <p style="margin:0 0 16px;font-size:15px;line-height:1.5">Sua conta na LittleEcommerce foi criada. Já dá para montar o carrinho e acompanhar seus pedidos.</p>
                    <p style="margin:24px 0">
                      <a href="{safeUrl}" style="background:#18181b;color:#ffffff;padding:12px 20px;border-radius:6px;text-decoration:none;font-size:15px">Ir para a loja</a>
                    </p>
                    <p style="margin:0;font-size:13px;color:#71717a">Se não foi você quem criou esta conta, ignore este e-mail.</p>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        var text = $"""
            Olá, {firstName}!

            Sua conta na LittleEcommerce foi criada. Já dá para montar o carrinho e acompanhar seus pedidos.

            Ir para a loja: {storeUrl}

            Se não foi você quem criou esta conta, ignore este e-mail.
            """;

        return new EmailMessage(payload.Email, payload.Name, subject, html, text, messageId);
    }
}
