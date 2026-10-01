using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Feature.Customers.Commands.CreateCustomerCommand;
using CreateCustomerCommandV4 = Ecommerce.Application.Feature.Customers.v4.Commands.CreateCustomer.CreateCustomerCommand; //Mismo nombre que el de v3: el alias evita la ambiguedad.
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Entities.Jwt;
using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce.Application.Feature.Users.Commands.SignUp;

namespace Ecommerce.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Solo se declaran las direcciones que se usan de verdad, y todo lo que el mapeo NO debe
            //rellenar se marca con Ignore(). Asi AssertConfigurationIsValid() (ver ApplicationTestBase)
            //puede revisar el perfil entero: si manana aparece una propiedad nueva sin mapear, el test
            //revienta en vez de dejarla en su valor por defecto sin que nadie se entere.

            //Lectura: la entidad se convierte en el DTO que sale por la Api.
            //Todas las propiedades de CustomerDto existen en Customer con el mismo nombre.
            CreateMap<Customer, CustomerDto>();

            //Alta: el DTO que entra por la Api se convierte en la entidad que se persiste.
            CreateMap<CustomerDto, Customer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())                //Lo genera la base de datos (columna identity).
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())         //Los cuatro campos de auditoria los rellena
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())         //AuditableEntitySaveChangesInterceptor al
                .ForMember(dest => dest.LastUpdatedAt, opt => opt.Ignore())     //confirmar, nunca el mapeo. Si el mapeo los
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore());    //tocara, pisaria lo que pone el interceptor.

            //Registro de usuario. La direccion inversa (User -> SignUpDto) no la usa nadie
            //y se ha quitado: un mapa que no se usa es codigo muerto que hay que mantener valido.
            CreateMap<SignUpDto, User>()
                //La contrasena llega en claro y se copia tal cual: cifrarla es tarea de
                //UserRepository.CreateUserAsync, el ultimo punto antes de persistir.
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore());

            CreateMap<SignUpCommand, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore());

            CreateMap<Customer, CreateCustomerCommand>().ReverseMap();
            CreateMap<Customer, CreateCustomerCommandV4>().ReverseMap();
        }
    }
}
