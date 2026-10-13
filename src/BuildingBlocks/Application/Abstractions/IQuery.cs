using MediatR;

namespace BazarKav.BuildingBlocks.Application.Abstractions;

/// <summary>A request that reads without changing business state.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;
