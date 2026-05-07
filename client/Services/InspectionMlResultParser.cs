using System.Globalization;
using System.Text.Json;
using client.Data.Entities;

namespace client.Services;

public sealed record DefectBox(decimal X, decimal Y, decimal Width, decimal Height);

public sealed record ModelResultView(
    string Model,
    decimal? SimilarityPercent,
    string? Status,
    string? FailureReason,
    List<DefectBox> Defects);

public static class InspectionMlResultParser
{
    public static List<ModelResultView> ParseModelResults(InspectionImage image)
    {
        if (!string.IsNullOrWhiteSpace(image.MlResultsJson))
        {
            try
            {
                using var document = JsonDocument.Parse(image.MlResultsJson);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var parsedResults = new List<ModelResultView>();

                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        parsedResults.Add(new ModelResultView(
                            GetStringProperty(element, "model") ?? $"Model {parsedResults.Count + 1}",
                            TryGetDecimalProperty(element, "similarity_percent"),
                            GetStringProperty(element, "status"),
                            GetStringProperty(element, "failure_reason") ?? GetStringProperty(element, "error"),
                            ParseDefects(element, "defects")));
                    }

                    if (parsedResults.Count > 0)
                    {
                        return parsedResults;
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        return
        [
            new ModelResultView(
                "Result",
                image.SimilarityPercent,
                null,
                image.FailureReason,
                ParseDefects(image.DefectsJson))
        ];
    }

    private static List<DefectBox> ParseDefects(string? defectsJson)
    {
        if (string.IsNullOrWhiteSpace(defectsJson))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(defectsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var parsedDefects = new List<DefectBox>();

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (TryParseDefectElement(element, out var defect))
                {
                    parsedDefects.Add(defect);
                }
            }

            return parsedDefects;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<DefectBox> ParseDefects(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var defectsElement) || defectsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsedDefects = new List<DefectBox>();
        foreach (var defectElement in defectsElement.EnumerateArray())
        {
            if (TryParseDefectElement(defectElement, out var defect))
            {
                parsedDefects.Add(defect);
            }
        }

        return parsedDefects;
    }

    private static bool TryParseDefectElement(JsonElement element, out DefectBox defect)
    {
        defect = default!;

        if (!TryReadDecimal(element, "x", out var x)
            || !TryReadDecimal(element, "y", out var y)
            || !TryReadDecimal(element, "width", out var width)
            || !TryReadDecimal(element, "height", out var height)
            || width <= 0
            || height <= 0)
        {
            return false;
        }

        defect = new DefectBox(x, y, width, height);
        return true;
    }

    private static bool TryReadDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value),
            _ => false
        };
    }

    private static decimal? TryGetDecimalProperty(JsonElement element, string propertyName)
        => TryReadDecimal(element, propertyName, out var value) ? value : null;

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }
}
