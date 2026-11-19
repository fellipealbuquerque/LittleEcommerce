namespace LittleEcommerce.Source.Shared.DTOs.Results.Meta;

public record SystemVersionResult(
    string Name,
    string Version,
    string Environment
);
