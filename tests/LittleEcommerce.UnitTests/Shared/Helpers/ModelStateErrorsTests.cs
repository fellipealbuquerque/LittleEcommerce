using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LittleEcommerce.UnitTests.Shared.Helpers;

public class ModelStateErrorsTests
{
    [Fact]
    public void From_ShouldCamelCaseEveryPathSegment()
    {
        var state = new ModelStateDictionary();
        state.AddModelError("Email", "E-mail inválido");
        state.AddModelError("Items[0].Quantity", "Quantidade inválida");
        state.AddModelError("Name", "");

        var errors = ModelStateErrors.From(state);

        Assert.Equal(["E-mail inválido"], errors["email"]);
        Assert.Equal(["Quantidade inválida"], errors["items[0].quantity"]);
        Assert.Equal([ModelStateErrors.InvalidValueMessage], errors["name"]);
    }

    [Fact]
    public void From_ShouldReportOnlyJsonErrors_WhenBodyCannotBeRead()
    {
        var state = new ModelStateDictionary();
        state.AddModelError("$", "Expected depth to be zero");
        state.AddModelError("$.Price", "could not be converted");
        state.AddModelError("request", "The request field is required.");

        var errors = ModelStateErrors.From(state);

        Assert.Equal(2, errors.Count);
        Assert.Equal([ModelStateErrors.InvalidJsonMessage], errors[ModelStateErrors.BodyKey]);
        Assert.Equal([ModelStateErrors.InvalidJsonMessage], errors["price"]);
    }
}
