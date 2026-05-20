global using Vitarish.Appointments.Domain.Dto;
global using Vitarish.Appointments.Domain.Entities;
global using Vitarish.Appointments.Domain.Enums;
global using Vitarish.Appointments.Domain.Models;
global using Vitarish.Appointments.Application.Abstractions;
global using MediatR;
global using Vitarish.Entities.Server.Abstractions;
global using Vitarish.Entities.Server.Core;
global using Vitarish.Entities.Server.Models;
global using Vitarish.Entities.Server.Repositories;
global using Vitarish.Entities.Server.Services;
// Aliases for types that conflict with Microsoft.Extensions.DependencyInjection names
global using AppointmentServiceProvider = Vitarish.Appointments.Domain.Entities.ServiceProvider;
global using AppointmentServiceDescriptor = Vitarish.Appointments.Domain.Models.ServiceDescriptor;
