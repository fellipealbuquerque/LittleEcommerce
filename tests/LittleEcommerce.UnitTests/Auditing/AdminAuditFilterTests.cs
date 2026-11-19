using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Infrastructure.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace LittleEcommerce.UnitTests.Auditing;

public class AdminAuditFilterTests
{
    private static ActionExecutedContext Executed(IActionResult? result = null, Exception? exception = null, int responseStatus = 200)
    {
        var http = new DefaultHttpContext();
        http.Response.StatusCode = responseStatus;
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());

        return new ActionExecutedContext(action, [], new object())
        {
            Result = result,
            Exception = exception
        };
    }

    [Fact]
    public void StatusCodeOf_ShouldUseAppExceptionStatus()
    {
        Assert.Equal(422, AdminAuditFilter.StatusCodeOf(Executed(exception: new DomainException("regra"))));
    }

    [Fact]
    public void StatusCodeOf_ShouldReportUnexpectedErrorsAs500()
    {
        Assert.Equal(500, AdminAuditFilter.StatusCodeOf(Executed(exception: new InvalidOperationException())));
    }

    [Fact]
    public void StatusCodeOf_ShouldUseResultStatus()
    {
        Assert.Equal(201, AdminAuditFilter.StatusCodeOf(Executed(new ObjectResult(null) { StatusCode = 201 })));
    }

    [Fact]
    public void StatusCodeOf_ShouldFallBackToResponseStatus()
    {
        Assert.Equal(204, AdminAuditFilter.StatusCodeOf(Executed(new EmptyResult(), responseStatus: 204)));
    }
}
