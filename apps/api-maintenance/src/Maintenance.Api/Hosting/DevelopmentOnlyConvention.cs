using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Maintenance.Api.Hosting;

/// <summary>Marks a controller that must not exist outside the Development environment.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DevelopmentOnlyAttribute : Attribute;

/// <summary>
/// Removes every action of <see cref="DevelopmentOnlyAttribute"/> controllers when the host is
/// not Development, so no route, and no OpenAPI entry, is ever created for them.
/// </summary>
public sealed class DevelopmentOnlyConvention(bool isDevelopment) : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (isDevelopment || !controller.Attributes.OfType<DevelopmentOnlyAttribute>().Any())
        {
            return;
        }

        controller.Actions.Clear();
        controller.ApiExplorer.IsVisible = false;
    }
}
