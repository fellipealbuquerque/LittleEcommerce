using LittleEcommerce.Source.Shared.DTOs;

namespace LittleEcommerce.UnitTests.Shared.DTOs;

public class ResponsesTests
{
    [Theory]
    [InlineData(0, 10, 0, 0, 1)]
    [InlineData(25, 10, 0, 3, 1)]
    [InlineData(25, 10, 20, 3, 3)]
    [InlineData(30, 10, 10, 3, 2)]
    [InlineData(5, 0, 0, 0, 1)]
    public void PaginatedResponse_ShouldComputePages(int total, int limit, int offset, int expectedTotalPages, int expectedPage)
    {
        var response = new PaginatedResponse<int>(200, "ok", [], total, limit, offset);

        Assert.Equal(expectedTotalPages, response.TotalPages);
        Assert.Equal(expectedPage, response.Page);
    }

    [Fact]
    public void ValidationError_ShouldReturnBadRequestWithErrors()
    {
        var errors = new Dictionary<string, string[]> { ["name"] = ["Obrigatório"] };

        var result = APIResponse.ValidationError(errors, "trace");

        var body = Assert.IsType<APIErrorResponse>(result.Value);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(APIErrors.ValidationCode, body.Code);
        Assert.Equal("trace", body.TraceId);
        Assert.Same(errors, body.Errors);
    }

    [Fact]
    public void Create_ShouldUseTitleAsMessage_WhenMessageIsMissing()
    {
        var error = APIErrors.Create(404);

        Assert.Equal("NOT_FOUND", error.Code);
        Assert.Equal(error.Title, error.Message);
    }
}
