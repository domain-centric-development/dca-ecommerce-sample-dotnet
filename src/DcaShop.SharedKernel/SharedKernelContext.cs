using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.SharedKernel;

/// <summary>Shared kernel: universal value objects and the in-process event plumbing every context uses.</summary>
[SharedKernel(Description = "Common value objects and cross-cutting event dispatch")]
public static class SharedKernelContext
{
}
