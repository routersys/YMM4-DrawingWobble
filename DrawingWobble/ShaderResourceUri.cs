namespace DrawingWobble;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/DrawingWobble;component/Resources/Shader/{shaderName}.cso", UriKind.Absolute);
}
