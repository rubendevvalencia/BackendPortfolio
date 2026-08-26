using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Configuramos el automapper para mapear entre la entidad Customer y el DTO CustomerDTO
            CreateMap<Customer, CustomerDto>().ReverseMap();
            //Esto solo se da para casos donde tienen los mismos nombres de propiedades, si no se da el caso, se debe mapear manualmente cada propiedad.
            //Ejemplo de forma manual:
            /*CreateMap<Customer, CustomerDto>().ReverseMap().
                ForMember(dest => dest.Id, source => source.MapFrom(src => src.Id))
                ForMember(dest => dest.CompanyName, source => source.MapFrom(src => src.CompanyName));*/
        }
    }
}
