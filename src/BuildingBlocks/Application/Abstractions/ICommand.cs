using MediatR;

namespace BazarKav.BuildingBlocks.Application.Abstractions;

/// <summary>A request that may change BazarKav's internal state, never accounting source state.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;
