// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.Authorization.given;

internal static class MaxAgeRequests
{
    internal static IEnumerable<DefaultHttpContext> Invalid()
    {
        foreach (var query in new[] { "?max_age=-1", "?max_age=invalid", "?max_age=", "?Max_Age=0", "?MAX_AGE", "?max_age=0&max_age=300", "?max_age=1.5", "?max_age=+5", "?max_age=99999999999999999999" })
        {
            var context = new DefaultHttpContext();
            context.Request.QueryString = new(query);
            yield return context;
        }
        yield return Post("?Max_Age=0", "scope=openid");
        yield return Post("?max_age=0", "max_age=300");
        yield return Post("", "Max_Age=0");
        yield return Post("", "MAX_AGE");
        yield return Post("", "max_age=0&max_age=300");
        yield return Post("", "max_age=-1");
        yield return Post("", "max_age=abc");
    }

    internal static IEnumerable<DefaultHttpContext> Valid()
    {
        foreach (var query in new[] { "?max_age=0", "?max_age=300", "?%6dax%5Fage=0" })
        {
            var context = new DefaultHttpContext();
            context.Request.QueryString = new(query);
            yield return context;
        }
        yield return Post("", "max_age=300");
        yield return Post("", "%6dax%5Fage=0");
    }

    internal static IEnumerable<DefaultHttpContext> WithoutMaxAge()
    {
        yield return new DefaultHttpContext();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new("?max_age_hint=0");
        yield return context;
        yield return Post("", "");
        yield return Post("?scope=openid", "max_age_hint=0");
    }

    static DefaultHttpContext Post(string query, string form)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.QueryString = new(query);
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(form));
        return context;
    }
}
