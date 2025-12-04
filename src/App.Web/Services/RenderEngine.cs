using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
using Fluid.Filters;
using Fluid.Values;
using Mediator;
using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;

namespace App.Web.Services;

public class RenderEngine : IRenderEngine
{
    private static readonly FluidParser _parser = new FluidParser(
        new FluidParserOptions { AllowFunctions = true }
    );

    private static readonly TemplateOptions _templateOptions;
    private static readonly ConcurrentDictionary<string, IFluidTemplate> _templateCache = new();
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IMediator _mediator;
    private readonly IFileStorageProvider _fileStorageProvider;

    static RenderEngine()
    {
        _templateOptions = new TemplateOptions();
        _templateOptions.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
        _templateOptions.Filters.AddFilter("attachment_redirect_url", AttachmentRedirectUrl);
        _templateOptions.Filters.AddFilter("attachment_public_url", AttachmentPublicUrl);
        _templateOptions.Filters.AddFilter("organization_time", LocalDateFilter);
        _templateOptions.Filters.AddFilter("groupby", GroupBy);
        _templateOptions.Filters.AddFilter("json", JsonFilter);
    }

    public RenderEngine(
        IMediator mediator,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        IFileStorageProvider fileStorageProvider
    )
    {
        _relativeUrlBuilder = relativeUrlBuilder;
        _currentOrganization = currentOrganization;
        _mediator = mediator;
        _fileStorageProvider = fileStorageProvider;
    }

    public string RenderAsHtml(string source, object entity)
    {
        var template = _templateCache.GetOrAdd(
            source,
            key =>
            {
                if (_parser.TryParse(key, out var parsedTemplate, out var error))
                {
                    return parsedTemplate;
                }
                throw new Exception(error);
            }
        );

        var context = new TemplateContext(entity, _templateOptions);
        context.TimeZone = DateTimeExtensions.GetTimeZoneInfo(_currentOrganization.TimeZone);

        // Store services in ambient values for filters to access
        context.AmbientValues["RelativeUrlBuilder"] = _relativeUrlBuilder;
        context.AmbientValues["FileStorageProvider"] = _fileStorageProvider;

        return template.Render(context);
    }

    private static ValueTask<FluidValue> AttachmentRedirectUrl(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context
    )
    {
        var relativeUrlBuilder = (IRelativeUrlBuilder)context.AmbientValues["RelativeUrlBuilder"];
        return new StringValue(relativeUrlBuilder.MediaRedirectToFileUrl(input.ToStringValue()));
    }

    private static async ValueTask<FluidValue> AttachmentPublicUrl(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context
    )
    {
        if (string.IsNullOrEmpty(input.ToStringValue()))
            return new StringValue(string.Empty);

        var fileStorageProvider = (IFileStorageProvider)
            context.AmbientValues["FileStorageProvider"];
        var downloadUrl = await fileStorageProvider
            .GetDownloadUrlAsync(input.ToStringValue(), FileStorageUtility.GetDefaultExpiry())
            .ConfigureAwait(false);
        return new StringValue(downloadUrl);
    }

    private static async ValueTask<FluidValue> GroupBy(
        FluidValue input,
        FilterArguments property,
        TemplateContext context
    )
    {
        var groupByProperty = property.At(0).ToStringValue();
        if (string.IsNullOrWhiteSpace(groupByProperty))
        {
            return new ArrayValue(Array.Empty<FluidValue>());
        }

        var buckets = new Dictionary<string, List<FluidValue>>(StringComparer.Ordinal);
        foreach (var item in input.Enumerate(context))
        {
            var key = await ApplyGroupByAsync(item, groupByProperty, context).ConfigureAwait(false);

            if (!buckets.TryGetValue(key, out var values))
            {
                values = new List<FluidValue>();
                buckets[key] = values;
            }

            values.Add(item);
        }

        var result = new List<FluidValue>(buckets.Count);
        foreach (var bucket in buckets)
        {
            result.Add(new ObjectValue(new { key = bucket.Key, items = bucket.Value }));
        }

        return new ArrayValue(result);
    }


    private static async Task<string> ApplyGroupByAsync(
        FluidValue p,
        string groupByProperty,
        TemplateContext context
    )
    {
        if (string.IsNullOrWhiteSpace(groupByProperty))
        {
            return string.Empty;
        }

        var value = await p.GetValueAsync(groupByProperty, context).ConfigureAwait(false);
        return value.ToStringValue();
    }

    private static ValueTask<FluidValue> LocalDateFilter(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context
    )
    {
        var value = TimeZoneConverter(input, context);
        return ReferenceEquals(value, NilValue.Instance)
            ? value
            : MiscFilters.Date(value, arguments, context);
    }

    private static ValueTask<FluidValue> JsonFilter(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context
    )
    {
        return new StringValue(
            JsonSerializer.Serialize(input.ToObjectValue(), _jsonSerializerOptions)
        );
    }

    private static FluidValue TimeZoneConverter(FluidValue input, TemplateContext context)
    {
        if (!input.TryGetDateTimeInput(context, out var value))
        {
            return NilValue.Instance;
        }

        var utc = DateTime.SpecifyKind(value.DateTime, DateTimeKind.Utc);

        // Create new offset for UTC
        var localOffset = new DateTimeOffset(utc, TimeSpan.Zero);

        var result = TimeZoneInfo.ConvertTime(localOffset, context.TimeZone);
        return new DateTimeValue(result);
    }

    /// <summary>
    /// Recursively converts a JsonElement object to a Dictionary that Fluid/Liquid can work with.
    /// This is necessary because JsonSerializer.Deserialize&lt;Dictionary&lt;string, object&gt;&gt;
    /// doesn't recursively convert nested objects and arrays - they remain as JsonElement.
    /// </summary>
    private static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = ConvertJsonValue(prop.Value);
        }

        return dict;
    }

    /// <summary>
    /// Converts a JsonElement value to the appropriate .NET type.
    /// </summary>
    private static object ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString(),
        };
    }
}
