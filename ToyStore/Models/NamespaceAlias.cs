// Namespace alias for backward compatibility
// This file allows existing code to continue using ToyStore.Models
// while the actual entities are in ToyStore.Domain.Entities

global using ToyStore.Models;
global using DomainEntities = ToyStore.Domain.Entities;
global using InfrastructureData = ToyStore.Infrastructure.Data;

// Re-export entities for backward compatibility
namespace ToyStore.Models
{
    // Entities are now in ToyStore.Domain.Entities
    // This namespace is kept for ViewModels and other model classes
}
