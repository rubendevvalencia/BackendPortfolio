using AutoMapper;
using Ticketing.Command.Application.Feature.Tickets.TicketsCreate;
using Ticketing.Common.Events;

namespace Ticketing.Command.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<TicketCreateCommand, TicketCreatedEvent>()
                .ForMember(destination => destination.TyperError, options => options.MapFrom(source => source.ErrorType))
                .ForMember(destination => destination.DetailError, options => options.MapFrom(source => source.ErrorDetail))
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.Version, options => options.Ignore())
                .ForMember(destination => destination.Type, options => options.Ignore());
        }
    }
}
